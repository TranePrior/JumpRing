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

        [SerializeField, Min(0)]
        private int speedGrowthStartScore = 120;

        [SerializeField, Min(0.01f)]
        private float speedPerStep = 0.1f;

        [SerializeField, Min(1)]
        private int speedGrowthInterval = 10;

        [SerializeField, Min(0.1f)]
        private float maxSpeedBonus = 1.6f;

        [SerializeField, Min(0.1f)]
        private float speedSmoothTime = 0.5f;

        private float targetSpeed;
        private float smoothVelocity;

        public float CurrentSpeed { get; private set; }

        /// <summary>
        /// External speed modifier (0-1). Used by BonusEffectManager for SlowMotion.
        /// </summary>
        public float SpeedModifier { get; set; } = 1f;

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
            var comfortSpeed = difficultyManager != null ? difficultyManager.ComfortSpeedScale : 1f;
            var scoreAboveThreshold = Mathf.Max(0, score - speedGrowthStartScore);
            var speedBonus = Mathf.Min((scoreAboveThreshold / speedGrowthInterval) * speedPerStep, maxSpeedBonus);
            targetSpeed = (baseSpeed + speedBonus) * comfortSpeed;

            CurrentSpeed = Mathf.SmoothDamp(CurrentSpeed, targetSpeed, ref smoothVelocity, speedSmoothTime);

            var vel = playerRigidbody.linearVelocity;
            vel.x = CurrentSpeed * SpeedModifier;
            playerRigidbody.linearVelocity = vel;
        }
    }
}
