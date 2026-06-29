---
title_ru: Реклама
title_en: Advertising API
source: https://yandex.ru/dev/games/doc/ru/sdk/sdk-adv
---

## Purpose
Display fullscreen ads, rewarded video, and sticky banners. All ad formats are exposed under `ysdk.adv`.

## API Surface

### `ysdk.adv.showFullscreenAdv(options?): void`
Shows a fullscreen ad that blocks the game.

```typescript
showFullscreenAdv(options?: {
  callbacks?: {
    onOpen?: () => void;
    onClose?: (wasShown: boolean) => void;
    onError?: (error: object) => void;
  }
}): void
```
- `onOpen` — ad opened on screen.
- `onClose(wasShown)` — fired on close, error, or when ad was not shown due to frequency limits.
- `onError(error)` — fired on error.

### `ysdk.adv.showRewardedVideo(options?): void`
Shows a rewarded video ad. Grant the reward in `onRewarded`.

```typescript
showRewardedVideo(options?: {
  callbacks?: {
    onOpen?: () => void;
    onRewarded?: () => void;
    onClose?: (wasShown: boolean) => void;
    onError?: (error: object) => void;
  }
}): void
```

### `ysdk.adv.getBannerAdvStatus(): Promise<{ stickyAdvIsShowing: boolean; reason?: 'ADV_IS_NOT_CONNECTED' | 'UNKNOWN' }>`
Reports the current sticky banner state.

### `ysdk.adv.showBannerAdv(): Promise<{ stickyAdvIsShowing: boolean; reason?: 'ADV_IS_NOT_CONNECTED' | 'UNKNOWN' }>`
Shows the sticky banner.

### `ysdk.adv.hideBannerAdv(): Promise<{ stickyAdvIsShowing: boolean }>`
Hides the sticky banner.

## Constants
Banner `reason` values: `'ADV_IS_NOT_CONNECTED'`, `'UNKNOWN'`.

## Code Examples

```javascript
const ysdk = await YaGames.init();
ysdk.adv.showFullscreenAdv({
    callbacks: {
        onOpen: () => console.log('Ad opened.'),
        onClose: (wasShown) => console.log(wasShown ? 'Shown and closed.' : 'Not shown.'),
        onError: (error) => console.log('Call error.'),
    }
})
```

```javascript
const ysdk = await YaGames.init();
ysdk.adv.showRewardedVideo({
    callbacks: {
        onOpen: () => console.log('Ad opened.'),
        onRewarded: () => console.log('User received reward.'),
        onClose: (wasShown) => console.log(wasShown ? 'Shown and closed.' : 'Not shown.'),
        onError: (error) => console.log('Call error.'),
    }
})
```

```javascript
const ysdk = await YaGames.init();
const { stickyAdvIsShowing, reason } = await ysdk.adv.getBannerAdvStatus();

if (stickyAdvIsShowing) {
    // Ad is displaying
} else if (reason) {
    console.log(reason);
} else {
    ysdk.adv.showBannerAdv();
}
```

## Pitfalls / Notes
- Fullscreen ad frequency is throttled by the platform; rewarded videos have no frequency limit.
- Never call ads during active interaction — accidental clicks can be flagged as ad fraud.
- Avoid timer-only triggers like `setInterval(() => ysdk.adv.showFullscreenAdv(), 180000)`; trigger after user actions or only after long (>5 min) levels.
- Always pair ad display with `GameplayAPI.stop()` before opening and `GameplayAPI.start()` after `onClose`.

## Source
https://yandex.ru/dev/games/doc/ru/sdk/sdk-adv
