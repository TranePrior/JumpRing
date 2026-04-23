using UnityEngine;

namespace JumpRing.Game.Gameplay
{
    public sealed class PlayerForwardMover : MonoBehaviour
    {
        [SerializeField]
        private Rigidbody2D playerRigidbody;

        [SerializeField]
        private RunSessionController runSessionController;

        [SerializeField]
        private DifficultyManager difficultyManager;

        [Header("Speed")]
        [SerializeField, Min(0.1f)]
        private float baseSpeed = 3f;

        [SerializeField, Min(0.01f)]
        private float speedPerStep = 0.4f;

        [SerializeField, Min(1)]
        private int scorePerSpeedStep = 30;

        [SerializeField, Min(0.1f)]
        private float maxSpeed = 6f;

        [SerializeField, Min(0.1f)]
        private float speedSmoothTime = 0.5f;

        private float targetSpeed;
        private float smoothVelocity;

        public float CurrentSpeed { get; private set; }

        private void FixedUpdate()
        {
            if (!runSessionController.CanControlPlayer)
            {
                var velocity = playerRigidbody.linearVelocity;
                velocity.x = 0f;
                playerRigidbody.linearVelocity = velocity;
                CurrentSpeed = baseSpeed;
                targetSpeed = baseSpeed;
                smoothVelocity = 0f;
                return;
            }

            var score = difficultyManager != null ? difficultyManager.CurrentScore : 0;
            var steps = score / scorePerSpeedStep;
            targetSpeed = Mathf.Min(baseSpeed + steps * speedPerStep, maxSpeed);

            CurrentSpeed = Mathf.SmoothDamp(CurrentSpeed, targetSpeed, ref smoothVelocity, speedSmoothTime);

            var vel = playerRigidbody.linearVelocity;
            vel.x = CurrentSpeed;
            playerRigidbody.linearVelocity = vel;
        }
    }
}
