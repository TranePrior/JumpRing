using System.Collections.Generic;
using UnityEngine;

namespace JumpRing.Game.Gameplay
{
    public sealed class LinePathGenerator : MonoBehaviour
    {
        private enum WaveType
        {
            Perlin = 0,
            Triangle = 1,
            Sawtooth = 2,
        }

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

            [Tooltip("Wave shape: Perlin (smooth), Triangle (zigzag), Sawtooth (ramp up + drop)")]
            public WaveType waveType;

            [Tooltip("Legacy field — use waveType instead")]
            [HideInInspector]
            public bool useTriangleWave;
        }

        [System.Serializable]
        private struct SlopeConfig
        {
            [Tooltip("Difficulty at which slopes start appearing")]
            [Range(0f, 1f)]
            public float minDifficulty;

            [Tooltip("Average distance between slope starts (in world X units)")]
            [Min(5f)]
            public float interval;

            [Tooltip("Length of a slope segment in world X units")]
            [Min(2f)]
            public float length;

            [Tooltip("Slope angle in degrees (30 = gentle, 60 = steep)")]
            [Range(15f, 70f)]
            public float maxAngle;

            [Tooltip("How much to dampen wave layers during a slope (0 = full dampen, 1 = no dampen)")]
            [Range(0f, 1f)]
            public float waveDampening;

            [Tooltip("Blend distance at slope entry/exit")]
            [Min(0.5f)]
            public float blendDistance;
        }

        [Header("Dependencies")]
        [SerializeField]
        private LineRenderer lineRenderer;

        [SerializeField]
        private EdgeCollider2D edgeCollider;

        [SerializeField]
        private RunSessionController runSessionController;

        [SerializeField]
        private DifficultyManager difficultyManager;

        [SerializeField]
        private MicroEventSystem microEventSystem;

        [Header("Window")]
        [SerializeField]
        private Camera gameplayCamera;

        [Header("Segment Length")]
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
        private float minY = -2.5f;

        [SerializeField]
        private float maxY = 2.5f;

        [Header("Line Width")]
        [SerializeField]
        private float baseLineWidth = 0.15f;

        [Header("Wave Layers")]
        [SerializeField]
        private WaveLayer[] waveLayers =
        {
            new() { frequency = 0.12f, amplitude = 0.80f, difficultyStart = 0.05f, difficultyEnd = 0.20f, waveType = WaveType.Perlin },
            new() { frequency = 0.25f, amplitude = 0.40f, difficultyStart = 0.15f, difficultyEnd = 0.35f, waveType = WaveType.Perlin },
            new() { frequency = 0.50f, amplitude = 0.55f, difficultyStart = 0.30f, difficultyEnd = 0.55f, waveType = WaveType.Triangle },
            new() { frequency = 0.07f, amplitude = 1.00f, difficultyStart = 0.50f, difficultyEnd = 0.70f, waveType = WaveType.Triangle },
            new() { frequency = 0.80f, amplitude = 0.25f, difficultyStart = 0.70f, difficultyEnd = 0.90f, waveType = WaveType.Triangle },
            new() { frequency = 0.04f, amplitude = 1.20f, difficultyStart = 0.30f, difficultyEnd = 0.60f, waveType = WaveType.Sawtooth },
        };

