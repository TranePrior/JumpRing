# Topic — Game completeness (1.15, 2.9)

Source: <https://yandex.ru/dev/games/doc/ru/requirements/1/15>

## Rules
- **1.15** Game looks finished. Not in development or pre-beta.
- **2.9** Main content takes >10 minutes to complete. Quizzes need ≥100 questions.

## What to verify
1. No "WIP" / "Beta" / "Coming soon" badges in UI.
2. No placeholder strings (`Lorem ipsum`, `TODO`, `XXX`, `placeholder`). Grep.
3. No empty menus or buttons that do nothing.
4. Tutorial or onboarding works end-to-end.
5. Win/lose screens implemented.
6. At least one full play loop reaches completion (level cleared, run finished).
7. Content count: enough levels/questions to fit the 10 min / 100 q rule.

## Common failures
- "More levels coming soon" message.
- Stub "settings" panel with no working toggles.
- Final level is a placeholder room.
