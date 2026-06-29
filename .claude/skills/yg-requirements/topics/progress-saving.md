# Topic — Progress saving (1.9, 1.13.3, 2.6)

Source: <https://yandex.ru/dev/games/doc/ru/requirements/1/9>

## Rules
> В играх с внутренним прогрессом (новые уровни, рекорды, достижения, улучшения) сохранение происходит сразу после действия игрока или по кнопке сохранения.

- Page refresh must not lose progress.
- Save is independent of auth state (guests must save too).
- Save survives device orientation change.

## Storage options Yandex permits
1. **Yandex Games servers** (recommended) — `ysdk.getPlayer().setData()` / `getData()`.
2. External server owned by the developer.
3. Browser storage (`localStorage` / `IndexedDB`) — only for simple games without IAP.

## When saving is mandatory
Records, achievements, level unlocks, progressive difficulty.

## When saving is optional
Coloring games, puzzles, quizzes without persistence.

## What to verify in code
1. Grep for save call sites: `setItem`, `setData`, `getStorage`, `*Storage`, `save()`.
2. Confirm save is invoked **inside every state-changing handler**, not only `useEffect` cleanup / page unload (which is unreliable on mobile background-kill).
3. With Yandex SDK: `ysdk.getPlayer({signed: true}).then(p => p.setData(...))`. For unauthorised users, fall back to local storage with same key shape.
4. **1.13.3 cross-device**: IAP-affected progress must be on a server (Yandex or own), not only local.
5. Test: open game → make progress → reload → progress restored.

## Common failures
- Save only on `beforeunload` — mobile browsers don't fire it reliably.
- Save under user-id key for guests where user-id is `null`/empty.
- Saves to `localStorage` while IAPs are mirrored only locally → fails 1.13.3.
- Orientation change re-mounts root and resets in-memory state without writing first.
