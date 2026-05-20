using System;
using UnityEngine;

namespace JumpRing.Game.Gameplay
{
    public enum DifficultyPhase
    {
        Tutorial = 0,
        Calm = 1,
        Rhythm = 2,
        Chaos = 3,
        Mastery = 4,
    }

    public sealed class DifficultyManager : MonoBehaviour
    {
        [Header("Base Difficulty")]
        [SerializeField, Min(1)]
        private int maxTapsForFullDifficulty = 75;

        [SerializeField]
        private AnimationCurve difficultyCurve = new(
            new Keyframe(0f, 0f, 0f, 3f),
            new Keyframe(0.3f, 0.5f, 1f, 0.6f),
            new Keyframe(1f, 1f, 0.3f, 0f)
        );

        [Header("Temporal Smoothing")]
        [SerializeField, Tooltip("How fast values smooth toward target (higher = faster)"), Range(1f, 20f)]
        private float smoothSpeed = 4f;

        [Header("Tension Curve")]
        [SerializeField, Min(1), Tooltip("Min taps per tension cycle")]
        private int tensionCycleMin = 20;

        [SerializeField, Min(1), Tooltip("Max taps per tension cycle")]
        private int tensionCycleMax = 30;

        [SerializeField, Range(0f, 0.5f)]
        private float tensionAmplitude = 0.25f;

        [SerializeField, Tooltip("Baseline rise per completed cycle")]
        private float tensionBaselineRise = 0.05f;

        [SerializeField, Range(0f, 1f), Tooltip("Chance to skip comfort zone each cycle")]
        private float comfortZoneSkipChance = 0.3f;

        [Header("Phase Thresholds")]
        [SerializeField]
        private int tutorialEndScore = 30;

        [SerializeField]
        private int rhythmPhaseScore = 60;

        [SerializeField]
        private int chaosPhaseScore = 105;

        [SerializeField]
        private int masteryPhaseScore = 180;

        [Header("Dynamic Difficulty Adjustment")]
        [SerializeField, Min(3), Tooltip("Sliding window size for distance tracking")]
        private int ddaWindowSize = 15;

        [SerializeField, Min(0.01f), Tooltip("Distance considered 'close' — player is skilled")]
        private float ddaCloseThreshold = 0.5f;

        [SerializeField, Min(0.01f), Tooltip("Distance considered 'far' — player is struggling")]
        private float ddaFarThreshold = 1.5f;

        [SerializeField, Range(0f, 0.5f), Tooltip("Max difficulty boost for skilled players")]
        private float ddaMaxBoost = 0.25f;

        [SerializeField, Range(0f, 0.5f), Tooltip("Max difficulty brake for struggling players")]
        private float ddaMaxBrake = 0.2f;

        [SerializeField, Range(1f, 20f)]
        private float ddaSmoothSpeed = 3f;

        [Header("Comfort Zones (tied to tension wave)")]
        [SerializeField, Range(0f, 1f), Tooltip("Triggers when raw wave drops below this negative threshold")]
        private float comfortZoneWaveThreshold = 0.7f;

        [SerializeField, Min(0.1f)]
        private float comfortZoneDuration = 4f;

        [SerializeField, Range(0f, 1f)]
        private float comfortZoneScale = 0.5f;

        [SerializeField, Tooltip("Seconds to fade into comfort zone"), Min(0.1f)]
        private float comfortZoneFadeIn = 0.5f;

        [SerializeField, Tooltip("Seconds to fade out of comfort zone"), Min(0.1f)]
        private float comfortZoneFadeOut = 1f;

        [SerializeField, Range(0.5f, 1f), Tooltip("Speed scale during comfort zone (softer than pattern scale)")]
        private float comfortZoneSpeedScale = 0.85f;

        [Header("Dimension Curves")]
        [SerializeField]
        private AnimationCurve frequencyScaling = AnimationCurve.Linear(0f, 1f, 1f, 1.8f);

        [SerializeField]
        private AnimationCurve amplitudeScaling = AnimationCurve.Linear(0f, 0.6f, 1f, 1.2f);

        [SerializeField]
        private AnimationCurve speedScaling = AnimationCurve.Linear(0f, 1f, 1f, 1.5f);

        [SerializeField]
        private AnimationCurve lineWidthScaling = new(
            new Keyframe(0f, 1f),
            new Keyframe(0.5f, 1.2f),
            new Keyframe(1f, 1.8f)
        );

        public event Action<DifficultyPhase> PhaseChanged;
        public event Action ComfortZoneStarted;
        public event Action ComfortZoneEnded;

        private float targetDifficulty;
        private float smoothedDifficulty;
        private float tensionValue;
        private int currentScore;
        private bool isRunActive;
        private DifficultyPhase currentPhase;

