mergeInto(LibraryManager.library, {
    // Unity's own focus callbacks are not enough on WebGL: mobile browsers minimize the page
    // (app switcher, home button, screen lock) without ever blurring the canvas, so the game
    // keeps running and sounding in the background. Yandex moderation tests exactly that
    // (requirement 1.3), hence this DOM-level bridge on top of OnApplicationFocus.
    JumpRing_RegisterPageVisibility: function (targetPtr) {
        // A scene reload re-runs the registration with a fresh handler object. Only the target
        // name is refreshed then — re-adding the listeners would leak one set per restart.
        var target = UTF8ToString(targetPtr);
        if (window.jumpRingPageVisibility) {
            window.jumpRingPageVisibility.target = target;
            return;
        }

        var state = { target: target };
        window.jumpRingPageVisibility = state;

        var notify = function (visible) {
            SendMessage(state.target, visible ? 'OnPageVisible' : 'OnPageHidden');
        };

        document.addEventListener('visibilitychange', function () {
            notify(document.visibilityState === 'visible');
        });

        // pagehide covers what visibilitychange misses: bfcache navigation on iOS and a browser
        // app killed while backgrounded. pageshow is its counterpart on restore.
        window.addEventListener('pagehide', function () {
            notify(false);
        });

        window.addEventListener('pageshow', function () {
            notify(true);
        });
    }
});
