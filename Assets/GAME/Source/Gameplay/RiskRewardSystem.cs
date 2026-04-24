using System;
using UnityEngine;

namespace JumpRing.Game.Gameplay
{
    public sealed class RiskRewardSystem : MonoBehaviour
    {
        [SerializeField]
        private DifficultyManager difficultyManager;

        [SerializeField]
        private LinePathGenerator linePathGenerator;

        [SerializeField]
        private Transform playerTransform;

        [Header("Combo")]
        [SerializeField, Tooltip("Distance to line considered 'close' for combo gain")]
        private float closeDistance = 0.5f;

        [SerializeField, Tooltip("Distance to line that resets combo")]
        private float farDistance = 2f;

        [SerializeField, Min(1)]
        private int maxCombo = 5;

        [SerializeField, Min(1)]
        private int tapsToIncreaseCombo = 3;

        [Header("Golden Zones")]
        [SerializeField, Range(0f, 1f)]
        private float goldenZoneChance = 0.08f;

        [SerializeField]
        private float goldenZoneDuration = 5f;

        [SerializeField]
        private float goldenZoneCoinMultiplier = 5f;

        [SerializeField, Range(0f, 1f)]
        private float goldenZoneMinDifficulty = 0.5f;

        public event Action<int> ComboChanged;
        public event Action GoldenZoneStarted;
        public event Action GoldenZoneEnded;
        public event Action NearMissDetected;

        private int currentCombo;
        private int consecutiveCloseTaps;
        private bool isRunActive;
        private bool isGoldenZone;
        private float goldenZoneTimer;

        public int CurrentCombo => currentCombo;
        public float ComboMultiplier => 1f + (currentCombo - 1) * 0.5f;
        public bool IsGoldenZone => isGoldenZone;

        /// <summary>
        /// External multiplier set by BonusEffectManager (ScoreBoost x2).
        /// </summary>
        public float ExternalCoinMultiplier { get; set; } = 1f;

        public float CoinValueMultiplier =>
            (isGoldenZone ? goldenZoneCoinMultiplier * ComboMultiplier : ComboMultiplier) * ExternalCoinMultiplier;

        public void OnRunStarted()
        {
            isRunActive = true;
            currentCombo = 1;
            consecutiveCloseTaps = 0;
            isGoldenZone = false;
            goldenZoneTimer = 0f;
            ExternalCoinMultiplier = 1f;
            ComboChanged?.Invoke(currentCombo);
        }

        public void OnRunFinished()
        {
            isRunActive = false;

            if (isGoldenZone)
            {
                isGoldenZone = false;
                GoldenZoneEnded?.Invoke();
            }
        }

        public void NotifyTap()
        {
            if (!isRunActive)
            {
                return;
            }

            UpdateCombo();
            TryStartGoldenZone();
        }

        private void Update()
        {
            if (!isRunActive || !isGoldenZone)
            {
                return;
            }

            goldenZoneTimer -= Time.deltaTime;

            if (goldenZoneTimer <= 0f)
            {
                isGoldenZone = false;
                GoldenZoneEnded?.Invoke();
            }
        }

        private void UpdateCombo()
        {
            var playerY = playerTransform.position.y;
            var lineY = linePathGenerator.EvaluateHeightAtX(playerTransform.position.x);
            var distance = Mathf.Abs(playerY - lineY);

            if (distance <= closeDistance)
            {
                NearMissDetected?.Invoke();
                consecutiveCloseTaps++;

                if (consecutiveCloseTaps >= tapsToIncreaseCombo && currentCombo < maxCombo)
                {
                    currentCombo++;
                    consecutiveCloseTaps = 0;
                    ComboChanged?.Invoke(currentCombo);
                }
            }
            else if (distance >= farDistance)
            {
                if (currentCombo > 1)
                {
                    currentCombo = Mathf.Max(1, currentCombo - 1);
                    consecutiveCloseTaps = 0;
                    ComboChanged?.Invoke(currentCombo);
                }
            }
            else
            {
                consecutiveCloseTaps = 0;
            }
        }

        private void TryStartGoldenZone()
        {
            if (isGoldenZone)
            {
                return;
            }

            if (difficultyManager.EffectiveDifficulty < goldenZoneMinDifficulty)
            {
                return;
            }

            if (UnityEngine.Random.value < goldenZoneChance)
            {
                isGoldenZone = true;
                goldenZoneTimer = goldenZoneDuration;
                GoldenZoneStarted?.Invoke();
            }
        }
    }
}