        // Variable tension cycle tracking
        private int currentCycleLength;
        private int cycleStartTap;
        private int completedCycles;

        private bool isInComfortZone;
        private float comfortZoneTimer;
        private int lastComfortCycle;
        private float comfortZoneTarget = 1f;
        private float comfortZoneSmoothed = 1f;

        // Comfort speed (separate from pattern comfort)
        private float comfortSpeedTarget = 1f;
        private float smoothedComfortSpeedScale = 1f;

        // DDA state
        private float[] ddaDistances;
        private int ddaWriteIndex;
        private int ddaSampleCount;
        private float targetSkillMultiplier = 1f;
        private float smoothedSkillMultiplier = 1f;

        public float BaseDifficulty => targetDifficulty;
        public float SmoothedDifficulty => smoothedDifficulty;

        public float EffectiveDifficulty =>
            Mathf.Clamp01(smoothedDifficulty * comfortZoneSmoothed * smoothedSkillMultiplier);

        public float TensionValue => tensionValue;
        public DifficultyPhase CurrentPhase => currentPhase;
        public bool IsComfortZone => isInComfortZone;
        public int CurrentScore => currentScore;
        public bool IsRunActive => isRunActive;

        public float ComfortSpeedScale => smoothedComfortSpeedScale;

        public float FrequencyMultiplier => frequencyScaling.Evaluate(EffectiveDifficulty);
        public float AmplitudeMultiplier => amplitudeScaling.Evaluate(EffectiveDifficulty);
        public float SpeedMultiplier => speedScaling.Evaluate(EffectiveDifficulty);
        public float LineWidthMultiplier => lineWidthScaling.Evaluate(EffectiveDifficulty);

        public void OnRunStarted()
        {
            isRunActive = true;
            currentScore = 0;
            targetDifficulty = 0f;
            smoothedDifficulty = 0f;
            tensionValue = 0f;
            comfortZoneTarget = 1f;
            comfortZoneSmoothed = 1f;
            isInComfortZone = false;
            comfortZoneTimer = 0f;
            lastComfortCycle = -1;

            // Comfort speed reset
            comfortSpeedTarget = 1f;
            smoothedComfortSpeedScale = 1f;

            // Tension cycle reset
            currentCycleLength = UnityEngine.Random.Range(tensionCycleMin, tensionCycleMax + 1);
            cycleStartTap = 0;
            completedCycles = 0;

            // DDA reset
            ddaDistances ??= new float[ddaWindowSize];
            for (var i = 0; i < ddaDistances.Length; i++)
            {
                ddaDistances[i] = 0f;
            }
            ddaWriteIndex = 0;
            ddaSampleCount = 0;
            targetSkillMultiplier = 1f;
            smoothedSkillMultiplier = 1f;

            SetPhase(DifficultyPhase.Tutorial);
        }

        public void OnRunFinished()
        {
            isRunActive = false;
        }

        public void NotifyTapDistance(float distanceToLine)
        {
            if (!isRunActive || currentScore < tutorialEndScore)
            {
                return;
            }

            ddaDistances ??= new float[ddaWindowSize];
            ddaDistances[ddaWriteIndex] = distanceToLine;
            ddaWriteIndex = (ddaWriteIndex + 1) % ddaWindowSize;
            if (ddaSampleCount < ddaWindowSize)
            {
                ddaSampleCount++;
            }

            UpdateSkillMultiplier();
        }

        public void NotifyTap(int score)
        {
            if (!isRunActive)
            {
                return;
            }

            currentScore = score;
            UpdateTargetDifficulty();
            UpdateTensionCurve();
            UpdatePhase();
            CheckComfortZoneTrigger();
        }

        private void Update()
        {
            if (!isRunActive)
            {
                return;
            }

            SmoothValues();
            UpdateComfortZoneTimer();
        }

        private void SmoothValues()
        {
            var dt = Time.deltaTime * smoothSpeed;

            smoothedDifficulty = Mathf.Lerp(smoothedDifficulty, targetDifficulty, dt);
            comfortZoneSmoothed = Mathf.Lerp(
                comfortZoneSmoothed,
                comfortZoneTarget,
                Time.deltaTime / (comfortZoneTarget < comfortZoneSmoothed ? comfortZoneFadeIn : comfortZoneFadeOut)
            );
            smoothedSkillMultiplier = Mathf.Lerp(
                smoothedSkillMultiplier,
                targetSkillMultiplier,
                Time.deltaTime * ddaSmoothSpeed
            );
            smoothedComfortSpeedScale = Mathf.Lerp(
                smoothedComfortSpeedScale,
                comfortSpeedTarget,
                Time.deltaTime / (comfortSpeedTarget < smoothedComfortSpeedScale ? comfortZoneFadeIn : comfortZoneFadeOut)
            );
        }

