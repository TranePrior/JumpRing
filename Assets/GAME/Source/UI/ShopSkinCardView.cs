using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
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

        [SerializeField]
        private Image selectionFrame;

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

        [Header("Button Colors")]
        [SerializeField]
        private Color buyButtonColor = Color.white;

        [SerializeField]
        private Color activeButtonColor = Color.white;

        [SerializeField]
        private Color disabledButtonColor = new Color(0.6f, 0.6f, 0.6f, 1f);

        public event Action<SkinItem> Clicked;
        public event Action<SkinItem> ActionClicked;

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
                nameLabel.text = skin.DisplayName;
            }

            if (currencyIconImage != null)
            {
                currencyIconImage.sprite = skin.CurrencyIcon;
                currencyIconImage.enabled = skin.CurrencyIcon != null;
            }
        }

        public void UpdateState(bool isOwned, bool isActive, bool canAfford)
        {
            UpdateState(isOwned, isActive, canAfford, false, 0, 0, 0, false);
        }

        public void UpdateState(
            bool isOwned,
            bool isActive,
            bool canAfford,
            bool upgradesUnlocked,
            int upgradeLevel,
            int maxLevel,
            int upgradePrice,
            bool canAffordUpgrade)
        {
            SetUpgradeLevelVisible(false);

            if (!isOwned)
            {
                if (priceLabel != null)
                {
                    priceLabel.text = skinItem.Price.ToString();
                    priceLabel.gameObject.SetActive(true);
                }

                if (coinIcon != null)
                {
                    coinIcon.gameObject.SetActive(true);
                }

                if (actionButtonLabel != null)
                {
                    actionButtonLabel.gameObject.SetActive(false);
                }

                SetButtonState(buyButtonSprite, canAfford ? buyButtonColor : disabledButtonColor);

                if (actionButton != null)
                {
                    actionButton.interactable = canAfford;
                }
            }
            else if (upgradesUnlocked)
            {
                bool isMaxed = upgradeLevel >= maxLevel;

                SetUpgradeLevelVisible(true, upgradeLevel, maxLevel);

                if (isMaxed)
                {
                    if (priceLabel != null) priceLabel.gameObject.SetActive(false);
                    if (coinIcon != null) coinIcon.gameObject.SetActive(false);

                    if (actionButtonLabel != null)
                    {
                        actionButtonLabel.text = isActive ? "Активен" : "Выбрать";
                        actionButtonLabel.gameObject.SetActive(true);
                    }

                    SetButtonState(
                        isActive ? activeButtonSprite : buyButtonSprite,
                        isActive ? activeButtonColor : buyButtonColor);
                    if (actionButton != null) actionButton.interactable = true;
                }
                else
                {
                    if (priceLabel != null)
                    {
                        priceLabel.text = upgradePrice.ToString();
                        priceLabel.gameObject.SetActive(true);
                    }

                    if (coinIcon != null) coinIcon.gameObject.SetActive(true);
                    if (actionButtonLabel != null) actionButtonLabel.gameObject.SetActive(false);

                    SetButtonState(
                        isActive ? activeButtonSprite : buyButtonSprite,
                        canAffordUpgrade
                            ? (isActive ? activeButtonColor : buyButtonColor)
                            : disabledButtonColor);
                    if (actionButton != null) actionButton.interactable = canAffordUpgrade;
                }
            }
            else
            {
                if (priceLabel != null) priceLabel.gameObject.SetActive(false);
                if (coinIcon != null) coinIcon.gameObject.SetActive(false);

                if (actionButtonLabel != null)
                {
                    actionButtonLabel.text = isActive ? "Активен" : "Выбрать";
                    actionButtonLabel.gameObject.SetActive(true);
                }

                SetButtonState(
                    isActive ? activeButtonSprite : buyButtonSprite,
                    isActive ? activeButtonColor : buyButtonColor);
                if (actionButton != null) actionButton.interactable = true;
            }

            if (selectionFrame != null)
            {
                selectionFrame.enabled = false;
            }
        }

        private void SetUpgradeLevelVisible(bool visible, int level = 0, int max = 0)
        {
            if (upgradeLevelLabel == null)
            {
                return;
            }

            upgradeLevelLabel.gameObject.SetActive(visible);
            if (visible)
            {
                upgradeLevelLabel.text = $"Ур. {level}/{max}";
            }
        }

        private void SetButtonState(Sprite sprite, Color buttonColor)
        {
            if (actionButtonImage != null)
            {
                if (sprite != null)
                    actionButtonImage.sprite = sprite;
                actionButtonImage.color = buttonColor;
            }
        }

        private void Awake()
        {
            if (cardButton != null)
            {
                cardButton.onClick.AddListener(OnClick);
            }

            if (actionButton != null)
            {
                actionButton.onClick.AddListener(OnActionClick);
            }
        }

        private void OnDestroy()
        {
            if (cardButton != null)
            {
                cardButton.onClick.RemoveListener(OnClick);
            }

            if (actionButton != null)
            {
                actionButton.onClick.RemoveListener(OnActionClick);
            }
        }

        private void OnClick()
        {
            Clicked?.Invoke(skinItem);
        }

        private void OnActionClick()
        {
            ActionClicked?.Invoke(skinItem);
        }
    }
}
