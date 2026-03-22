using UnityEngine;

namespace JumpRing.Game.Gameplay
{
    public sealed class PlayerForwardMover : MonoBehaviour
    {
        [SerializeField]
        private Rigidbody2D playerRigidbody;

        [SerializeField]
        private RunSessionController runSessionController;

        [SerializeField, Min(0.1f)]
        private float forwardSpeed = 4f;

        private void FixedUpdate()
        {
            var velocity = playerRigidbody.linearVelocity;
            velocity.x = runSessionController.CanControlPlayer ? forwardSpeed : 0f;
            playerRigidbody.linearVelocity = velocity;
        }
    }
}
