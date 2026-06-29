mergeInto(LibraryManager.library, {
    JumpRing_GameplayApiStart: function () {
        if (typeof gameplayApiStart === 'function') {
            gameplayApiStart();
        }
    },

    JumpRing_GameplayApiStop: function () {
        if (typeof gameplayApiStop === 'function') {
            gameplayApiStop();
        }
    }
});
