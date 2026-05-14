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

        [Header("Grid")]
        [SerializeField]
        private Transform gridContent;

        [SerializeField]
        private ShopSkinCardView cardPrefab;

        [Header("Balance")]
        [SerializeField]
        private TMP_Text balanceLabel;

        [Header("External")]
        [SerializeField]
        private GameObject iconBar;

        [SerializeField]
        private GameObject shopButton;

        [SerializeField]
        private GameObject bestScoreBackground;

        [SerializeField]
        private GameObject coinBackground;

        [SerializeField]
        private GameObject tapToStartLabel;

        private readonly List<ShopSkinCardView> activeCards = new();

        private ICurrencyService CurrencyService => (ICurrencyService)currencyServiceComponent;

        private void OnEnable()
        {
            if (skinShopService == null)
            {
                skinShopService = FindFirstObjectByType<SkinShopService>();
            }

            if (currencyServiceComponent == null)
            {
                var services = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
                foreach (var s in services)
                {
                    if (s is ICurrencyService)
                    {
                        currencyServiceComponent = s;
                        break;
                    }
                }
            }

            if (iconBar == null)
            {
                var candidate = GameObject.Find("IconBar");
                if (candidate == null)
                {
                    candidate = GameObject.Find("IconBar(Clone)");
                }
                iconBar = candidate;
            }

            if (shopButton == null)
            {
                shopButton = GameObject.Find("ShopButton");
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
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

            if (iconBar != null) iconBar.SetActive(false);
            if (shopButton != null) shopButton.SetActive(false);
            if (bestScoreBackground != null) bestScoreBackground.SetActive(false);
            if (coinBackground != null) coinBackground.SetActive(false);
            if (tapToStartLabel != null) tapToStartLabel.SetActive(false);

            UpdateBalance();
            RebuildGrid();
        }

        public void Close()
        {
            shopPanel.alpha = 0f;
            shopPanel.interactable = false;
            shopPanel.blocksRaycasts = false;

            if (iconBar != null) iconBar.SetActive(true);
            if (shopButton != null) shopButton.SetActive(true);
            if (bestScoreBackground != null) bestScoreBackground.SetActive(true);
            if (coinBackground != null) coinBackground.SetActive(true);
            if (tapToStartLabel != null) tapToStartLabel.SetActive(true);

            gameObject.SetActive(false);
        }

        private void RebuildGrid()
        {
            ClearGrid();

            var catalog = skinShopService.Catalog;
            if (catalog == null || catalog.Packs == null)
            {
                return;
            }

            foreach (var pack in catalog.Packs)
            {
                foreach (var skin in pack.Skins)
                {
                    var card = Instantiate(cardPrefab, gridContent);
                    var isOwned = skinShopService.IsOwned(skin);
                    var isActive = skinShopService.ActiveSkin == skin;
                    var canAfford = skinShopService.CanAfford(skin);
                    card.Setup(skin, isOwned, isActive, canAfford);
                    card.ActionClicked += OnCardActionClicked;
                    activeCards.Add(card);
                }
            }
        }

        private void ClearGrid()
        {
            foreach (var card in activeCards)
            {
                card.ActionClicked -= OnCardActionClicked;
                Destroy(card.gameObject);
            }

            activeCards.Clear();
        }

        private void OnCardActionClicked(SkinItem skin)
        {
            if (!skinShopService.IsOwned(skin))
            {
                if (skinShopService.TryPurchase(skin))
                {
                    skinShopService.SelectSkin(skin);
                }
            }
            else
            {
                skinShopService.SelectSkin(skin);
            }
        }

        private void OnSkinPurchased(SkinItem skin)
        {
            UpdateBalance();
            RefreshCards();
        }

        private void OnSkinSelectionChanged(SkinItem skin)
        {
            RefreshCards();
        }

        private void RefreshCards()
        {
            foreach (var card in activeCards)
            {
                card.UpdateState(
                    skinShopService.IsOwned(card.SkinItem),
                    skinShopService.ActiveSkin == card.SkinItem,
                    skinShopService.CanAfford(card.SkinItem)
                );
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
