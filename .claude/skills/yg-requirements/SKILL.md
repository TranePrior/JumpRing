---
name: yg-requirements
description: Audit a HTML5 game project against the Yandex Games publishing requirements. Use when the user types /yg-requirements <topic> (free-form Russian or English, e.g. "settings window", "локализация", "main screen", "rewarded ads", "сохранения"), wants to verify a feature complies with Yandex.Games rules, or asks "does this pass Yandex moderation". Mirrors the structure of https://yandex.ru/dev/games/doc/ru/concepts/requirements with one file per documentation subsection plus alias-based topic routing.
---

# Yandex Games Requirement Auditor

You audit the current project against the official Yandex Games publishing requirements documented at <https://yandex.ru/dev/games/doc/ru/concepts/requirements>. The skill is invoked as `/yg-requirements <topic>` where `<topic>` is a free-form description of what to check (Russian or English).

## How to invoke

The user passes a topic in any wording. Examples:
- `/yg-requirements settings window`
- `/yg-requirements окно настроек`
- `/yg-requirements localization`
- `/yg-requirements главный экран`
- `/yg-requirements rewarded ads`
- `/yg-requirements сохранение прогресса`
- `/yg-requirements 1.10` (direct requirement ID lookup)

If the user does not pass a topic, ask them which area to audit and list the top-level sections from `sections/`.

## Workflow

1. **Resolve the topic** to one or more requirement IDs using the **Alias index** below. If the topic doesn't match, fall back to scanning all section titles in `sections/` and ask the user to pick.
2. **Read the matched files** from `sections/` (whole numbered sections) or `topics/` (subsection-level deep dives). Read every file the alias resolves to — don't skip.
3. **Audit the project**: for each requirement in scope, read the actual code/config/assets that would implement it. Use Grep, Read, Bash. Do not trust assumptions — open files.
4. **Report findings** as a table:

   | ID | Requirement (short) | Status | Evidence | Action |
   |----|---------------------|--------|----------|--------|
   | 1.9 | Save after every action | ✅ Pass | `ProgressStorage.ts:42` writes on every move | — |
   | 6.3 | Pause game | ❌ Missing | No pause button in SettingsPanel | Add pause toggle |
   | 2.10 | Localized to ≥1 selected language | ⚠ Partial | i18n covers ru/en; tr keys missing 12 strings | Fill `tr.json` gaps |

   Status uses: ✅ Pass / ⚠ Partial / ❌ Fail / ❓ Unverifiable / ➖ N/A.
5. **Finish with a one-line verdict**: "Ready for moderation in topic X" or "N blockers, M warnings — see table".

Never invent evidence. If you cannot verify a requirement from code (e.g. promo screenshots, draft form fields), mark it ❓ Unverifiable and explain what to check manually.

## File layout

```
yg-requirements/
├── SKILL.md                     # this file — alias index + workflow
├── sections/                    # one file per top-level Yandex section
│   ├── 01-technical.md          # section 1: technical requirements
│   ├── 02-user-experience.md    # section 2: UX requirements
│   ├── 03-content.md            # section 3: content
│   ├── 04-advertising.md        # section 4: ads
│   ├── 05-promo-materials.md    # section 5: description & promo
│   ├── 06-recommended.md        # section 6: recommended (non-mandatory)
│   └── 08-text-media.md         # section 8: text & media
└── topics/                      # one file per Yandex docs subsection page + alias clusters
    ├── authorization.md         # ru/requirements/1/2
    ├── sound-on-minimize.md     # ru/requirements/1/3
    ├── system-player.md         # ru/requirements/1/6
    ├── tv-support.md            # ru/requirements/1/6/3
    ├── progress-saving.md       # ru/requirements/1/9
    ├── display-scaling.md       # ru/requirements/1/10
    ├── monetization.md          # ru/requirements/1/12
    ├── in-app-purchases.md      # ru/requirements/1/13
    ├── stability.md             # ru/requirements/1/14
    ├── game-completeness.md     # ru/requirements/1/15
    ├── sdk-initialization.md    # ru/requirements/1/19
    ├── duplicates.md            # ru/requirements/3/6
    ├── ad-placement.md          # ru/requirements/4/4
    ├── screenshots.md           # ru/requirements/5/1/1/2
    ├── title-consistency.md     # ru/requirements/5/1/3
    ├── localization.md          # ru/requirements/8/2/3
    ├── interface-in-media.md    # ru/requirements/8/3/4
    ├── media-content.md         # ru/requirements/8/3/5
    ├── media-safety.md          # ru/requirements/8/3/6
    └── links-navigation.md      # ru/requirements/8/4/1
```

