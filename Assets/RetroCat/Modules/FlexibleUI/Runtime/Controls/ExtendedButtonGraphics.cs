using DG.Tweening;
using UnityEngine;

namespace RetroCat.Modules.Core.UI.Contols.Buttons
{
    [DisallowMultipleComponent]
    public class ExtendedButtonAlpha : MonoBehaviour
    {
        [SerializeField] private ExtendedButton _button;

        [Header("UI")]
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Tween")]
        [SerializeField] private float _transitionDuration = 0.2f;
        [SerializeField] private Ease _ease = Ease.OutQuad;
        [SerializeField] private bool _ignoreTimeScale = true;

        [Header("Alphas")] 
        [SerializeField] private float _normalAlpha = 1f;
        [SerializeField] private float _disabledAlpha = 1f;
        [SerializeField] private float _highlightedAlpha = 1f;
        [SerializeField] private float _pressedAlpha = 1f;
        [SerializeField] private float _selectedAlpha = 1f;

        private Tween _alphaTween;

        private void Awake()
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            if (_button != null)
            {
                OnStateChanged(_button.State);
                _button.StateChanged += OnStateChanged;
            }
        }

        private void OnDisable()
        {
            if (_button != null)
                _button.StateChanged -= OnStateChanged;

            KillTween();
        }

        private void OnDestroy()
        {
            KillTween();
        }

        private void OnStateChanged(ButtonState state)
        {
            if (_canvasGroup == null)
                return;

            float target = GetTargetAlpha(state);

            KillTween(false);

            if (_transitionDuration <= 0f)
            {
                _canvasGroup.alpha = target;
                return;
            }

            _alphaTween = _canvasGroup
                .DOFade(target, _transitionDuration)
                .SetEase(_ease)
                .SetUpdate(_ignoreTimeScale)
                .SetLink(gameObject);
        }

        private void KillTween(bool complete = false)
        {
            if (_alphaTween != null && _alphaTween.IsActive())
            {
                _alphaTween.Kill(complete);
                _alphaTween = null;
            }
        }

        private float GetTargetAlpha(ButtonState state)
        {
            switch (state)
            {
                case ButtonState.Normal:      return _normalAlpha;
                case ButtonState.Disabled:    return _disabledAlpha;
                case ButtonState.Pressed:     return _pressedAlpha;
                case ButtonState.Highlighted: return _highlightedAlpha;
                case ButtonState.Selected:    return _selectedAlpha;
                default:                      return _normalAlpha;
            }
        }
    }
}