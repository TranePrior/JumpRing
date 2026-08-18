using UnityEngine;

namespace JumpRing.Game.Core.Services
{
    public sealed class AudioSettingsService : MonoBehaviour
    {
        private const string MusicKey = StorageKeys.SettingsMusic;
        private const string EffectsKey = StorageKeys.SettingsEffects;

        [SerializeField]
        private PlatformStorageService storageService;

        [SerializeField]
        private AudioSource musicSource;

        [SerializeField]
        private AudioSource[] effectsSources;

        public bool IsMusicEnabled { get; private set; } = true;
        public bool IsEffectsEnabled { get; private set; } = true;

        public void Initialize()
        {
            IsMusicEnabled = storageService.GetInt(MusicKey, 1) == 1;
            IsEffectsEnabled = storageService.GetInt(EffectsKey, 1) == 1;
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
