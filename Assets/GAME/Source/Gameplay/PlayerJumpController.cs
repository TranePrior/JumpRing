using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace JumpRing.Game.Gameplay
{
    public sealed class PlayerJumpController : MonoBehaviour
    {
        [SerializeField]
        private Rigidbody2D playerRigidbody;

        [SerializeField, Min(0.1f)]
        private float jumpImpulse = 8f;

        [SerializeField]
        private RunSessionController runSessionController;

        private void Update()
        {
            if (!WasJumpPressed())
            {
                return;
            }

            if (!runSessionController.CanControlPlayer)
            {
                if (!runSessionController.CanStartRun)
                {
                    return;
                }

                runSessionController.StartRun();
            }

            var velocity = playerRigidbody.linearVelocity;
            velocity.y = 0f;
            playerRigidbody.linearVelocity = velocity;
            playerRigidbody.AddForce(Vector2.up * jumpImpulse, ForceMode2D.Impulse);
        }

        private static bool WasJumpPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                return true;
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                return true;
            }

            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);
#else
            return false;
#endif
        }
    }
}
