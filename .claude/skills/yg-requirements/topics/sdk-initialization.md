# Topic — SDK initialization (1.19.x)

Source: <https://yandex.ru/dev/games/doc/ru/requirements/1/19>

## Rules
- **1.19.1** SDK initialised exactly as per the "Подключение и использование" docs. The **current loader** must be used.
- **1.19.2** Call `LoadingAPI.ready()` (a.k.a. Game Ready) the moment the user can actually start playing.
- **1.19.3** If `GameplayAPI.start()` / `GameplayAPI.stop()` are used, their emission moments must match the "Геймплей" docs.
- **1.19.4** If `ysdk.on()` / `ysdk.off()` are used, event handling must match the "Пауза и возобновление" docs.

## What to verify in code
1. `index.html` includes the current loader: `<script src="/sdk.js"></script>` (must be `/sdk.js`, not a pinned version path).
2. SDK init pattern: `YaGames.init().then(ysdk => { ... })` — confirm a single init at startup.
3. `LoadingAPI.ready()` is called **after** assets are loaded **and** the main menu / first interactive scene is mounted. Not on `YaGames.init().then` directly.
4. `GameplayAPI.start()` fires when actual gameplay starts (level entered), `stop()` when it ends (menu, pause, game over). Pair check.
5. `ysdk.on(EVENTS.GAME_API_PAUSE, ...)` pairs with `ysdk.on(EVENTS.GAME_API_RESUME, ...)` — pause stops audio + game loop, resume restores them.
6. Grep `'/sdk_local.js'` / `'sdk_local.js'` — must not ship to production.

## Common failures
- `ready()` called inside SDK init resolve — splash screen counts as "ready".
- `start()` fires on app boot, never paired with `stop()`.
- Old loader path pinned to a specific SDK version.
- Pause event ignored; game keeps running under ad.
