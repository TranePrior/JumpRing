mergeInto(LibraryManager.library, {
    // Unity does not populate Screen.safeArea on WebGL — the browser exposes device insets to CSS
    // only. The probe element resolves env(safe-area-inset-*) against the page, and the result is
    // intersected with the canvas rect, because the canvas is letterboxed and does not necessarily
    // touch the edges the insets are measured from. Values are written back in Unity screen pixels.
    RetroCat_GetSafeAreaInsets: function (insetsPtr) {
        var probe = document.getElementById('retrocat-safe-area-probe');
        if (!probe) {
            probe = document.createElement('div');
            probe.id = 'retrocat-safe-area-probe';
            probe.style.cssText =
                'position:fixed;top:0;left:0;width:0;height:0;visibility:hidden;pointer-events:none;' +
                'padding-top:env(safe-area-inset-top);padding-right:env(safe-area-inset-right);' +
                'padding-bottom:env(safe-area-inset-bottom);padding-left:env(safe-area-inset-left);';
            document.body.appendChild(probe);
        }

        var style = window.getComputedStyle(probe);
        var insetLeft = parseFloat(style.paddingLeft) || 0;
        var insetTop = parseFloat(style.paddingTop) || 0;
        var insetRight = parseFloat(style.paddingRight) || 0;
        var insetBottom = parseFloat(style.paddingBottom) || 0;

        var canvas = document.querySelector('#unity-canvas');
        var rect = canvas.getBoundingClientRect();
        var scale = canvas.width / rect.width;

        var left = Math.max(0, insetLeft - rect.left) * scale;
        var top = Math.max(0, insetTop - rect.top) * scale;
        var right = Math.max(0, rect.right - (window.innerWidth - insetRight)) * scale;
        var bottom = Math.max(0, rect.bottom - (window.innerHeight - insetBottom)) * scale;

        var index = insetsPtr >> 2;
        HEAPF32[index] = left;
        HEAPF32[index + 1] = top;
        HEAPF32[index + 2] = right;
        HEAPF32[index + 3] = bottom;
    }
});
