using System;
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

        private LocalizationData activeData;

        public Language CurrentLanguage { get; private set; }

        public event Action<Language> LanguageChanged;

        public static LocalizationService Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            Initialize();
        }

        private void OnDestroy()
        {
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
            CurrentLanguage = language;
            activeData = language == Language.RU ? russianData : englishData;
            PlayerPrefs.SetString(LanguagePrefsKey, language.ToString());
            PlayerPrefs.Save();
            LanguageChanged?.Invoke(language);
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
