using System;
using PlatformLink;
using RetroCat.Modules.Core.UI.Activities.Popups.Core;
using RetroCat.Modules.Core.UI.Controls.Toggles;
using TMPro;
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
        private const string ChangeLanguageEvent = "change-language";

        [SerializeField] private ToggleButton _musicToggle;
        [SerializeField] private ToggleButton _effectsToggle;
        [SerializeField] private ToggleButton _vibrationsToggle;
        [SerializeField] private RectTransform _vibrationsItem;

        [Header("Language")]
        [SerializeField] private ToggleButton _languageToggle;
        [SerializeField] private TMP_Text _languageCodeLabel;
        [SerializeField] private Image _languageFlagIcon;

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
        public event Action<bool> LanguageToggled;

        public void SetInitialState(bool musicOn, bool effectsOn, bool vibrationsOn)
        {
            _musicToggle.IsOn = musicOn;
            _effectsToggle.IsOn = effectsOn;
            _vibrationsToggle.IsOn = vibrationsOn;
        }

        /// <summary>
        /// Initial state of the language row. Call before the popup opens, like
        /// <see cref="SetInitialState"/>, so setting the toggle does not raise
        /// <see cref="LanguageToggled"/> back at the caller.
        /// </summary>
        public void SetLanguageState(string code, Sprite flag, bool toggleOn)
        {
            _languageToggle.IsOn = toggleOn;
            SetLanguageCode(code, flag);
        }

        /// <summary>
        /// Shows which language the game is running in as its two-letter code ("RU", "EN") plus the
        /// matching flag. A code rather than a word, because the row has to stay readable for a
        /// player who does not speak the language currently selected. The flag comes from the caller
        /// so this popup stays a template that knows nothing about which languages a game ships.
        /// </summary>
        public void SetLanguageCode(string code, Sprite flag)
        {
            _languageCodeLabel.text = code;
            _languageFlagIcon.sprite = flag;
        }

        /// <summary>
        /// Hides the whole vibration row on platforms without a vibration API (iOS browsers),
        /// where the toggle would switch nothing.
        /// </summary>
        public void SetVibrationsAvailable(bool available)
        {
            _vibrationsItem.gameObject.SetActive(available);
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

            _languageToggle.StateChanged += OnLanguageToggled;
        }

        private void OnLanguageToggled(bool isOn)
        {
            if (PLink.IsInitialized)
                PLink.Analytics.SendEvent(ChangeLanguageEvent);

            LanguageToggled?.Invoke(isOn);
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

        protected override void OnCloseFinished()
        {
            _musicToggle.StateEnabled -= OnMusicStateEnabled;
            _musicToggle.StateDisabled -= OnMusicStateDisabled;

            _effectsToggle.StateEnabled -= OnEffectsStateEnabled;
            _effectsToggle.StateDisabled -= OnEffectsStateDisabled;

            _vibrationsToggle.StateEnabled -= OnVibrationStateEnabled;
            _vibrationsToggle.StateDisabled -= OnVibrationStateDisabled;

            _languageToggle.StateChanged -= OnLanguageToggled;

            MusicChanged = null;
            EffectsChanged = null;
            VibrationsChanged = null;
            LanguageToggled = null;

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
