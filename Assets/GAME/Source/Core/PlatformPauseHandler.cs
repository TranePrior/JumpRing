using System.Runtime.InteropServices;
using UnityEngine;

namespace JumpRing.Game.Core
{
    /// <summary>
    /// Holds <see cref="PauseReason.Platform"/> while Yandex asks the game to stop, and releases it
    /// when the platform resumes. Owns only that reason and never touches timeScale directly.
    /// </summary>
    /// <remarks>
    /// The stop button in the SDK panel does not hide the page, so neither Unity's focus callbacks
    /// nor the visibilitychange bridge in <see cref="WebGLFocusHandler"/> ever see it — pressing it
    /// left the game running and sounding behind the panel. The platform sends the same pair of
    /// events around store windows and fullscreen ads, which is why the reason is separate: a focus
    /// event trailing an ad must not resume a run the platform still holds frozen.
    /// </remarks>
    public sealed class PlatformPauseHandler : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void JumpRing_RegisterPlatformPause(string targetObjectName);
#endif

        private void Start()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            JumpRing_RegisterPlatformPause(gameObject.name);
#endif
        }

        private void OnDisable()
        {
            // A scene reload destroys the receiver; a platform pause left behind would freeze the
            // fresh scene with nothing alive to release it.
            PauseService.Remove(PauseReason.Platform);
        }

        /// <summary>Called from the browser by PlatformPause.jslib on `game_api_pause`.</summary>
        public void OnPlatformPause() => PauseService.Add(PauseReason.Platform);

        /// <summary>Called from the browser by PlatformPause.jslib on `game_api_resume`.</summary>
        public void OnPlatformResume() => PauseService.Remove(PauseReason.Platform);
    }
}
