using UnityEngine;

namespace RetroCat.Modules.FlexibleUI.Runtime.Canvases
{
    /// <summary>
    /// Pushes an edge-anchored UI element out of the device insets reported by
    /// <see cref="SafeAreaProvider"/> — notch, dynamic island, home indicator, rounded corners.
    ///
    /// Works on the element itself instead of re-parenting it into a safe-area container, so the
    /// sibling order (and with it the draw order against full-screen overlays) stays untouched.
    /// The shift follows the element's own anchors: anchored to the top it moves down, to the
    /// bottom it moves up, and so on. Centered axes are left alone.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaPadding : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Canvas _canvas;
        private Vector2 _basePosition;
        private Vector2Int _appliedScreenSize;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
            _basePosition = _rectTransform.anchoredPosition;
            Apply();
        }

        // The viewport changes without notice in a browser (fullscreen toggle, URL bar collapsing,
        // orientation change, window resize) and Unity raises no event for it. Insets only ever
        // change together with the screen size, so this guard keeps the JS interop off the hot path.
        private void Update()
        {
            if (Screen.width == _appliedScreenSize.x && Screen.height == _appliedScreenSize.y)
            {
                return;
            }

            Apply();
        }

        private void Apply()
        {
            _appliedScreenSize = new Vector2Int(Screen.width, Screen.height);

            // Insets arrive in screen pixels; anchoredPosition lives in canvas units.
            var insets = SafeAreaProvider.GetInsets() / _canvas.scaleFactor;
            var offset = CalculateOffset(_rectTransform.anchorMin, _rectTransform.anchorMax, insets);

            _rectTransform.anchoredPosition = _basePosition + offset;
        }

        /// <summary>
        /// Shift that moves an element with the given anchors clear of <paramref name="insets"/>
        /// (left, top, right, bottom). An axis anchored to neither edge is not moved.
        /// </summary>
        public static Vector2 CalculateOffset(Vector2 anchorMin, Vector2 anchorMax, Vector4 insets)
        {
            var anchorX = (anchorMin.x + anchorMax.x) * 0.5f;
            var anchorY = (anchorMin.y + anchorMax.y) * 0.5f;

            var offset = Vector2.zero;

            if (Mathf.Approximately(anchorX, 0f))
            {
                offset.x = insets.x;
            }
            else if (Mathf.Approximately(anchorX, 1f))
            {
                offset.x = -insets.z;
            }

            if (Mathf.Approximately(anchorY, 0f))
            {
                offset.y = insets.w;
            }
            else if (Mathf.Approximately(anchorY, 1f))
            {
                offset.y = -insets.y;
            }

            return offset;
        }
    }
}