The file tree mirrors the Yandex documentation 1:1 — every `topics/*.md` corresponds to one subsection page under `ru/requirements/`. No invented groupings.

## Alias index

Map free-form input → files. **Case- and language-insensitive.** Match by substring/keyword, not exact equality. If multiple aliases hit, read all matched files. Numeric IDs (e.g. `1.10`, `2.10`) resolve to the section file that owns them.

Aliases route to **real Yandex docs subsections only** (files in `topics/` / `sections/`). When a user topic does not correspond to a Yandex page (e.g. "settings window", "main screen") but spans several Yandex rules, route to **multiple** Yandex files — never invent a synthetic file.

| User input (RU & EN keywords) | Files to read |
|-------------------------------|---------------|
| `localization`, `i18n`, `translation`, `локализация`, `перевод`, `языки`, `language` | `topics/localization.md`, `sections/02-user-experience.md`, `sections/06-recommended.md` |
| `auth`, `authorization`, `login`, `авторизация`, `вход`, `yandex id` | `topics/authorization.md` |
| `sound on minimize`, `mute on blur`, `звук при сворачивании`, `звук вне игры` | `topics/sound-on-minimize.md` |
| `system player`, `системный плеер` | `topics/system-player.md` |
| `tv`, `tv support`, `android tv`, `пульт`, `телевизор` | `topics/tv-support.md` |
| `save`, `saves`, `progress`, `cloud save`, `сохранение`, `прогресс`, `облако` | `topics/progress-saving.md` |
| `display`, `scaling`, `layout`, `viewport`, `responsive`, `отображение`, `масштаб`, `адаптив`, `fullscreen`, `полноэкранный` | `topics/display-scaling.md`, `sections/01-technical.md` |
| `monetization`, `rsya`, `ads sdk`, `монетизация`, `рся` | `topics/monetization.md` |
| `iap`, `in-app`, `purchases`, `payment`, `покупки`, `инап`, `платежи`, `consume`, `консумирование` | `topics/in-app-purchases.md` |
| `stability`, `errors`, `crash`, `freeze`, `ошибки`, `зависание`, `вылеты`, `тех. сообщения`, `console errors`, `консоль ошибок`, `webgl` | `topics/stability.md`, `sections/06-recommended.md` |
| `completeness`, `finished`, `beta`, `завершенность`, `готовность` | `topics/game-completeness.md` |
| `sdk`, `loading api`, `gameplay api`, `loadingapi.ready`, `gameplayapi.start`, `инициализация sdk` | `topics/sdk-initialization.md` |
| `duplicates`, `copy`, `clone`, `дубликаты`, `копия` | `topics/duplicates.md` |
| `ads`, `ad placement`, `interstitial`, `sticky`, `реклама`, `расположение рекламы`, `pause on ad`, `пауза в рекламе` | `topics/ad-placement.md`, `sections/04-advertising.md` |
| `rewarded`, `reward video`, `rv`, `награда за рекламу`, `вознаграждение` | `sections/04-advertising.md` (rules 4.5.x) |
| `screenshots`, `media`, `скриншоты`, `видео`, `геймплей в скриншотах` | `topics/screenshots.md`, `sections/05-promo-materials.md` |
| `title`, `name consistency`, `название`, `название игры` | `topics/title-consistency.md`, `sections/05-promo-materials.md`, `sections/06-recommended.md` |
| `interface in media`, `ui on icon`, `ui на иконке`, `интерфейс в медиа` | `topics/interface-in-media.md` |
| `media content`, `nsfw`, `эротика`, `содержание медиа` | `topics/media-content.md` |
| `safety`, `age`, `discrimination`, `этика`, `безопасность медиа` | `topics/media-safety.md` |
| `links`, `navigation`, `external links`, `ссылки`, `навигация` | `topics/links-navigation.md` |
| `settings`, `settings window`, `настройки`, `окно настроек`, `pause`, `пауза`, `mute`, `звук`, `music` | `sections/06-recommended.md` (rules 6.2, 6.3, 6.9), `topics/localization.md`, `topics/sound-on-minimize.md` |
| `main screen`, `main menu`, `главный экран`, `главное меню`, `menu` | `topics/display-scaling.md`, `sections/05-promo-materials.md` (5.1.3 title), `sections/06-recommended.md` (6.5–6.7), `sections/01-technical.md` (1.10, 1.6.x) |
| `controls`, `input`, `gestures`, `управление`, `жесты` | `sections/01-technical.md` (1.6.x, 1.8), `sections/02-user-experience.md` (2.2) |
| `1.x`, `2.x`, ... `8.x` (numeric ID) | matching `sections/0X-*.md` |
| `all`, `everything`, `full audit`, `всё`, `полная проверка` | every file in `sections/` and `topics/` (warn: long audit) |

