using UnityEngine;

namespace JumpRing.Game.Core.Services
{
    public sealed class AudioSettingsService : MonoBehaviour
    {
        private const string MusicKey = "Settings_Music";
        private const string EffectsKey = "Settings_Effects";
        private const string VibrationKey = "Settings_Vibration";

        [SerializeField]
        private PlatformStorageService storageService;

        [SerializeField]
        private AudioSource musicSource;

        [SerializeField]
        private AudioSource[] effectsSources;

        public bool IsMusicEnabled { get; private set; } = true;
        public bool IsEffectsEnabled { get; private set; } = true;
        public bool IsVibrationEnabled { get; private set; } = true;

        public void Initialize()
        {
            IsMusicEnabled = storageService.GetInt(MusicKey, 1) == 1;
            IsEffectsEnabled = storageService.GetInt(EffectsKey, 1) == 1;
            IsVibrationEnabled = storageService.GetInt(VibrationKey, 1) == 1;
            ApplyMusic();
            ApplyEffects();
        }

        public void SetMusic(bool enabled)
        {
            IsMusicEnabled = enabled;
            storageService.SetInt(MusicKey, enabled ? 1 : 0);
            ApplyMusic();
        }

        public void SetEffects(bool enabled)
        {
            IsEffectsEnabled = enabled;
            storageService.SetInt(EffectsKey, enabled ? 1 : 0);
            ApplyEffects();
        }

        public void SetVibration(bool enabled)
        {
            IsVibrationEnabled = enabled;
            storageService.SetInt(VibrationKey, enabled ? 1 : 0);
        }

        private void ApplyMusic()
        {
            if (musicSource != null)
            {
                musicSource.mute = !IsMusicEnabled;
            }
        }

        private void ApplyEffects()
        {
            foreach (var source in effectsSources)
            {
                if (source != null)
                {
                    source.mute = !IsEffectsEnabled;
                }
            }
        }
    }
}
