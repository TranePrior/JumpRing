---
title_ru: Другие игры разработчика
title_en: Other Games (Developer Catalog)
source: https://yandex.ru/dev/games/doc/ru/sdk/sdk-other-games
---

## Purpose
List the developer's other games available on the current platform/domain, or check whether a specific game is available, for in-game cross-promotion.

## API Surface

### `ysdk.features.GamesAPI.getAllGames(): Promise<{ games: IGame[]; developerURL: string }>`
Returns all of the developer's games available on the current platform and domain.

### `ysdk.features.GamesAPI.getGameByID(appID: number): Promise<{ game?: IGame; isAvailable: boolean }>`
Returns availability and metadata for a specific game ID (from the developer console). `game` is `undefined` when `isAvailable === false`.

```typescript
interface IGame {
    appID: string;
    title: string;
    url: string;
    coverURL: string;
    iconURL: string;
}
```

## Code Examples

```javascript
ysdk.features.GamesAPI.getAllGames().then(({games, developerURL}) => {
    games.forEach((game) => {
        // Game processing logic
    })
}).catch(err => {
    // Error handling
})
```

```javascript
ysdk.features.GamesAPI.getGameByID(100000).then(({isAvailable, game}) => {
    if (isAvailable) {
        // Process game if available
    } else {
        // game object is undefined when unavailable
    }
}).catch(err => {
    // Error handling
})
```

## Pitfalls / Notes
- Always guard on `isAvailable` before accessing `game` properties.
- Lists only games visible on the *current* platform/domain — results differ across platforms.

## Source
https://yandex.ru/dev/games/doc/ru/sdk/sdk-other-games
