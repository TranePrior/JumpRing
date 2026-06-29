---
title_ru: Окружение
title_en: Environment Info
source: https://yandex.ru/dev/games/doc/ru/sdk/sdk-environment
---

## Purpose
Read static information about the running game instance: app ID, interface language, and the optional URL `payload` string.

## API Surface

### `ysdk.environment`
```typescript
{
  app: { id: string };
  i18n: { lang: string };   // ISO 639-1 code, e.g. 'ru', 'en', 'tr'
  payload?: string;          // value of the ?payload= query parameter
}
```

## Code Examples

```javascript
const ysdk = (await YaGames.init());
const lang = ysdk.environment.i18n.lang; // 'en', 'ru', ...
```

```javascript
ysdk.environment.payload // returns 'test' for URL ?payload=test
```

## Pitfalls / Notes
- Use `i18n.lang` to auto-detect UI language; do not rely on `navigator.language`.
- `payload` exists only when explicitly present in the URL query string.

## Source
https://yandex.ru/dev/games/doc/ru/sdk/sdk-environment
