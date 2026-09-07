using System;
using PlatformLink;
using UnityEngine;

namespace JumpRing.Game.Core.Localization
{
    /// <summary>
    /// Serves the game's texts in the language the platform reports for the player.
    /// </summary>
    /// <remarks>
    /// The platform locale is the only input (Yandex requirement 2.14). There is deliberately no
    /// in-game override: a stored choice used to outrank the SDK, and the moment moderation switched
    /// the locale mock the game kept its old language and was rejected for "not using the SDK".
    /// A player who wants another language changes it on the platform, and the game follows.
    /// </remarks>
    public sealed class LocalizationService : MonoBehaviour
    {
        [SerializeField]
        private LocalizationData russianData;

        [SerializeField]
        private LocalizationData englishData;

        private LocalizationData activeData;

        public Language CurrentLanguage { get; private set; }

        public event Action<Language> LanguageChanged;

        public static LocalizationService Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            ApplyLanguage(DetectPlatformLanguage());

            // PLink is not ready at Awake, so the first frames run on the browser locale. Re-detect
            // from the platform as soon as it answers; LocalizedText picks the change up.
            if (!PLink.IsInitialized)
            {
                PLink.Initilized += OnPlinkReadyDetectLanguage;
            }
        }

        private void OnDestroy()
        {
            PLink.Initilized -= OnPlinkReadyDetectLanguage;

            if (Instance == this)
                Instance = null;
        }

        public string GetText(LocalizationKey key)
        {
            return activeData.GetText(key);
        }

        public string GetText(LocalizationKey key, params object[] args)
        {
            return string.Format(activeData.GetText(key), args);
        }

        private void OnPlinkReadyDetectLanguage()
        {
            PLink.Initilized -= OnPlinkReadyDetectLanguage;

            Language detected = DetectPlatformLanguage();
            if (detected != CurrentLanguage)
            {
                ApplyLanguage(detected);
            }
        }

        private void ApplyLanguage(Language language)
        {
            CurrentLanguage = language;
            activeData = language == Language.RU ? russianData : englishData;
            LanguageChanged?.Invoke(language);
        }

        private static Language DetectPlatformLanguage()
        {
            if (PLink.IsInitialized)
            {
                string platformLocale = PLink.Environment.Language;
                Language detected = LanguageResolver.FromPlatformLocale(platformLocale);

                // Logged because the platform locale is the one input that cannot be reproduced
                // locally: it comes from the player's account, so a wrong language in the published
                // build is only diagnosable from the browser console.
                Debug.Log($"[Localization] Platform locale '{platformLocale}' resolved to {detected}");
                return detected;
            }

            // Placeholder for the frames before the platform SDK answers: the browser locale, which
            // WebGL surfaces as Application.systemLanguage. Awake re-detects from the SDK as soon as
            // it initializes, so this never decides the language of a launch that reaches the SDK.
            return Application.systemLanguage == SystemLanguage.Russian ? Language.RU : Language.EN;
        }
    }
}
