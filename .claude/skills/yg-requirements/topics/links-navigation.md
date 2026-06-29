# Topic — Links & navigation (8.4.x)

Source: <https://yandex.ru/dev/games/doc/ru/requirements/8/4/1>

## Rules
- **8.4.1** Links embedded via SDK and pointing to your games in the Yandex Games catalog.
- **8.4.2** No links to external resources: developer website, app stores, other-platform games, partner sites, file downloads.
- **8.4.3** Community links: only if (a) community covers this developer's Yandex Games titles, (b) "Yandex Games" is in the community name, (c) the community itself posts no external links.
- **8.4.4** No auto-redirects to external sites; no recommendations to visit external resources.

## What to verify in code
1. Grep `window.open`, `<a href="http`, `target="_blank"`, `location.href = `, `location.assign`.
2. Every external URL must either:
   - Point to a Yandex Games catalog page and go through the SDK link method.
   - Be removed.
3. No "Visit our website", "Download on App Store", "Play on Steam" buttons.
4. No socials in the game UI other than the limited community pattern in 8.4.3.
5. Confirm no automatic external redirect on launch (some analytics SDKs do this).
6. Embedded videos must not let the player click out to YouTube/Vimeo (cross-ref 3.9).

## Common failures
- "Follow us on Twitter" in the credits panel.
- Telegram / Discord invite link in settings.
- Direct YouTube embed with default controls — clickable channel link.
