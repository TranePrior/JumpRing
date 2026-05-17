using System.Collections;
using System.Collections.Generic;
using JumpRing.Game.Gameplay;
using TMPro;
using UnityEngine;

namespace JumpRing.Game.UI
{
    public sealed class FloatingCoinText : MonoBehaviour
    {
        private const string TextFormat = "+{0}";

        [Header("Prefab")]
        [SerializeField]
        private GameObject popupPrefab;

        [Header("Pool")]
        [SerializeField, Min(1)]
        private int preWarmCount = 6;

        [Header("Animation")]
        [SerializeField, Min(0.1f)]
        private float floatHeight = 1.5f;

        [SerializeField, Min(0.1f)]
        private float duration = 0.8f;

        [SerializeField, Min(0.01f)]
        private float scaleInDuration = 0.1f;

        [SerializeField]
        private float spreadX = 0.3f;

        [SerializeField]
        private float startOffsetY = 0.5f;

        private readonly Queue<PopupInstance> pool = new();

        private void Awake()
        {
            for (var i = 0; i < preWarmCount; i++)
            {
                var popup = CreatePopupInstance();
                popup.Root.SetActive(false);
                pool.Enqueue(popup);
            }
        }

        private void OnEnable()
        {
            CoinCollectible.Collected += OnCoinCollected;
        }

        private void OnDisable()
        {
            CoinCollectible.Collected -= OnCoinCollected;
        }

        private void OnCoinCollected(Vector3 worldPosition, int amount)
        {
            var popup = GetFromPool();
            popup.Label.text = string.Format(TextFormat, amount);

            var offsetX = Random.Range(-spreadX, spreadX);
            var startPos = new Vector3(
                worldPosition.x + offsetX,
                worldPosition.y + startOffsetY,
                worldPosition.z);

            popup.Root.transform.position = startPos;
            popup.Root.transform.localScale = Vector3.zero;
            popup.Root.SetActive(true);

            StartCoroutine(PlayFloatAnimation(popup, startPos));
        }

        private IEnumerator PlayFloatAnimation(PopupInstance popup, Vector3 startPos)
        {
            var rootTransform = popup.Root.transform;

            // Scale in
            var elapsed = 0f;
            while (elapsed < scaleInDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, elapsed / scaleInDuration);
                rootTransform.localScale = Vector3.one * t;
                yield return null;
            }

            rootTransform.localScale = Vector3.one;

            // Float up + fade out
            elapsed = 0f;
            var fadeDuration = duration - scaleInDuration;
            var labelBaseColor = popup.Label.color;
            labelBaseColor.a = 1f;
            var iconBaseColor = popup.Icon != null ? popup.Icon.color : Color.white;
            iconBaseColor.a = 1f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / fadeDuration;

                var y = Mathf.Lerp(startPos.y, startPos.y + floatHeight, Mathf.SmoothStep(0f, 1f, t));
                rootTransform.position = new Vector3(rootTransform.position.x, y, startPos.z);

                var alpha = Mathf.Lerp(1f, 0f, t * t);

                var labelColor = labelBaseColor;
                labelColor.a = alpha;
                popup.Label.color = labelColor;

                if (popup.Icon != null)
                {
                    var iconColor = iconBaseColor;
                    iconColor.a = alpha;
                    popup.Icon.color = iconColor;
                }

                yield return null;
            }

            ReturnToPool(popup);
        }

        private PopupInstance GetFromPool()
        {
            if (pool.Count > 0)
            {
                return pool.Dequeue();
            }

            return CreatePopupInstance();
        }

        private void ReturnToPool(PopupInstance popup)
        {
            popup.Root.SetActive(false);

            var labelColor = popup.Label.color;
            labelColor.a = 1f;
            popup.Label.color = labelColor;

            if (popup.Icon != null)
            {
                var iconColor = popup.Icon.color;
                iconColor.a = 1f;
                popup.Icon.color = iconColor;
            }

            popup.Root.transform.localScale = Vector3.one;
            pool.Enqueue(popup);
        }

        private PopupInstance CreatePopupInstance()
        {
            var go = Instantiate(popupPrefab, transform);
            var label = go.GetComponentInChildren<TextMeshPro>();
            var icon = go.GetComponentInChildren<SpriteRenderer>();
            return new PopupInstance(go, label, icon);
        }

        private readonly struct PopupInstance
        {
            public readonly GameObject Root;
            public readonly TextMeshPro Label;
            public readonly SpriteRenderer Icon;

            public PopupInstance(GameObject root, TextMeshPro label, SpriteRenderer icon)
            {
                Root = root;
                Label = label;
                Icon = icon;
            }
        }
    }
}
