# Topic — RSYa monetization (1.12)

Source: <https://yandex.ru/dev/games/doc/ru/requirements/1/12>

## Rule
> Подключена монетизация Рекламной сети Яндекса (РСЯ).

Default: every game must wire monetization. Opt-out requires a note in "Комментарий разработчика" in the Console.

## Game must contain at least one of
- **IAP** — purchase entries on the "Инап-покупки" page with status "Покупки подключены".
- **Ads** — interstitial, rewarded, or sticky banner blocks (the initial loading block does **not** count).
- Or a Console comment stating that monetization is intentionally absent.

## What to verify in code
1. Grep for `ysdk.adv.showFullscreenAdv`, `showRewardedVideo`, `showBannerAdv`, `ysdk.getPayments`.
2. If only an "initial loading ad" is shown — moderation will reject; need one more block.
3. If "Remove ads" IAP exists, confirm it disables future ad calls (4.4 deep dive).
4. Cross-ref: rewarded video must follow section 4.5.x in [`../sections/04-advertising.md`](../sections/04-advertising.md).
