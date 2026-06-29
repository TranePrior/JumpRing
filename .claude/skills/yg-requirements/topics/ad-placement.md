# Topic — Ad placement (4.4, 4.7)

Source: <https://yandex.ru/dev/games/doc/ru/requirements/4/4>

## Rule
> Реклама не мешает взаимодействовать с игрой и показывается только в логических паузах.

Key principle: ad timing must be predictable and never interrupt input.

## Concrete numbers
- Max delay between user action and ad start: **0.33 s**.
- Pre-ad warning duration: **2 s** (do not stretch — players will quit).
- During warning + ad: game paused (animations may keep playing; **input + state must not advance**).
- If "remove ads" IAP is purchased — no more ad calls.

## What to verify in code
1. Find ad trigger sites: `showFullscreenAdv`, `showRewardedVideo`, `showBannerAdv`.
2. Each call sits at a logical boundary: level end, restart, menu transition. **Not** mid-gameplay.
3. Pause hook fires on `onOpen` callback and resume on `onClose` / `onError`. Audio + game-loop both paused.
4. The 2 s warning is a real overlay, not a 0.5 s flash.
5. Ads-removed flag (purchased) gates every ad call: `if (!adsRemoved) showAdv(...)`.
6. Confirm 0.33 s gap: button click → handler → ad call must be synchronous (no debouncer with longer delay).

## Common failures
- Ad fires every N seconds on a timer, regardless of game state.
- Game keeps accepting input during the pre-roll warning.
- "Remove ads" flag is read once at startup; purchase mid-session still shows ads until next reload.
- Music continues at full volume during the ad.
