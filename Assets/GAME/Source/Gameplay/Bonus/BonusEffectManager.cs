using System;
using UnityEngine;

namespace JumpRing.Game.Gameplay
{
    public sealed class BonusEffectManager : MonoBehaviour
    {
        [SerializeField]
        private BonusConfig bonusConfig;

        [SerializeField]
        private PlayerForwardMover playerForwardMover;

        [SerializeField]
        private PlayerJumpController playerJumpController;

        [SerializeField]
        private LinePathGenerator linePathGenerator;

        [SerializeField]
        private RunSessionController runSessionController;

        [SerializeField]
        private RiskRewardSystem riskRewardSystem;

        [Header("SlowMotion")]
        [SerializeField, Range(0.2f, 0.8f)]
        private float slowMotionSpeedScale = 0.6f;

        [SerializeField, Range(0.1f, 0.5f), Tooltip("Gravity scale during SlowMotion (lower = more floaty)")]
        private float slowMotionGravityScale = 0.5f;

        [SerializeField, Range(0.1f, 1f), Tooltip("Duration of smooth transition back to normal speed")]
        private float slowMotionFadeDuration = 0.5f;

        [Header("SizeUp")]
        [SerializeField, Min(0.1f)]
        private float sizeUpAmount = 1f;

        [Header("CalmLine")]
        [SerializeField, Min(1)]
        private int calmLineSegments = 8;

        [Header("Start Safe Zone")]
        [SerializeField, Min(0f)]
        private float startSafeZoneDuration = 7f;

        [Header("ScoreBoost")]
        [SerializeField, Min(1f)]
        private float scoreBoostCoinMultiplier = 2f;

        [Header("SecondChance")]
        [SerializeField, Min(1)]
        private int maxSecondChances = 3;

        [SerializeField, Min(0.1f)]
        private float invincibilityDuration = 3f;

        [SerializeField, Min(1f)]
        private float secondChanceTimerDuration = 7f;

        public event Action<BonusType> BonusActivated;
        public event Action<BonusType> BonusDeactivated;

        /// <summary>
        /// Fired when second chance count changes. Parameter is the new count.
        /// </summary>
        public event Action<int> SecondChanceCountChanged;

        private BonusType activeBonus;
        private float remainingTime;
        private int secondChanceCount;
        private bool isRunActive;
        private bool isFadingSlowMotion;
        private float slowMotionFadeProgress;
        private float invincibilityRemaining;
        private float safeZoneRemaining;

        public BonusType ActiveBonus => activeBonus;
        public bool HasActiveBonus => activeBonus != BonusType.None;
        public float RemainingTime => remainingTime;
        public bool HasSecondChance => secondChanceCount > 0;
        public int SecondChanceCount => secondChanceCount;
        public int MaxSecondChances => maxSecondChances;
        public bool IsInvincible => invincibilityRemaining > 0f;
        public float SecondChanceTimerDuration => secondChanceTimerDuration;
        public float CoinMultiplier => activeBonus == BonusType.ScoreBoost ? scoreBoostCoinMultiplier : 1f;

        public void OnRunStarted()
        {
            isRunActive = true;
            invincibilityRemaining = 0f;
            safeZoneRemaining = startSafeZoneDuration;
            secondChanceCount = 0;
            SecondChanceCountChanged?.Invoke(0);
            DeactivateBonus();
        }

        public void OnRunFinished()
        {
            isRunActive = false;
            safeZoneRemaining = 0f;
            CancelSlowMotionFade();
            DeactivateBonus();
        }

