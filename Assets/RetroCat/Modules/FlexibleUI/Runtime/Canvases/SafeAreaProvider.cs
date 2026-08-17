using System.Runtime.InteropServices;
using UnityEngine;

namespace RetroCat.Modules.FlexibleUI.Runtime.Canvases
{
    /// <summary>
    /// Device insets (notch, dynamic island, home indicator, rounded corners) in screen pixels,
    /// ordered left, top, right, bottom.
    ///
    /// Unity leaves <see cref="Screen.safeArea"/> equal to the full screen on WebGL, so the values
    /// come from the page through SafeArea.jslib, already intersected with the canvas rect.
    /// </summary>
    public static class SafeAreaProvider
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void RetroCat_GetSafeAreaInsets(float[] insets);

        private static readonly float[] InsetsBuffer = new float[4];
#endif

        public static Vector4 GetInsets()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            RetroCat_GetSafeAreaInsets(InsetsBuffer);
            return new Vector4(InsetsBuffer[0], InsetsBuffer[1], InsetsBuffer[2], InsetsBuffer[3]);
#else
            var safeArea = Screen.safeArea;
            return new Vector4(
                safeArea.xMin,
                Screen.height - safeArea.yMax,
                Screen.width - safeArea.xMax,
                safeArea.yMin);
#endif
        }
    }
}
