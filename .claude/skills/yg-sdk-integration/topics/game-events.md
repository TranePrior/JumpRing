---
title_ru: События готовности игры и геймплея
title_en: Game Ready & Gameplay Lifecycle Events
source: https://yandex.ru/dev/games/doc/ru/sdk/sdk-game-events
---

## Purpose
Notify the platform that loading has completed and signal active gameplay windows. Required for correct ad timing and platform analytics.

## API Surface

### `ysdk.features.LoadingAPI.ready(): void`
Signals that the game has finished loading, all resources are ready, and the player can interact. Call after the loading screen disappears.

### `ysdk.features.GameplayAPI.start(): void`
Marks the beginning of active gameplay. Call when:
- a level starts
- a menu closes
- the game resumes from pause
- gameplay continues after an ad
- the browser tab regains focus

### `ysdk.features.GameplayAPI.stop(): void`
Marks the end of active gameplay. Call when:
- a level completes or fails
- a menu opens
- the game pauses
- a fullscreen or rewarded ad is shown
- the browser tab loses focus

Both `LoadingAPI` and `GameplayAPI` are optional features — use optional chaining (`?.`) before calling.

## Code Examples

```javascript
const ysdk = await YaGames.init();
ysdk.features.LoadingAPI?.ready()
```

```javascript
YaGames.init()
    .then((ysdk) => {
        ysdk.features.LoadingAPI?.ready()
    })
    .catch(console.error);
```

```javascript
const ysdk = await YaGames.init();
ysdk.features.GameplayAPI?.start()
// Active gameplay
ysdk.features.GameplayAPI?.stop()
```

## Pitfalls / Notes
- Always re-call `start()` after `stop()` when gameplay resumes; the platform tracks paired calls.
- Missing `LoadingAPI.ready()` can leave the platform loading indicator visible.

## Source
https://yandex.ru/dev/games/doc/ru/sdk/sdk-game-events
