using System;
using JumpRing.Game.Core.Services;
using PlatformLink;
using UnityEngine;

namespace JumpRing.Game.Core.Localization
{
    public sealed class LocalizationService : MonoBehaviour
    {
        private const string LanguagePrefsKey = StorageKeys.SelectedLanguage;

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

        /// <remarks>
        /// Flushed immediately rather than on the storage batching delay: a choice that only made it
        /// into the local mirror is discarded on the next launch by
        /// <see cref="ReconcileWithStorage"/>, and a player who switches the language and closes the
        /// tab right away is exactly the case where that delay loses the write.
        /// </remarks>
        public void SetLanguage(Language language)
        {
            ApplyLanguage(language);
            storageService.SetString(LanguagePrefsKey, language.ToString());
            storageService.FlushNow();
        }

        private void ApplyLanguage(Language language)
        {
            CurrentLanguage = language;
            activeData = language == Language.RU ? russianData : englishData;
            LanguageChanged?.Invoke(language);
        }

        /// <summary>
        /// Settles the language once the save is in, which is the first moment the game can tell an
        /// explicit choice from a leftover.
        /// </summary>
        /// <remarks>
        /// <see cref="Initialize"/> runs at Awake, where the local mirror is the only thing to read,
        /// so a language the player once picked and then cleared from the cloud still decides the
        /// first frames. Here that leftover is dropped and the platform locale gets to speak again.
        /// </remarks>
        private void ReconcileWithStorage()
        {
            bool trustStored = LanguageResolver.IsStoredChoiceTrustworthy(
                storageService.IsCloudAuthoritative,
                storageService.IsResolvedFromCloud(LanguagePrefsKey));

            if (!trustStored)
            {
                storageService.ForgetLocal(LanguagePrefsKey);
                ApplyIfChanged(DetectSystemLanguage());
                return;
            }

            string saved = storageService.GetString(LanguagePrefsKey, string.Empty);
            if (!LanguageResolver.TryParseStored(saved, out Language stored))
            {
                return;
            }

            ApplyIfChanged(stored);
        }

        private void ApplyIfChanged(Language language)
        {
            if (language == CurrentLanguage)
            {
                return;
            }

            ApplyLanguage(language);
        }

        private void Initialize()
        {
            if (PlayerPrefs.HasKey(LanguagePrefsKey)
                && LanguageResolver.TryParseStored(PlayerPrefs.GetString(LanguagePrefsKey), out Language chosen))
            {
                CurrentLanguage = chosen;
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
