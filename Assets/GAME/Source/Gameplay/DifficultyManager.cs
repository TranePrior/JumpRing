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
        [SerializeField, Tooltip("Taps per full tension cycle"), Min(1)]
        private int tensionCycleTaps = 25;

        [SerializeField, Range(0f, 0.5f)]
        private float tensionAmplitude = 0.25f;

        [SerializeField, Tooltip("Baseline rise per completed cycle")]
        private float tensionBaselineRise = 0.05f;

        [Header("Phase Thresholds")]
        [SerializeField]
        private int tutorialEndScore = 30;

        [SerializeField]
        private int rhythmPhaseScore = 60;

        [SerializeField]
        private int chaosPhaseScore = 105;

        [SerializeField]
        private int masteryPhaseScore = 180;

        [Header("Comfort Zones")]
        [SerializeField, Min(1)]
        private int comfortZoneInterval = 20;

        [SerializeField, Min(0.1f)]
        private float comfortZoneDuration = 4f;

        [SerializeField, Range(0f, 1f)]
        private float comfortZoneScale = 0.5f;

        [SerializeField, Tooltip("Seconds to fade into comfort zone"), Min(0.1f)]
        private float comfortZoneFadeIn = 0.5f;

        [SerializeField, Tooltip("Seconds to fade out of comfort zone"), Min(0.1f)]
        private float comfortZoneFadeOut = 1f;

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

        private bool isInComfortZone;
        private float comfortZoneTimer;
        private int lastComfortZoneStep;
        private float comfortZoneTarget = 1f;
        private float comfortZoneSmoothed = 1f;

        public float BaseDifficulty => targetDifficulty;
        public float SmoothedDifficulty => smoothedDifficulty;

        public float EffectiveDifficulty =>
            Mathf.Clamp01(smoothedDifficulty * comfortZoneSmoothed);

        public float TensionValue => tensionValue;
        public DifficultyPhase CurrentPhase => currentPhase;
        public bool IsComfortZone => isInComfortZone;
        public int CurrentScore => currentScore;
        public bool IsRunActive => isRunActive;

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
            lastComfortZoneStep = 0;
            SetPhase(DifficultyPhase.Tutorial);
        }

        public void OnRunFinished()
        {
            isRunActive = false;
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
            var cycleProgress = (currentScore % tensionCycleTaps) / (float)tensionCycleTaps;
            var wave = Mathf.Sin(cycleProgress * Mathf.PI * 2f);

            var completedCycles = currentScore / tensionCycleTaps;
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
            if (currentScore <= 0)
            {
                return;
            }

            var comfortStep = currentScore / comfortZoneInterval;

            if (comfortStep > lastComfortZoneStep && !isInComfortZone)
            {
                lastComfortZoneStep = comfortStep;
                isInComfortZone = true;
                comfortZoneTimer = comfortZoneDuration;
                comfortZoneTarget = comfortZoneScale;
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
                ComfortZoneEnded?.Invoke();
            }
        }
    }
}