        private void UpdateSkillMultiplier()
        {
            if (ddaSampleCount == 0)
            {
                targetSkillMultiplier = 1f;
                return;
            }

            var sum = 0f;
            var count = Mathf.Min(ddaSampleCount, ddaWindowSize);
            for (var i = 0; i < count; i++)
            {
                sum += ddaDistances[i];
            }
            var avgDistance = sum / count;

            // Close to line → boost, far from line → brake
            if (avgDistance <= ddaCloseThreshold)
            {
                var t = 1f - avgDistance / ddaCloseThreshold;
                targetSkillMultiplier = 1f + t * ddaMaxBoost;
            }
            else if (avgDistance >= ddaFarThreshold)
            {
                targetSkillMultiplier = 1f - ddaMaxBrake;
            }
            else
            {
                var t = (avgDistance - ddaCloseThreshold) / (ddaFarThreshold - ddaCloseThreshold);
                targetSkillMultiplier = Mathf.Lerp(1f, 1f - ddaMaxBrake, t);
            }
        }

        private void UpdateTargetDifficulty()
        {
            if (currentScore < tutorialEndScore)
            {
                targetDifficulty = 0f;
                return;
            }

            var scoreAfterTutorial = currentScore - tutorialEndScore;
            var normalized = Mathf.Clamp01((float)scoreAfterTutorial / maxTapsForFullDifficulty);
            var baseDiff = difficultyCurve.Evaluate(normalized);
            targetDifficulty = baseDiff * (1f + tensionValue);
        }

        private void UpdateTensionCurve()
        {
            var tapsIntoCycle = currentScore - cycleStartTap;

            // Cycle boundary — start new cycle with random length
            if (tapsIntoCycle >= currentCycleLength)
            {
                completedCycles++;
                cycleStartTap = currentScore;
                tapsIntoCycle = 0;
                currentCycleLength = UnityEngine.Random.Range(tensionCycleMin, tensionCycleMax + 1);
            }

            var cycleProgress = tapsIntoCycle / (float)currentCycleLength;
            var wave = Mathf.Sin(cycleProgress * Mathf.PI * 2f);
            var baselineBoost = completedCycles * tensionBaselineRise;

            tensionValue = wave * tensionAmplitude + baselineBoost;
        }

        private void UpdatePhase()
        {
            DifficultyPhase newPhase;

            if (currentScore >= masteryPhaseScore)
            {
                newPhase = DifficultyPhase.Mastery;
            }
            else if (currentScore >= chaosPhaseScore)
            {
                newPhase = DifficultyPhase.Chaos;
            }
            else if (currentScore >= rhythmPhaseScore)
            {
                newPhase = DifficultyPhase.Rhythm;
            }
            else if (currentScore >= tutorialEndScore)
            {
                newPhase = DifficultyPhase.Calm;
            }
            else
            {
                newPhase = DifficultyPhase.Tutorial;
            }

            if (newPhase != currentPhase)
            {
                SetPhase(newPhase);
            }
        }

        private void SetPhase(DifficultyPhase phase)
        {
            currentPhase = phase;
            PhaseChanged?.Invoke(phase);
        }

        private void CheckComfortZoneTrigger()
        {
            if (currentScore < tutorialEndScore || isInComfortZone)
            {
                return;
            }

            if (completedCycles <= lastComfortCycle)
            {
                return;
            }

            var tapsIntoCycle = currentScore - cycleStartTap;
            var cycleProgress = tapsIntoCycle / (float)currentCycleLength;
            var wave = Mathf.Sin(cycleProgress * Mathf.PI * 2f);

            if (wave <= -comfortZoneWaveThreshold)
            {
                lastComfortCycle = completedCycles;

                // Random skip — not every cycle gives a comfort zone
                if (UnityEngine.Random.value < comfortZoneSkipChance)
                {
                    return;
                }

                isInComfortZone = true;
                comfortZoneTimer = comfortZoneDuration;
                comfortZoneTarget = comfortZoneScale;
                comfortSpeedTarget = comfortZoneSpeedScale;
                ComfortZoneStarted?.Invoke();
            }
        }

        private void UpdateComfortZoneTimer()
        {
            if (!isInComfortZone)
            {
                return;
            }

            comfortZoneTimer -= Time.deltaTime;

            if (comfortZoneTimer <= 0f)
            {
                isInComfortZone = false;
                comfortZoneTarget = 1f;
                comfortSpeedTarget = 1f;
                ComfortZoneEnded?.Invoke();
            }
        }
    }
}