        [Header("Slope Segments")]
        [SerializeField]
        private SlopeConfig slopeConfig = new()
        {
            minDifficulty = 0.5f,
            interval = 30f,
            length = 6f,
            maxAngle = 45f,
            waveDampening = 0.3f,
            blendDistance = 1.5f,
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

        private float[] noiseOffsets;
        private float slopeNoiseOffset;

        // Baking system: once a point is generated, it never changes
        private readonly Dictionary<int, float> bakedHeights = new(256);
        private float activeSegmentLength;

        public LineRenderer LineRenderer => lineRenderer;
        public float CurrentDifficulty => difficultyManager != null ? difficultyManager.EffectiveDifficulty : 0f;
        public float CurrentWidth => lineRenderer.widthMultiplier;

        private float ActiveSegmentLength
        {
            get
            {
                if (isRunActive)
                {
                    return activeSegmentLength;
                }

                var t = segmentByDifficulty.Evaluate(CurrentDifficulty);
                return Mathf.Lerp(baseSegmentLength, minSegmentLength, t);
            }
        }

        private void OnEnable()
        {
            lineRenderer.useWorldSpace = true;
            runSessionController = Object.FindFirstObjectByType<RunSessionController>();

            if (difficultyManager == null)
            {
                difficultyManager = Object.FindFirstObjectByType<DifficultyManager>();
            }

            if (microEventSystem == null)
            {
                microEventSystem = Object.FindFirstObjectByType<MicroEventSystem>();
            }

            InitializeNoiseOffsets();
            isRunActive = false;
            runStartX = 0f;
            yOffset = 0f;
            activeSegmentLength = baseSegmentLength;

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
            bakedHeights.Clear();
            hasWindow = false;
            UpdateWindow(force: true);
        }

        public void AlignAndRebuildToPoint(Vector2 anchorPoint)
        {
            runStartX = anchorPoint.x;
            var rawY = EvaluateRawY(anchorPoint.x, 0f);
            yOffset = anchorPoint.y - rawY;
            bakedHeights.Clear();
            hasWindow = false;
            UpdateWindow(force: true);
        }

        public bool IsAlignedWithPoint(Vector2 point, float tolerance)
        {
            return Mathf.Abs(EvaluateHeightAtX(point.x) - point.y) <= tolerance;
        }

        public float EvaluateHeightAtX(float x)
        {
            var seg = ActiveSegmentLength;
            var step = Mathf.RoundToInt(x / seg);

            if (bakedHeights.TryGetValue(step, out var bakedY))
            {
                return bakedY;
            }

            // Not baked yet — evaluate fresh (used for points ahead of frontier)
            var difficulty = isRunActive ? CurrentDifficulty : 0f;
            var rawY = EvaluateRawY(x, difficulty);
            return Mathf.Clamp(rawY + yOffset, minY, maxY);
        }

        public bool IsTouchingLine(Collider2D collider, float tolerance)
        {
            var distance = edgeCollider.Distance(collider);
            return distance.isOverlapped || distance.distance <= tolerance;
        }

        private void LateUpdate()
        {
            UpdateLineVisibility();
            UpdateLineWidth();
            UpdateWindow(force: false);
        }

        private void UpdateLineVisibility()
        {
            if (microEventSystem != null)
            {
                lineRenderer.enabled = !microEventSystem.IsLineHidden;
            }
        }

        private void UpdateLineWidth()
        {
            if (difficultyManager != null)
            {
                lineRenderer.widthMultiplier = baseLineWidth * difficultyManager.LineWidthMultiplier;
            }
        }

        private void OnRunStarted()
        {
            isRunActive = true;
            var t = segmentByDifficulty.Evaluate(0f);
            activeSegmentLength = Mathf.Lerp(baseSegmentLength, minSegmentLength, t);
            bakedHeights.Clear();
        }

        private void OnRunFinished()
        {
            isRunActive = false;
            bakedHeights.Clear();
        }

        private void InitializeNoiseOffsets()
        {
            var rng = new System.Random(randomSeed);
            noiseOffsets = new float[waveLayers.Length];

            for (var i = 0; i < waveLayers.Length; i++)
            {
                noiseOffsets[i] = (float)(rng.NextDouble() * 10000.0 + 1000.0);
            }

            slopeNoiseOffset = (float)(rng.NextDouble() * 10000.0 + 1000.0);
        }

        private float BakeOrGetHeight(int step, float seg)
        {
            if (bakedHeights.TryGetValue(step, out var cachedY))
            {
                return cachedY;
            }

            var x = step * seg;
            var difficulty = isRunActive ? CurrentDifficulty : 0f;
            var rawY = EvaluateRawY(x, difficulty);
            var clampedY = Mathf.Clamp(rawY + yOffset, minY, maxY);

            bakedHeights[step] = clampedY;
            return clampedY;
        }

        private float EvaluateRawY(float x, float difficulty)
        {
            var freqMult = difficultyManager != null ? difficultyManager.FrequencyMultiplier : 1f;
            var ampMult = difficultyManager != null ? difficultyManager.AmplitudeMultiplier : 1f;
            var eventAmpMult = microEventSystem != null ? microEventSystem.EventAmplitudeMultiplier : 1f;
            var inversionSign = (microEventSystem != null && microEventSystem.IsInverted) ? -1f : 1f;

            var slopeResult = EvaluateSlope(x, difficulty);
            var waveDampen = Mathf.Lerp(1f, slopeResult.waveDampening, slopeResult.blend);

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

                var sampleX = x * layer.frequency * freqMult + noiseOffsets[i];

                float value;
                switch (layer.waveType)
                {
                    case WaveType.Triangle:
                        value = TriangleWave(sampleX);
                        break;
                    case WaveType.Sawtooth:
                        value = SawtoothWave(sampleX);
                        break;
                    default:
                        value = Mathf.PerlinNoise(sampleX, noiseOffsets[i] * 0.7f) * 2f - 1f;
                        break;
                }

                y += blend * layer.amplitude * ampMult * eventAmpMult * value * inversionSign * waveDampen;
            }

            y += slopeResult.yOffset * slopeResult.blend;

            return y;
        }

