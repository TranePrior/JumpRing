using System;
using JumpRing.Game.Core.Services;
using PlatformLink;
using UnityEngine;

namespace JumpRing.Game.Core.Localization
{
    public sealed class LocalizationService : MonoBehaviour
    {
        private const string LanguagePrefsKey = "SelectedLanguage";

        [SerializeField]
        private LocalizationData russianData;

        [SerializeField]
        private LocalizationData englishData;

        [SerializeField]
        private PlatformStorageService storageService;

        private LocalizationData activeData;

        public Language CurrentLanguage { get; private set; }

        public event Action<Language> LanguageChanged;

        public static LocalizationService Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            Initialize();

            // PLink isn't ready at Awake, so a first-launch default may have fallen back to
            // the unreliable Application.systemLanguage. Re-detect from the platform once it
            // initializes — but only when the player has no explicit saved preference.
            if (!PlayerPrefs.HasKey(LanguagePrefsKey))
            {
                if (PLink.IsInitialized)
                {
                    ApplyLanguage(DetectSystemLanguage());
                }
                else
                {
                    PLink.Initilized += OnPlinkReadyDetectLanguage;
                }
            }
        }

        private void OnPlinkReadyDetectLanguage()
        {
            PLink.Initilized -= OnPlinkReadyDetectLanguage;

            if (!PlayerPrefs.HasKey(LanguagePrefsKey))
            {
                ApplyLanguage(DetectSystemLanguage());
            }
        }

        private void Start()
        {
            if (storageService.IsLoaded)
            {
                ReconcileWithStorage();
            }
            else
            {
                storageService.Loaded += ReconcileWithStorage;
            }
        }

        private void OnDestroy()
        {
            storageService.Loaded -= ReconcileWithStorage;
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

        public void SetLanguage(Language language)
        {
            ApplyLanguage(language);
            storageService.SetString(LanguagePrefsKey, language.ToString());
        }

        private void ApplyLanguage(Language language)
        {
            CurrentLanguage = language;
            activeData = language == Language.RU ? russianData : englishData;
            LanguageChanged?.Invoke(language);
        }

        private void ReconcileWithStorage()
        {
            string saved = storageService.GetString(LanguagePrefsKey, string.Empty);
            if (string.IsNullOrEmpty(saved))
            {
                return;
            }

            Language stored = saved == Language.EN.ToString() ? Language.EN : Language.RU;
            if (stored != CurrentLanguage)
            {
                ApplyLanguage(stored);
            }
        }

        private void Initialize()
        {
            if (PlayerPrefs.HasKey(LanguagePrefsKey))
            {
                string saved = PlayerPrefs.GetString(LanguagePrefsKey);
                CurrentLanguage = saved == Language.EN.ToString() ? Language.EN : Language.RU;
            }
            else
            {
                CurrentLanguage = DetectSystemLanguage();
            }

            activeData = CurrentLanguage == Language.RU ? russianData : englishData;
        }

        private static Language DetectSystemLanguage()
        {
            if (PLink.IsInitialized)
            {
                string platformLang = PLink.Environment.Language;
                if (!string.IsNullOrEmpty(platformLang))
                {
                    return platformLang == "ru" ? Language.RU : Language.EN;
                }
            }

            return Application.systemLanguage == SystemLanguage.Russian ? Language.RU : Language.EN;
        }
    }
}
