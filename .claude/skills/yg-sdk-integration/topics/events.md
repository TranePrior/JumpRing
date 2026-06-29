---
title_ru: События SDK
title_en: SDK Events
source: https://yandex.ru/dev/games/doc/ru/sdk/sdk-events
---

## Purpose
Subscribe to platform events (pause/resume, TV back button, account switching) and dispatch game-originated events (custom exit) to the platform.

## API Surface

### `ysdk.EVENTS`
```typescript
enum ESdkEventName {
    EXIT = 'EXIT',
    HISTORY_BACK = 'HISTORY_BACK',
    ACCOUNT_SELECTION_DIALOG_OPENED = 'ACCOUNT_SELECTION_DIALOG_OPENED',
    ACCOUNT_SELECTION_DIALOG_CLOSED = 'ACCOUNT_SELECTION_DIALOG_CLOSED',
}
```

### `ysdk.on(eventName, listener): () => void`
Subscribes a listener. Returns an unsubscribe function.

### `ysdk.off(eventName, listener): void`
Removes a previously registered listener.

### `ysdk.dispatchEvent(eventName, detail?): Promise<unknown>`
Dispatches an event from the game to the platform.

## Platform-Pushed Events
- `'game_api_pause'` — fires before fullscreen ads, store windows, tab switch, window minimize. Call `GameplayAPI.stop()`.
- `'game_api_resume'` — fires when game regains focus. Call `GameplayAPI.start()`.
- `ysdk.EVENTS.HISTORY_BACK` — TV-only, back button pressed; show custom exit dialog.
- `ysdk.EVENTS.ACCOUNT_SELECTION_DIALOG_OPENED` / `ACCOUNT_SELECTION_DIALOG_CLOSED` — fired around the account selection dialog; reload player data on close.

## Game-Dispatched Events
- `ysdk.EVENTS.EXIT` — dispatched after the user confirms a custom exit dialog.

## Code Examples

```javascript
const pauseCallback = () => {
    pauseGame();
    console.log('GAME PAUSED');
};
ysdk.on('game_api_pause', pauseCallback);
ysdk.off('game_api_pause', pauseCallback);
```

```javascript
const resumeCallback = () => {
    resumeGame();
    console.log('GAME RESUMED');
};
ysdk.on('game_api_resume', resumeCallback);
ysdk.off('game_api_resume', resumeCallback);
```

```javascript
ysdk.on(ysdk.EVENTS.HISTORY_BACK, () => {
    // Show custom exit dialog
});
```

```javascript
ysdk.dispatchEvent(ysdk.EVENTS.EXIT);
```

```javascript
ysdk.on(ysdk.EVENTS.ACCOUNT_SELECTION_DIALOG_OPENED, () => {
    // Pause player data sync
});

ysdk.on(ysdk.EVENTS.ACCOUNT_SELECTION_DIALOG_CLOSED, async () => {
    const player = await ysdk.getPlayer();
    const data = await player.getData();
});
```

## Constants
Event names: `'EXIT'`, `'HISTORY_BACK'`, `'ACCOUNT_SELECTION_DIALOG_OPENED'`, `'ACCOUNT_SELECTION_DIALOG_CLOSED'`, `'game_api_pause'`, `'game_api_resume'`.

## Pitfalls / Notes
- The platform may auto-show a fullscreen ad on startup — handle it through `game_api_pause` / `game_api_resume`, not the ad callbacks.
- If the game already paused itself via `GameplayAPI.stop()`, a subsequent `game_api_pause` should be idempotent.
- Logged-in vs anonymous players may have different save states; always re-read player data after `ACCOUNT_SELECTION_DIALOG_CLOSED`.

## Source
https://yandex.ru/dev/games/doc/ru/sdk/sdk-events
