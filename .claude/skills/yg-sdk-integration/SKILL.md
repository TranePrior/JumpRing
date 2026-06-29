---
name: yg-sdk-integration
description: Reference and implementation guide for the Yandex Games HTML5 SDK. Use when the user types /yg-sdk-integration <topic> (RU or EN, e.g. "rewarded ads", "лидерборды", "player data", "пауза"), asks how to integrate a Yandex Games SDK feature, or wants to wire YaGames into a project. Mirrors https://yandex.ru/dev/games/doc/ru/sdk/sdk-about with one file per HTML5 SDK documentation page.
---

# Yandex Games SDK — HTML5 Integration Reference

Reference for the JavaScript SDK at `https://yandex.ru/dev/games/doc/ru/sdk/`. Each `topics/*.md` file mirrors one documentation page (HTML5 section) and lists every method, parameter, callback, constant and code sample.

## How to invoke

`/yg-sdk-integration <topic>` where `<topic>` is free-form (RU or EN). Examples:

- `/yg-sdk-integration rewarded ads`
- `/yg-sdk-integration лидерборды`
- `/yg-sdk-integration player data`
- `/yg-sdk-integration инап покупки`
- `/yg-sdk-integration pause resume`
- `/yg-sdk-integration init`
- `/yg-sdk-integration example` (full HTML boilerplate)

If no topic is passed, ask the user to pick one from the file list below.

## Workflow

1. **Resolve the topic** using the alias index. Multiple matches → read every matched file.
2. **Read the matched topic files** in full. They contain verbatim signatures and code samples.
3. **Detect intent** from the user message:
   - **Reference / "how does X work"** → respond with the API surface, code snippets, and pitfalls from the topic file. Do not touch project code.
   - **Implementation / "add X to this project" / "wire up X"** → read the project first (find existing SDK init, ad service, payment layer), then propose minimal edits that match the project's style. Show diffs, do not auto-apply unless the user asks.
4. **Always cross-reference moderation rules** when the feature has compliance implications (ads → 4.x, IAP → 1.13.x, save → 1.9, init → 1.19). Mention the relevant Yandex requirement IDs from the [`yg-requirements`](../yg-requirements/SKILL.md) skill so the user knows what moderation will check.
5. **Cite the source page URL** at the end so the user can deep-link to Yandex docs.

## File layout

```
yg-sdk-integration/
├── SKILL.md                       # this file
└── topics/                        # one file per HTML5 SDK doc page
    ├── connection-init.md         # sdk-about — script tag + YaGames.init
    ├── game-events.md             # sdk-game-events — LoadingAPI.ready, GameplayAPI.start/stop
    ├── player-data.md             # sdk-player — getPlayer, setData, setStats
    ├── remote-config.md           # sdk-config — getFlags, defaultFlags, clientFeatures
    ├── advertising.md             # sdk-adv — showFullscreenAdv, showRewardedVideo, sticky banner
    ├── in-app-purchases.md        # sdk-purchases — getPayments, purchase, consumePurchase
    ├── leaderboards.md            # sdk-leaderboard — getDescription, setScore, getEntries
    ├── async-multiplayer.md       # sdk-multiplayer-sessions — sessions.init/commit/push
    ├── review.md                  # sdk-review — canReview, requestReview
    ├── desktop-shortcut.md        # sdk-shortcut — canShowPrompt, showPrompt
    ├── environment.md             # sdk-environment — app.id, i18n.lang, payload
    ├── server-time.md             # sdk-server-time — ysdk.serverTime
    ├── events.md                  # sdk-events — EVENTS enum, on/off/dispatchEvent, pause/resume
    ├── other-games.md             # sdk-other-games — GamesAPI.getAllGames, getGameByID
    ├── other-params.md            # sdk-params — deviceInfo, screen.fullscreen, clipboard
    └── example.md                 # sdk-example — full HTML boilerplate (sync + async)
```

Each topic file follows the same shape: **YAML header → Purpose → API Surface → Constants → Code Examples → Pitfalls/Notes → Source URL**.

## Alias index

Match by substring/keyword, case- and language-insensitive. Multiple matches → load all.

