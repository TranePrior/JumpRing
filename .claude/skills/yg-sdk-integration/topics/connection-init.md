---
title_ru: Подключение SDK
title_en: SDK Connection & Initialization
source: https://yandex.ru/dev/games/doc/ru/sdk/sdk-about
---

## Purpose
Load the Yandex Games SDK script into the page and initialize the global `ysdk` object that exposes the rest of the SDK surface.

## API Surface

### Script tag
- Relative path (game hosted on Yandex): `/sdk.js`
- Absolute path (custom domain / S3): `https://sdk.games.s3.yandex.net/sdk.js`

### `YaGames.init(options?): Promise<YsdkInstance>`
Initializes the SDK. Must run after the script is loaded.

Options:
- `signed?: boolean` (default `false`) — when `true`, payment-related responses are encrypted with a signature and intended for server-side processing.

Returns: Promise resolving to the `ysdk` instance.

## Code Examples

```html
<script src="/sdk.js"></script>
```

```html
<script async src="/sdk.js" onload="initSDK()"></script>
```

```javascript
const script = document.createElement('script');
script.src = '/sdk.js';
script.async = true;
script.onload = initSDK;
document.body.append(script);
```

```html
<script src="https://sdk.games.s3.yandex.net/sdk.js"></script>
```

```javascript
async function initSDK() {
    const ysdk = await YaGames.init();
    // SDK methods available here
}
```

```javascript
const ysdk = await YaGames.init();
```

```javascript
const ysdk = await YaGames.init({ signed: true });
```

## Pitfalls / Notes
- Script must finish loading before `YaGames.init()` is called.
- Always `await` or `.then()` the init promise before using subsystems like `adv`, `payments`, `leaderboards`.
- Append `debug-mode=16` to the page URL to see SDK loader status in the bottom-left corner.

## Source
https://yandex.ru/dev/games/doc/ru/sdk/sdk-about
