using PlatformLink;
using RetroCat.Modules.Core.UI.Activities.Popups.Core;
using RetroCat.Modules.Core.UI.Controls.Toggles;
using UnityEngine;
using UnityEngine.Events;

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

        [Header("Events")]
        [SerializeField] private UnityEvent<bool> _onMusicChanged;
        [SerializeField] private UnityEvent<bool> _onEffectsChanged;
        [SerializeField] private UnityEvent<bool> _onVibrationsChanged;

        private bool _isMusicEnabledOnOpen;
        private bool _isEffectsEnabledOnOpen;
        private bool _isVibrationsEnabledOnOpen;

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
        }

        private void OnVibrationStateDisabled()
        {
            _onVibrationsChanged?.Invoke(false);
        }

        private void OnVibrationStateEnabled()
        {
            _onVibrationsChanged?.Invoke(true);
        }

        private void OnMusicStateEnabled()
        {
            _onMusicChanged?.Invoke(true);
        }

        private void OnMusicStateDisabled()
        {
            _onMusicChanged?.Invoke(false);
        }

        private void OnEffectsStateEnabled()
        {
            _onEffectsChanged?.Invoke(true);
        }

        private void OnEffectsStateDisabled()
        {
            _onEffectsChanged?.Invoke(false);
        }

        protected override void OnOpenFinished() { }
        protected override void OnCloseStarted() { }

        protected override void OnCloseFinished()
        {
            _musicToggle.StateEnabled -= OnMusicStateEnabled;
            _musicToggle.StateDisabled -= OnMusicStateDisabled;

            _effectsToggle.StateEnabled -= OnEffectsStateEnabled;
            _effectsToggle.StateDisabled -= OnEffectsStateDisabled;

            _vibrationsToggle.StateEnabled -= OnVibrationStateEnabled;
            _vibrationsToggle.StateDisabled -= OnVibrationStateDisabled;

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
