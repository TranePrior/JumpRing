# Topic — In-app purchases (1.13.x)

Source: <https://yandex.ru/dev/games/doc/ru/requirements/1/13>

## Rules
- **1.13.1** Consume method is wired. *If purchases are tested before consume is set up, leftover unprocessed payments make moderation impossible.*
- **1.13.2** Portal-currency name and icon come from the SDK (not hardcoded).
- **1.13.3** Purchases made on one account are saved server-side (Yandex or external) and accessible across devices / browsers.
- **1.13.4** Every purchase displays a numeric price + the portal currency.
- **1.13.5** Image, name, contents and properties of the delivered purchase must match what was advertised.

## What to verify in code
1. Payment init: `ysdk.getPayments({ signed: true })`.
2. Consume: after `payments.purchase(...)` resolves and the player is granted the goods, call `payments.consumePurchase(purchaseToken)`. Grep for `consumePurchase` — must be reachable from every successful purchase path.
3. Currency: prices rendered from `payments.getCatalog()` results (`item.priceValue` + `item.priceCurrencyImage`), not hardcoded "100 ⚡".
4. Cross-device: granted items written to `player.setData()` (or your server), not only `localStorage`.
5. Catalog parity: every item shown in the in-game shop matches an entry in the Console catalog (id, price, art).

## Common failures
- Consume only on app close — purchases re-deliver on next launch.
- Currency icon is a static emoji/asset, ignoring `priceCurrencyImage`.
- "Remove ads" granted locally only; reinstall loses the entitlement.
- Shop card shows different art / contents than the Console offer.
