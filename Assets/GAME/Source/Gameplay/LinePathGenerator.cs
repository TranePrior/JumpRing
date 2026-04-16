using System.Collections.Generic;
using UnityEngine;

namespace JumpRing.Game.Gameplay
{
    public sealed class LinePathGenerator : MonoBehaviour
    {
        [System.Serializable]
        private struct WaveLayer
        {
            [Tooltip("Noise sampling frequency — higher values create tighter oscillations")]
            [Min(0.001f)]
            public float frequency;

            [Tooltip("Maximum vertical displacement this layer can add")]
            [Min(0f)]
            public float amplitude;

            [Tooltip("Difficulty value (0–1) at which this layer begins to fade in")]
            [Range(0f, 1f)]
            public float difficultyStart;

            [Tooltip("Difficulty value (0–1) at which this layer is fully active")]
            [Range(0f, 1f)]
            public float difficultyEnd;

            [Tooltip("Use triangle wave instead of Perlin noise for sharp zigzag peaks")]
            public bool useTriangleWave;
        }

        [Header("Dependencies")]
        [SerializeField]
        private LineRenderer lineRenderer;

        [SerializeField]
        private EdgeCollider2D edgeCollider;

        [SerializeField]
        private RunSessionController runSessionController;

        [Header("Window")]
        [SerializeField]
        private Camera gameplayCamera;

        [Header("Segment Length (dynamic)")]
        [SerializeField, Min(0.5f)]
        private float baseSegmentLength = 3f;

        [SerializeField, Min(0.5f)]
        private float minSegmentLength = 2f;

        [SerializeField]
        private AnimationCurve segmentByDifficulty = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [SerializeField]
        private float behindCameraDistance = 10f;

        [SerializeField]
        private float aheadCameraDistance = 10f;

        [SerializeField, Min(0.1f)]
        private float rebuildDistance = 0.5f;

        [Header("Path Bounds")]
        [SerializeField]
        private float minY = -3.5f;

        [SerializeField]
        private float maxY = 3.5f;

        [Header("Difficulty Progression")]
        [SerializeField, Tooltip("Number of clicks (taps) to advance one difficulty step"), Min(1)]
        private int clicksPerDifficultyStep = 5;

        [SerializeField, Tooltip("Total number of difficulty steps to reach maximum difficulty"), Min(1)]
        private int maxDifficultySteps = 15;

        [SerializeField, Tooltip("Maps normalized step progress (0–1) to difficulty (0–1)")]
        private AnimationCurve difficultyCurve = new(
            new Keyframe(0f, 0f, 0f, 3f),
            new Keyframe(0.3f, 0.5f, 1f, 0.6f),
            new Keyframe(1f, 1f, 0.3f, 0f)
        );

        [Header("Comfort Zones")]
        [SerializeField, Min(1)]
        private int comfortZoneInterval = 20;

        [SerializeField, Min(0.1f)]
        private float comfortZoneDuration = 4f;

        [SerializeField, Range(0f, 1f)]
        private float comfortZoneAmplitudeScale = 0.5f;

        [Header("Wave Layers")]
        [SerializeField]
        private WaveLayer[] waveLayers =
        {
            new() { frequency = 0.18f, amplitude = 0.60f, difficultyStart = 0.05f, difficultyEnd = 0.20f, useTriangleWave = true },
            new() { frequency = 0.29f, amplitude = 0.50f, difficultyStart = 0.15f, difficultyEnd = 0.35f, useTriangleWave = true },
            new() { frequency = 0.47f, amplitude = 0.45f, difficultyStart = 0.30f, difficultyEnd = 0.55f, useTriangleWave = true },
            new() { frequency = 0.08f, amplitude = 0.80f, difficultyStart = 0.50f, difficultyEnd = 0.70f, useTriangleWave = true },
            new() { frequency = 0.63f, amplitude = 0.35f, difficultyStart = 0.70f, difficultyEnd = 0.90f, useTriangleWave = true },
        };

        [Header("Seed")]
        [SerializeField]
        private int randomSeed = 12345;

        [Header("Lifecycle")]
        [SerializeField]
        private bool generateOnEnable = true;

