using System;
using System.Collections.Generic;
using UnityEngine;

namespace JumpRing.Game.Gameplay
{
    public enum MicroEventType
    {
        None = 0,
        Narrowing = 1,
        Storm = 2,
        Inversion = 3,
        BlindZone = 4,
        SpeedBurst = 5,
    }

    public sealed class MicroEventSystem : MonoBehaviour
    {
        [Serializable]
        private struct EventConfig
        {
            public MicroEventType type;
            public float duration;
            public float amplitudeMultiplier;
            public float speedMultiplier;
            public float coinMultiplier;
            public bool hideLine;

            [Range(0f, 1f)]
            public float minDifficulty;
        }

        [SerializeField]
        private DifficultyManager difficultyManager;

        [Header("Timing")]
        [SerializeField, Tooltip("Min seconds between events at max difficulty")]
        private float minInterval = 10f;

        [SerializeField, Tooltip("Max seconds between events at low difficulty")]
        private float maxInterval = 30f;

        [SerializeField, Range(0f, 1f)]
        private float activationDifficulty = 0.3f;

        [Header("Fade")]
        [SerializeField, Tooltip("Seconds to fade event effects in"), Min(0.05f)]
        private float fadeInDuration = 0.5f;

        [SerializeField, Tooltip("Seconds to fade event effects out"), Min(0.05f)]
        private float fadeOutDuration = 0.5f;

        [Header("Events")]
        [SerializeField]
        private EventConfig[] events =
        {
            new()
            {
                type = MicroEventType.Narrowing,
                duration = 2.5f,
                amplitudeMultiplier = 1.8f,
                speedMultiplier = 1f,
                coinMultiplier = 1f,
                hideLine = false,
                minDifficulty = 0.3f,
            },
            new()
            {
                type = MicroEventType.Storm,
                duration = 3f,
                amplitudeMultiplier = 2f,
                speedMultiplier = 1f,
                coinMultiplier = 3f,
                hideLine = false,
                minDifficulty = 0.4f,
            },
            new()
            {
                type = MicroEventType.Inversion,
                duration = 3f,
                amplitudeMultiplier = 1f,
                speedMultiplier = 1f,
                coinMultiplier = 1.5f,
                hideLine = false,
                minDifficulty = 0.5f,
            },
            new()
            {
                type = MicroEventType.BlindZone,
                duration = 1.5f,
                amplitudeMultiplier = 0.7f,
                speedMultiplier = 0.9f,
                coinMultiplier = 2f,
                hideLine = true,
                minDifficulty = 0.6f,
            },
            new()
            {
                type = MicroEventType.SpeedBurst,
                duration = 2f,
                amplitudeMultiplier = 0.8f,
                speedMultiplier = 1.5f,
                coinMultiplier = 2f,
                hideLine = false,
                minDifficulty = 0.35f,
            },
        };

        public event Action<MicroEventType> EventStarted;
        public event Action<MicroEventType> EventEnded;

        private enum EventState
        {
            Idle,
            FadingIn,
            Active,
            FadingOut,
        }

        private MicroEventType activeEventType;
        private EventConfig activeConfig;
        private EventState eventState;
        private float eventTimer;
        private float fadeProgress;
        private float nextEventTimer;
        private bool isRunActive;

        public MicroEventType ActiveEvent => activeEventType;

        public float EventAmplitudeMultiplier =>
            Mathf.Lerp(1f, activeEventType != MicroEventType.None ? activeConfig.amplitudeMultiplier : 1f, fadeProgress);

        public float EventSpeedMultiplier =>
            Mathf.Lerp(1f, activeEventType != MicroEventType.None ? activeConfig.speedMultiplier : 1f, fadeProgress);

        public float EventCoinMultiplier =>
            activeEventType != MicroEventType.None ? Mathf.Lerp(1f, activeConfig.coinMultiplier, fadeProgress) : 1f;

        public bool IsLineHidden =>
            activeEventType != MicroEventType.None && activeConfig.hideLine && fadeProgress > 0.5f;

        public bool IsInverted =>
            activeEventType == MicroEventType.Inversion && fadeProgress > 0.5f;

        public float FadeProgress => fadeProgress;

        public void OnRunStarted()
        {
            isRunActive = true;
            activeEventType = MicroEventType.None;
            eventState = EventState.Idle;
            eventTimer = 0f;
            fadeProgress = 0f;
            ScheduleNextEvent();
        }

        public void OnRunFinished()
        {
            isRunActive = false;

            if (activeEventType != MicroEventType.None)
            {
                var ended = activeEventType;
                activeEventType = MicroEventType.None;
                eventState = EventState.Idle;
                fadeProgress = 0f;
                EventEnded?.Invoke(ended);
            }
        }

        private void Update()
        {
            if (!isRunActive)
            {
                return;
            }

            switch (eventState)
            {
                case EventState.FadingIn:
                    UpdateFadeIn();
                    break;

                case EventState.Active:
                    UpdateActive();
                    break;

                case EventState.FadingOut:
                    UpdateFadeOut();
                    break;

                case EventState.Idle:
                    UpdateIdle();
                    break;
            }
        }

        private void UpdateFadeIn()
        {
            fadeProgress = Mathf.MoveTowards(fadeProgress, 1f, Time.deltaTime / fadeInDuration);

            if (fadeProgress >= 1f)
            {
                fadeProgress = 1f;
                eventState = EventState.Active;
            }
        }

        private void UpdateActive()
        {
            eventTimer -= Time.deltaTime;

            if (eventTimer <= 0f)
            {
                eventState = EventState.FadingOut;
            }
        }

        private void UpdateFadeOut()
        {
            fadeProgress = Mathf.MoveTowards(fadeProgress, 0f, Time.deltaTime / fadeOutDuration);

            if (fadeProgress <= 0f)
            {
                fadeProgress = 0f;
                var ended = activeEventType;
                activeEventType = MicroEventType.None;
                eventState = EventState.Idle;
                EventEnded?.Invoke(ended);
                ScheduleNextEvent();
            }
        }

        private void UpdateIdle()
        {
            if (difficultyManager.EffectiveDifficulty < activationDifficulty)
            {
                return;
            }

            if (difficultyManager.IsComfortZone)
            {
                return;
            }

            nextEventTimer -= Time.deltaTime;

            if (nextEventTimer <= 0f)
            {
                TryStartEvent();
            }
        }

        private void TryStartEvent()
        {
            var difficulty = difficultyManager.EffectiveDifficulty;
            var eligible = new List<EventConfig>();

            for (var i = 0; i < events.Length; i++)
            {
                if (difficulty >= events[i].minDifficulty)
                {
                    eligible.Add(events[i]);
                }
            }

            if (eligible.Count == 0)
            {
                ScheduleNextEvent();
                return;
            }

            var chosen = eligible[UnityEngine.Random.Range(0, eligible.Count)];
            activeConfig = chosen;
            activeEventType = chosen.type;
            eventTimer = chosen.duration;
            fadeProgress = 0f;
            eventState = EventState.FadingIn;
            EventStarted?.Invoke(activeEventType);
        }

        private void ScheduleNextEvent()
        {
            var difficulty = difficultyManager.EffectiveDifficulty;
            var t = Mathf.InverseLerp(activationDifficulty, 1f, difficulty);
            nextEventTimer = Mathf.Lerp(maxInterval, minInterval, t);
        }
    }
}