        private struct SlopeResult
        {
            public float yOffset;
            public float blend;
            public float waveDampening;
        }

        private SlopeResult EvaluateSlope(float x, float difficulty)
        {
            var result = new SlopeResult { yOffset = 0f, blend = 0f, waveDampening = 1f };

            if (difficulty < slopeConfig.minDifficulty)
            {
                return result;
            }

            var slopePhase = (x + slopeNoiseOffset) / slopeConfig.interval;
            var slopeIndex = Mathf.FloorToInt(slopePhase);
            var posInCycle = (slopePhase - slopeIndex) * slopeConfig.interval;

            if (posInCycle > slopeConfig.length + slopeConfig.blendDistance * 2f)
            {
                return result;
            }

            // Deterministic direction: hash slope index to get up/down
            var direction = ((slopeIndex * 73856093) & 1) == 0 ? 1f : -1f;

            var slopeRise = Mathf.Tan(slopeConfig.maxAngle * Mathf.Deg2Rad);
            var totalSlopeWithBlend = slopeConfig.length + slopeConfig.blendDistance * 2f;

            float blend;
            float localT;

            if (posInCycle < slopeConfig.blendDistance)
            {
                // Blending in
                blend = SmoothStep(0f, slopeConfig.blendDistance, posInCycle);
                localT = posInCycle;
            }
            else if (posInCycle > slopeConfig.length + slopeConfig.blendDistance)
            {
                // Blending out
                blend = 1f - SmoothStep(
                    slopeConfig.length + slopeConfig.blendDistance,
                    totalSlopeWithBlend,
                    posInCycle);
                localT = posInCycle;
            }
            else
            {
                // Fully in slope
                blend = 1f;
                localT = posInCycle;
            }

            var difficultyBlend = SmoothStep(
                slopeConfig.minDifficulty,
                Mathf.Min(slopeConfig.minDifficulty + 0.2f, 1f),
                difficulty);

            blend *= difficultyBlend;

            // Y offset: centered ramp (goes up half, then down half)
            var halfLength = (slopeConfig.length + slopeConfig.blendDistance * 2f) * 0.5f;
            var distFromCenter = localT - halfLength;
            result.yOffset = direction * distFromCenter * slopeRise;
            result.blend = blend;
            result.waveDampening = slopeConfig.waveDampening;

            return result;
        }

        private static float TriangleWave(float x)
        {
            var t = x - Mathf.Floor(x);
            return 4f * Mathf.Abs(t - 0.5f) - 1f;
        }

        private static float SawtoothWave(float x)
        {
            var t = x - Mathf.Floor(x);
            // Ramp up 0→1 over 75% of period, sharp drop over 25%
            if (t < 0.75f)
            {
                return (t / 0.75f) * 2f - 1f;
            }

            return 1f - ((t - 0.75f) / 0.25f) * 2f;
        }

        private static float SmoothStep(float edge0, float edge1, float x)
        {
            var t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        private void UpdateWindow(bool force)
        {
            var seg = ActiveSegmentLength;
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

            // Prune baked points far behind the camera
            var pruneStep = startStep - 20;
            PruneBakedBefore(pruneStep);

            for (var index = 0; index < pointsCount; index++)
            {
                var step = startStep + index;
                var x = step * seg;
                var y = BakeOrGetHeight(step, seg);

                var worldPoint = new Vector3(x, y, 0f);
                worldPointsBuffer[index] = worldPoint;
                localPointsBuffer[index] = transform.InverseTransformPoint(worldPoint);
                lineRenderer.SetPosition(index, worldPoint);
                colliderPoints.Add(localPointsBuffer[index]);
            }

            edgeCollider.SetPoints(colliderPoints);
        }

        private void PruneBakedBefore(int minStep)
        {
            // Collect keys to remove to avoid modifying dictionary during iteration
            var toRemove = new List<int>();

            foreach (var key in bakedHeights.Keys)
            {
                if (key < minStep)
                {
                    toRemove.Add(key);
                }
            }

            for (var i = 0; i < toRemove.Count; i++)
            {
                bakedHeights.Remove(toRemove[i]);
            }
        }
    }
}
