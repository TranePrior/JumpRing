using System.Collections.Generic;
using UnityEngine;

namespace JumpRing.Game.Gameplay
{
    public sealed class LinePathGenerator : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField]
        private LineRenderer lineRenderer;

        [SerializeField]
        private EdgeCollider2D edgeCollider;

        [SerializeField]
        private RunSessionController runSessionController;

        [SerializeField]
        private DifficultyManager difficultyManager;

        [Header("Window")]
        [SerializeField]
        private Camera gameplayCamera;

        [Header("Segment Length")]
        [SerializeField, Min(0.5f)]
        private float baseSegmentLength = 1.5f;

        [SerializeField, Min(0.5f)]
        private float minSegmentLength = 0.8f;

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

        [SerializeField, Min(0.1f), Tooltip("Max slope delta at lowest difficulty")]
        private float minSlopeDelta = 0.6f;

        [SerializeField, Min(0.1f), Tooltip("Max slope delta at highest difficulty")]
        private float maxSlopeDelta = 2.0f;

        [Header("Line Width")]
        [SerializeField]
        private float baseLineWidth = 0.5f;

        [Header("Start Flat Zone")]
        [SerializeField, Min(0), Tooltip("Flat segments at run start (~baseSpeed * seconds / segmentLength)")]
        private int startFlatSegments = 10;

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
        private float yOffset;

        private int frontierStepForward;
        private float frontierYForward;
        private int frontierStepBackward;
        private float frontierYBackward;

        // Pattern state (separate for forward/backward generation)
        private float[] patternForward;
        private int patternPosForward;
        private float prevSlopeForward;
        private float[] patternBackward;
        private int patternPosBackward;
        private float prevSlopeBackward;

        // Baking system: once a point is generated, it never changes
        private readonly Dictionary<int, float> bakedHeights = new(256);
        private float activeSegmentLength;
        private bool lineHiddenByTheme;

        // tan values for angle types
        private const float Tan15 = 0.2679f;
        private const float Tan30 = 0.5774f;
        private const float Tan45 = 1f;

        // Score thresholds aligned with DifficultyManager phases
        // Tutorial=0, Calm=30, Rhythm=60, Chaos=105, Mastery=180
        private static readonly int[] LevelThresholds = { 0, 30, 60, 105, 180 };

        // Level 0 — Tutorial (score 0–29): very gentle, learning the mechanic
        private static readonly float[][] PatternsLevel0 =
        {
            new[] { 0f, 0f, 0f },
            new[] { 0f, 0f, 0f, 0f },
            new[] { Tan15, Tan15, 0f, 0f },
            new[] { -Tan15, -Tan15, 0f, 0f },
            new[] { Tan15, 0f, -Tan15, 0f },
            new[] { -Tan15, 0f, Tan15, 0f },
        };

        // Level 1 — Calm (score 30–59): Tan15 + gentle Tan30, flat transitions
        private static readonly float[][] PatternsLevel1 =
        {
            new[] { Tan15, Tan15, Tan15, 0f },
            new[] { -Tan15, -Tan15, -Tan15, 0f },
            new[] { Tan30, 0f, 0f, -Tan30 },
            new[] { -Tan30, 0f, 0f, Tan30 },
            new[] { Tan30, Tan30, 0f, -Tan30, -Tan30, 0f },
            new[] { -Tan30, -Tan30, 0f, Tan30, Tan30, 0f },
            new[] { Tan15, Tan15, -Tan15, -Tan15 },
            new[] { -Tan15, -Tan15, Tan15, Tan15 },
            new[] { Tan30, 0f, -Tan15, -Tan15 },
            new[] { -Tan30, 0f, Tan15, Tan15 },
        };

        // Level 2 — Rhythm (score 60–104): full Tan30, Tan45 with flat transitions
        private static readonly float[][] PatternsLevel2 =
        {
            new[] { Tan30, Tan30, Tan30, 0f },
            new[] { -Tan30, -Tan30, -Tan30, 0f },
            new[] { Tan45, 0f, 0f, -Tan45 },
            new[] { -Tan45, 0f, 0f, Tan45 },
            new[] { Tan30, Tan30, -Tan30, -Tan30 },
            new[] { -Tan30, -Tan30, Tan30, Tan30 },
            new[] { Tan45, 0f, -Tan30, -Tan30 },
            new[] { -Tan45, 0f, Tan30, Tan30 },
            new[] { Tan30, Tan30, Tan30, -Tan45, 0f },
            new[] { -Tan30, -Tan30, -Tan30, Tan45, 0f },
        };

        // Level 3 — Chaos (score 105–179): Tan45 zigzags, rare flat breathers
        private static readonly float[][] PatternsLevel3 =
        {
            new[] { Tan45, -Tan45, Tan45, -Tan45 },
            new[] { -Tan45, Tan45, -Tan45, Tan45 },
            new[] { Tan45, Tan45, -Tan45, -Tan45 },
            new[] { -Tan45, -Tan45, Tan45, Tan45 },
            new[] { Tan45, -Tan45, Tan45 },
            new[] { -Tan45, Tan45, -Tan45 },
            new[] { Tan45, Tan45, -Tan30, -Tan30, -Tan30 },
            new[] { -Tan45, -Tan45, Tan30, Tan30, Tan30 },
            new[] { Tan45, 0f, -Tan45, 0f },
            new[] { -Tan45, 0f, Tan45, 0f },
            new[] { Tan30, Tan30, Tan30, -Tan45, 0f },
            new[] { -Tan30, -Tan30, -Tan30, Tan45, 0f },
        };

        // Level 4 — Mastery (score 180+): hardcore, rapid 45° direction changes
        private static readonly float[][] PatternsLevel4 =
        {
            new[] { Tan45, -Tan45, Tan45, -Tan45 },
            new[] { -Tan45, Tan45, -Tan45, Tan45 },
            new[] { Tan45, -Tan45, Tan45 },
            new[] { -Tan45, Tan45, -Tan45 },
            new[] { Tan45, -Tan45, Tan45, -Tan45, Tan45, -Tan45 },
            new[] { -Tan45, Tan45, -Tan45, Tan45, -Tan45, Tan45 },
            new[] { Tan45, Tan45, Tan45, -Tan45, -Tan45, -Tan45 },
            new[] { -Tan45, -Tan45, -Tan45, Tan45, Tan45, Tan45 },
            new[] { Tan45, -Tan30, Tan45, -Tan30, Tan45 },
            new[] { -Tan45, Tan30, -Tan45, Tan30, -Tan45 },
            new[] { Tan45, Tan45, -Tan45, -Tan45 },
            new[] { -Tan45, -Tan45, Tan45, Tan45 },
            new[] { Tan45, -Tan45, Tan30, -Tan45, Tan45 },
            new[] { -Tan45, Tan45, -Tan30, Tan45, -Tan45 },
        };

        // All pools indexed by level
        private static readonly float[][][] PatternsByLevel =
        {
            PatternsLevel0,
            PatternsLevel1,
            PatternsLevel2,
            PatternsLevel3,
            PatternsLevel4,
        };

        public LineRenderer LineRenderer => lineRenderer;

        public void SetLineMaterial(Material material)
        {
            if (lineRenderer != null && material != null)
            {
                lineRenderer.material = material;
            }
        }

        public void SetLineVisible(bool visible)
        {
            lineHiddenByTheme = !visible;
        }
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

            ResetFrontier();
            isRunActive = false;
            randomSeed = Random.Range(0, int.MaxValue);
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
            ResetFrontier();
            hasWindow = false;
            UpdateWindow(force: true);
        }

        public float EvaluateHeightAtX(float x)
        {
            var seg = ActiveSegmentLength;
            var stepFloat = x / seg;
            var stepLow = Mathf.FloorToInt(stepFloat);
            var stepHigh = stepLow + 1;

            var yLow = BakeOrGetHeight(stepLow, seg);
            var yHigh = BakeOrGetHeight(stepHigh, seg);

            var t = stepFloat - stepLow;
            return Mathf.Lerp(yLow, yHigh, t);
        }

        /// <summary>
        /// Forces the line to be flat for a number of segments ahead of the given X position.
        /// Used by the CalmLine bonus.
        /// </summary>
        public void ForceFlatAhead(float fromX, int segmentCount)
        {
            var seg = ActiveSegmentLength;
            var startStep = Mathf.RoundToInt(fromX / seg) + 1;
            var currentY = EvaluateHeightAtX(fromX);

            for (var i = 0; i < segmentCount; i++)
            {
                bakedHeights[startStep + i] = currentY;
            }

            // Remove cached points beyond the flat zone so they regenerate from the flat height
            var lastFlatStep = startStep + segmentCount - 1;
            var toRemove = new List<int>();

            foreach (var key in bakedHeights.Keys)
            {
                if (key > lastFlatStep)
                {
                    toRemove.Add(key);
                }
            }

            for (var i = 0; i < toRemove.Count; i++)
            {
                bakedHeights.Remove(toRemove[i]);
            }

            // Update forward frontier so future generation continues from flat
            frontierStepForward = lastFlatStep;
            frontierYForward = currentY;
            patternForward = null;
            patternPosForward = 0;
            prevSlopeForward = 0f;

            UpdateWindow(force: true);
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
            lineRenderer.enabled = !lineHiddenByTheme;
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

            var seg = activeSegmentLength;
            var playerX = gameplayCamera.transform.position.x;
            var playerStep = Mathf.RoundToInt(playerX / seg);
            var flatY = bakedHeights.TryGetValue(playerStep, out var cachedY) ? cachedY : yOffset;

            bakedHeights.Clear();

            var behindSteps = Mathf.CeilToInt(behindCameraDistance / seg) + 2;
            var flatEnd = playerStep + startFlatSegments;

            for (var s = playerStep - behindSteps; s <= flatEnd; s++)
            {
                bakedHeights[s] = flatY;
            }

            frontierStepForward = flatEnd;
            frontierYForward = flatY;
            frontierStepBackward = playerStep - behindSteps;
            frontierYBackward = flatY;
            patternForward = null;
            patternPosForward = 0;
            prevSlopeForward = 0f;
            patternBackward = null;
            patternPosBackward = 0;
            prevSlopeBackward = 0f;

            hasWindow = false;
            UpdateWindow(force: true);
        }

        private void OnRunFinished()
        {
            isRunActive = false;
        }

        private void ResetFrontier()
        {
            frontierStepForward = int.MinValue;
            frontierYForward = yOffset;
            frontierStepBackward = int.MaxValue;
            frontierYBackward = yOffset;
            patternForward = null;
            patternPosForward = 0;
            prevSlopeForward = 0f;
            patternBackward = null;
            patternPosBackward = 0;
            prevSlopeBackward = 0f;
        }

        private int StepHash(int step)
        {
            return ((step * 73856093) ^ (randomSeed * 19349663)) & 0x7FFFFFFF;
        }

        private int GetDifficultyLevel()
        {
            if (!isRunActive) return 0;

            var score = difficultyManager != null ? difficultyManager.CurrentScore : 0;
            for (var i = LevelThresholds.Length - 1; i >= 0; i--)
            {
                if (score >= LevelThresholds[i]) return i;
            }

            return 0;
        }

        private float GenerateFromPattern(int step, float seg, float prevY,
            ref float[] pattern, ref int patternPos, ref float prevSlope)
        {
            // Pick new pattern if current is exhausted
            if (pattern == null || patternPos >= pattern.Length)
            {
                var level = GetDifficultyLevel();
                var pool = PatternsByLevel[level];
                var hash = StepHash(step);

                // Fix #2: near bounds — pick pattern starting in the safe direction
                var headroom = maxY - prevY;
                var footroom = prevY - minY;
                var maxStep = Tan45 * seg;

                if (headroom < maxStep || footroom < maxStep)
                {
                    var needDescending = headroom < maxStep;
                    pattern = PickBoundSafePattern(pool, hash, needDescending);
                }
                else
                {
                    pattern = pool[hash % pool.Length];
                }

                patternPos = 0;
            }

            var slope = pattern[patternPos];
            patternPos++;

            // Fix #1: limit slope change — scales with difficulty
            var currentMaxDelta = Mathf.Lerp(minSlopeDelta, maxSlopeDelta, CurrentDifficulty);
            var slopeDelta = slope - prevSlope;
            if (Mathf.Abs(slopeDelta) > currentMaxDelta)
            {
                slope = prevSlope + Mathf.Sign(slopeDelta) * currentMaxDelta;
            }

            var dy = slope * seg;

            // Soft boundary: clamp if still out of bounds after delta limiting
            if (prevY + dy > maxY || prevY + dy < minY)
            {
                dy = Mathf.Clamp(prevY + dy, minY, maxY) - prevY;
                slope = seg > 0.001f ? dy / seg : 0f;
            }

            prevSlope = slope;
            return prevY + dy;
        }

        private static float[] PickBoundSafePattern(float[][] pool, int hash, bool needDescending)
        {
            var startIndex = hash % pool.Length;

            for (var i = 0; i < pool.Length; i++)
            {
                var candidate = pool[(startIndex + i) % pool.Length];
                var firstSlope = candidate[0];

                if (needDescending ? firstSlope <= 0f : firstSlope >= 0f)
                {
                    return candidate;
                }
            }

            return pool[startIndex];
        }

        private float BakeOrGetHeight(int step, float seg)
        {
            if (!isRunActive)
            {
                bakedHeights[step] = yOffset;
                return yOffset;
            }

            if (bakedHeights.TryGetValue(step, out var cachedY))
            {
                return cachedY;
            }

            // Initialize frontiers if needed
            if (frontierStepForward == int.MinValue)
            {
                frontierStepForward = step - 1;
                frontierYForward = yOffset;
                frontierStepBackward = step - 1;
                frontierYBackward = yOffset;
            }

            // Generate forward if step is ahead of forward frontier
            if (step > frontierStepForward)
            {
                for (var s = frontierStepForward + 1; s <= step; s++)
                {
                    if (!bakedHeights.TryGetValue(s, out var y))
                    {
                        y = GenerateFromPattern(s, seg, frontierYForward,
                            ref patternForward, ref patternPosForward, ref prevSlopeForward);
                        bakedHeights[s] = y;
                    }

                    frontierYForward = y;
                    frontierStepForward = s;
                }
            }

            // Generate backward if step is behind backward frontier
            if (step < frontierStepBackward)
            {
                for (var s = frontierStepBackward - 1; s >= step; s--)
                {
                    if (!bakedHeights.TryGetValue(s, out var y))
                    {
                        y = GenerateFromPattern(s, seg, frontierYBackward,
                            ref patternBackward, ref patternPosBackward, ref prevSlopeBackward);
                        bakedHeights[s] = y;
                    }

                    frontierYBackward = y;
                    frontierStepBackward = s;
                }
            }

            // Fallback: step was pruned but sits between frontiers — regenerate it
            if (!bakedHeights.TryGetValue(step, out var result))
            {
                result = GenerateFromPattern(step, seg, frontierYForward,
                    ref patternForward, ref patternPosForward, ref prevSlopeForward);
                bakedHeights[step] = result;
            }

            return result;
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
