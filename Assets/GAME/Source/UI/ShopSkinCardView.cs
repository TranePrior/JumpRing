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
        private GameObject ownedMark;

        [SerializeField]
        private Image selectionFrame;

        [SerializeField]
        private Button cardButton;

        public event Action<SkinItem> Clicked;

        private SkinItem skinItem;

        public SkinItem SkinItem => skinItem;

        public void Setup(SkinItem skin, bool isOwned, bool isSelected)
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

            UpdateState(isOwned, isSelected);
        }

        public void UpdateState(bool isOwned, bool isSelected)
        {
            if (priceLabel != null)
            {
                priceLabel.text = isOwned ? "" : skinItem.Price.ToString();
                priceLabel.gameObject.SetActive(!isOwned);
            }

            if (ownedMark != null)
            {
                ownedMark.SetActive(isOwned);
            }

            if (selectionFrame != null)
            {
                selectionFrame.enabled = isSelected;
            }
        }

        private void Awake()
        {
            if (cardButton != null)
            {
                cardButton.onClick.AddListener(OnClick);
            }
        }

        private void OnDestroy()
        {
            if (cardButton != null)
            {
                cardButton.onClick.RemoveListener(OnClick);
            }
        }

        private void OnClick()
        {
            Clicked?.Invoke(skinItem);
        }
    }
}
