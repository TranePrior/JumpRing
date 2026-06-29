# Topic — Sound on minimize (1.3)

Source: <https://yandex.ru/dev/games/doc/ru/requirements/1/3>

## Rule
> При сворачивании страницы с игрой на десктопных и мобильных устройствах звук останавливается.

## Test scenarios (moderation runs these 3)
1. Minimize browser window (desktop) / app (mobile).
2. Switch to another tab in the same browser.
3. Open the tab-switcher in the mobile browser.

## Allowed exceptions (not violations)
- Up to **2 seconds** delay between leaving the page and audio stopping.
- Audio continuing after a click on an ad banner that opens another tab.
- iOS audio that keeps playing while the tab-switcher overlay is open.

## What to verify in code
1. Grep for `visibilitychange`, `document.hidden`, `pagehide`, `window.blur`.
2. Find the audio/music manager — confirm it pauses on `visibilitychange` when `document.visibilityState !== 'visible'`.
3. Also pause Phaser scene / game loop sounds, not only the music track.
4. SDK events: respect `ysdk.on('EVENTS.GAME_API_PAUSE')` if used.
5. Test with throttled CPU — pause must occur within 2 s.

## Common failures
- Only the music pauses; SFX continue.
- Listener attached to `blur` instead of `visibilitychange` — fires on every focus change inside the page.
- Pause logic lives in React effect that does not run when the tab loses focus.
