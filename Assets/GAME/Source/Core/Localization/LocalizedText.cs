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

        /// <remarks>
        /// A label can be shown long after <see cref="Start"/> — the double reward button only
        /// appears when an ad is ready, and the game over card freezes its layout in the same frame
        /// it is shown. Translating on activation keeps the frozen layout sized for the text that
        /// actually renders instead of the one authored in the prefab.
        /// </remarks>
        private void OnEnable()
        {
            UpdateText();
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