| User input | Files to read |
|------------|---------------|
| `init`, `connect`, `script`, `подключение`, `инициализация`, `yagames.init`, `sdk.js` | `topics/connection-init.md` |
| `loading`, `ready`, `gameplay api`, `loadingapi.ready`, `gameplayapi.start`, `gameplayapi.stop`, `разметка геймплея`, `loading api` | `topics/game-events.md` |
| `player`, `auth`, `getplayer`, `setdata`, `getdata`, `setstats`, `incrementstats`, `профиль`, `игрок`, `данные игрока`, `авторизация`, `авторизоваться` | `topics/player-data.md` |
| `config`, `flags`, `feature flag`, `a/b`, `remote config`, `getflags`, `defaultflags`, `clientfeatures`, `конфиг`, `флаги`, `удаленная конфигурация` | `topics/remote-config.md` |
| `ad`, `ads`, `реклама`, `fullscreen ad`, `interstitial`, `showfullscreenadv`, `rewarded`, `rv`, `rewarded video`, `награда за рекламу`, `sticky`, `banner`, `баннер`, `стики` | `topics/advertising.md` |
| `iap`, `purchase`, `payments`, `getpayments`, `consume`, `consumepurchase`, `getcatalog`, `getpurchases`, `покупки`, `платежи`, `инап`, `консумирование`, `каталог` | `topics/in-app-purchases.md` |
| `leaderboard`, `leaderboards`, `setscore`, `getentries`, `getplayerentry`, `рейтинг`, `лидерборд`, `таблица лидеров` | `topics/leaderboards.md` |
| `multiplayer`, `sessions`, `session`, `мультиплеер`, `асинхронный мультиплеер` | `topics/async-multiplayer.md` |
| `review`, `canreview`, `requestreview`, `оценка`, `оценить игру`, `rate game` | `topics/review.md` |
| `shortcut`, `desktop shortcut`, `ярлык`, `рабочий стол` | `topics/desktop-shortcut.md` |
| `environment`, `env`, `app.id`, `i18n.lang`, `payload`, `язык игры`, `переменные окружения`, `app id` | `topics/environment.md` |
| `server time`, `time`, `servertime`, `серверное время`, `время сервера` | `topics/server-time.md` |
| `events`, `event`, `on`, `off`, `dispatch`, `pause`, `resume`, `pause/resume`, `history_back`, `exit`, `account dialog`, `события`, `пауза`, `возобновление`, `game_api_pause`, `game_api_resume` | `topics/events.md` |
| `other games`, `gamesapi`, `getallgames`, `getgamebyid`, `другие игры`, `каталог игр` | `topics/other-games.md` |
| `params`, `deviceinfo`, `device`, `screen`, `fullscreen api`, `clipboard`, `isavailablemethod`, `другие объекты`, `устройство` | `topics/other-params.md` |
| `example`, `boilerplate`, `template`, `пример`, `шаблон`, `полный пример` | `topics/example.md` |
| `all`, `everything`, `всё`, `всё sdk`, `full reference` | every file (warn: long) |

If the input does not match anything, do not guess — list the file inventory and ask the user to pick.

## Stack note — Unity + RetroCat PlatformLink projects

If the project is **Unity → WebGL** (presence of `Assets/`, `*.cs`, `*.unity`, and a `PlatformLink` / `PLink` reference), the Yandex JS SDK is **already wired** through the RetroCat **PlatformLink** framework. Do NOT write raw `ysdk.*` calls into game code.

- The topic files here remain valid as a **specification reference** — exact method signatures, callbacks, constants, pitfalls, and the compliance rule IDs. Use them to verify the existing wiring is correct.
- JS SDK surface is bridged in `Assets/WebGLTemplates/PlatformLinkTemplate/TemplateData/plugin.js` + `index.html` (loader `yandex.ru/games/sdk/v2`).
- Game code calls the C# facade `PLink`: `PLink.Advertisement.RewardedAd/InterstetialAd`, `PLink.Storage`, `PLink.Purchases`, `PLink.Leaderboard`, `PLink.Environment`, `PLink.Analytics`.
- Existing service seams to extend (don't reinvent): `RewardedAdService.cs`, `InterstitialAdService.cs`, `PlatformStorageService.cs`, `NoAdsService.cs`, `ScoreService.cs`, `LocalizationService.cs`.
- To add a genuinely missing SDK feature: extend `plugin.js` (JS side) + the jslib bridge + a C# service/PLink module — in that order. Match the project's coroutine/event style; this codebase avoids `async/await` (WebGL is single-threaded).

For a non-Unity (vanilla HTML5/JS/TS) project, follow the implementation steps below as written.

## When the user wants to implement, not just read

Detect implementation intent ("add X", "wire X", "integrate X", "подключи X", "добавь X", "реализуй X"). Steps:

1. **Audit existing wiring first.** Grep for `YaGames`, `ysdk`, `sdk.js` in the project. If the SDK is not wired yet → start from `topics/connection-init.md` + `topics/example.md`.
2. **Locate the right insertion seam.** Most projects keep an SDK facade (`services/ads/*`, `services/sdk/*`, etc.). Add to the facade, not into UI components.
3. **Follow the project's async style.** If the codebase uses `async/await`, do not introduce raw `.then` chains, and vice versa.
4. **Type imports.** If TypeScript is used, write minimal type declarations for the SDK shape (Yandex does not ship `.d.ts`). Place in a single `*.d.ts` or local interface, not scattered.
5. **Pair every feature with its compliance rule.** Ads → cross-ref Yandex moderation 4.x. IAP → 1.13.x. Save → 1.9. Init → 1.19. Mention the rule IDs in the explanation.
6. **Guard for local dev.** SDK loads on `yandex.com/games` domains. Local dev needs a stub or a `if (typeof YaGames === 'undefined')` fallback. Suggest the project's existing pattern; do not invent one.

## Output style

- For **reference questions** — lead with the signature, then a code snippet, then pitfalls. Keep under 30 lines unless the user asks for more.
- For **implementation requests** — short plan first (3–5 bullets), then diffs. Ask before writing files unless the user already said "go".
- Never paste an entire topic file back to the user — extract the relevant ~10 lines.
- Always link the source URL at the end.
