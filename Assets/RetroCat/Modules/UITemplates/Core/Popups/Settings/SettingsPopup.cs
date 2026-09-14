using System;
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

        [SerializeField] private ToggleButton _musicToggle;
        [SerializeField] private ToggleButton _effectsToggle;

        [Header("Events")]
        [SerializeField] private UnityEvent<bool> _onMusicChanged;
        [SerializeField] private UnityEvent<bool> _onEffectsChanged;

        private bool _isMusicEnabledOnOpen;
        private bool _isEffectsEnabledOnOpen;

        public event Action<bool> MusicChanged;
        public event Action<bool> EffectsChanged;

        public void SetInitialState(bool musicOn, bool effectsOn)
        {
            _musicToggle.IsOn = musicOn;
            _effectsToggle.IsOn = effectsOn;
        }

        protected override void OnInit() { }

        protected override void OnOpenStarted()
        {
            _isMusicEnabledOnOpen = _musicToggle.IsOn;
            _isEffectsEnabledOnOpen = _effectsToggle.IsOn;

            if (PLink.IsInitialized)
                PLink.Analytics.SendEvent(OpenSettingsEvent);

            _musicToggle.StateEnabled += OnMusicStateEnabled;
            _musicToggle.StateDisabled += OnMusicStateDisabled;

            _effectsToggle.StateEnabled += OnEffectsStateEnabled;
            _effectsToggle.StateDisabled += OnEffectsStateDisabled;
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

            MusicChanged = null;
            EffectsChanged = null;

            if (!PLink.IsInitialized)
                return;

            if (_isMusicEnabledOnOpen && !_musicToggle.IsOn)
                PLink.Analytics.SendEvent(DisableMusicEvent);

            if (_isEffectsEnabledOnOpen && !_effectsToggle.IsOn)
                PLink.Analytics.SendEvent(DisableEffectsEvent);
        }
    }
}
