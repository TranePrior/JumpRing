---
title_ru: Прочие параметры (устройство, экран, буфер обмена)
title_en: Other Parameters (Device Info, Fullscreen, Clipboard, Method Availability)
source: https://yandex.ru/dev/games/doc/ru/sdk/sdk-params
---

## Purpose
Miscellaneous helpers: detect device type, control fullscreen mode, write to system clipboard, and check whether an SDK method exists at runtime.

## API Surface

### `ysdk.deviceInfo`
- `type: 'desktop' | 'mobile' | 'tablet' | 'tv'` — device class string.
- `isMobile(): boolean`
- `isDesktop(): boolean`
- `isTablet(): boolean`
- `isTV(): boolean`

### `ysdk.screen.fullscreen`
- `STATUS_ON: 'on'` — constant.
- `STATUS_OFF: 'off'` — constant.
- `status: 'on' | 'off'` — current mode.
- `request(): Promise<void>` — enter fullscreen.
- `exit(): Promise<void>` — leave fullscreen.

### `ysdk.clipboard`
- `writeText(text: string): void` — copy text to the system clipboard.

### `ysdk.isAvailableMethod(methodPath: string): Promise<boolean>`
Runtime feature detection (e.g. `'leaderboards.setScore'`, `'player.getIDsPerGame'`). Use before calling methods that may not be present on every platform/SDK version.

## Constants
- Device types: `'desktop'`, `'mobile'`, `'tablet'`, `'tv'`.
- Fullscreen status: `'on'`, `'off'`.

## Pitfalls / Notes
- Most browsers block `screen.fullscreen.request()` outside a direct user gesture (click/tap).
- `clipboard.writeText` can also fail silently without a user gesture in some browsers.
- Always gate optional methods behind `isAvailableMethod` to avoid runtime errors on older SDK builds.

## Source
https://yandex.ru/dev/games/doc/ru/sdk/sdk-params
