# Topic — Display & scaling (1.10.x, 1.6.1.x, 1.6.2.x)

Source: <https://yandex.ru/dev/games/doc/ru/requirements/1/10>

## Rules
- **1.10.1** Game field does not exceed the screen; nothing is cut off.
- **1.10.2** No browser scroll, no swipe-to-refresh.
- **1.10.3** Internal elements and texts do not overlap each other.
- **1.10.4** Reachable with one hand; the main scene stays in view without extra scrolling.

### Mobile-specific
- **1.6.1.1** Fullscreen during play/launch.
- **1.6.1.3** Elements do not deform / stretch disproportionately on orientation change.
- **1.6.1.5** Fully controllable by touch gestures (optionally augmented by accelerometer).

### Desktop-specific
- **1.6.2.1** Active field stretches to the edge of the available area (excluding sticky banners).
- **1.6.2.2** Long side ≤ 2× short side (no extreme aspect ratios).
- **1.6.2.3** Elements do not deform on viewport resize.

## What to verify in code
1. `index.html` viewport meta: `width=device-width, initial-scale=1.0, viewport-fit=cover` and ideally `maximum-scale=1.0, user-scalable=no`.
2. CSS: `html, body { overflow: hidden; overscroll-behavior: none; touch-action: none; }`.
3. Any scaling hook (e.g. `useUiScale`) — must react to `resize` and `visualViewport.resize` (URL bar collapsing).
4. Phaser scale config: `Phaser.Scale.FIT` or `RESIZE` with proper aspect logic.
5. Safe-area handling: `env(safe-area-inset-*)` used on edges if iOS notch matters.
6. Check that the active play area never exceeds 2:1 ratio after layout (`Math.min(w,h) * 2 >= Math.max(w,h)`).
7. Open DevTools → device toolbar → cycle through 360×640, 768×1024, 1920×1080, 1280×720 (TV). No element clipped or overlapping.

## Common failures
- `overflow: auto` somewhere up the tree enables swipe-to-refresh.
- Fixed pixel sizes on root grid → board cut off on small screens.
- Modal popup taller than viewport with no internal scroll.
- Text wraps and pushes button below fold.
- Orientation change relayouts without resetting Phaser scale.
