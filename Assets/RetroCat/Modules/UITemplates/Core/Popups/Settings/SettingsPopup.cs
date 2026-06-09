using System;
using PlatformLink;
using RetroCat.Modules.Core.UI.Activities.Popups.Core;
using RetroCat.Modules.Core.UI.Controls.Toggles;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RetroCat.Modules.UITemplates.Core.Popups.Settings
{
    public class SettingsPopup : PopupBase
    {
        private const string OpenSettingsEvent = "open-settings";
        private const string DisableMusicEvent = "disable-music";
        private const string DisableEffectsEvent = "disable-effects";
        private const string DisableVibrationsEvent = "disable-vibrations";

        [SerializeField] private ToggleButton _musicToggle;
        [SerializeField] private ToggleButton _effectsToggle;
        [SerializeField] private ToggleButton _vibrationsToggle;

        [Header("Links")]
        [SerializeField] private Button _privacyPolicyButton;
        [SerializeField] private Button _termsOfUseButton;
        [SerializeField] private string _privacyPolicyUrl;
        [SerializeField] private string _termsOfUseUrl;

        [Header("Events")]
        [SerializeField] private UnityEvent<bool> _onMusicChanged;
        [SerializeField] private UnityEvent<bool> _onEffectsChanged;
        [SerializeField] private UnityEvent<bool> _onVibrationsChanged;

        private bool _isMusicEnabledOnOpen;
        private bool _isEffectsEnabledOnOpen;
        private bool _isVibrationsEnabledOnOpen;

        public event Action<bool> MusicChanged;
        public event Action<bool> EffectsChanged;
        public event Action<bool> VibrationsChanged;

        public void SetInitialState(bool musicOn, bool effectsOn, bool vibrationsOn)
        {
            _musicToggle.IsOn = musicOn;
            _effectsToggle.IsOn = effectsOn;
            _vibrationsToggle.IsOn = vibrationsOn;
        }

        protected override void OnInit() { }

        protected override void OnOpenStarted()
        {
            _isMusicEnabledOnOpen = _musicToggle.IsOn;
            _isEffectsEnabledOnOpen = _effectsToggle.IsOn;
            _isVibrationsEnabledOnOpen = _vibrationsToggle.IsOn;

            if (PLink.IsInitialized)
                PLink.Analytics.SendEvent(OpenSettingsEvent);

            _musicToggle.StateEnabled += OnMusicStateEnabled;
            _musicToggle.StateDisabled += OnMusicStateDisabled;

            _effectsToggle.StateEnabled += OnEffectsStateEnabled;
            _effectsToggle.StateDisabled += OnEffectsStateDisabled;

            _vibrationsToggle.StateEnabled += OnVibrationStateEnabled;
            _vibrationsToggle.StateDisabled += OnVibrationStateDisabled;

            if (_privacyPolicyButton != null)
                _privacyPolicyButton.onClick.AddListener(OnPrivacyPolicyClicked);

            if (_termsOfUseButton != null)
                _termsOfUseButton.onClick.AddListener(OnTermsOfUseClicked);
        }

        private void OnVibrationStateDisabled()
        {
            _onVibrationsChanged?.Invoke(false);
            VibrationsChanged?.Invoke(false);
        }

        private void OnVibrationStateEnabled()
        {
            _onVibrationsChanged?.Invoke(true);
            VibrationsChanged?.Invoke(true);
        }

        private void OnMusicStateEnabled()
        {
            _onMusicChanged?.Invoke(true);
            MusicChanged?.Invoke(true);
        }

        private void OnMusicStateDisabled()
        {
            _onMusicChanged?.Invoke(false);
            MusicChanged?.Invoke(false);
        }

        private void OnEffectsStateEnabled()
        {
            _onEffectsChanged?.Invoke(true);
            EffectsChanged?.Invoke(true);
        }

        private void OnEffectsStateDisabled()
        {
            _onEffectsChanged?.Invoke(false);
            EffectsChanged?.Invoke(false);
        }

        protected override void OnOpenFinished() { }
        protected override void OnCloseStarted() { }

        private void OnPrivacyPolicyClicked()
        {
            if (!string.IsNullOrEmpty(_privacyPolicyUrl) && PLink.IsInitialized)
                PLink.Platform.OpenLink(_privacyPolicyUrl);
        }

        private void OnTermsOfUseClicked()
        {
            if (!string.IsNullOrEmpty(_termsOfUseUrl) && PLink.IsInitialized)
                PLink.Platform.OpenLink(_termsOfUseUrl);
        }

        protected override void OnCloseFinished()
        {
            _musicToggle.StateEnabled -= OnMusicStateEnabled;
            _musicToggle.StateDisabled -= OnMusicStateDisabled;

            _effectsToggle.StateEnabled -= OnEffectsStateEnabled;
            _effectsToggle.StateDisabled -= OnEffectsStateDisabled;

            _vibrationsToggle.StateEnabled -= OnVibrationStateEnabled;
            _vibrationsToggle.StateDisabled -= OnVibrationStateDisabled;

            if (_privacyPolicyButton != null)
                _privacyPolicyButton.onClick.RemoveListener(OnPrivacyPolicyClicked);

            if (_termsOfUseButton != null)
                _termsOfUseButton.onClick.RemoveListener(OnTermsOfUseClicked);

            MusicChanged = null;
            EffectsChanged = null;
            VibrationsChanged = null;

            if (!PLink.IsInitialized)
                return;

            if (_isMusicEnabledOnOpen && !_musicToggle.IsOn)
                PLink.Analytics.SendEvent(DisableMusicEvent);

            if (_isEffectsEnabledOnOpen && !_effectsToggle.IsOn)
                PLink.Analytics.SendEvent(DisableEffectsEvent);

            if (_isVibrationsEnabledOnOpen && !_vibrationsToggle.IsOn)
                PLink.Analytics.SendEvent(DisableVibrationsEvent);
        }
    }
}
