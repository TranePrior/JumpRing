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
        private RingSizeUpgradeService ringSizeUpgradeService;

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
        private GameObject tapToStartLabel;

        [SerializeField]
        private GameObject coinLabel;

        [SerializeField]
        private GameObject bestScoreLabel;

        [SerializeField]
        private GameObject tapHand;

        [Header("Overlay")]
        [SerializeField]
        private DimOverlay dimOverlay;

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

            if (ringSizeUpgradeService != null)
            {
                ringSizeUpgradeService.SkinUpgraded += OnSkinUpgraded;
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

            if (ringSizeUpgradeService != null)
            {
                ringSizeUpgradeService.SkinUpgraded -= OnSkinUpgraded;
            }
        }

        public void Open()
        {
            gameObject.SetActive(true);

            if (dimOverlay != null) dimOverlay.Show();

            shopPanel.alpha = 1f;
            shopPanel.interactable = true;
            shopPanel.blocksRaycasts = true;

            if (iconBar != null) iconBar.SetActive(false);
            if (shopButton != null) shopButton.SetActive(false);
            if (tapToStartLabel != null) tapToStartLabel.SetActive(false);
            if (coinLabel != null) coinLabel.SetActive(false);
            if (bestScoreLabel != null) bestScoreLabel.SetActive(false);
            if (tapHand != null) tapHand.SetActive(false);

            UpdateBalance();
            RebuildGrid();
        }

        public void Close()
        {
            shopPanel.alpha = 0f;
            shopPanel.interactable = false;
            shopPanel.blocksRaycasts = false;

            if (dimOverlay != null) dimOverlay.Hide();

            if (iconBar != null) iconBar.SetActive(true);
            if (shopButton != null) shopButton.SetActive(true);
            if (tapToStartLabel != null) tapToStartLabel.SetActive(true);
            if (coinLabel != null) coinLabel.SetActive(true);
            if (bestScoreLabel != null) bestScoreLabel.SetActive(true);
            if (tapHand != null) tapHand.SetActive(true);

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

            bool upgradesUnlocked = skinShopService.UpgradesUnlocked;

            foreach (var pack in catalog.Packs)
            {
                foreach (var skin in pack.Skins)
                {
                    var card = Instantiate(cardPrefab, gridContent);
                    card.Setup(skin);

                    if (upgradesUnlocked && ringSizeUpgradeService != null)
                    {
                        card.UpdateState(
                            skinShopService.IsOwned(skin),
                            skinShopService.ActiveSkin == skin,
                            skinShopService.CanAfford(skin),
                            true,
                            ringSizeUpgradeService.GetLevel(skin.SkinId),
                            ringSizeUpgradeService.MaxLevel,
                            ringSizeUpgradeService.GetUpgradePrice(skin),
                            ringSizeUpgradeService.CanAffordUpgrade(skin)
                        );
                    }
                    else
                    {
                        card.UpdateState(
                            skinShopService.IsOwned(skin),
                            skinShopService.ActiveSkin == skin,
                            skinShopService.CanAfford(skin)
                        );
                    }

                    card.Clicked += OnCardClicked;
                    card.ActionClicked += OnCardActionClicked;
                    activeCards.Add(card);
                }
            }
        }

        private void ClearGrid()
        {
            foreach (var card in activeCards)
            {
                card.Clicked -= OnCardClicked;
                card.ActionClicked -= OnCardActionClicked;
                Destroy(card.gameObject);
            }

            activeCards.Clear();
        }

        private void OnCardClicked(SkinItem skin)
        {
            if (skinShopService.IsOwned(skin))
            {
                skinShopService.SelectSkin(skin);
            }
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
            else if (skinShopService.UpgradesUnlocked
                     && ringSizeUpgradeService != null
                     && !ringSizeUpgradeService.IsMaxed(skin.SkinId))
            {
                if (ringSizeUpgradeService.TryUpgrade(skin))
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

        private void OnSkinUpgraded(SkinItem skin, int level)
        {
            UpdateBalance();
            RefreshCards();
        }

        private void RefreshCards()
        {
            bool upgradesUnlocked = skinShopService.UpgradesUnlocked;

            foreach (var card in activeCards)
            {
                var skinItem = card.SkinItem;
                if (upgradesUnlocked && ringSizeUpgradeService != null)
                {
                    card.UpdateState(
                        skinShopService.IsOwned(skinItem),
                        skinShopService.ActiveSkin == skinItem,
                        skinShopService.CanAfford(skinItem),
                        true,
                        ringSizeUpgradeService.GetLevel(skinItem.SkinId),
                        ringSizeUpgradeService.MaxLevel,
                        ringSizeUpgradeService.GetUpgradePrice(skinItem),
                        ringSizeUpgradeService.CanAffordUpgrade(skinItem)
                    );
                }
                else
                {
                    card.UpdateState(
                        skinShopService.IsOwned(skinItem),
                        skinShopService.ActiveSkin == skinItem,
                        skinShopService.CanAfford(skinItem)
                    );
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
