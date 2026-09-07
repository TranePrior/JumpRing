mergeInto(LibraryManager.library, {
    // Bridge for the platform's own pause: the stop button in the Yandex SDK panel, a store
    // window, or the pause the SDK takes around a fullscreen ad. None of those hide the page, so
    // PageVisibility.jslib never fires for them and the game kept running underneath.
    // plugin.js subscribes to game_api_pause/game_api_resume and calls notify() below; the
    // dispatch has to live here because SendMessage only exists inside the Unity module scope.
    JumpRing_RegisterPlatformPause: function (targetPtr) {
        var target = UTF8ToString(targetPtr);
        var bridge = window.jumpRingPlatformPause;

        // A scene reload re-runs the registration with a fresh handler object. Only the target
        // name is refreshed then, so plugin.js keeps talking to the same bridge object.
        if (bridge) {
            bridge.target = target;
        } else {
            bridge = {
                target: target,
                notify: function (paused) {
                    SendMessage(this.target, paused ? 'OnPlatformPause' : 'OnPlatformResume');
                }
            };
            window.jumpRingPlatformPause = bridge;
        }

        // The platform can pause the game before the first scene registers its handler (it shows
        // a fullscreen ad on startup). Replay that pause so the game does not run under it.
        if (window.jumpRingPlatformPausePending === true) {
            window.jumpRingPlatformPausePending = false;
            bridge.notify(true);
        }
    }
});
