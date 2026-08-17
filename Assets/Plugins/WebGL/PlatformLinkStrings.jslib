// PlatformLink's own jslib returns strings to C# through a shared helper, `jslib_convertString`.
// That helper is never referenced from C# (PLink declares the extern but never calls it), and its
// library entry declares no dependency on itself, so Emscripten strips the definition while keeping
// the call sites in jslib_getLanguage / jslib_getAppId / jslib_getDeviceType /
// jslib_loadFromLocalStorage. Every one of those then throws "ReferenceError: _jslib_convertString
// is not defined" in a real build — which is why the game could never read the player's language
// from the Yandex SDK and shipped Russian to English players.
//
// Declaring the dependency here pulls the helper back into the build for the whole PlatformLink
// library. The verify entry point exists so a regression fails loudly on boot instead of silently
// falling back to the browser locale. The package itself stays untouched.
mergeInto(LibraryManager.library, {
    JumpRing_VerifyPlatformLinkStrings__deps: ['jslib_convertString'],
    JumpRing_VerifyPlatformLinkStrings: function () {
        return _jslib_convertString('ok');
    }
});
