---
title_ru: Данные игрока
title_en: Player Data API
source: https://yandex.ru/dev/games/doc/ru/sdk/sdk-player
---

## Purpose
Identify the player, persist per-player save data and numeric stats, and access profile attributes (name, avatar, paying status).

## API Surface

### `ysdk.getPlayer(options?): Promise<Player>`
Initializes the `Player` object. Provides user ID for all users, avatar/name for authorized users, purchase data for RF players in games with IAP enabled.
- `options.signed?: true` — request a signed payload (`<signature>.<profile-data>`) for server verification.
- Rate limit: 20 requests per 5 minutes.

### `player.isAuthorized(): boolean`
Returns whether the user is logged in.

### `ysdk.auth.openAuthDialog(): Promise<void>`
Opens the Yandex authorization dialog.

### `player.setData(data: object, flush?: boolean): Promise<void>`
Persists key-value save data (200 KB limit per player).
- `flush=false` (default) — request is queued.
- `flush=true` — sent immediately.
- Rate limit: 100 per 5 minutes.

### `player.getData(keys?: string[]): Promise<object>`
Retrieves save data. Omit `keys` for all data. Rate limit: 100 per 5 minutes.

### `player.setStats(stats: object): Promise<void>`
Stores numeric stats (10 KB limit). Rate limit: 60 per minute.

### `player.getStats(keys?: string[]): Promise<object>`
Reads numeric stats. Rate limit: 60 per minute.

### `player.incrementStats(increments: object): Promise<object>`
Atomically modifies numeric values, returns the resulting changed/added pairs. Rate limit: 60 per minute.

### `player.getUniqueID(): string`
Permanent unique user identifier.

### `player.getName(): string`
Username (empty for anonymous).

### `player.getPhoto(size: 'small' | 'medium' | 'large'): string`
Avatar URL at requested size.

### `player.getPayingStatus(): EPayingStatus`
Returns one of:
- `'paying'` — spent >500 RUB in last month
- `'partially_paying'` — at least one purchase in last year
- `'not_paying'` — no real-money purchases in last year
- `'unknown'` — non-RF user or data sharing denied

### `player.getIDsPerGame(): Promise<Array<{ appID: number, userID: string }>>`
Returns this user's IDs across all of the developer's games (authorized users only). Gate with `ysdk.isAvailableMethod('player.getIDsPerGame')`.

### `player.signature: string`
Signed payload, present when `getPlayer({ signed: true })` is used.

## Code Examples

```javascript
const ysdk = await YaGames.init();
const player = await ysdk.getPlayer();
```

```javascript
const player = await ysdk.getPlayer({ signed: true });
const authData = await fetch('https://your.game.server/auth', {
    method: 'POST',
    headers: { 'Content-Type': 'text/plain' },
    body: player.signature
});
```

```javascript
await player.setData({ achievements: ['trophy1', 'trophy2', 'trophy3'] });
```

```javascript
const payingStatus = player.getPayingStatus();
if (payingStatus === 'paying' || payingStatus === 'partially_paying') {
    // Offer in-app item
}
```

## Constants
Paying status string values: `'paying'`, `'partially_paying'`, `'not_paying'`, `'unknown'`.
Photo sizes: `'small'`, `'medium'`, `'large'`.

## Pitfalls / Notes
- Anonymous users have no name/avatar; check `isAuthorized()` before reading profile fields.
- Signature is two base64 strings joined by a period: `<signature>.<profile-data>`.
- Respect rate limits — batch writes via the default queued `setData`.
- Data visibility for cross-game IDs depends on the user's profile privacy settings.

## Source
https://yandex.ru/dev/games/doc/ru/sdk/sdk-player
