# Topic — Interface in media (8.3.4)

Source: <https://yandex.ru/dev/games/doc/ru/requirements/8/3/4>

## Rule
> Не содержат элементов системного интерфейса и пользовательского интерфейса Яндекс Игр.

Promo media (icons, covers, ad creatives) must not show OS chrome (browser address bar, mobile status bar) or Yandex Games platform UI (catalog wrapper, top bar, "play" button).

In-game UI on **gameplay screenshots** is allowed and expected.

## Cannot check from code
Manual review of every uploaded asset:

1. Icon — no system chrome, no Yandex Games platform UI.
2. Cover — same.
3. Promo video — same; gameplay UI inside the game is fine.
4. No browser scrollbars / mock browser frames around the screenshot.