        private readonly Vector3[] worldPointsBuffer = new Vector3[2048];
        private readonly Vector2[] localPointsBuffer = new Vector2[2048];

        private float lastStartX;
        private float lastEndX;
        private bool hasWindow;
        private bool isRunActive;
        private float runStartX;
        private float yOffset;
        private float clickDifficulty;
        private float comfortZoneMultiplier = 1f;
        private float comfortZoneTimer;
        private bool isInComfortZone;
        private int lastComfortZoneStep;

        private float[] noiseOffsets;

        public LineRenderer LineRenderer => lineRenderer;
        public float CurrentDifficulty => clickDifficulty;
        public float CurrentWidth => lineRenderer.widthMultiplier;

        private float CurrentSegmentLength
        {
            get
            {
                var t = segmentByDifficulty.Evaluate(clickDifficulty);
                return Mathf.Lerp(baseSegmentLength, minSegmentLength, t);
            }
        }

        private void OnEnable()
        {
            lineRenderer.useWorldSpace = true;
            runSessionController = Object.FindFirstObjectByType<RunSessionController>();
            InitializeNoiseOffsets();
            isRunActive = false;
            runStartX = 0f;
            yOffset = 0f;
            clickDifficulty = 0f;
            comfortZoneMultiplier = 1f;
            comfortZoneTimer = 0f;
            isInComfortZone = false;
            lastComfortZoneStep = 0;
            runSessionController.RunStarted += OnRunStarted;
            runSessionController.RunFinished += OnRunFinished;

            if (generateOnEnable)
            {
                ForceRebuild();
            }
        }

        private void OnDisable()
        {
            runSessionController.RunStarted -= OnRunStarted;
            runSessionController.RunFinished -= OnRunFinished;
        }

        [ContextMenu("Generate Line")]
        public void ForceRebuild()
        {
            hasWindow = false;
            UpdateWindow(force: true);
        }

        public void AlignAndRebuildToPoint(Vector2 anchorPoint)
        {
            runStartX = anchorPoint.x;
            var rawY = EvaluateRawY(anchorPoint.x, 0f);
            yOffset = anchorPoint.y - rawY;
            hasWindow = false;
            UpdateWindow(force: true);
        }

        public bool IsAlignedWithPoint(Vector2 point, float tolerance)
        {
            return Mathf.Abs(EvaluateHeightAtX(point.x) - point.y) <= tolerance;
        }

        public float EvaluateHeightAtX(float x)
        {
            var difficulty = GetDifficultyAtX(x);
            var rawY = EvaluateRawY(x, difficulty);
            return Mathf.Clamp(rawY + yOffset, minY, maxY);
        }

        public bool IsTouchingLine(Collider2D collider, float tolerance)
        {
            var distance = edgeCollider.Distance(collider);
            return distance.isOverlapped || distance.distance <= tolerance;
        }

        public void NotifyScoreChanged(int score)
        {
            if (score <= 0)
            {
                clickDifficulty = 0f;
                comfortZoneMultiplier = 1f;
                isInComfortZone = false;
                lastComfortZoneStep = 0;
                return;
            }

            var difficultyStep = score / clicksPerDifficultyStep;
            var normalized = Mathf.Clamp01((float)difficultyStep / maxDifficultySteps);
            clickDifficulty = difficultyCurve.Evaluate(normalized);

            var comfortStep = score / comfortZoneInterval;
            if (comfortStep > lastComfortZoneStep && !isInComfortZone)
            {
                lastComfortZoneStep = comfortStep;
                isInComfortZone = true;
                comfortZoneTimer = comfortZoneDuration;
                comfortZoneMultiplier = comfortZoneAmplitudeScale;
            }
        }

        private void LateUpdate()
        {
            UpdateComfortZone();
            UpdateWindow(force: false);
        }

