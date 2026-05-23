using UnityEngine;
using UnityEngine.UI;

namespace JumpRing.Game.UI
{
    public sealed class DimOverlay : MonoBehaviour
    {
        [SerializeField]
        private GameObject overlay;

        [Header("Blur")]
        [SerializeField, Range(2, 32)]
        private int downsampleFactor = 8;

        private GameObject blurObject;
        private RawImage blurRawImage;
        private RenderTexture blurRT;

        public void Show()
        {
            CaptureBlur();
            overlay.SetActive(true);
        }

        public void Hide()
        {
            overlay.SetActive(false);
            ReleaseBlur();
        }

        private void CaptureBlur()
        {
            var cam = Camera.main;
            if (cam == null) return;

            int w = Screen.width / downsampleFactor;
            int h = Screen.height / downsampleFactor;
            if (w < 1) w = 1;
            if (h < 1) h = 1;

            ReleaseRT();
            EnsureBlurObject();

            blurRT = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
            blurRT.filterMode = FilterMode.Bilinear;

            var prevTarget = cam.targetTexture;
            cam.targetTexture = blurRT;
            cam.Render();
            cam.targetTexture = prevTarget;

            blurRawImage.texture = blurRT;
            blurObject.SetActive(true);
        }

        private void ReleaseBlur()
        {
            if (blurObject != null)
                blurObject.SetActive(false);

            if (blurRawImage != null)
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
            ReleaseRT();
            if (blurObject != null)
                Destroy(blurObject);
        }
    }
}
