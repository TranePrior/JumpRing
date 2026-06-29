---
title_ru: Асинхронный мультиплеер (сессии)
title_en: Async Multiplayer (Sessions)
source: https://yandex.ru/dev/games/doc/ru/sdk/sdk-multiplayer-sessions
---

## Purpose
Run asynchronous "ghost-style" competitive gameplay by recording the current player's session timeline and replaying opponents' timelines locally. No dedicated server required.

## API Surface

### `ysdk.multiplayer.sessions.init(config): Promise<Session[]>`
Initialize multiplayer and load opponent sessions.

```typescript
config: {
  count: number;                  // up to 10
  isEventBased: boolean;
  maxOpponentTurnTime?: number;   // milliseconds, caps opponent move time
  meta?: {
    meta1?: { min: number; max: number };
    meta2?: { min: number; max: number };
    meta3?: { min: number; max: number };
  };
}
```

### `ysdk.multiplayer.sessions.commit(payload: object): void`
Append a transaction to the current player's timeline. SDK fills in `id` and `time`.

### `ysdk.multiplayer.sessions.push(meta): void`
Save the current timeline to the server.

```typescript
meta: {
  meta1?: number;
  meta2?: number;
  meta3?: number;
}
```
At least one meta field is required.

### Events (via `ysdk.on`)

#### `'multiplayer-sessions-transaction'`
Fires when an opponent transaction should run locally.
Payload: `{ opponentId: string, transactions: Transaction[] }`.

#### `'multiplayer-sessions-finish'`
Fires when an opponent's session has finished. Payload: `opponentId: string`.

## Response Structures
```typescript
interface Session {
  id: string;
  meta: { meta1: number; meta2: number; meta3: number };
  player: { avatar: string; name: string };
  timeline: Array<{
    id: string;
    payload: object | string | undefined;
    time: number;
  }>;
}

interface Transaction {
  id: number;
  payload: object;
  time: number;
}
```

## Code Examples

```javascript
const work = async () => {
  const opponents = await ysdk.multiplayer.sessions.init({
    count: 2,
    isEventBased: true,
    maxOpponentTurnTime: 200,
    meta: {
      meta1: { min: 0, max: 6000 },
      meta2: { min: 2, max: 10 },
    },
  });
  console.log(opponents);
}
work();
```

```javascript
ysdk.multiplayer.sessions.commit({ x: 1, y: 2, z: 3, health: 67 });
ysdk.multiplayer.sessions.commit({ x: 4, y: -2, z: 19, health: 15 });
```

```javascript
ysdk.multiplayer.sessions.push({ meta1: 12, meta2: -2 });
```

```javascript
ysdk.on('multiplayer-sessions-transaction', ({ opponentId, transactions }) => {
  // transactions: array of events with payload and timing
});
```

```javascript
ysdk.on('multiplayer-sessions-finish', (opponentId) => {
  console.log(opponentId);
});
```

```javascript
ysdk.features.GameplayAPI.start();  // Resume multiplayer
ysdk.features.GameplayAPI.stop();   // Pause multiplayer
```

## Constants
Event names: `'multiplayer-sessions-transaction'`, `'multiplayer-sessions-finish'`.

## Pitfalls / Notes
- Maximum session payload: 200 KB.
- Maximum loadable opponents: 10.
- At least one `meta` filter must be supplied to `init` for matchmaking.
- Self-managed mode returns raw sessions; `isEventBased: true` lets the SDK schedule transactions automatically.

## Source
https://yandex.ru/dev/games/doc/ru/sdk/sdk-multiplayer-sessions
