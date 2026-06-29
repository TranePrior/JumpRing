---
title_ru: Примеры подключения SDK
title_en: SDK Integration Examples
source: https://yandex.ru/dev/games/doc/ru/sdk/sdk-example
---

## Purpose
Two end-to-end HTML examples demonstrating synchronous and asynchronous SDK loading with a fullscreen ad on button click.

## Synchronous Connection Example

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1, maximum-scale=1" />
    <meta name="mobile-web-app-capable" content="yes">
    <meta name="apple-mobile-web-app-capable" content="yes">
    <title>Пример страницы с синхронным подключением SDK</title>
    <script src="/sdk.js"></script>
    <script>
        YaGames.init().then(ysdk => {
            ysdk.adv.showFullscreenAdv();

            const buttonElem = document.querySelector('#button');

            let commonCounter = 0;
            buttonElem.addEventListener('click', () => {
                let counter = 0;

                function getCallback(callbackName) {
                    return () => {
                        counter += 1;
                        commonCounter += 1;

                        console.log(`showFullscreenAdv; callback ${callbackName}; ${counter} call`);
                    }
                }

                ysdk.adv.showFullscreenAdv({
                    callbacks: {
                        onClose: getCallback('onClose'),
                        onOpen: getCallback('onOpen'),
                        onError: getCallback('onError')
                    }
                });
            });
        });
    </script>
</head>
<body>
    <button id="button">Показать рекламу</button>
</body>
</html>
```

## Asynchronous Connection Example

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1, maximum-scale=1" />
    <meta name="mobile-web-app-capable" content="yes">
    <meta name="apple-mobile-web-app-capable" content="yes">
    <title>Пример страницы с асинхронным подключением SDK</title>
    <script>
        let ysdk;

        function initSDK() {
            YaGames
                .init()
                .then(ysdk_ => {
                    ysdk = ysdk_;
                    ysdk.adv.showFullscreenAdv({
                        callbacks: {
                            onClose: wasShown => {
                                console.info('First close')
                            }
                        }
                    });
                })
        }

        document.addEventListener('DOMContentLoaded', () => {
            const buttonElem = document.querySelector('#button');

            let commonCounter = 0;
            buttonElem.addEventListener('click', () => {
                let counter = 0;

                function getCallback(callbackName) {
                    return () => {
                        counter += 1;
                        commonCounter += 1;

                        if (commonCounter % 3 === 0) {
                            throw new Error(`Test error in ${callbackName}, everything okey, it should not abort other code execution`);
                        }

                        console.info(`showFullscreenAdv; callback ${callbackName}; ${counter} call`);
                    }
                }

                function makeSomethingImportant() {
                    console.info('It\'s very important \'console.info\'');
                }

                if (ysdk) {
                    ysdk.adv.showFullscreenAdv({
                        callbacks: {
                            onClose: makeSomethingImportant,
                            onOpen: getCallback('onOpen'),
                            onError: function(error) {
                                console.error(error);
                            }
                        }
                    });
                } else {
                    makeSomethingImportant();
                }
            });
        });
    </script>
</head>
<body>
<!-- Yandex Games SDK -->
<script>
    (function(d) {
        var t = d.getElementsByTagName('script')[0];
        var s = d.createElement('script');
        s.src = '/sdk.js';
        s.async = true;
        t.parentNode.insertBefore(s, t);
        s.onload = initSDK;
    })(document);
</script>
<button id="button">Показать рекламу</button>
</body>
</html>
```

## Pitfalls / Notes
- Errors thrown inside ad callbacks must not abort the rest of the game flow — wrap risky logic.
- In async mode, button clicks before `ysdk` resolves must still run any business-critical fallback path (the example calls `makeSomethingImportant()`).

## Source
https://yandex.ru/dev/games/doc/ru/sdk/sdk-example
