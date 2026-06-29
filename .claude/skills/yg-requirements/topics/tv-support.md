# Topic — TV support (1.6.3.x)

Source: <https://yandex.ru/dev/games/doc/ru/requirements/1/6/3>

Only applies if "Android TV" is selected in the publishing draft.

## Rules
- **1.6.3.1** Fullscreen, takes the whole screen.
- **1.6.3.2** Fully playable with D-pad arrows on the TV remote — no other input required.
- **1.6.3.3** **Back** and **OK** buttons handled.
- **1.6.3.4** **No IAP** on TV builds.
- **1.6.3.5** **No links to other developer games** on TV builds.

## What to verify in code
1. Keyboard handler: arrow keys move focus through menus / game pieces. Grep for `ArrowUp|ArrowDown|ArrowLeft|ArrowRight`, `keydown`.
2. `Enter` / "OK" activates focused element. `Escape` / "Back" returns/closes.
3. Focus management: every interactive element is reachable by D-pad and has a visible focus state.
4. IAP gated behind `Capacitor.getPlatform()` / `ysdk.deviceInfo.type === 'tv'` check — UI hides shop on TV.
5. No "more games" carousel or external links on TV builds.

## Common failures
- Mouse-only controls; menus react to clicks but ignore Enter on focused button.
- Focus ring suppressed via `outline: none` — moderator can't see what's selected.
- Shop button still visible on TV build.
