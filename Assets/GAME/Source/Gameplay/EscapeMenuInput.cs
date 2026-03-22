using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace JumpRing.Game.Gameplay
{
    public sealed class EscapeMenuInput : MonoBehaviour
    {
        [SerializeField]
        private RunSessionController runSessionController;

        private void Update()
        {
            if (!WasEscapePressed())
            {
                return;
            }

            runSessionController.ToggleMainMenu();
        }

        private static bool WasEscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape);
#else
            return false;
#endif
        }
    }
}
