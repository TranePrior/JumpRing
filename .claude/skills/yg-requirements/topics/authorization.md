# Topic — Authorization (1.2.x)

Source: <https://yandex.ru/dev/games/doc/ru/requirements/1/2>

## Rules
- **1.2** Only Yandex ID. No third-party logins, no required registration.
- **1.2.1** Auth happens **only after explicit user action** — pressing an in-game button. No auto-prompts on game start.
- **1.2.2** Guest play must be available. Internal progress persists for guests.
- The auth prompt must explain the benefits of signing in (cloud sync, notifications, IAP).
- User can decline and continue playing.

## What to verify in code
1. Grep for `ysdk.auth`, `getPlayer`, `signin`, `isAuthorized`. Confirm none of these fire on startup.
2. Find the auth trigger — it must be wired to a dedicated button, not a modal that pops on first load.
3. Open the auth UI: it must contain a "continue without signing in" path.
4. Verify guest progress: read `ProgressStorage` / save layer — does it write before login?
5. No links to non-Yandex auth providers (Google, Facebook, VK, etc.).

## Common failures
- Auth modal shown automatically on first visit.
- Save layer keyed on `playerId` that is `null` for guests → progress lost.
- "Sign in to play" gate blocking gameplay.
