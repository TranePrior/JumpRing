using DG.Tweening;
using UnityEngine;

namespace RetroCat.Modules.Core.UI.Activities.Core
{
    public class ActivityFade : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _fade;
        [SerializeField] private float _duration = 0.25f;
        
        private Tween _tween;
    
#if UNITY_EDITOR
        private void Reset()
        {
            _fade = GetComponent<CanvasGroup>();
        }
#endif

        private void Awake()
        {
            _fade.alpha = 0f;
        }

        public Tween FadeIn()
        {
            _tween = _fade.DOFade(1f, _duration);
            return _tween;
        }
    
        public Tween FadeOut()
        {
            _tween = _fade.DOFade(0f, _duration);
            return _tween;
        }

        private void OnDestroy()
        {
            _tween?.Kill();
        }
    }
}
