---
title_ru: Серверное время
title_en: Server Time
source: https://yandex.ru/dev/games/doc/ru/sdk/sdk-server-time
---

## Purpose
Obtain a tamper-resistant timestamp synchronized with Yandex servers. Use whenever device clock manipulation must be prevented (daily rewards, seasonal events, cooldowns).

## API Surface

### `ysdk.serverTime(): number`
Returns the current server time in milliseconds since Unix epoch — same shape as `Date.now()`.

## Code Examples

```javascript
const ysdk = await YaGames.init();
ysdk.serverTime(); // Example: 1720613073778
ysdk.serverTime(); // Example: 1720613132635
```

```javascript
YaGames.init().then(ysdk => {
    ysdk.serverTime(); // Example: 1720613073778
    ysdk.serverTime(); // Example: 1720613132635
});
```

## Pitfalls / Notes
- Call every time you need "now"; do not cache and recompute via `Date.now()` deltas — that re-introduces device-clock dependency.
- Unlike `Date.now()`, this value cannot be forged by changing the system clock.

## Source
https://yandex.ru/dev/games/doc/ru/sdk/sdk-server-time
