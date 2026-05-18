using System.Collections.Generic;
using UnityEngine;

namespace JumpRing.Game.Gameplay
{
    [DefaultExecutionOrder(100)]
    public sealed class LineCornerRenderer : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField]
        private LinePathGenerator linePathGenerator;

        [SerializeField]
        private Camera gameplayCamera;

        [Header("Spawn")]
        [SerializeField]
        private int sortingOrder;

        [SerializeField]
        private string sortingLayerName = "Line";

        private Sprite cornerSprite;
        private readonly List<SpriteRenderer> activeCorners = new(64);
        private readonly Queue<SpriteRenderer> pool = new(32);
        private bool isActive;
        private int lastPositionCount;
        private float lastCameraX;

        private void OnEnable()
        {
            if (linePathGenerator == null)
                linePathGenerator = Object.FindFirstObjectByType<LinePathGenerator>();

            if (gameplayCamera == null)
                gameplayCamera = Camera.main;
        }

        public void Configure(Sprite sprite)
        {
            cornerSprite = sprite;

            foreach (var corner in activeCorners)
                corner.sprite = cornerSprite;
        }

        public void Activate()
        {
            if (linePathGenerator == null)
                linePathGenerator = Object.FindFirstObjectByType<LinePathGenerator>();

            if (gameplayCamera == null)
                gameplayCamera = Camera.main;

            isActive = true;
            lastPositionCount = -1;
            lastCameraX = float.MinValue;
        }

        public void Deactivate()
        {
            isActive = false;

            foreach (var corner in activeCorners)
                ReturnToPool(corner);

            activeCorners.Clear();
            lastPositionCount = -1;
            lastCameraX = float.MinValue;
        }

        private void LateUpdate()
        {
            if (!isActive || cornerSprite == null || gameplayCamera == null || linePathGenerator == null)
                return;

            var lr = linePathGenerator.LineRenderer;
            if (lr == null || lr.positionCount < 3)
                return;

            var cameraX = gameplayCamera.transform.position.x;
            if (lr.positionCount == lastPositionCount && Mathf.Abs(cameraX - lastCameraX) < 0.1f)
            {
                UpdatePositionsFromLineRenderer(lr);
                return;
            }

            RebuildCorners(lr);
            lastPositionCount = lr.positionCount;
            lastCameraX = cameraX;
        }

        private void RebuildCorners(LineRenderer lr)
        {
            // Return all to pool
            foreach (var corner in activeCorners)
                ReturnToPool(corner);
            activeCorners.Clear();

            var count = lr.positionCount;

            for (var i = 1; i < count - 1; i++)
            {
                var prev = lr.GetPosition(i - 1);
                var curr = lr.GetPosition(i);
                var next = lr.GetPosition(i + 1);

                var slopeBefore = curr.y - prev.y;
                var slopeAfter = next.y - curr.y;

                if (Mathf.Abs(slopeBefore - slopeAfter) > 0.01f)
                {
                    var corner = GetFromPool();
                    corner.transform.position = new Vector3(curr.x, curr.y, 0f);
                    activeCorners.Add(corner);
                }
            }
        }

        private void UpdatePositionsFromLineRenderer(LineRenderer lr)
        {
            var count = lr.positionCount;
            var cornerIdx = 0;

            for (var i = 1; i < count - 1 && cornerIdx < activeCorners.Count; i++)
            {
                var prev = lr.GetPosition(i - 1);
                var curr = lr.GetPosition(i);
                var next = lr.GetPosition(i + 1);

                var slopeBefore = curr.y - prev.y;
                var slopeAfter = next.y - curr.y;

                if (Mathf.Abs(slopeBefore - slopeAfter) > 0.01f)
                {
                    var corner = activeCorners[cornerIdx];
                    corner.transform.position = new Vector3(curr.x, curr.y, 0f);
                    cornerIdx++;
                }
            }
        }

        private SpriteRenderer GetFromPool()
        {
            SpriteRenderer sr;

            if (pool.Count > 0)
            {
                sr = pool.Dequeue();
                sr.gameObject.SetActive(true);
            }
            else
            {
                var go = new GameObject("LineCorner");
                go.transform.SetParent(transform);
                sr = go.AddComponent<SpriteRenderer>();
                sr.sortingLayerName = sortingLayerName;
                sr.sortingOrder = sortingOrder;
            }

            sr.sprite = cornerSprite;
            sr.transform.localScale = Vector3.one;
            return sr;
        }

        private void ReturnToPool(SpriteRenderer sr)
        {
            sr.gameObject.SetActive(false);
            pool.Enqueue(sr);
        }
    }
}