If a user-provided topic doesn't match any alias and isn't a numeric ID, do not guess — list available topics and ask which to use.

When an alias spans multiple files (e.g. "main screen" → 4 files), state in your reply which Yandex rules you're auditing against so the user understands the scope.

## Audit hints (what to actually check in code)

These shortcuts speed up evidence-gathering. They are **starting points**; always verify by reading the files.

- **Save / progress (1.9, 1.13.3)** → search `localStorage`, `ysdk.getStorage`, `setItem`, `setData`, classes named `*Storage`, `*Progress`. Verify save is called inside every state-changing handler, not only on unmount.
- **Sound on minimize (1.3)** → grep `visibilitychange`, `document.hidden`, `pagehide`, `blur`, audio/music pause logic. Test the 3 scenarios in `topics/sound-on-minimize.md`.
- **Display & scaling (1.10)** → look at viewport `<meta>` in `index.html`, CSS `overflow`, `touch-action`, `overscroll-behavior`, any scale hook (e.g. `useUiScale`), `resize` listeners, safe-area-inset.
- **SDK init (1.19)** → grep `YaGames.init`, `LoadingAPI.ready`, `GameplayAPI.start`, `GameplayAPI.stop`, `ysdk.on`, `ysdk.off`. Confirm `ready()` fires when game is playable (not on script load).
- **Auth (1.2)** → grep `ysdk.auth`, `getPlayer`, `signin`. Confirm guest flow exists and progress persists without login.
- **Rewarded video (4.5)** → grep `showRewardedVideo`, `rewarded`, RV button labels include "за рекламу" / "for ad" wording. Reward must be a bonus, not a gate.
- **Pause on ad (4.7)** → confirm game loop + audio pause on `AdvOpened`/`RewardedAdvOpened` and resume on close.
- **IAP (1.13)** → grep `ysdk.getPayments`, `purchase`, `consumePurchase`. Check consume is called after every fulfilment.
- **Localization (2.10, 8.2.3)** → check `i18n/`, `react-i18next` usage, translation files coverage (run a key-diff between languages), check that `2.14` auto-detect uses `ysdk.environment.i18n.lang`.
- **External links (8.4)** → grep `window.open`, `href="http`, `target="_blank"`. All external URLs must go through SDK and only to Yandex.Games catalogue.
- **Promo / screenshots / draft fields** → cannot be checked from code; mark ❓ Unverifiable and list what the user must verify in the developer console.

