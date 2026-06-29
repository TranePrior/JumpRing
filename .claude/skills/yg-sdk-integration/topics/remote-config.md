---
title_ru: Удалённая конфигурация (флаги)
title_en: Remote Config (Flags)
source: https://yandex.ru/dev/games/doc/ru/sdk/sdk-config
---

## Purpose
Fetch server-controlled feature flags / A-B test variants so game behavior can be tuned without redeploy.

## API Surface

### `ysdk.getFlags(params?: IGetFlagsParams): Promise<IFlags>`

```typescript
interface IFlags {
    [key: string]: string;
}

interface IClientFeature {
    name: string;
    value: string;
}

interface IGetFlagsParams {
    defaultFlags?: IFlags;
    clientFeatures?: IClientFeature[];
}
```

Parameters:
- `defaultFlags` — flat object of key/value strings used as a fallback if remote is unavailable.
- `clientFeatures` — array of player attributes used by the console to evaluate conditional flag values.

Returns: `IFlags` (string values only). Remote values override defaults.

## Code Examples

```javascript
const ysdk = await YaGames.init();
const flags = await ysdk.getFlags();
if (flags.difficult === 'hard') {
    // Enable high difficulty
}
```

```javascript
const flags = await ysdk.getFlags({
    defaultFlags: { difficult: 'easy' }
});
```

```javascript
const player = await ysdk.getPlayer();
const payingStatus = player.getPayingStatus();
const flags = await ysdk.getFlags({
    clientFeatures: [
        { name: 'payingStatus', value: payingStatus }
    ]
});
```

## Pitfalls / Notes
- Request flags once at game start; do not poll.
- Always pass `defaultFlags` so the game stays functional when remote is unreachable.
- All values are strings — parse numerics/booleans explicitly.

## Source
https://yandex.ru/dev/games/doc/ru/sdk/sdk-config
