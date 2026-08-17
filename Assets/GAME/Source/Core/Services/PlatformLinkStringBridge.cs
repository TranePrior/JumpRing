#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using UnityEngine;

namespace JumpRing.Game.Core.Services
{
    /// <summary>
    /// Keeps PlatformLink's jslib string marshalling alive in WebGL builds.
    /// <para>
    /// Every string PlatformLink hands back to C# — the player's language, the app id, the device
    /// type, local storage reads — goes through one jslib helper that the build used to strip,
    /// because nothing referenced it. The extern below is that reference: it makes Emscripten emit
    /// the helper (see PlatformLinkStrings.jslib), and calling it on boot turns a future regression
    /// into a console error instead of a game that silently ignores the platform locale.
    /// </para>
    /// </summary>
    public static class PlatformLinkStringBridge
    {
        private const string ExpectedProbeResult = "ok";

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern string JumpRing_VerifyPlatformLinkStrings();
#endif

        /// <summary>
        /// Call before <c>PLink.Initialize</c>, while the loading screen can still report a failure.
        /// </summary>
        public static void Verify()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            string probe = JumpRing_VerifyPlatformLinkStrings();

            if (probe != ExpectedProbeResult)
            {
                Debug.LogError(
                    $"[PlatformLinkStringBridge] jslib string marshalling is broken (probe returned '{probe}'). " +
                    "PLink.Environment.Language, AppId and DeviceType will fail, and the game cannot detect the player's language.");
            }
#endif
        }
    }
}
