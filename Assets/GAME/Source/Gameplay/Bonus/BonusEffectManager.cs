using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace JumpRing.Game.Gameplay
{
    /// <summary>
    /// What a revive needs from the bonus system: the stock of hearts it may spend, and the grace
    /// period every revived player gets regardless of what paid for the revive.
    /// </summary>
    public interface ISecondChanceStock
    {
        int SecondChanceCount { get; }

        void ConsumeSecondChance();

        void StartInvincibility();
    }

    public sealed class BonusEffectManager : MonoBehaviour, ISecondChanceStock
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

        [Header("TimeWarp")]
        [FormerlySerializedAs("slowMotionSpeedScale")]
        [SerializeField, Range(0.3f, 1f), Tooltip("Forward speed multiplier during SlowMotion (lower = slower)")]
        private float timeWarpSpeedScale = 0.7f;

        [SerializeField, Range(0.2f, 1f), Tooltip("Jump impulse multiplier during SlowMotion (lower = tiny hops)")]
        private float timeWarpJumpScale = 0.45f;

        [FormerlySerializedAs("slowMotionGravityScale")]
        [SerializeField, Range(0.3f, 1.5f), Tooltip("Gravity scale during TimeWarp (lower = floatier landings)")]
        private float timeWarpGravityScale = 0.55f;

        [FormerlySerializedAs("slowMotionFadeDuration")]
        [SerializeField, Range(0.1f, 1f), Tooltip("Duration of smooth transition back to normal")]
        private float timeWarpFadeDuration = 0.5f;

        [Header("SizeUp")]
        [SerializeField, Min(0.1f)]
        private float sizeUpAmount = 0.3f;

        [Header("CalmLine")]
        [SerializeField, Min(1)]
        private int calmLineSegments = 8;

        [Header("Start Safe Zone")]
        [SerializeField, Min(0f)]
        private float startSafeZoneDuration = 7f;

        [Header("ScoreBoost")]
        [SerializeField, Min(1f)]
        private float scoreBoostCoinMultiplier = 1.5f;

        [Header("SecondChance")]
        [SerializeField, Min(1)]
        private int maxSecondChances = 3;

        [SerializeField, Min(0.1f)]
        private float invincibilityDuration = 3f;

        // A revive puts the player back a couple of units behind the death position, into the exact
        // stretch of line that just killed them — at full speed that is a third of a second away,
        // and a ring clipped by the line dies through invincibility. Flattening the line ahead for
        // a moment is the same treatment the start of a run gets, and the zone travels with the
        // player, so there is no seam between where they stand and where it begins.
        [SerializeField, Min(0f)]
        private float reviveSafeZoneDuration = 2f;

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
        private bool isFadingTimeWarp;
        private float timeWarpFadeProgress;
        private float invincibilityRemaining;
        private float safeZoneRemaining;
        private bool pendingInvincibility;

        public BonusType ActiveBonus => activeBonus;
        public bool HasActiveBonus => activeBonus != BonusType.None;
        public float RemainingTime => remainingTime;
        public bool HasSecondChance => secondChanceCount > 0;
        public int SecondChanceCount => secondChanceCount;
        public int MaxSecondChances => maxSecondChances;
        public bool IsInvincible => invincibilityRemaining > 0f;
        public float CoinMultiplier => activeBonus == BonusType.ScoreBoost ? scoreBoostCoinMultiplier : 1f;

        public void OnRunStarted()
        {
            isRunActive = true;
            invincibilityRemaining = 0f;
            pendingInvincibility = false;
            safeZoneRemaining = startSafeZoneDuration;
            secondChanceCount = 0;
            SecondChanceCountChanged?.Invoke(0);
            DeactivateBonus();
        }

        public void OnRunFinished()
        {
            isRunActive = false;
            safeZoneRemaining = 0f;
            CancelTimeWarpFade();
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

            CancelTimeWarpFade();

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
                    playerJumpController.GravityScale = timeWarpGravityScale;
                    playerJumpController.JumpScale = timeWarpJumpScale;
                    playerForwardMover.SpeedModifier = timeWarpSpeedScale;
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
                    isFadingTimeWarp = true;
                    timeWarpFadeProgress = 0f;
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
        /// Consumes one heart and defers invincibility until gameplay begins.
        /// Called by SecondChancePresenter when player uses a heart to revive.
        /// </summary>
        public void ConsumeSecondChance()
        {
            if (secondChanceCount <= 0)
            {
                return;
            }

            secondChanceCount--;
            BeginRevivalGrace();
            SecondChanceCountChanged?.Invoke(secondChanceCount);
        }

        /// <summary>
        /// Activates pending invincibility when gameplay begins after revive.
        /// </summary>
        public void ActivatePendingInvincibility()
        {
            if (!pendingInvincibility)
            {
                return;
            }

            pendingInvincibility = false;
            invincibilityRemaining = invincibilityDuration;

            // Restarted from the moment the player actually moves: the safe zone armed at revive
            // time keeps the line ahead honest while they decide, but the full stretch has to be
            // measured from the first tap, not from however long they spent looking at it.
            ExtendSafeZone(reviveSafeZoneDuration);
        }

        /// <summary>
        /// Defers invincibility without consuming a heart.
        /// Used for ad-based revival.
        /// </summary>
        public void StartInvincibility()
        {
            BeginRevivalGrace();
        }

        private void BeginRevivalGrace()
        {
            pendingInvincibility = true;
            ExtendSafeZone(reviveSafeZoneDuration);
        }

        // Never shortens a safe zone that is already running: a revive inside the opening seconds
        // of a run must not cut the start zone down to the shorter revive one.
        private void ExtendSafeZone(float duration)
        {
            safeZoneRemaining = Mathf.Max(safeZoneRemaining, duration);
        }

        public void NotifyTap()
        {
        }

        private void Update()
        {
            UpdateTimeWarpFade();

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

        private void UpdateTimeWarpFade()
        {
            if (!isFadingTimeWarp)
            {
                return;
            }

            timeWarpFadeProgress += Time.deltaTime / timeWarpFadeDuration;

            if (timeWarpFadeProgress >= 1f)
            {
                CancelTimeWarpFade();
                return;
            }

            var t = Mathf.SmoothStep(0f, 1f, timeWarpFadeProgress);
            playerJumpController.GravityScale = Mathf.Lerp(timeWarpGravityScale, 1f, t);
            playerJumpController.JumpScale = Mathf.Lerp(timeWarpJumpScale, 1f, t);
            playerForwardMover.SpeedModifier = Mathf.Lerp(timeWarpSpeedScale, 1f, t);
        }

        private void CancelTimeWarpFade()
        {
            if (!isFadingTimeWarp)
            {
                return;
            }

            isFadingTimeWarp = false;
            timeWarpFadeProgress = 0f;
            playerJumpController.GravityScale = 1f;
            playerJumpController.JumpScale = 1f;
            playerForwardMover.SpeedModifier = 1f;
        }
    }
}
