using UnityEngine;

namespace JumpRing.Game.UI
{
    public sealed class PopupTracker : MonoBehaviour
    {
        private static int activeCount;

        public static bool IsAnyPopupActive => activeCount > 0;

        // The counter is static and survives domain-reload-off play sessions. A leftover positive
        // value would report a popup as open forever and block gameplay input for the session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            activeCount = 0;
        }

        private void OnEnable()
        {
            activeCount++;
        }

        private void OnDisable()
        {
            activeCount--;
        }
    }
}