        public void ActivateBonus(BonusType type)
        {
            // SecondChance is passive — stacks up to max, doesn't occupy the active slot
            if (type == BonusType.SecondChance)
            {
                secondChanceCount = Mathf.Min(secondChanceCount + 1, maxSecondChances);
                SecondChanceCountChanged?.Invoke(secondChanceCount);
                BonusActivated?.Invoke(type);
                return;
            }

            CancelSlowMotionFade();

            if (HasActiveBonus)
            {
                DeactivateBonus();
            }

            activeBonus = type;
            var entry = bonusConfig.GetEntry(type);

            switch (type)
            {
                case BonusType.SlowMotion:
                    remainingTime = entry.duration;
                    playerJumpController.PhysicsScale = slowMotionGravityScale;
                    break;

                case BonusType.ScoreBoost:
                    remainingTime = entry.duration;
                    break;

                case BonusType.CalmLine:
                    remainingTime = entry.duration;
                    ForceFlatAheadFromPlayer();
                    break;

                case BonusType.SizeUp:
                    remainingTime = entry.duration;
                    playerJumpController.ApplySizeModifier(sizeUpAmount);
                    break;
            }

            BonusActivated?.Invoke(type);
        }

        public void DeactivateBonus()
        {
            if (activeBonus == BonusType.None)
            {
                return;
            }

            var previous = activeBonus;

            switch (activeBonus)
            {
                case BonusType.SlowMotion:
                    isFadingSlowMotion = true;
                    slowMotionFadeProgress = 0f;
                    break;

                case BonusType.ScoreBoost:
                    break;

                case BonusType.SizeUp:
                    playerJumpController.ApplySizeModifier(0f);
                    break;
            }

            activeBonus = BonusType.None;
            remainingTime = 0f;

            BonusDeactivated?.Invoke(previous);
        }

        /// <summary>
        /// Consumes one heart and starts invincibility.
        /// Called by SecondChancePresenter when player uses a heart to revive.
        /// </summary>
        public void ConsumeSecondChance()
        {
            if (secondChanceCount <= 0)
            {
                return;
            }

            secondChanceCount--;
            invincibilityRemaining = invincibilityDuration;
            SecondChanceCountChanged?.Invoke(secondChanceCount);
        }

        /// <summary>
        /// Starts invincibility without consuming a heart.
        /// Used for ad-based revival.
        /// </summary>
        public void StartInvincibility()
        {
            invincibilityRemaining = invincibilityDuration;
        }

        public void NotifyTap()
        {
        }

        private void Update()
        {
            UpdateSlowMotionFade();

            if (invincibilityRemaining > 0f)
            {
                invincibilityRemaining -= Time.deltaTime;
            }

            if (safeZoneRemaining > 0f)
            {
                ForceFlatAheadFromPlayer();
                safeZoneRemaining -= Time.deltaTime;
            }

            if (!isRunActive || activeBonus == BonusType.None)
            {
                return;
            }

            // Time-based bonuses
            if (remainingTime > 0f)
            {
                if (activeBonus == BonusType.CalmLine)
                {
                    ForceFlatAheadFromPlayer();
                }

                remainingTime -= Time.deltaTime;

                if (remainingTime <= 0f)
                {
                    DeactivateBonus();
                }
            }
        }

        private void ForceFlatAheadFromPlayer()
        {
            var playerX = playerForwardMover.transform.position.x;
            linePathGenerator.ForceFlatAhead(playerX, calmLineSegments);
        }

        private void UpdateSlowMotionFade()
        {
            if (!isFadingSlowMotion)
            {
                return;
            }

            slowMotionFadeProgress += Time.deltaTime / slowMotionFadeDuration;

            if (slowMotionFadeProgress >= 1f)
            {
                CancelSlowMotionFade();
                return;
            }

            var t = Mathf.SmoothStep(0f, 1f, slowMotionFadeProgress);
            playerJumpController.PhysicsScale = Mathf.Lerp(slowMotionGravityScale, 1f, t);
        }

        private void CancelSlowMotionFade()
        {
            if (!isFadingSlowMotion)
            {
                return;
            }

            isFadingSlowMotion = false;
            slowMotionFadeProgress = 0f;
            playerJumpController.PhysicsScale = 1f;
        }
    }
}
