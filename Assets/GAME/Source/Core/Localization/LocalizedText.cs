using TMPro;
using UnityEngine;

namespace JumpRing.Game.Core.Localization
{
    [RequireComponent(typeof(TMP_Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        [SerializeField]
        private LocalizationKey key;

        private TMP_Text label;

        private void Awake()
        {
            label = GetComponent<TMP_Text>();
        }

        private void Start()
        {
            UpdateText();

            if (LocalizationService.Instance != null)
                LocalizationService.Instance.LanguageChanged += OnLanguageChanged;
        }

        private void OnDestroy()
        {
            if (LocalizationService.Instance != null)
                LocalizationService.Instance.LanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged(Language language)
        {
            UpdateText();
        }

        private void UpdateText()
        {
            if (LocalizationService.Instance != null)
                label.text = LocalizationService.Instance.GetText(key);
        }
    }
}
