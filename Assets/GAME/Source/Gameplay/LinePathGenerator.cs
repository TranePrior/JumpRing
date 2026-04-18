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

        [SerializeField]
        private MicroEventSystem microEventSystem;

        [Header("Window")]
        [SerializeField]
        private Camera gameplayCamera;

        [Header("Segment Length")]
        [SerializeField, Min(0.5f)]
        private float baseSegmentLength = 0.8f;

        [SerializeField, Min(0.5f)]
        private float minSegmentLength = 0.5f;

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
        private float baseLineWidth = 0.38f;

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

        private int frontierStepForward;
        private float frontierYForward;
        private int frontierStepBackward;
        private float frontierYBackward;

        // Pattern state (separate for forward/backward generation)
        private float[] patternForward;
        private int patternPosForward;
        private float[] patternBackward;
        private int patternPosBackward;

        // Baking system: once a point is generated, it never changes
        private readonly Dictionary<int, float> bakedHeights = new(256);
        private float activeSegmentLength;
        private bool lineHiddenByTheme;

        // tan values for angle types
        private const float Tan30 = 0.5774f;
        private const float Tan45 = 1f;
        private const float Tan60 = 1.7321f;

        // Predefined patterns: each value = tan(angle) * direction
        // Positive = up, negative = down, 0 = flat
        private static readonly float[][] Patterns =
        {
            // Staircase up (3 steps 45°)
            new[] { Tan45, Tan45, Tan45 },
            // Staircase down
            new[] { -Tan45, -Tan45, -Tan45 },
            // Peak Λ
            new[] { Tan45, Tan45, -Tan45, -Tan45 },
            // Valley V
            new[] { -Tan45, -Tan45, Tan45, Tan45 },
            // Zigzag fast
            new[] { Tan45, -Tan45, Tan45, -Tan45 },
            // Shelf high: climb, rest, descend
            new[] { Tan45, Tan45, 0f, 0f, -Tan45 },
            // Shelf low: descend, rest, climb
            new[] { -Tan45, -Tan45, 0f, 0f, Tan45 },
            // Spike up (60° sharp)
            new[] { Tan60, -Tan60 },
            // Spike down (60° sharp)
            new[] { -Tan60, Tan60 },
            // Gentle rise (30°)
            new[] { Tan30, Tan30, Tan30, 0f },
            // Gentle descent (30°)
            new[] { -Tan30, -Tan30, -Tan30, 0f },
            // Flat rest (short breather)
            new[] { 0f, 0f },
            // W shape
            new[] { -Tan45, Tan45, -Tan45, Tan45 },
            // Long staircase up (30°)
            new[] { Tan30, Tan30, Tan30, Tan30, Tan30 },
            // Long staircase down (30°)
            new[] { -Tan30, -Tan30, -Tan30, -Tan30, -Tan30 },
            // Bump: up-flat-down
            new[] { Tan60, 0f, -Tan60 },
            // Dip: down-flat-up
            new[] { -Tan60, 0f, Tan60 },
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

            if (microEventSystem == null)
            {
                microEventSystem = Object.FindFirstObjectByType<MicroEventSystem>();
            }

            ResetFrontier();
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
            ResetFrontier();
            hasWindow = false;
            UpdateWindow(force: true);
        }

        public void AlignAndRebuildToPoint(Vector2 anchorPoint)
        {
            runStartX = anchorPoint.x;
            randomSeed = Random.Range(0, int.MaxValue);
            yOffset = anchorPoint.y;
            bakedHeights.Clear();

            // Pin anchor and a few surrounding steps to anchorPoint.y (flat safe zone)
            var seg = ActiveSegmentLength;
            var anchorStep = Mathf.RoundToInt(anchorPoint.x / seg);
            const int safeRadius = 3;
            for (var s = anchorStep - safeRadius; s <= anchorStep + safeRadius; s++)
            {
                bakedHeights[s] = anchorPoint.y;
            }

            frontierStepForward = anchorStep + safeRadius;
            frontierYForward = anchorPoint.y;
            frontierStepBackward = anchorStep - safeRadius;
            frontierYBackward = anchorPoint.y;
            patternForward = null;
            patternPosForward = 0;
            patternBackward = null;
            patternPosBackward = 0;

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
            return BakeOrGetHeight(step, seg);
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
            if (lineHiddenByTheme)
            {
                lineRenderer.enabled = false;
                return;
            }

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
        }

        private void OnRunFinished()
        {
            isRunActive = false;
            bakedHeights.Clear();
            ResetFrontier();
        }

        private void ResetFrontier()
        {
            frontierStepForward = int.MinValue;
            frontierYForward = yOffset;
            frontierStepBackward = int.MaxValue;
            frontierYBackward = yOffset;
            patternForward = null;
            patternPosForward = 0;
            patternBackward = null;
            patternPosBackward = 0;
        }

        private int StepHash(int step)
        {
            return ((step * 73856093) ^ (randomSeed * 19349663)) & 0x7FFFFFFF;
        }

        private float GenerateFromPattern(int step, float seg, float prevY,
            ref float[] pattern, ref int patternPos)
        {
            // Pick new pattern if current is exhausted
            if (pattern == null || patternPos >= pattern.Length)
            {
                var hash = StepHash(step);
                pattern = Patterns[hash % Patterns.Length];
                patternPos = 0;
            }

            var dy = pattern[patternPos] * seg;
            patternPos++;

            // If result goes out of bounds, flip direction
            if (prevY + dy > maxY || prevY + dy < minY)
            {
                dy = -dy;
            }

            return Mathf.Clamp(prevY + dy, minY, maxY);
        }

        private float BakeOrGetHeight(int step, float seg)
        {
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
                            ref patternForward, ref patternPosForward);
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
                            ref patternBackward, ref patternPosBackward);
                        bakedHeights[s] = y;
                    }

                    frontierYBackward = y;
                    frontierStepBackward = s;
                }
            }

            return bakedHeights[step];
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
