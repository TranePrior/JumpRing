using UnityEngine;

namespace JumpRing.Game.UI
{
    public sealed class PopupTracker : MonoBehaviour
    {
        private static int activeCount;

        public static bool IsAnyPopupActive => activeCount > 0;

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
