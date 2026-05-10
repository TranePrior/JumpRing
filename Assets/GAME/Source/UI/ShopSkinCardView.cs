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
        private Image actionButtonShadow;

        [SerializeField]
        private Image coinIcon;

        [Header("Colors")]
        [SerializeField]
        private Color buyButtonColor = new Color(0.392f, 0.584f, 0.973f);

        [SerializeField]
        private Color buyButtonShadowColor = new Color(0.122f, 0.278f, 0.675f);

        [SerializeField]
        private Color activeButtonColor = new Color(0.675f, 0.82f, 0.235f);

        [SerializeField]
        private Color activeButtonShadowColor = new Color(0.294f, 0.424f, 0.078f);

        [SerializeField]
        private Color disabledButtonColor = new Color(0.5f, 0.5f, 0.5f);

        [SerializeField]
        private Color disabledButtonShadowColor = new Color(0.3f, 0.3f, 0.3f);

        public event Action<SkinItem> Clicked;
        public event Action<SkinItem> ActionClicked;

        private SkinItem skinItem;

        public SkinItem SkinItem => skinItem;

        public void Setup(SkinItem skin, bool isOwned, bool isActive, bool canAfford)
        {
            skinItem = skin;

            if (iconImage != null)
            {
                iconImage.sprite = skin.Icon;
                iconImage.enabled = skin.Icon != null;
            }

            if (nameLabel != null)
            {
                nameLabel.text = skin.DisplayName;
            }

            UpdateState(isOwned, isActive, canAfford);
        }

        public void UpdateState(bool isOwned, bool isActive, bool canAfford)
        {
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

                if (actionButtonImage != null)
                {
                    actionButtonImage.color = canAfford ? buyButtonColor : disabledButtonColor;
                }

                if (actionButtonShadow != null)
                {
                    actionButtonShadow.color = canAfford ? buyButtonShadowColor : disabledButtonShadowColor;
                }

                if (actionButton != null)
                {
                    actionButton.interactable = canAfford;
                }
            }
            else if (isActive)
            {
                if (priceLabel != null)
                {
                    priceLabel.gameObject.SetActive(false);
                }

                if (coinIcon != null)
                {
                    coinIcon.gameObject.SetActive(false);
                }

                if (actionButtonLabel != null)
                {
                    actionButtonLabel.text = "\u0410\u043a\u0442\u0438\u0432\u0435\u043d";
                    actionButtonLabel.gameObject.SetActive(true);
                }

                if (actionButtonImage != null)
                {
                    actionButtonImage.color = activeButtonColor;
                }

                if (actionButtonShadow != null)
                {
                    actionButtonShadow.color = activeButtonShadowColor;
                }

                if (actionButton != null)
                {
                    actionButton.interactable = false;
                }
            }
            else
            {
                if (priceLabel != null)
                {
                    priceLabel.gameObject.SetActive(false);
                }

                if (coinIcon != null)
                {
                    coinIcon.gameObject.SetActive(false);
                }

                if (actionButtonLabel != null)
                {
                    actionButtonLabel.text = "\u0412\u044b\u0431\u0440\u0430\u0442\u044c";
                    actionButtonLabel.gameObject.SetActive(true);
                }

                if (actionButtonImage != null)
                {
                    actionButtonImage.color = buyButtonColor;
                }

                if (actionButtonShadow != null)
                {
                    actionButtonShadow.color = buyButtonShadowColor;
                }

                if (actionButton != null)
                {
                    actionButton.interactable = true;
                }
            }

            if (selectionFrame != null)
            {
                selectionFrame.enabled = isActive;
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
