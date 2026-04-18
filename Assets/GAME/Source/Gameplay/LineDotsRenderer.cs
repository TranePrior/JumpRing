using System.Collections.Generic;
using UnityEngine;

namespace JumpRing.Game.Gameplay
{
    public sealed class LineDotsRenderer : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField]
        private LinePathGenerator linePathGenerator;

        [SerializeField]
        private Camera gameplayCamera;

        [Header("Spawn")]
        [SerializeField, Min(0.1f)]
        private float spacing = 1.5f;

        [SerializeField, Min(0.01f)]
        private float dotSize = 0.5f;

        [SerializeField]
        private float behindCameraDistance = 15f;

        [SerializeField]
        private float aheadCameraDistance = 15f;

        private Sprite dotSprite;
        private readonly List<SpriteRenderer> activeDots = new(64);
        private readonly Queue<SpriteRenderer> pool = new(32);
        private int lastStartStep;
        private int lastEndStep;
        private bool isActive;

        private void OnEnable()
        {
            if (linePathGenerator == null)
            {
                linePathGenerator = Object.FindFirstObjectByType<LinePathGenerator>();
            }

            if (gameplayCamera == null)
            {
                gameplayCamera = Camera.main;
            }
        }

        public void Configure(Sprite sprite, float dotSpacing, float size)
        {
            dotSprite = sprite;
            spacing = dotSpacing;
            dotSize = size;

            foreach (var dot in activeDots)
            {
                dot.sprite = dotSprite;
                dot.transform.localScale = Vector3.one * dotSize;
            }
        }

        public void Activate()
        {
            if (linePathGenerator == null)
            {
                linePathGenerator = Object.FindFirstObjectByType<LinePathGenerator>();
            }

            if (gameplayCamera == null)
            {
                gameplayCamera = Camera.main;
            }

            isActive = true;
            lastStartStep = int.MaxValue;
            lastEndStep = int.MinValue;
        }

        public void Deactivate()
        {
            isActive = false;

            foreach (var dot in activeDots)
            {
                ReturnToPool(dot);
            }

            activeDots.Clear();
            lastStartStep = int.MaxValue;
            lastEndStep = int.MinValue;
        }

        private void LateUpdate()
        {
            if (!isActive || dotSprite == null || gameplayCamera == null || linePathGenerator == null)
            {
                return;
            }

            UpdateDots();
        }

        private void UpdateDots()
        {
            var cameraX = gameplayCamera.transform.position.x;
            var startX = cameraX - behindCameraDistance;
            var endX = cameraX + aheadCameraDistance;

            var startStep = Mathf.FloorToInt(startX / spacing);
            var endStep = Mathf.CeilToInt(endX / spacing);

            if (startStep == lastStartStep && endStep == lastEndStep)
            {
                UpdatePositions();
                return;
            }

            // Recycle dots that left the window
            for (var i = activeDots.Count - 1; i >= 0; i--)
            {
                var dot = activeDots[i];
                var dotStep = Mathf.RoundToInt(dot.transform.position.x / spacing);

                if (dotStep < startStep || dotStep > endStep)
                {
                    ReturnToPool(dot);
                    activeDots.RemoveAt(i);
                }
            }

            // Build a set of existing steps
            var existingSteps = new HashSet<int>(activeDots.Count);
            foreach (var dot in activeDots)
            {
                existingSteps.Add(Mathf.RoundToInt(dot.transform.position.x / spacing));
            }

            // Spawn missing dots
            for (var step = startStep; step <= endStep; step++)
            {
                if (existingSteps.Contains(step))
                {
                    continue;
                }

                var x = step * spacing;
                var y = linePathGenerator.EvaluateHeightAtX(x);
                var dot = GetFromPool();
                dot.transform.position = new Vector3(x, y, 0f);
                activeDots.Add(dot);
            }

            lastStartStep = startStep;
            lastEndStep = endStep;
        }

        private void UpdatePositions()
        {
            foreach (var dot in activeDots)
            {
                var x = dot.transform.position.x;
                var y = linePathGenerator.EvaluateHeightAtX(x);
                dot.transform.position = new Vector3(x, y, dot.transform.position.z);
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
                var go = new GameObject("LineDot");
                go.transform.SetParent(transform);
                sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 0;
            }

            sr.sprite = dotSprite;
            sr.transform.localScale = Vector3.one * dotSize;
            return sr;
        }

        private void ReturnToPool(SpriteRenderer sr)
        {
            sr.gameObject.SetActive(false);
            pool.Enqueue(sr);
        }
    }
}