        private void UpdateComfortZone()
        {
            if (!isInComfortZone)
            {
                return;
            }

            comfortZoneTimer -= Time.deltaTime;
            if (comfortZoneTimer <= 0f)
            {
                isInComfortZone = false;
                comfortZoneMultiplier = 1f;
            }
            else
            {
                var remainingRatio = comfortZoneTimer / comfortZoneDuration;
                if (remainingRatio < 0.3f)
                {
                    comfortZoneMultiplier = Mathf.Lerp(1f, comfortZoneAmplitudeScale, remainingRatio / 0.3f);
                }
            }
        }

        private void OnRunStarted()
        {
            isRunActive = true;
        }

        private void OnRunFinished()
        {
            isRunActive = false;
        }

        private void InitializeNoiseOffsets()
        {
            var rng = new System.Random(randomSeed);
            noiseOffsets = new float[waveLayers.Length];
            for (var i = 0; i < waveLayers.Length; i++)
            {
                noiseOffsets[i] = (float)(rng.NextDouble() * 10000.0 + 1000.0);
            }
        }

        private float GetDifficultyAtX(float x)
        {
            if (!isRunActive)
            {
                return 0f;
            }

            return clickDifficulty;
        }

        private float EvaluateRawY(float x, float difficulty)
        {
            var y = 0f;

            for (var i = 0; i < waveLayers.Length; i++)
            {
                ref var layer = ref waveLayers[i];

                float blend;
                if (layer.difficultyStart <= 0f && layer.difficultyEnd <= 0f)
                {
                    blend = 1f;
                }
                else
                {
                    blend = SmoothStep(
                        layer.difficultyStart,
                        Mathf.Max(layer.difficultyStart + 0.001f, layer.difficultyEnd),
                        difficulty);
                }

                if (blend < 0.001f)
                {
                    continue;
                }

                var sampleX = x * layer.frequency + noiseOffsets[i];

                float value;
                if (layer.useTriangleWave)
                {
                    value = TriangleWave(sampleX);
                }
                else
                {
                    value = Mathf.PerlinNoise(sampleX, noiseOffsets[i] * 0.7f) * 2f - 1f;
                }

                y += blend * layer.amplitude * value * comfortZoneMultiplier;
            }

            return y;
        }

        private static float TriangleWave(float x)
        {
            var t = x - Mathf.Floor(x);
            return 4f * Mathf.Abs(t - 0.5f) - 1f;
        }

        private static float SmoothStep(float edge0, float edge1, float x)
        {
            var t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        private void UpdateWindow(bool force)
        {
            var seg = CurrentSegmentLength;
            var cameraX = gameplayCamera.transform.position.x;
            var desiredStartX = cameraX - behindCameraDistance;
            var desiredEndX = cameraX + aheadCameraDistance;

            var startX = Mathf.Floor(desiredStartX / seg) * seg;
            var endX = Mathf.Ceil(desiredEndX / seg) * seg;

            if (!force && hasWindow)
            {
                var movedStart = Mathf.Abs(startX - lastStartX);
                var movedEnd = Mathf.Abs(endX - lastEndX);
                if (movedStart < rebuildDistance && movedEnd < rebuildDistance)
                {
                    return;
                }
            }

            BuildWindow(startX, endX, seg);
            lastStartX = startX;
            lastEndX = endX;
            hasWindow = true;
        }

        private void BuildWindow(float startX, float endX, float seg)
        {
            var startStep = Mathf.FloorToInt(startX / seg);
            var endStep = Mathf.CeilToInt(endX / seg);
            var rawPointsCount = endStep - startStep + 1;
            var pointsCount = Mathf.Clamp(rawPointsCount, 2, worldPointsBuffer.Length);
            lineRenderer.positionCount = pointsCount;
            var colliderPoints = new List<Vector2>(pointsCount);

            for (var index = 0; index < pointsCount; index++)
            {
                var x = (startStep + index) * seg;
                var y = EvaluateHeightAtX(x);

                var worldPoint = new Vector3(x, y, 0f);
                worldPointsBuffer[index] = worldPoint;
                localPointsBuffer[index] = transform.InverseTransformPoint(worldPoint);
                lineRenderer.SetPosition(index, worldPoint);
                colliderPoints.Add(localPointsBuffer[index]);
            }

            edgeCollider.SetPoints(colliderPoints);
        }
    }
}
