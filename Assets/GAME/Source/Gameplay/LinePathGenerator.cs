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

        [Header("Path")]
        [SerializeField]
        private Camera gameplayCamera;

        [SerializeField, Min(0.1f)]
        private float segmentLength = 1f;

        [SerializeField]
        private float behindCameraDistance = 10f;

        [SerializeField]
        private float aheadCameraDistance = 10f;

        [SerializeField, Min(0.1f)]
        private float rebuildDistance = 0.5f;

        [SerializeField]
        private float baseY = 0f;

        [SerializeField]
        private float waveAmplitude = 1.5f;

        [SerializeField, Min(0.01f)]
        private float waveFrequency = 0.5f;

        [SerializeField]
        private bool randomizePhase = true;

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
        private float currentPhase;
        private float verticalOffsetY;
        private float lastStartX;
        private float lastEndX;
        private bool hasWindow;
        private int appliedGradientStep;
        private Color gradientTransitionStartColor = Color.white;
        private Color gradientTransitionEndColor = Color.white;
        private float gradientTransitionTime;
        private bool isGradientTransitionRunning;

        private void OnEnable()
        {
            lineRenderer.useWorldSpace = true;
            currentPhase = randomizePhase ? Random.Range(0f, Mathf.PI * 2f) : 0f;
            verticalOffsetY = baseY;
            appliedGradientStep = 0;
            ApplyGradient(Color.white);
            isGradientTransitionRunning = false;

            if (generateOnEnable)
            {
                ForceRebuild();
            }
        }

        [ContextMenu("Generate Line")]
        public void ForceRebuild()
        {
            hasWindow = false;
            UpdateWindow(force: true);
        }

        public void AlignAndRebuildToPoint(Vector2 anchorPoint)
        {
            currentPhase = randomizePhase ? Random.Range(0f, Mathf.PI * 2f) : 0f;
            var anchorWaveY = Mathf.Sin((anchorPoint.x * waveFrequency) + currentPhase) * waveAmplitude;
            verticalOffsetY = anchorPoint.y - anchorWaveY;
            ForceRebuildAroundX(anchorPoint.x);
        }

        public bool IsAlignedWithPoint(Vector2 point, float tolerance)
        {
            return Mathf.Abs(EvaluateY(point.x) - point.y) <= tolerance;
        }

        public float EvaluateHeightAtX(float x)
        {
            return EvaluateY(x);
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
            UpdateWindow(force: false);
            UpdateGradientTransition();
        }

        private void ForceRebuildAroundX(float centerX)
        {
            var startX = Mathf.Floor((centerX - behindCameraDistance) / segmentLength) * segmentLength;
            var endX = Mathf.Ceil((centerX + aheadCameraDistance) / segmentLength) * segmentLength;
            BuildWindow(startX, endX);
            lastStartX = startX;
            lastEndX = endX;
            hasWindow = true;
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

            BuildWindow(startX, endX);
            lastStartX = startX;
            lastEndX = endX;
            hasWindow = true;
        }

        private void BuildWindow(float startX, float endX)
        {
            var rawPointsCount = Mathf.FloorToInt((endX - startX) / segmentLength) + 1;
            var pointsCount = Mathf.Clamp(rawPointsCount, 2, worldPointsBuffer.Length);
            lineRenderer.positionCount = pointsCount;
            var colliderPoints = new System.Collections.Generic.List<Vector2>(pointsCount);

            for (var index = 0; index < pointsCount; index++)
            {
                var x = startX + index * segmentLength;
                var y = EvaluateY(x);

                var worldPoint = new Vector3(x, y, 0f);
                worldPointsBuffer[index] = worldPoint;
                localPointsBuffer[index] = transform.InverseTransformPoint(worldPoint);
                lineRenderer.SetPosition(index, worldPoint);
                colliderPoints.Add(localPointsBuffer[index]);
            }

            edgeCollider.SetPoints(colliderPoints);
        }

        private float EvaluateY(float x)
        {
            return verticalOffsetY + Mathf.Sin((x * waveFrequency) + currentPhase) * waveAmplitude;
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
