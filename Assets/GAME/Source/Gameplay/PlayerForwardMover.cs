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

        [SerializeField]
        private MicroEventSystem microEventSystem;

        [Header("Speed")]
        [SerializeField, Min(0.1f)]
        private float baseSpeed = 4f;

        [SerializeField, Min(0.1f)]
        private float maxSpeed = 6.5f;

        [SerializeField]
        private AnimationCurve speedByDifficulty = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        public float CurrentSpeed { get; private set; }

        private void FixedUpdate()
        {
            if (!runSessionController.CanControlPlayer)
            {
                var velocity = playerRigidbody.linearVelocity;
                velocity.x = 0f;
                playerRigidbody.linearVelocity = velocity;
                CurrentSpeed = 0f;
                return;
            }

            var difficulty = difficultyManager != null ? difficultyManager.EffectiveDifficulty : 0f;
            var speedT = speedByDifficulty.Evaluate(difficulty);
            CurrentSpeed = Mathf.Lerp(baseSpeed, maxSpeed, speedT);

            var eventSpeedMult = microEventSystem != null ? microEventSystem.EventSpeedMultiplier : 1f;
            CurrentSpeed *= eventSpeedMult;

            var vel = playerRigidbody.linearVelocity;
            vel.x = CurrentSpeed;
            playerRigidbody.linearVelocity = vel;
        }
    }
}
