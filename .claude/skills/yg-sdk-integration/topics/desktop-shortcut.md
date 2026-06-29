---
title_ru: Ярлык на рабочий стол
title_en: Desktop Shortcut Prompt
source: https://yandex.ru/dev/games/doc/ru/sdk/sdk-shortcut
---

## Purpose
Prompt the user to add a shortcut (to the Yandex Games catalog on first call, to the game on subsequent calls) to their desktop.

## API Surface

### `ysdk.shortcut.canShowPrompt(): Promise<{ canShow: boolean }>`
Reports whether the platform/browser/device currently allows the shortcut prompt.

### `ysdk.shortcut.showPrompt(): Promise<{ outcome: 'accepted' | string }>`
Displays the native shortcut prompt. `outcome === 'accepted'` indicates the user confirmed.

## Constants
Outcome value documented: `'accepted'`.

## Code Examples

```javascript
const ysdk = await YaGames.init();
const prompt = await ysdk.shortcut.canShowPrompt();

if (prompt.canShow) {
  // Display button for adding shortcut
}
```

```javascript
const ysdk = await YaGames.init();
const result = await ysdk.shortcut.showPrompt();

if (result.outcome === 'accepted') {
  // Award user for adding shortcut
}
```

## Pitfalls / Notes
- First successful prompt creates a shortcut to the Yandex Games catalog; subsequent prompts link directly to the game.
- Availability is gated by browser/OS rules — always check `canShowPrompt()` first.

## Source
https://yandex.ru/dev/games/doc/ru/sdk/sdk-shortcut
