using UnityEngine;

namespace JumpRing.Game.Core
{
    /// <summary>
    /// Pauses the game while the WebGL page loses focus. Owns only the
    /// <see cref="PauseReason.FocusLost"/> reason and never touches timeScale directly.
    /// </summary>
    public sealed class WebGLFocusHandler : MonoBehaviour
    {
        // After an ad closes the browser emits a trailing focus/blur burst around the WebGL canvas.
        // Ignoring focus events for a short window after the ad prevents that burst from stranding
        // the game under a leftover FocusLost pause (the interstitial "tap counts, ring won't jump" bug).
        private const float AdSettleSeconds = 0.5f;

        private bool adWasActive;
        private float ignoreFocusUntil;

        private void OnEnable()
        {
            PauseService.ReasonsChanged += OnReasonsChanged;
        }

        private void OnDisable()
        {
            PauseService.ReasonsChanged -= OnReasonsChanged;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            // In the editor these callbacks track the Unity window's focus, not the browser tab.
            // Entering Play mode fires OnApplicationFocus(false) (focus is on the Play button, not the
            // Game view), which would strand the game under a FocusLost pause. Only the real WebGL
            // build should react to focus; tests drive HandleFocus directly.
#if !UNITY_EDITOR
            HandleFocus(hasFocus);
#endif
        }

        private void OnApplicationPause(bool isPaused)
        {
#if !UNITY_EDITOR
            HandleFocus(!isPaused);
#endif
        }

        private void OnReasonsChanged()
        {
            var adActive = PauseService.HasReason(PauseReason.Ad);
            if (adWasActive && !adActive)
            {
                ignoreFocusUntil = Time.realtimeSinceStartup + AdSettleSeconds;
            }

            adWasActive = adActive;
        }

        private void HandleFocus(bool hasFocus)
        {
            // Ads own the pause while shown, and their trailing focus noise is meaningless —
            // don't let it add a FocusLost reason that outlives the ad.
            if (PauseService.HasReason(PauseReason.Ad) || Time.realtimeSinceStartup < ignoreFocusUntil)
            {
                return;
            }

            if (hasFocus)
            {
                PauseService.Remove(PauseReason.FocusLost);
            }
            else
            {
                PauseService.Add(PauseReason.FocusLost);
            }
        }
    }
}
