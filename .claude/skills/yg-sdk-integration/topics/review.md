---
title_ru: Запрос оценки игры
title_en: Review / Feedback Prompt
source: https://yandex.ru/dev/games/doc/ru/sdk/sdk-review
---

## Purpose
Show the platform's native review dialog so authenticated players can rate and comment on the game.

## API Surface

### `ysdk.feedback.canReview(): Promise<{ value: boolean; reason?: string }>`
Checks whether the player is eligible for a review prompt.

Reason codes (when `value === false`):
- `'NO_AUTH'` — user not authenticated
- `'GAME_RATED'` — user has already rated
- `'REVIEW_ALREADY_REQUESTED'` — prompt is pending the player's action
- `'REVIEW_WAS_REQUESTED'` — user already acted on a prior prompt
- `'UNKNOWN'` — server error

### `ysdk.feedback.requestReview(): Promise<{ feedbackSent: boolean }>`
Opens the native review dialog. May be called at most once per session, and only after `canReview()` returns `value: true`.

## Constants
Reason codes: `'NO_AUTH'`, `'GAME_RATED'`, `'REVIEW_ALREADY_REQUESTED'`, `'REVIEW_WAS_REQUESTED'`, `'UNKNOWN'`.

## Code Example

```javascript
const ysdk = await YaGames.init();

const { value, reason } = await ysdk.feedback.canReview();

if (value) {
    const { sentFeedback } = ysdk.feedback.requestReview();
} else {
    console.log(reason);
}
```

## Pitfalls / Notes
- Calling `requestReview()` without first calling `canReview()` triggers error: `"use canReview before requestReview"`.
- Only one prompt per session; second attempts fail silently.
- Unauthenticated users can never see the prompt.

## Source
https://yandex.ru/dev/games/doc/ru/sdk/sdk-review
