using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace JumpRing.Game.UI
{
    public sealed class DimOverlay : MonoBehaviour
    {
        [SerializeField]
        private GameObject overlay;

        [SerializeField]
        private CanvasGroup overlayCanvasGroup;

        [Header("Blur")]
        [SerializeField]
        private Camera blurSourceCamera;

        [SerializeField, Range(2, 32)]
        private int downsampleFactor = 8;

        private const float FadeDuration = 0.2f;

        private GameObject blurObject;
        private RawImage blurRawImage;
        private RenderTexture blurRT;
        private Tween fadeTween;
        private bool isVisible;

        /// <summary>
        /// Fades the overlay in over the current frame. Re-entrant: while the overlay is already
        /// visible this is a no-op.
        /// </summary>
        /// <remarks>
        /// Presenters share one overlay, so a second <c>Show</c> used to re-capture the blur — an
        /// extra full <see cref="Camera.Render"/> — and reset the alpha to 0, which read on screen
        /// as the whole game flashing bright for the length of the fade.
        /// </remarks>
        public void Show()
        {
            if (isVisible)
            {
                return;
            }

            isVisible = true;
            EnsureBlurObject();
            fadeTween?.Kill();
            CaptureBlur();
            overlay.SetActive(true);
            SetAlpha(0f);
            fadeTween = FadeTo(1f, Ease.OutQuad);
        }

        public void Hide()
        {
            if (!isVisible)
            {
                return;
            }

            isVisible = false;
            fadeTween?.Kill();
            fadeTween = FadeTo(0f, Ease.InQuad);
            fadeTween.OnComplete(() =>
            {
                overlay.SetActive(false);
                ReleaseBlur();
            });
        }

        public void HideImmediate()
        {
            isVisible = false;
            EnsureBlurObject();
            fadeTween?.Kill();
            SetAlpha(0f);
            overlay.SetActive(false);
            ReleaseBlur();
        }

        /// <summary>
        /// Drives the dim and the blur off a single value. The blur lives outside
        /// <see cref="overlayCanvasGroup"/> — it has to render under the dim, and a child always
        /// draws over its parent's graphic — so fading only the canvas group snapped the blur in at
        /// full strength while the dim was still ramping up.
        /// </summary>
        private Tween FadeTo(float target, Ease ease)
        {
            return DOTween.To(() => overlayCanvasGroup.alpha, SetAlpha, target, FadeDuration)
                .SetEase(ease)
                .SetUpdate(true)
                .SetTarget(overlayCanvasGroup);
        }

        private void SetAlpha(float alpha)
        {
            overlayCanvasGroup.alpha = alpha;

            var color = blurRawImage.color;
            color.a = alpha;
            blurRawImage.color = color;
        }

        private void CaptureBlur()
        {
            int w = Screen.width / downsampleFactor;
            int h = Screen.height / downsampleFactor;
            if (w < 1) w = 1;
            if (h < 1) h = 1;

            ReleaseRT();

            blurRT = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
            blurRT.filterMode = FilterMode.Bilinear;

            var prevTarget = blurSourceCamera.targetTexture;
            blurSourceCamera.targetTexture = blurRT;
            blurSourceCamera.Render();
            blurSourceCamera.targetTexture = prevTarget;

            blurRawImage.texture = blurRT;
            blurObject.SetActive(true);
        }

        private void ReleaseBlur()
        {
            blurObject.SetActive(false);
            blurRawImage.texture = null;
            ReleaseRT();
        }

        private void EnsureBlurObject()
        {
            if (blurObject != null) return;

            blurObject = new GameObject("BlurOverlay");
            blurObject.transform.SetParent(overlay.transform.parent, false);
            blurObject.transform.SetSiblingIndex(overlay.transform.GetSiblingIndex());

            var rect = blurObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            blurRawImage = blurObject.AddComponent<RawImage>();
            blurRawImage.raycastTarget = false;

            blurObject.SetActive(false);
        }

        private void ReleaseRT()
        {
            if (blurRT != null)
            {
                RenderTexture.ReleaseTemporary(blurRT);
                blurRT = null;
            }
        }

        private void OnDestroy()
        {
            fadeTween?.Kill();
            ReleaseRT();

            if (blurObject != null)
            {
                Destroy(blurObject);
            }
        }
    }
}