## Project stack adapter (Unity 6 → WebGL + RetroCat PlatformLink)

**THIS project is NOT vanilla HTML5/JS.** It is a Unity WebGL game. The Yandex SDK is wired through the RetroCat **PlatformLink** framework (namespace `PlatformLink`, static facade `PLink`), not by calling `ysdk.*` directly in game code. The raw JS lives in `Assets/WebGLTemplates/PlatformLinkTemplate/` (`index.html` + `TemplateData/plugin.js`, ~940 lines); C# services call into it via a jslib bridge. The active platform is set in `Assets/Resources/ProjectConfigs/PlatformLinkConfig.asset` (`_activePlatformIndex`, Yandex = 2).

When auditing this project, translate every web hint above to its Unity/PlatformLink equivalent:

| Requirement | Web hint (ignore) | Unity + PlatformLink: where to actually look |
|-------------|-------------------|----------------------------------------------|
| Save / progress (1.9, 1.13.3) | `localStorage`, `setData` | `PlatformStorageService.cs` (`PLink.Storage.SaveInt/SaveString` + PlayerPrefs mirror); grep `SetInt`/`SetString` call sites — must fire in each state-changing handler |
| Sound on minimize (1.3) | `visibilitychange` | `WebGLFocusHandler.cs` (`OnApplicationFocus` → `AudioListener.pause`/`Time.timeScale`). NB: WebGL focus ≠ `visibilitychange` — still test the 3 manual scenarios |
| Display & scaling (1.10, 1.6) | viewport `<meta>`, CSS | `index.html` viewport + `TemplateData/fit.js`; canvas right-click/long-press → check for `contextmenu` prevention in the template |
| SDK init (1.19) | `YaGames.init`, `LoadingAPI.ready` | `plugin.js` (`ysdk.features.LoadingAPI.ready`) triggered from C# (`GameCompositionRoot` → `PLink.Analytics.SendGameReady`). `GameplayAPI.start/stop` often absent — flag it |
| Auth (1.2) | `ysdk.auth` | `plugin.js` (`ysdk.auth.openAuthDialog`); confirm it fires only on explicit button, guest save works via `PlatformStorageService` |
| Rewarded (4.5) | `showRewardedVideo` | `RewardedAdService.cs` (`PLink.Advertisement.RewardedAd`); button labels live in `Assets/GAME/Data/Localization/*.asset` + prefabs, NOT in JS |
| Pause on ad (4.7) | `AdvOpened` | `RewardedAdService.cs` / `InterstitialAdService.cs` `PauseGame()`/`ResumeGame()` (`Time.timeScale=0` + `AudioListener.pause`) |
| IAP (1.13) | `getPayments` | `NoAdsService.cs` (`PLink.Purchases` — `Purchased`, `GetPurchases`, `ConsumePurchase`) |
| Localization (2.10, 2.14) | `react-i18next`, `ysdk.environment.i18n.lang` | `LocalizationService.cs` (`LocalizationData` ScriptableObjects per language; `PLink.Environment.Language` for auto-detect). Key-diff the `Localization_*.asset` files |
| Leaderboards | — | `ScoreService.cs` (`PLink.Leaderboard.SetScore`) |
| External links (8.4) | `window.open` | `plugin.js` + `OurGamesPopup.cs`/`SharePopup.cs` (`PLink.*` GamesAPI) |

If `PLink` / `PlatformLink` is **not** present in the project, fall back to the generic web hints above.

## Output style

- Lead with the table.
- Group findings by section if you audited more than ~6 items.
- Use file:line references in the Evidence column.
- Keep the Action column terse — a one-line fix description.
- End with a single-line verdict.

Do not lecture about Yandex Games in general — only report what's relevant to the requested topic.
