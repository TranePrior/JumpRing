using DG.Tweening;
using RetroCat.Modules.Core.UI.Activities.Core;
using UnityEngine;

namespace RetroCat.Modules.Core.UI.Activities.Popups.Core
{
    public abstract class PopupBase : ActivityBase
    {
        [Header("Dependencies")]
        [SerializeField] private ActivityFade _fade;
        [SerializeField] private RectTransform _popupContainer;
        
        [Header("Animation")]
        [SerializeField] private float _openDuration = 0.35f;
        [SerializeField] private float _closeDuration = 0.25f;
        [SerializeField] private float _openStartScale = 0.7f;
        [SerializeField] private float _closeScale = 0.7f;
        [SerializeField] private Ease _openEase = Ease.OutBack;
        [SerializeField] private Ease _closeEase = Ease.InBack;

        private CanvasGroup _canvasGroup;
        private Tween _animationTween;
        private bool _isAnimating;
        private bool _isOpen;
        
#if UNITY_EDITOR
        private void Reset()
        {
            _fade = GetComponentInChildren<ActivityFade>();
        }
#endif
        private void Awake()
        {
            if (_popupContainer.TryGetComponent(out _canvasGroup) == false)
            {
                _canvasGroup = _popupContainer.gameObject.AddComponent<CanvasGroup>();
                _canvasGroup.alpha = 0.5f;
            }
            
            OnInit();
        }

        public override void Open()
        {
            if (_isOpen && !_isAnimating)
                return;

            _animationTween?.Kill();

            gameObject.SetActive(true);

            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.alpha = 0.5f;
            _popupContainer.localScale = Vector3.one * _openStartScale;
  
            OnOpenStarted();

            _isAnimating = true;
            _animationTween = DOTween.Sequence()
                .Join(_popupContainer.DOScale(1f, _openDuration).SetEase(_openEase))
                .Join(_canvasGroup.DOFade(1f, _openDuration).SetEase(_openEase))
                .Join(_fade.FadeIn())
                .OnComplete(() =>
                {
                    _isAnimating = false;
                    _isOpen = true;
                    _canvasGroup.interactable = true;
                    _canvasGroup.blocksRaycasts = true;
                    OnOpenFinished();
                });
        }

        public override void Close()
        {
            if (!_isOpen && !_isAnimating)
                return;

            _animationTween?.Kill();

            OnCloseStarted();

            _isAnimating = true;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            _animationTween = DOTween.Sequence()
                .Append(_canvasGroup.DOFade(0f, _closeDuration).SetEase(_closeEase))
                .Join(_popupContainer.DOScale(_closeScale, _closeDuration).SetEase(_closeEase))
                .Join(_fade.FadeOut())
                .OnComplete(() =>
                {
                    _isAnimating = false;
                    _isOpen = false;
                    gameObject.SetActive(false);
                    OnCloseFinished();
                });
        }

        private void OnDestroy()
        {
            _animationTween?.Kill();
        }
    }
}
