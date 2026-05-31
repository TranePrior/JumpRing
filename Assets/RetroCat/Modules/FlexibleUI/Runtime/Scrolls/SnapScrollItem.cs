using UnityEngine;

namespace RetroCat.Modules.Core.UI.Scrolls
{
    public abstract class SnapScrollItem : MonoBehaviour
    {
        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private CanvasGroup _canvasGroup;
    
        public RectTransform RectTransform => _rectTransform;

        public void SetOpacity(float opacity)
        {
            _canvasGroup.alpha = opacity;
        }
    }
}