using UnityEngine;
using UnityEngine.UI;

namespace RetroCat.Modules.FlexibleUI.Runtime.Canvases
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler))]
    public class CanvasScalerAdaptive : MonoBehaviour
    {
        private float _currentWidthOrHeight = 0;
        private float _currentAspect = 0;
        
        [Header("Reference")] public Vector2 referenceResolution = new Vector2(1080, 1920);

        [Range(0, 1)] public float _matchWidthOrHeightMobile = 0f;
        [Range(0, 1)] public float _matchWidthOrHeightDesktop = 1f;

        [Header("Aspect blending (width/height)")]
        public float desktopStartAspect = 0.70f;

        public float desktopEndAspect = 1.20f;

        [Header("Scale clamp (mobile -> desktop)")]
        public float minScaleMobile = 0.75f;

        public float maxScaleMobile = 1.10f;

        public float minScaleDesktop = 0.90f;
        public float maxScaleDesktop = 1.40f;

        private CanvasScaler _scaler;
        
        void Awake()
        {
            _scaler = GetComponent<CanvasScaler>();

            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        }

        void LateUpdate()
        {
            _currentAspect = (float)Screen.width / Screen.height;

            _currentWidthOrHeight = Mathf.InverseLerp(desktopStartAspect, desktopEndAspect, _currentAspect);
            _currentWidthOrHeight = Mathf.Clamp01(_currentWidthOrHeight);

            float match = Mathf.Lerp(_matchWidthOrHeightMobile, _matchWidthOrHeightDesktop, _currentWidthOrHeight);

            float sx = Screen.width / referenceResolution.x;
            float sy = Screen.height / referenceResolution.y;

            float scale = Mathf.Exp(Mathf.Lerp(Mathf.Log(sx), Mathf.Log(sy), match));

            float minScale = Mathf.Lerp(minScaleMobile, minScaleDesktop, _currentWidthOrHeight);
            float maxScale = Mathf.Lerp(maxScaleMobile, maxScaleDesktop, _currentWidthOrHeight);

            scale = Mathf.Clamp(scale, minScale, maxScale);

            _scaler.scaleFactor = scale;
        }
    }
}