---
title_ru: Таблицы лидеров
title_en: Leaderboards
source: https://yandex.ru/dev/games/doc/ru/sdk/sdk-leaderboard
---

## Purpose
Publish per-player scores to named leaderboards configured in the developer console; read top entries and the current player's rank.

## API Surface
Current entry point: `ysdk.leaderboards.*`. The older `ysdk.getLeaderboards()` accessor is deprecated.

### `ysdk.leaderboards.getDescription(leaderboardName: string): Promise<ILeaderboardDescription>`
Returns metadata for a leaderboard.

```typescript
interface ILeaderboardDescription {
    appID: string;
    default: boolean;
    description: {
        invert_sort_order: boolean;
        score_format: {
            options: { decimal_offset: number };
            type: 'numeric' | 'time';
        };
        sort_order: string; // 'DESC' | 'ASC'
    };
    name: string;
    title: Record<Locale, string>;
}
```

### `ysdk.leaderboards.setScore(leaderboardName: string, score: number, extraData?: string): Promise<void>`
Requires authentication. `score` must be non-negative; for `type: 'time'` it is milliseconds. `extraData` is an optional free-form string. Rate limit: 1 request/second. Check availability via `ysdk.isAvailableMethod('leaderboards.setScore')`.

### `ysdk.leaderboards.getPlayerEntry(leaderboardName: string): Promise<ILeaderboardEntry>`
Requires authentication. Throws with `code === 'LEADERBOARD_PLAYER_NOT_PRESENT'` if the player has no entry. Rate limit: 60 per 5 minutes.

```typescript
interface ILeaderboardEntry {
    extraData: string;
    rank: number;
    score: number;
    player: {
        publicName: string;
        uniqueID: string;
        getAvatarSrc: (size?: 'small' | 'medium' | 'large') => string;
        getAvatarSrcSet: (size?: 'small' | 'medium' | 'large') => string;
    };
}
```

### `ysdk.leaderboards.getEntries(leaderboardName: string, options): Promise<ILeaderboardEntries>`
Authentication optional. Rate limit: 20 per 5 minutes.

```typescript
options: {
    includeUser?: boolean;     // default false
    quantityAround?: number;   // 1–10, default 5
    quantityTop?: number;      // 1–20, default 5
}

interface ILeaderboardEntries {
    leaderboard: ILeaderboardDescription;
    ranges: Array<{ start: number; size: number }>;
    userRank: number; // 0 if not present
    entries: ILeaderboardEntry[];
}
```

## Constants
- Score format types: `'numeric'`, `'time'`.
- Sort orders: `'DESC'`, `'ASC'`.
- Error code: `'LEADERBOARD_PLAYER_NOT_PRESENT'`.

## Code Examples

```javascript
const ysdk = await YaGames.init();
const lb = await ysdk.leaderboards.getDescription('leaderboard2021');
console.log(lb);
```

```javascript
await ysdk.isAvailableMethod('leaderboards.setScore');
```

```javascript
await ysdk.leaderboards.setScore('leaderboard2021', 120);
await ysdk.leaderboards.setScore('leaderboard2021', 120, 'My favourite player!');
```

```javascript
try {
    const res = await ysdk.leaderboards.getPlayerEntry('leaderboard2021');
    console.log(res);
} catch (err) {
    if (err.code === 'LEADERBOARD_PLAYER_NOT_PRESENT') {
        // Player has no entry in leaderboard
    }
}
```

```javascript
const entries = await ysdk.leaderboards.getEntries('leaderboard2021', {
    quantityTop: 10,
    includeUser: true,
    quantityAround: 3
});
console.log(entries);
```

## Pitfalls / Notes
- 404: the technical name must match the one configured in the console.
- "Object already exists": leaderboard names cannot be reused after deletion.
- "Пользователь скрыт": player has hidden avatar/name — `publicName` / avatar may be empty.
- Unauthenticated players' scores are not persisted; for full coverage maintain a custom server-side leaderboard.

## Source
https://yandex.ru/dev/games/doc/ru/sdk/sdk-leaderboard
