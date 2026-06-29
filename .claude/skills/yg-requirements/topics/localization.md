# Topic — Localization (2.10, 2.14, 6.9, 8.2.1, 8.2.3)

Source: <https://yandex.ru/dev/games/doc/ru/requirements/8/2/3>

## Rules
- **2.10** Localization for at least one language selected in the draft.
- **2.14** Auto-detect language via SDK.
- **6.9** If language is picked manually, the switcher works without knowing the current language (icons/flags, not text).
- **8.2.1** Orthography and punctuation correct for the language.
- **8.2.3** Texts that differ by language **and** matter to gameplay must be translated to that language.

## Must be translated
UI buttons, gameplay-critical text, currency names, warnings, control descriptions.

## Does not need translation
Abbreviations, proper names, keyboard key labels, environmental art text, sound-effect labels, symbols.

## What to verify in code
1. i18n setup: `react-i18next`, `i18next`, or similar. Grep `i18n`, `useTranslation`, `t(`.
2. Translation file coverage — diff keys across `*.json` per language; every key present in the default language must exist in every supported language.
3. Auto-detect: SDK gives the user's language → `ysdk.environment.i18n.lang`. Confirm i18n init reads it.
4. No hardcoded user-facing strings in components (grep for Cyrillic / English text inside JSX outside `t()` calls).
5. Language switcher: icons or flags + native language names (e.g. "Русский / English / Türkçe"), not "Switch language" written in one language.
6. Numbers/dates formatted via `Intl.NumberFormat` / `Intl.DateTimeFormat` with the active locale, not hardcoded.

## Common failures
- Default language hardcoded to `'en'` ignoring `ysdk.environment.i18n.lang`.
- Some buttons translated, some leftover in Russian/English.
- Language switcher labelled only in the current language ("Сменить язык") — non-Russian players can't find it.
- Apostrophes/quotes not localised (`"` vs `«»`).
