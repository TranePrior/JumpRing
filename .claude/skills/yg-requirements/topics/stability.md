# Topic — Stability & errors (1.14)

Source: <https://yandex.ru/dev/games/doc/ru/requirements/1/14>

## Rule
> Нет технических сообщений, ошибок и вылетов, зависаний через некоторое время после начала использования или при определенных действиях пользователя.

Example trigger actions moderation tries: screen rotation, long-press on game field, gestures/swipes, minimising browser, opening ads, navigating browser history.

## The game must
- Load correctly (no black screen, no icon-only).
- Not freeze; not contain dead-end screens with no way back.
- Not show technical text, debug overlays, popup texts.
- Not raise console errors or browser notifications.

## Forbidden states
- Non-loading games (black screen, only the icon).
- Console errors.
- Freezes after tab switch.
- Cannot exit fullscreen.
- Dead-end screen after game over.
- Broken state after watching an ad.
- Visible technical text / code.
- Browser error notifications.

## What to verify in code
1. Open DevTools console while playing — must be empty (also covers 6.4).
2. Trigger every transition: start → play → pause → ad → resume → win/lose → menu → restart. Each must complete without errors.
3. Rotate device (mobile emulator) mid-game — no crash, layout recovers.
4. Long-press / right-click on the game canvas — no context menu (1.6.1.8 / 1.6.2.7).
5. Cycle browser history (back/forward then return) — game still functional.
6. Throttle CPU/network in DevTools — no race-condition crashes during slow asset load.
7. Grep for `console.error`, `throw new Error` left in production code; route them to a safe logger.
