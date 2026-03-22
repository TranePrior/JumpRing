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

        [Header("Path")]
        [SerializeField, Min(2)]
        private int pointsCount = 20;

        [SerializeField, Min(0.1f)]
        private float segmentLength = 1f;

        [SerializeField]
        private float startX = -6f;

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

        private readonly Vector3[] worldPointsBuffer = new Vector3[256];
        private readonly Vector2[] localPointsBuffer = new Vector2[256];

        private void OnEnable()
        {
            lineRenderer.useWorldSpace = true;
            runSessionController.RunStarted += OnRunStarted;

            if (generateOnEnable)
            {
                Generate();
            }
        }

        private void OnDisable()
        {
            runSessionController.RunStarted -= OnRunStarted;
        }

        [ContextMenu("Generate Line")]
        public void Generate()
        {
            var clampedCount = Mathf.Clamp(pointsCount, 2, worldPointsBuffer.Length);
            lineRenderer.positionCount = clampedCount;
            var colliderPoints = new System.Collections.Generic.List<Vector2>(clampedCount);
            var phase = randomizePhase ? Random.Range(0f, Mathf.PI * 2f) : 0f;

            for (var index = 0; index < clampedCount; index++)
            {
                var x = startX + index * segmentLength;
                var y = baseY + Mathf.Sin((x * waveFrequency) + phase) * waveAmplitude;

                var worldPoint = new Vector3(x, y, 0f);
                worldPointsBuffer[index] = worldPoint;
                localPointsBuffer[index] = transform.InverseTransformPoint(worldPoint);
                lineRenderer.SetPosition(index, worldPoint);
                colliderPoints.Add(localPointsBuffer[index]);
            }

            edgeCollider.SetPoints(colliderPoints);
        }

        private void OnRunStarted()
        {
            Generate();
        }
    }
}
