# Section 4 — Advertising requirements

Source: <https://yandex.ru/dev/games/doc/ru/concepts/requirements>

## 4.1 No third-party ads
> Отсутствует реклама от сторонних рекламодателей, в том числе статичные рекламные изображения и текст.

## 4.2 Resume after ad
> Внутренний процесс сохраняется после перехода по рекламе и возврата обратно.

## 4.3 Ad orientation
> Все рекламные блоки имеют ориентацию, соответствующую ориентации игры.

## 4.4 Ad placement
> Реклама не мешает взаимодействовать с игрой и показывается только в логических паузах.

Extra rules from the deep dive page:
- Maximum allowed delay between user action and ad start: **0.33 s**.
- Покупка отключения рекламы должна реально убирать показы.
- Pre-ad warning must last **2 seconds**.
- During the warning and the ad itself the game must be paused (animation may keep playing, but input/state must not advance).

Deep dive: [`../topics/ad-placement.md`](../topics/ad-placement.md)

## 4.5 Rewarded video
> При наличии механики рекламы за вознаграждение пользователь может по желанию просмотреть rewarded video и получить вознаграждение.

### 4.5.1
> Кнопка вызова рекламы за вознаграждение привязана к тексту или кнопке, которые однозначно отображают, что пользователь посмотрит рекламу.

### 4.5.2
> Награда за просмотр RV представляет собой дополнительный бонус к основной игре и не должна влиять на возможность продолжить игровой процесс.

## 4.6 Additional ad blocks
- **4.6.1** Дополнительными рекламными блоками считаются исключительно стики-баннеры.
- **4.6.2** Использование кастомных RTB-баннеров запрещено.

## 4.7 Pause on full-screen ad
> При показе полноэкранной рекламы (interstitial или rewarded video) звук в игре и игровой процесс должны ставиться на паузу.
