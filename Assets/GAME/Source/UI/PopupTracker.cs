using JumpRing.Game.Core;
using UnityEngine;

namespace JumpRing.Game.UI
{
    /// <summary>
    /// Counts the modal windows that are currently on screen and holds the game frozen while any
    /// of them is open.
    /// </summary>
    /// <remarks>
    /// The counter used to be a pure input gate: taps were refused while a window was up, but the
    /// world behind it kept running — the line kept generating, the spawners kept spawning and a
    /// run in progress kept falling. Opening the settings and flipping a toggle was enough to die
    /// behind the window. The pause is taken on the first window and released by the last one, so
    /// stacked windows can't resume the game out from under each other.
    /// </remarks>
    public sealed class PopupTracker : MonoBehaviour
    {
        private static int activeCount;

        public static bool IsAnyPopupActive => activeCount > 0;

        // The counter is static and survives domain-reload-off play sessions. A leftover positive
        // value would report a popup as open forever and block gameplay input for the session, and
        // the pause it stands for would strand the game at timeScale 0.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            activeCount = 0;
            PauseService.Remove(PauseReason.Popup);
        }

        private void OnEnable()
        {
            activeCount++;

            if (activeCount == 1)
            {
                PauseService.Add(PauseReason.Popup);
            }
        }

        private void OnDisable()
        {
            activeCount--;

            if (activeCount == 0)
            {
                PauseService.Remove(PauseReason.Popup);
            }
        }
    }
}
