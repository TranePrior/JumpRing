using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RetroCat.Modules.Core.UI.Controls.Toggles
{
    public class ToggleButton : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image _handleImage;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Color _colorHandleEnabled;
        [SerializeField] private Color _colorHandleDisabled;
        [SerializeField] private Color _colorBackgroundEnabled;
        [SerializeField] private Color _colorBackgroundDisabled;
        [SerializeField] private float _switchDuration = 0.2f;
        [SerializeField] private bool _state;

        public event Action Clicked;
        public event Action<bool> StateChanged; 
        
        public event Action StateEnabled;
        public event Action StateDisabled;

        public bool IsOn
        {
            get => _state;
            set => SetState(value);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            SetStateWithoutNotify(_state);
        }

        private void Reset()
        {
            _handleImage = GetComponentInChildren<Image>();
            _backgroundImage = GetComponent<Image>();
        }
#endif

        private void Awake()
        {
            SetStateWithoutNotify(_state);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            SetState(!_state);
            Clicked?.Invoke();
        }

        public void SetColor(Color color)
        {
            _handleImage.color = color;
        }

        private void SetState(bool state) 
        {
            if (_state != state)
            {
                _state = state;

                if (_state == true)
                {
                    SetEnabledState();
                    StateEnabled?.Invoke();
                }
                else
                {
                    SetDisabledState();
                    StateDisabled?.Invoke();
                }

                StateChanged?.Invoke(state);
            }
        }

        private void SetStateWithoutNotify(bool state)
        {
            if (state == true)
                SetEnabledState();
            else
                SetDisabledState();
        }

        private void SetEnabledState()
        {
            _handleImage.rectTransform
                .DOAnchorPosX(_handleImage.rectTransform.sizeDelta.x, _switchDuration)
                .SetUpdate(true);

            DoColor(_colorHandleEnabled, _colorBackgroundEnabled);
        }

        private void SetDisabledState()
        {
            _handleImage.rectTransform
                .DOAnchorPosX(0, _switchDuration)
                .SetUpdate(true);

            DoColor(_colorHandleDisabled, _colorBackgroundDisabled);
        }
        
        private void DoColor(Color handleColor, Color backgroundColor)
        {
            _backgroundImage.DOColor(backgroundColor, _switchDuration);
            _handleImage.DOColor(handleColor, _switchDuration);
        }
    }
}