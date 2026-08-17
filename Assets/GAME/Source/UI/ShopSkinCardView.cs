using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using JumpRing.Game.Core.Localization;
using JumpRing.Game.Theming;

namespace JumpRing.Game.UI
{
    public sealed class ShopSkinCardView : MonoBehaviour
    {
        [SerializeField]
        private Image iconImage;

        [SerializeField]
        private Image skinImage;

        [SerializeField]
        private TMP_Text nameLabel;

        [SerializeField]
        private TMP_Text priceLabel;

        [Header("Card Button")]
        [SerializeField]
        private Button cardButton;

        [Header("Action Button")]
        [SerializeField]
        private Button actionButton;

        [SerializeField]
        private TMP_Text actionButtonLabel;

        [SerializeField]
        private Image actionButtonImage;

        [SerializeField]
        private Image coinIcon;

        [SerializeField]
        private Image currencyIconImage;

        [Header("Upgrade")]
        [SerializeField]
        private TMP_Text upgradeLevelLabel;

        [Header("Button Sprites")]
        [SerializeField]
        private Sprite buyButtonSprite;

        [SerializeField]
        private Sprite activeButtonSprite;

        [SerializeField]
        private Sprite activateButtonSprite;

        [Header("Button Colors")]
        [SerializeField]
        private Color buyButtonColor = Color.white;

        [SerializeField]
        private Color activeButtonColor = Color.white;

        [SerializeField]
        private Color activateButtonColor = Color.white;

        [SerializeField]
        private float disabledContentAlpha = 0.5f;

        /// <summary>Raised by the action button: buy, upgrade or select, depending on card state.</summary>
        public event Action<SkinItem> ActionClicked;

        /// <summary>Raised by a click anywhere on the card outside the action button.</summary>
        public event Action<SkinItem> SelectClicked;

        private SkinItem skinItem;

        public SkinItem SkinItem => skinItem;

        public void Setup(SkinItem skin)
        {
            skinItem = skin;

            bool hasShopSprite = skin.ShopSprite != null;

            if (skinImage != null)
            {
                skinImage.sprite = skin.ShopSprite;
                skinImage.enabled = hasShopSprite;
            }

            if (iconImage != null)
            {
                iconImage.sprite = hasShopSprite ? null : skin.Icon;
                iconImage.enabled = !hasShopSprite && skin.Icon != null;
            }

            if (nameLabel != null)
            {
                nameLabel.text = GetLocalizedText(skin.NameKey);
            }

            if (currencyIconImage != null)
            {
                currencyIconImage.sprite = skin.CurrencyIcon;
                currencyIconImage.enabled = skin.CurrencyIcon != null;
            }
        }

        public void UpdateState(in SkinCardState state)
        {
            upgradeLevelLabel.gameObject.SetActive(state.IsOwned);

            if (state.IsOwned)
            {
                ShowUpgradeLevel(state);
            }

            switch (state.ResolveAction())
            {
                case SkinCardAction.Buy:
                    // A locked skin has nothing to select yet, so the whole card buys it.
                    ShowPriceContent(skinItem.Price);
                    SetButtonState(buyButtonSprite, buyButtonColor);
                    SetContentAlpha(state.CanAfford ? 1f : disabledContentAlpha);
                    break;

                case SkinCardAction.Upgrade:
                    // Owned: the button buys the next ring size, the card body selects the skin. Its
                    // colour is the only marker of the active skin until the ring is fully upgraded.
                    ShowPriceContent(state.UpgradePrice);
                    SetButtonState(
                        state.IsActive ? activeButtonSprite : buyButtonSprite,
                        state.IsActive ? activeButtonColor : buyButtonColor);
                    SetContentAlpha(state.CanAffordUpgrade ? 1f : disabledContentAlpha);
                    break;

                case SkinCardAction.Select:
                    // Fully upgraded: nothing left to sell, so the button selects too.
                    ShowLabelContent(GetLocalizedText(state.IsActive ? LocalizationKey.Active : LocalizationKey.Select));
                    SetButtonState(
                        state.IsActive ? activeButtonSprite : activateButtonSprite,
                        state.IsActive ? activeButtonColor : activateButtonColor);
                    SetContentAlpha(1f);
                    break;
            }

            actionButton.interactable = state.IsActionAvailable();
            cardButton.interactable = state.IsCardClickable();
        }

        private void ShowUpgradeLevel(in SkinCardState state)
        {
            upgradeLevelLabel.gameObject.SetActive(true);
            upgradeLevelLabel.text = state.IsMaxUpgraded
                ? GetLocalizedText(LocalizationKey.MaxUpgradeLevel)
                : string.Format(
                    GetLocalizedText(LocalizationKey.UpgradeLevel),
                    state.UpgradeLevel,
                    state.MaxUpgradeLevel);
        }

        private void ShowPriceContent(int price)
        {
            priceLabel.SetText("{0}", price);
            priceLabel.gameObject.SetActive(true);
            coinIcon.gameObject.SetActive(true);
            actionButtonLabel.gameObject.SetActive(false);
        }

        private void ShowLabelContent(string text)
        {
            priceLabel.gameObject.SetActive(false);
            coinIcon.gameObject.SetActive(false);

            actionButtonLabel.text = text;
            actionButtonLabel.gameObject.SetActive(true);
        }

        private void SetContentAlpha(float alpha)
        {
            priceLabel.alpha = alpha;

            var coinColor = coinIcon.color;
            coinColor.a = alpha;
            coinIcon.color = coinColor;
        }

        private void SetButtonState(Sprite sprite, Color buttonColor)
        {
            actionButtonImage.sprite = sprite;
            actionButtonImage.color = buttonColor;
        }

        private void Awake()
        {
            actionButton.onClick.AddListener(OnActionClick);
            cardButton.onClick.AddListener(OnCardClick);
        }

        private void OnDestroy()
        {
            actionButton.onClick.RemoveListener(OnActionClick);
            cardButton.onClick.RemoveListener(OnCardClick);
        }

        private void OnActionClick()
        {
            ActionClicked?.Invoke(skinItem);
        }

        private void OnCardClick()
        {
            SelectClicked?.Invoke(skinItem);
        }

        private static string GetLocalizedText(LocalizationKey key)
        {
            return LocalizationService.Instance != null
                ? LocalizationService.Instance.GetText(key)
                : key.ToString();
        }
    }
}
