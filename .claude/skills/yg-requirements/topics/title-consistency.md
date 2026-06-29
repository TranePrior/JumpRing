# Topic — Game title consistency (5.1.3, 5.12, 6.5, 6.6)

Source: <https://yandex.ru/dev/games/doc/ru/requirements/5/1/3>

## Rules
- **5.1.3** For each language, the game name must be identical in-game and in every draft asset.
- **5.12** Unique across the Yandex Games catalog for every selected language.
- **6.5** No "game" / "игра" word in the name unless it's intrinsic.
- **6.6** Concise.

## What to verify in code
1. In-game title strings: grep i18n files for the title key. Compare to the manifest / page title.
2. `<title>` in `index.html`, app name in `package.json`, `capacitor.config.ts` `appName`, Yandex draft.
3. For every selected language, the title is translated (or intentionally kept original) and stays identical between the running game and the draft.

## Cannot check from code
- Uniqueness across the catalog → manual search.
- Draft form vs game equivalence → manual diff.
