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
        private float extraMargin = 5f;

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

        [SerializeField, Tooltip("Use the serialized seed instead of a random one, so a run can be reproduced exactly.")]
        private bool useFixedSeed;

        // The serialized field used to be overwritten in OnEnable, which dirtied the scene on every
        // play and made a specific layout impossible to reproduce — even though the whole generator
        // is deterministic in the seed by design.
        private int activeSeed;

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

        // Reused by ForceFlatAhead and PruneBakedBefore — both run from per-frame paths.
        private readonly List<int> stepRemovalBuffer = new(64);

        // EdgeCollider2D.SetPoints takes a List, so the window rebuild used to allocate one every
        // time even though worldPointsBuffer/localPointsBuffer already exist to avoid exactly that.
        private readonly List<Vector2> colliderPointsBuffer = new(2048);

        // Guards ForceFlatAhead against redundant per-frame rebuilds. int.MinValue means "no flat
        // zone applied yet"; reset whenever generation state is thrown away.
        private int lastForcedFlatStep = int.MinValue;
        private int lastForcedFlatCount = -1;
        private float activeSegmentLength;
        private bool lineHiddenByTheme;
        private bool hideForRebuild;

        // tan values for angle types
        private const float Tan15 = 0.2679f;
        private const float Tan30 = 0.5774f;
        private const float Tan45 = 1f;

        // Score thresholds for pattern-pool selection, read from DifficultyManager every evaluation
        // so geometry difficulty and pacing phases can not drift apart. This used to be a static
        // array with a comment claiming it was aligned with DifficultyManager — by then the two had
        // already diverged (60/105/180 here against 30/75/150 in the scene), and nothing reported it.
        // Reused buffer: this runs from LateUpdate and must not allocate.
        private readonly int[] levelThresholds = new int[5];

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

        private float CameraHalfWidth => gameplayCamera.orthographicSize * gameplayCamera.aspect;

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
            activeSeed = useFixedSeed ? randomSeed : Random.Range(0, int.MaxValue);
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
            hideForRebuild = true;
            lineRenderer.enabled = false;
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

            // Callers drive this from Update for as long as a flat zone is active, but the result
            // only changes once the player crosses into a new step — about twice a second at normal
            // speed. Running it per frame rebuilt the window and re-uploaded the EdgeCollider2D
            // geometry ~60 times a second, for 7 seconds at the start of every single run.
            if (startStep == lastForcedFlatStep && segmentCount == lastForcedFlatCount)
            {
                return;
            }

            lastForcedFlatStep = startStep;
            lastForcedFlatCount = segmentCount;

            var currentY = EvaluateHeightAtX(fromX);

            var minStepsToEdge = Mathf.CeilToInt((CameraHalfWidth + extraMargin) / seg) + 2;
            segmentCount = Mathf.Max(segmentCount, minStepsToEdge);

            for (var i = 0; i < segmentCount; i++)
            {
                bakedHeights[startStep + i] = currentY;
            }

            // Remove cached points beyond the flat zone so they regenerate from the flat height
            var lastFlatStep = startStep + segmentCount - 1;
            stepRemovalBuffer.Clear();

            foreach (var key in bakedHeights.Keys)
            {
                if (key > lastFlatStep)
                {
                    stepRemovalBuffer.Add(key);
                }
            }

            for (var i = 0; i < stepRemovalBuffer.Count; i++)
            {
                bakedHeights.Remove(stepRemovalBuffer[i]);
            }

            stepRemovalBuffer.Clear();

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
            if (hideForRebuild)
            {
                lineRenderer.enabled = false;
                hideForRebuild = false;
                return;
            }

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
            hideForRebuild = true;
            lineRenderer.enabled = false;
            isRunActive = true;
            var t = segmentByDifficulty.Evaluate(0f);
            activeSegmentLength = Mathf.Lerp(baseSegmentLength, minSegmentLength, t);

            var seg = activeSegmentLength;
            var playerX = gameplayCamera.transform.position.x;
            var playerStep = Mathf.RoundToInt(playerX / seg);
            var flatY = bakedHeights.TryGetValue(playerStep, out var cachedY) ? cachedY : yOffset;

            bakedHeights.Clear();

            // A fresh run throws the baked field away, so any previously applied flat zone is gone.
            lastForcedFlatStep = int.MinValue;
            lastForcedFlatCount = -1;

            var behindSteps = Mathf.CeilToInt((CameraHalfWidth + extraMargin) / seg) + 2;
            var aheadSteps = Mathf.CeilToInt((CameraHalfWidth + extraMargin) / seg) + 2;
            var minFlatSteps = Mathf.Max(startFlatSegments, aheadSteps);
            var flatEnd = playerStep + minFlatSteps;

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
            lastForcedFlatStep = int.MinValue;
            lastForcedFlatCount = -1;
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
            return ((step * 73856093) ^ (activeSeed * 19349663)) & 0x7FFFFFFF;
        }

        private void RefreshLevelThresholds()
        {
            levelThresholds[0] = 0;
            levelThresholds[1] = difficultyManager.TutorialEndScore;
            levelThresholds[2] = difficultyManager.RhythmPhaseScore;
            levelThresholds[3] = difficultyManager.ChaosPhaseScore;
            levelThresholds[4] = difficultyManager.MasteryPhaseScore;
        }

        private float GetDifficultyLevelFloat()
        {
            if (!isRunActive) return 0f;

            // Same result the old code produced for a missing manager: score 0 lands on level 0.
            if (difficultyManager == null)
            {
                return 0f;
            }

            var score = difficultyManager.CurrentScore;

            RefreshLevelThresholds();

            for (var i = levelThresholds.Length - 1; i >= 1; i--)
            {
                if (score >= levelThresholds[i])
                {
                    return i;
                }

                if (score >= levelThresholds[i - 1])
                {
                    var rangeStart = levelThresholds[i - 1];
                    var rangeEnd = levelThresholds[i];
                    var range = rangeEnd - rangeStart;

                    // Thresholds are authored in the inspector and neighbours may be equal, which
                    // would divide by zero here. An empty band is simply fully crossed.
                    var t = range > 0 ? (float)(score - rangeStart) / range : 1f;
                    return (i - 1) + t;
                }
            }

            return 0f;
        }

        private float GenerateFromPattern(int step, float seg, float prevY,
            ref float[] pattern, ref int patternPos, ref float prevSlope)
        {
            // Pick new pattern if current is exhausted
            if (pattern == null || patternPos >= pattern.Length)
            {
                var levelFloat = GetDifficultyLevelFloat();
                var pool = PickBlendedPool(levelFloat, step);
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

        private float[][] PickBlendedPool(float levelFloat, int step)
        {
            var levelLow = Mathf.FloorToInt(levelFloat);
            var levelHigh = Mathf.Min(levelLow + 1, PatternsByLevel.Length - 1);
            var blend = levelFloat - levelLow;

            if (blend < 0.01f || levelLow == levelHigh)
            {
                return PatternsByLevel[levelLow];
            }

            // Use step hash to deterministically pick which pool
            var hash = StepHash(step + 7919);
            var roll = (hash % 1000) / 1000f;
            return roll < blend ? PatternsByLevel[levelHigh] : PatternsByLevel[levelLow];
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
            var halfWidth = CameraHalfWidth + extraMargin;
            var desiredStartX = cameraX - halfWidth;
            var desiredEndX = cameraX + halfWidth;

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
            colliderPointsBuffer.Clear();

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
                colliderPointsBuffer.Add(localPointsBuffer[index]);
            }

            edgeCollider.SetPoints(colliderPointsBuffer);
        }

        private void PruneBakedBefore(int minStep)
        {
            stepRemovalBuffer.Clear();

            foreach (var key in bakedHeights.Keys)
            {
                if (key < minStep)
                {
                    stepRemovalBuffer.Add(key);
                }
            }

            for (var i = 0; i < stepRemovalBuffer.Count; i++)
            {
                bakedHeights.Remove(stepRemovalBuffer[i]);
            }

            stepRemovalBuffer.Clear();
        }
    }
}
