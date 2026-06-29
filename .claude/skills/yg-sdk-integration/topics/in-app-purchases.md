---
title_ru: Внутриигровые покупки
title_en: In-App Purchases
source: https://yandex.ru/dev/games/doc/ru/sdk/sdk-purchases
---

## Purpose
Sell consumable / non-consumable in-game items via Yandex's payment gateway. Supports client-side or signed server-side verification.

## API Surface

### `ysdk.getPayments(options?): Promise<Payments>`
Pre-loads the payments subsystem (avoids first-call delay vs direct `ysdk.payments`).
- `options.signed?: true` — return signed payloads for server verification.

### `payments.purchase(data): Promise<IPurchase | ISign>`
```typescript
purchase(data: {
    id: string;                   // product ID
    developerPayload?: string;    // opaque string echoed back
}): Promise<IPurchase | ISign>
```

### `payments.getPurchases(): Promise<IPurchase[] | ISign>`
Returns unconsumed purchases. Call on every startup to recover from interruptions.

### `payments.getCatalog(): Promise<IProduct[]>`
Returns available products.

### `payments.consumePurchase(purchaseToken: string): Promise<void>`
Marks a consumable purchase as processed. Call only AFTER applying the in-game effect.

## Interfaces

```typescript
interface IPurchase {
    productID: string;
    purchaseToken: string;
    developerPayload: string;
}

interface ISign {
    signature: string; // "<signature>.<base64-encoded-JSON>"
}

interface IProduct {
    id: string;
    title: string;
    description: string;
    imageURI: string;
    price: string;              // "<price> <currency>"
    priceValue: string;         // "<price>"
    priceCurrencyCode: string;
    getPriceCurrencyImage(size: 'small' | 'medium' | 'svg'): string;
}
```

## Code Examples

```javascript
const ysdk = await YaGames.init();
try {
    const payments = await ysdk.getPayments();
} catch (err) {
    // Purchases unavailable
}
```

```javascript
const ysdk = await YaGames.init();
try {
    const payments = await ysdk.getPayments({ signed: true });
} catch (err) {
    // Purchases unavailable
}
```

```javascript
const ysdk = await YaGames.init();
try {
    const purchase = await ysdk.payments.purchase({ id: 'gold500' });
} catch (err) {
    // Purchase failed
}
```

```javascript
try {
    const purchase = await ysdk.payments.purchase({
        id: 'gold500',
        developerPayload: '{serverId:42}'
    });
} catch (err) {
    // Handle error
}
```

```javascript
const ysdk = await YaGames.init({ signed: true });
try {
    const purchase = await ysdk.payments.purchase({ id: 'gold500' });
    await serverPurchase(purchase.signature);
} catch (err) {
    // Purchase error
}
```

```javascript
const ysdk = await YaGames.init();
let SHOW_ADS = true;

try {
    const purchases = await ysdk.payments.getPurchases();
    if (purchases.some(p => p.productID === 'disable_ads')) {
        SHOW_ADS = false;
    }
} catch (err) {
    // Error: PAYMENT_FAILURE
}
```

```javascript
const ysdk = await YaGames.init({ signed: true });
try {
    const purchases = await ysdk.payments.getPurchases();
    const response = await fetch('https://your.game.server/handlePurchases', {
        method: 'POST',
        headers: { 'Content-Type': 'text/plain' },
        body: purchases.signature
    });
} catch (err) {
    // Error handling
}
```

```javascript
const ysdk = await YaGames.init();
let gameShop = [];

try {
    gameShop = await ysdk.payments.getCatalog();
} catch (err) {
    // Error fetching catalog
}
```

```javascript
const ysdk = await YaGames.init();

function addGold(value) {
    return ysdk.player.incrementStats({ gold: value });
}

try {
    const purchase = await ysdk.payments.purchase({ id: 'gold500' });
    await addGold(500);
    await ysdk.payments.consumePurchase(purchase.purchaseToken);
} catch (err) {
    // Error handling
}
```

```javascript
const ysdk = await YaGames.init();

async function handlePurchase(purchase) {
    if (purchase.productID === 'gold500') {
        await ysdk.player.incrementStats({ gold: 500 });
        await ysdk.payments.consumePurchase(purchase.purchaseToken);
    }
}

const purchases = await ysdk.payments.getPurchases();
for (let purchase of purchases) {
    await handlePurchase(purchase);
}
```

## Signature Format
`<signature>.<base64-encoded-JSON>`

Decoded payload structure:
```json
{
  "algorithm": "HMAC-SHA256",
  "issuedAt": 1571233371,
  "requestPayload": "qwe",
  "data": {
    "token": "d85ae0b1-9166-4fbb-bb38-6d2a4ca4416d",
    "status": "waiting",
    "errorCode": "",
    "errorDescription": "",
    "url": "https://yandex.ru/games/sdk/payments/trust-fake.html",
    "product": {
      "id": "noads",
      "title": "Без рекламы",
      "description": "Отключить рекламу в игре",
      "price": { "code": "YAN", "value": "49" },
      "imagePrefix": "https://avatars.mds.yandex.net/..."
    },
    "developerPayload": "TEST DEVELOPER PAYLOAD"
  }
}
```

### Python 3 verification
```python
import hashlib, hmac, base64, json

usedTokens = {}
key = 't0p$ecret'  # Keep secret
secret = bytes(key, 'utf-8')
signature = 'hQ8adIRJWD29Nep+0P36Z6edI5uzj6F3tddz6Dqgclk=.eyJhbGdvcml0aG0i...'

sign, data = signature.split('.')
message = base64.b64decode(data)
purchaseData = json.loads(message)
result = base64.b64encode(hmac.new(secret, message, digestmod=hashlib.sha256).digest())

if result.decode('utf-8') == sign:
    print('Signature check ok!')
    if not purchaseData['data']['token'] in usedTokens:
        usedTokens[purchaseData['data']['token']] = True
        print('Double spend check ok!')
        print('Apply purchase:', purchaseData['data']['product'])
```

### Node.js verification
```javascript
const crypto = require('crypto');

const usedTokens = {};
const key = 't0p$ecret';
const signature = 'hQ8adIRJWD29Nep+0P36Z6edI5uzj6F3tddz6Dqgclk=.eyJhbGdvcml0aG0i...';

const [sign, data] = signature.split('.');
const purchaseDataString = Buffer.from(data, 'base64').toString('utf8');
const hmac = crypto.createHmac('sha256', key);
hmac.update(purchaseDataString);
const purchaseData = JSON.parse(purchaseDataString);

if (sign === hmac.digest('base64')) {
    console.log('Signature check ok!');
    if (!usedTokens[purchaseData.data.token]) {
        usedTokens[purchaseData.data.token] = true;
        console.log('Double spend check ok!');
        console.log('Apply purchase:', purchaseData.data.product);
    }
}
```

## Constants
- Currency image sizes: `'small'`, `'medium'`, `'svg'`.
- Error class observed: `PAYMENT_FAILURE`.

## Pitfalls / Notes
- Always call `getPurchases()` at startup and consume unprocessed purchases — internet drops can leave items unprocessed.
- Call `consumePurchase()` AFTER granting the in-game item, never before.
- Use `signed: true` in production to prevent client-side tampering.
- Test purchases will only pass moderation if `consumePurchase()` is implemented.
- Store used tokens server-side to prevent double-spend replays.

## Source
https://yandex.ru/dev/games/doc/ru/sdk/sdk-purchases
