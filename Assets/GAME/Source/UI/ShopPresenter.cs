using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using JumpRing.Game.Core.Services;
using JumpRing.Game.Theming;

namespace JumpRing.Game.UI
{
    public sealed class ShopPresenter : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField]
        private SkinShopService skinShopService;

        [SerializeField]
        private MonoBehaviour currencyServiceComponent;

        [Header("Panels")]
        [SerializeField]
        private CanvasGroup shopPanel;

        [Header("Header")]
        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private TMP_Text balanceLabel;

        [Header("Pack Tabs")]
        [SerializeField]
        private Button[] packTabButtons;

        [SerializeField]
        private Image[] packTabHighlights;

        [Header("Grid")]
        [SerializeField]
        private Transform gridContent;

        [SerializeField]
        private ShopSkinCardView cardPrefab;

        [Header("Action Panel")]
        [SerializeField]
        private TMP_Text selectedSkinNameLabel;

        [SerializeField]
        private Button actionButton;

        [SerializeField]
        private TMP_Text actionButtonLabel;

        [Header("Colors")]
        [SerializeField]
        private Color buyColor = new Color(1f, 0.84f, 0f);

        [SerializeField]
        private Color selectColor = new Color(0f, 0.8f, 0.2f);

        [SerializeField]
        private Color disabledColor = new Color(0.5f, 0.5f, 0.5f);

        private readonly List<ShopSkinCardView> activeCards = new();
        private int currentPackIndex;
        private SkinItem selectedCard;

        private ICurrencyService CurrencyService => (ICurrencyService)currencyServiceComponent;

        private void OnEnable()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }

            if (actionButton != null)
            {
                actionButton.onClick.AddListener(OnActionButtonClicked);
            }

            for (var i = 0; i < packTabButtons.Length; i++)
            {
                var index = i;
                packTabButtons[i].onClick.AddListener(() => SelectPack(index));
            }

            if (skinShopService != null)
            {
                skinShopService.SkinPurchased += OnSkinPurchased;
                skinShopService.SkinSelected += OnSkinSelectionChanged;
            }
        }

        private void OnDisable()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
            }

            if (actionButton != null)
            {
                actionButton.onClick.RemoveListener(OnActionButtonClicked);
            }

            for (var i = 0; i < packTabButtons.Length; i++)
            {
                packTabButtons[i].onClick.RemoveAllListeners();
            }

            if (skinShopService != null)
            {
                skinShopService.SkinPurchased -= OnSkinPurchased;
                skinShopService.SkinSelected -= OnSkinSelectionChanged;
            }
        }

        public void Open()
        {
            gameObject.SetActive(true);
            shopPanel.alpha = 1f;
            shopPanel.interactable = true;
            shopPanel.blocksRaycasts = true;

            UpdateBalance();
            SelectPack(0);
        }

        public void Close()
        {
            shopPanel.alpha = 0f;
            shopPanel.interactable = false;
            shopPanel.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        private void SelectPack(int index)
        {
            currentPackIndex = index;

            for (var i = 0; i < packTabHighlights.Length; i++)
            {
                if (packTabHighlights[i] != null)
                {
                    packTabHighlights[i].enabled = i == index;
                }
            }

            RebuildGrid();
        }

        private void RebuildGrid()
        {
            ClearGrid();

            var catalog = skinShopService.Catalog;
            if (catalog == null || catalog.Packs == null || currentPackIndex >= catalog.Packs.Length)
            {
                return;
            }

            var pack = catalog.Packs[currentPackIndex];
            selectedCard = null;

            foreach (var skin in pack.Skins)
            {
                var card = Instantiate(cardPrefab, gridContent);
                var isOwned = skinShopService.IsOwned(skin);
                var isSelected = skinShopService.ActiveSkin == skin;
                card.Setup(skin, isOwned, isSelected);
                card.Clicked += OnCardClicked;
                activeCards.Add(card);

                if (isSelected)
                {
                    selectedCard = skin;
                }
            }

            if (selectedCard == null && pack.Skins.Length > 0)
            {
                selectedCard = pack.Skins[0];
            }

            UpdateActionPanel();
        }

        private void ClearGrid()
        {
            foreach (var card in activeCards)
            {
                card.Clicked -= OnCardClicked;
                Destroy(card.gameObject);
            }

            activeCards.Clear();
        }

        private void OnCardClicked(SkinItem skin)
        {
            selectedCard = skin;

            foreach (var card in activeCards)
            {
                card.UpdateState(
                    skinShopService.IsOwned(card.SkinItem),
                    card.SkinItem == selectedCard
                );
            }

            UpdateActionPanel();
        }

        private void OnActionButtonClicked()
        {
            if (selectedCard == null)
            {
                return;
            }

            if (!skinShopService.IsOwned(selectedCard))
            {
                if (skinShopService.TryPurchase(selectedCard))
                {
                    skinShopService.SelectSkin(selectedCard);
                }
            }
            else
            {
                skinShopService.SelectSkin(selectedCard);
            }
        }

        private void OnSkinPurchased(SkinItem skin)
        {
            UpdateBalance();
            RefreshCards();
            UpdateActionPanel();
        }

        private void OnSkinSelectionChanged(SkinItem skin)
        {
            RefreshCards();
            UpdateActionPanel();
        }

        private void RefreshCards()
        {
            foreach (var card in activeCards)
            {
                card.UpdateState(
                    skinShopService.IsOwned(card.SkinItem),
                    card.SkinItem == selectedCard
                );
            }
        }

        private void UpdateActionPanel()
        {
            if (selectedCard == null)
            {
                if (selectedSkinNameLabel != null)
                {
                    selectedSkinNameLabel.text = "";
                }

                if (actionButton != null)
                {
                    actionButton.interactable = false;
                }

                return;
            }

            if (selectedSkinNameLabel != null)
            {
                selectedSkinNameLabel.text = selectedCard.DisplayName;
            }

            var isOwned = skinShopService.IsOwned(selectedCard);
            var isActive = skinShopService.ActiveSkin == selectedCard;

            if (actionButton != null)
            {
                var buttonImage = actionButton.GetComponent<Image>();

                if (!isOwned)
                {
                    var canAfford = skinShopService.CanAfford(selectedCard);
                    actionButtonLabel.text = selectedCard.Price + " BUY";
                    actionButton.interactable = canAfford;

                    if (buttonImage != null)
                    {
                        buttonImage.color = canAfford ? buyColor : disabledColor;
                    }
                }
                else if (isActive)
                {
                    actionButtonLabel.text = "SELECTED";
                    actionButton.interactable = false;

                    if (buttonImage != null)
                    {
                        buttonImage.color = disabledColor;
                    }
                }
                else
                {
                    actionButtonLabel.text = "SELECT";
                    actionButton.interactable = true;

                    if (buttonImage != null)
                    {
                        buttonImage.color = selectColor;
                    }
                }
            }
        }

        private void UpdateBalance()
        {
            if (balanceLabel != null && CurrencyService != null)
            {
                balanceLabel.text = CurrencyService.Balance.ToString();
            }
        }
    }
}
