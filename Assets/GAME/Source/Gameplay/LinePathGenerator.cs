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

        [Header("Window")]
        [SerializeField]
        private Camera gameplayCamera;

        [SerializeField, Min(0.1f)]
        private float segmentLength = 0.7f;

        [SerializeField]
        private float behindCameraDistance = 10f;

        [SerializeField]
        private float aheadCameraDistance = 10f;

        [SerializeField, Min(0.1f)]
        private float rebuildDistance = 0.5f;

        [Header("Jagged Path")]
        [SerializeField]
        private float startY = 0f;

        [SerializeField]
        private float minY = -2f;

        [SerializeField]
        private float maxY = 2f;

        [SerializeField, Min(0.05f)]
        private float baseMinVerticalStep = 0.03f;

        [SerializeField, Min(0.05f)]
        private float baseMaxVerticalStep = 0.12f;

        [SerializeField, Min(0f)]
        private float verticalStepIncreasePerMinute = 0.04f;

        [SerializeField, Range(0f, 1f)]
        private float baseTurnChance = 0.03f;

        [SerializeField, Min(0f)]
        private float turnChanceIncreasePerMinute = 0.02f;

        [SerializeField, Range(0f, 1f)]
        private float baseFlatChance = 0.88f;

        [SerializeField, Min(0f)]
        private float flatChanceDecreasePerMinute = 0.06f;

        [SerializeField, Min(1)]
        private int baseMinSegmentRunLength = 10;

        [SerializeField, Min(1)]
        private int baseMaxSegmentRunLength = 28;

        [SerializeField, Min(1)]
        private int baseMinFlatRunLength = 18;

        [SerializeField, Min(1)]
        private int baseMaxFlatRunLength = 40;

        [SerializeField, Range(0f, 1f)]
        private float baseMountainChance = 0.02f;

        [SerializeField, Min(1f)]
        private float mountainStepMultiplier = 1.4f;

        [SerializeField, Min(0)]
        private int maxDifficultyLevel = 8;

        [SerializeField]
        private int randomSeed = 12345;

        [Header("Lifecycle")]
        [SerializeField]
        private bool generateOnEnable = true;

        [Header("Visual")]
        [SerializeField, Min(1)]
        private int clicksPerGradientStep = 25;

        [SerializeField, Min(0.01f)]
        private float gradientTransitionDuration = 0.35f;

        private readonly Vector3[] worldPointsBuffer = new Vector3[2048];
        private readonly Vector2[] localPointsBuffer = new Vector2[2048];
        private readonly Dictionary<int, float> pathPoints = new();
        private float lastStartX;
        private float lastEndX;
        private bool hasWindow;
        private bool isRunActive;
        private float runElapsedSeconds;

        private int minGeneratedStep;
        private int maxGeneratedStep;
        private int forwardDirection;
        private int backwardDirection;
        private int forwardStepsRemaining;
        private int backwardStepsRemaining;
        private float forwardStepDelta;
        private float backwardStepDelta;
        private System.Random forwardRandom;
        private System.Random backwardRandom;

        private int appliedGradientStep;
        private Color gradientTransitionStartColor = Color.white;
        private Color gradientTransitionEndColor = Color.white;
        private float gradientTransitionTime;
        private bool isGradientTransitionRunning;

        private void OnEnable()
        {
            lineRenderer.useWorldSpace = true;
            runSessionController = Object.FindFirstObjectByType<RunSessionController>();
            forwardRandom = new System.Random(randomSeed);
            backwardRandom = new System.Random(unchecked(randomSeed * 486187739 + 1013904223));
            runElapsedSeconds = 0f;
            isRunActive = false;
            forwardDirection = 0;
            backwardDirection = 0;
            forwardStepsRemaining = 0;
            backwardStepsRemaining = 0;
            forwardStepDelta = 0f;
            backwardStepDelta = 0f;
            appliedGradientStep = 0;
            ApplyGradient(Color.white);
            isGradientTransitionRunning = false;
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
            pathPoints.Clear();
            UpdateWindow(force: true);
        }

        public void AlignAndRebuildToPoint(Vector2 anchorPoint)
        {
            ResetPathAroundAnchor(anchorPoint);
            hasWindow = false;
            UpdateWindow(force: true);
        }

        public bool IsAlignedWithPoint(Vector2 point, float tolerance)
        {
            return Mathf.Abs(EvaluateHeightAtX(point.x) - point.y) <= tolerance;
        }

        public float EvaluateHeightAtX(float x)
        {
            EnsureRangeForX(x);

            var leftStep = Mathf.FloorToInt(x / segmentLength);
            var rightStep = leftStep + 1;
            var leftY = pathPoints[leftStep];
            var rightY = pathPoints[rightStep];
            var leftX = StepToX(leftStep);
            var interpolation = Mathf.Clamp01((x - leftX) / segmentLength);
            return Mathf.Lerp(leftY, rightY, interpolation);
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
                appliedGradientStep = 0;
                StartGradientTransition(Color.white);
                return;
            }

            var gradientStep = score / clicksPerGradientStep;
            if (gradientStep <= appliedGradientStep)
            {
                return;
            }

            appliedGradientStep = gradientStep;
            StartGradientTransition(GenerateRandomGradientColor());
        }

        private void LateUpdate()
        {
            if (isRunActive)
            {
                runElapsedSeconds += Time.deltaTime;
            }

            UpdateWindow(force: false);
            UpdateGradientTransition();
        }

        private void OnRunStarted()
        {
            runElapsedSeconds = 0f;
            isRunActive = true;
        }

        private void OnRunFinished()
        {
            isRunActive = false;
        }

        private void UpdateWindow(bool force)
        {
            var cameraX = gameplayCamera.transform.position.x;
            var desiredStartX = cameraX - behindCameraDistance;
            var desiredEndX = cameraX + aheadCameraDistance;

            var startX = Mathf.Floor(desiredStartX / segmentLength) * segmentLength;
            var endX = Mathf.Ceil(desiredEndX / segmentLength) * segmentLength;

            if (!force && hasWindow)
            {
                var movedStart = Mathf.Abs(startX - lastStartX);
                var movedEnd = Mathf.Abs(endX - lastEndX);
                if (movedStart < rebuildDistance && movedEnd < rebuildDistance)
                {
                    return;
                }
            }

            EnsureRange(startX, endX);
            BuildWindow(startX, endX);
            lastStartX = startX;
            lastEndX = endX;
            hasWindow = true;
        }

        private void ResetPathAroundAnchor(Vector2 anchorPoint)
        {
            pathPoints.Clear();

            var anchorStep = Mathf.RoundToInt(anchorPoint.x / segmentLength);
            pathPoints[anchorStep] = anchorPoint.y;
            minGeneratedStep = anchorStep;
            maxGeneratedStep = anchorStep;
            forwardDirection = 0;
            backwardDirection = 0;
            forwardStepsRemaining = 0;
            backwardStepsRemaining = 0;
            forwardStepDelta = 0f;
            backwardStepDelta = 0f;
            forwardRandom = new System.Random(randomSeed);
            backwardRandom = new System.Random(unchecked(randomSeed * 486187739 + 1013904223));
        }

        private void EnsureRange(float startX, float endX)
        {
            var startStep = Mathf.FloorToInt(startX / segmentLength);
            var endStep = Mathf.CeilToInt(endX / segmentLength);
            EnsureRange(startStep, endStep);
        }

        private void EnsureRangeForX(float x)
        {
            var leftStep = Mathf.FloorToInt(x / segmentLength);
            EnsureRange(leftStep, leftStep + 1);
        }

        private void EnsureRange(int startStep, int endStep)
        {
            if (pathPoints.Count == 0)
            {
                InitializePath(startStep, endStep);
            }

            while (maxGeneratedStep < endStep)
            {
                var currentY = pathPoints[maxGeneratedStep];
                var nextY = GenerateNextY(
                    currentY,
                    ref forwardDirection,
                    ref forwardStepsRemaining,
                    ref forwardStepDelta,
                    forwardRandom);
                maxGeneratedStep += 1;
                pathPoints[maxGeneratedStep] = nextY;
            }

            while (minGeneratedStep > startStep)
            {
                var currentY = pathPoints[minGeneratedStep];
                var nextY = GenerateNextY(
                    currentY,
                    ref backwardDirection,
                    ref backwardStepsRemaining,
                    ref backwardStepDelta,
                    backwardRandom);
                minGeneratedStep -= 1;
                pathPoints[minGeneratedStep] = nextY;
            }
        }

        private void InitializePath(int startStep, int endStep)
        {
            var centerStep = Mathf.RoundToInt(((startStep + endStep) * 0.5f));
            var clampedStartY = Mathf.Clamp(startY, minY, maxY);
            pathPoints[centerStep] = clampedStartY;
            minGeneratedStep = centerStep;
            maxGeneratedStep = centerStep;
        }

        private float GenerateNextY(
            float currentY,
            ref int direction,
            ref int stepsRemaining,
            ref float stepDelta,
            System.Random random)
        {
            var difficultyLevel = GetDifficultyLevel();
            var maxDelta = Mathf.Min(baseMaxVerticalStep, baseMinVerticalStep + verticalStepIncreasePerMinute * difficultyLevel);
            var minDelta = Mathf.Min(maxDelta, baseMinVerticalStep + verticalStepIncreasePerMinute * 0.5f * difficultyLevel);
            var turnChance = Mathf.Clamp01(baseTurnChance + turnChanceIncreasePerMinute * difficultyLevel);
            var flatChance = Mathf.Clamp01(baseFlatChance - flatChanceDecreasePerMinute * difficultyLevel);
            var mountainChance = Mathf.Clamp01(baseMountainChance + 0.02f * difficultyLevel);
            var minRunLength = Mathf.Max(1, baseMinSegmentRunLength - difficultyLevel / 2);
            var maxRunLength = Mathf.Max(minRunLength, baseMaxSegmentRunLength - difficultyLevel);
            var minFlatRunLength = Mathf.Max(1, baseMinFlatRunLength - difficultyLevel / 3);
            var maxFlatRunLength = Mathf.Max(minFlatRunLength, baseMaxFlatRunLength - difficultyLevel / 2);

            if (stepsRemaining <= 0)
            {
                var roll = (float)random.NextDouble();
                if (roll < flatChance)
                {
                    direction = 0;
                    stepsRemaining = random.Next(minFlatRunLength, maxFlatRunLength + 1);
                    stepDelta = 0f;
                }
                else
                {
                    if (direction == 0)
                    {
                        direction = random.NextDouble() < 0.5d ? -1 : 1;
                    }
                    else if (random.NextDouble() < turnChance)
                    {
                        direction = -direction;
                    }

                    stepsRemaining = random.Next(minRunLength, maxRunLength + 1);
                    stepDelta = SelectSegmentStepDelta(minDelta, maxDelta, mountainChance, random);
                }
            }

            stepsRemaining = Mathf.Max(0, stepsRemaining - 1);

            if (direction == 0)
            {
                return currentY;
            }

            var nextY = currentY + direction * stepDelta;
            if (nextY > maxY)
            {
                nextY = maxY;
                direction = -1;
                stepsRemaining = 0;
            }
            else if (nextY < minY)
            {
                nextY = minY;
                direction = 1;
                stepsRemaining = 0;
            }

            return nextY;
        }

        private float SelectSegmentStepDelta(float minDelta, float maxDelta, float mountainChance, System.Random random)
        {
            var deltaRange = Mathf.Max(0.001f, maxDelta - minDelta);
            if (random.NextDouble() < mountainChance)
            {
                // Mountain segments: steeper but still straight.
                var steepMin = minDelta + deltaRange * 0.65f;
                var steepMax = maxDelta * mountainStepMultiplier;
                return Mathf.Lerp(steepMin, steepMax, (float)random.NextDouble());
            }

            // Normal segments: pick from a few discrete slopes for clean straight visuals.
            var discreteLevel = random.Next(0, 3); // 0..2
            var t = discreteLevel / 2f;
            return Mathf.Lerp(minDelta, minDelta + deltaRange * 0.75f, t);
        }

        private int GetDifficultyLevel()
        {
            if (!isRunActive)
            {
                return 0;
            }

            var rawLevel = Mathf.FloorToInt(runElapsedSeconds / 60f);
            return Mathf.Clamp(rawLevel, 0, maxDifficultyLevel);
        }

        private void BuildWindow(float startX, float endX)
        {
            var startStep = Mathf.FloorToInt(startX / segmentLength);
            var endStep = Mathf.CeilToInt(endX / segmentLength);
            var rawPointsCount = endStep - startStep + 1;
            var pointsCount = Mathf.Clamp(rawPointsCount, 2, worldPointsBuffer.Length);
            lineRenderer.positionCount = pointsCount;
            var colliderPoints = new List<Vector2>(pointsCount);

            for (var index = 0; index < pointsCount; index++)
            {
                var step = startStep + index;
                var x = StepToX(step);
                var y = pathPoints[step];

                var worldPoint = new Vector3(x, y, 0f);
                worldPointsBuffer[index] = worldPoint;
                localPointsBuffer[index] = transform.InverseTransformPoint(worldPoint);
                lineRenderer.SetPosition(index, worldPoint);
                colliderPoints.Add(localPointsBuffer[index]);
            }

            edgeCollider.SetPoints(colliderPoints);
        }

        private float StepToX(int step)
        {
            return step * segmentLength;
        }

        private void StartGradientTransition(Color targetColor)
        {
            gradientTransitionStartColor = GetCurrentGradientEndColor();
            gradientTransitionEndColor = targetColor;
            gradientTransitionTime = 0f;
            isGradientTransitionRunning = true;
        }

        private void UpdateGradientTransition()
        {
            if (!isGradientTransitionRunning)
            {
                return;
            }

            gradientTransitionTime += Time.deltaTime;
            var progress = Mathf.Clamp01(gradientTransitionTime / gradientTransitionDuration);
            var currentEndColor = Color.Lerp(gradientTransitionStartColor, gradientTransitionEndColor, progress);
            ApplyGradient(currentEndColor);

            if (progress >= 1f)
            {
                isGradientTransitionRunning = false;
            }
        }

        private Color GetCurrentGradientEndColor()
        {
            var gradient = lineRenderer.colorGradient;
            var colorKeys = gradient.colorKeys;
            if (colorKeys.Length == 0)
            {
                return Color.white;
            }

            return colorKeys[colorKeys.Length - 1].color;
        }

        private void ApplyGradient(Color endColor)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(endColor, 1f),
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f),
                });

            lineRenderer.colorGradient = gradient;
        }

        private static Color GenerateRandomGradientColor()
        {
            return Random.ColorHSV(
                0f,
                1f,
                0.65f,
                1f,
                0.75f,
                1f,
                1f,
                1f);
        }
    }
}
