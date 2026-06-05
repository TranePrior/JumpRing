using UnityEngine;

namespace JumpRing.Game.Core
{
    public sealed class WebGLFocusHandler : MonoBehaviour
    {
        public static bool IsAdActive { get; set; }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                Time.timeScale = 0f;
                AudioListener.pause = true;
                return;
            }

            if (IsAdActive)
            {
                return;
            }

            Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }
}
