using UnityEngine;

namespace JumpRing.Game.Core
{
    public sealed class WebGLFocusHandler : MonoBehaviour
    {
        public static bool IsAdActive { get; set; }

        private bool focusPaused;
        private float scaleBeforeFocusLoss = 1f;

        private void OnApplicationFocus(bool hasFocus)
        {
            SetPaused(!hasFocus);
        }

        private void OnApplicationPause(bool isPaused)
        {
            SetPaused(isPaused);
        }

        private void SetPaused(bool paused)
        {
            // Ads manage their own timeScale/audio while shown — stay out of their way.
            if (IsAdActive)
            {
                return;
            }

            // Ignore duplicate focus events (focus + pause can both fire on the same blur).
            if (paused == focusPaused)
            {
                return;
            }

            focusPaused = paused;

            if (paused)
            {
                // Remember whatever the game's own timeScale was (0 if the game is
                // intentionally paused on a death/second-chance dialog, 1 while playing)
                // so regaining focus restores that exact state instead of force-resuming.
                scaleBeforeFocusLoss = Time.timeScale;
                Time.timeScale = 0f;
                AudioListener.pause = true;
            }
            else
            {
                Time.timeScale = scaleBeforeFocusLoss;
                AudioListener.pause = false;
            }
        }
    }
}
