using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using JumpRing.Game.Core.Services;
using JumpRing.Game.Theming;
using System.Collections;

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

        [Header("Ad Reward")]
        [SerializeField]
        private Button watchAdButton;

        [SerializeField]
        private WatchAdButtonView adButtonView;

        [SerializeField]
        private RewardedAdService rewardedAdService;

        [SerializeField]
        private int adRewardAmount = 50;

        [SerializeField]
        private float adCooldownSeconds = 180f;

        [Header("Overlay")]
        [SerializeField]
        private DimOverlay dimOverlay;

        private readonly List<ShopSkinCardView> activeCards = new();
        private Sequence openSequence;
        private float lastAdWatchTime = float.NegativeInfinity;

        public static bool IsOpen { get; private set; }

        private ICurrencyService CurrencyService => (ICurrencyService)currencyServiceComponent;

        // IsOpen is static and survives scene reloads and domain-reload-off play sessions. It can
        // not be reset from OnEnable: Close() deactivates this very GameObject, so a shop that was
        // open on exit stays disabled on the next run, OnEnable never fires, and a leftover true
        // would leave tap-to-start and jumping dead for the whole session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOpenState()
        {
            IsOpen = false;
        }

        private void OnEnable()
        {
            closeButton.onClick.AddListener(Close);
            watchAdButton.onClick.AddListener(OnWatchAdClicked);

            skinShopService.SkinPurchased += OnSkinPurchased;
            skinShopService.SkinSelected += OnSkinSelectionChanged;

            ringSizeUpgradeService.SkinUpgraded += OnSkinUpgraded;
        }

        private void OnDisable()
        {
            closeButton.onClick.RemoveListener(Close);
            watchAdButton.onClick.RemoveListener(OnWatchAdClicked);

            skinShopService.SkinPurchased -= OnSkinPurchased;
            skinShopService.SkinSelected -= OnSkinSelectionChanged;

            ringSizeUpgradeService.SkinUpgraded -= OnSkinUpgraded;
        }

        private void OnDestroy()
        {
            openSequence?.Kill();
        }

        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            UpdateAdButton();
        }

        public void Open()
        {
            openSequence?.Kill();
            gameObject.SetActive(true);
            IsOpen = true;

            dimOverlay.Show();

            shopPanel.interactable = true;
            shopPanel.blocksRaycasts = true;

            iconBar.SetActive(false);
            shopButton.SetActive(false);
            tapToStartLabel.SetActive(false);
            coinLabel.SetActive(false);
            bestScoreLabel.SetActive(false);
            tapHand.SetActive(false);

            UpdateBalance();
            RebuildGrid();
            UpdateAdButton();

            openSequence = WindowAnimations.AnimateOpen(shopPanel, shopPanel.transform);
        }

        public void Close()
        {
            openSequence?.Kill();
            IsOpen = false;

            dimOverlay.Hide();

            iconBar.SetActive(true);
            shopButton.SetActive(true);
            tapToStartLabel.SetActive(true);
            coinLabel.SetActive(true);
            bestScoreLabel.SetActive(true);
            tapHand.SetActive(true);

            openSequence = WindowAnimations.AnimateClose(shopPanel, shopPanel.transform, gameObject);
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

                    if (upgradesUnlocked)
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

                    card.ActionClicked += OnCardActionClicked;
                    activeCards.Add(card);
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)gridContent);
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
            else if (skinShopService.UpgradesUnlocked
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
                if (upgradesUnlocked)
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
            balanceLabel.SetText("{0}", CurrencyService.Balance);
        }

        private void OnWatchAdClicked()
        {
            if (!IsAdAvailable())
            {
                return;
            }

            // Only a watched ad starts the cooldown: a skipped or broken one leaves the offer
            // available, since the player never collected anything for it.
            rewardedAdService.ShowAd(result =>
            {
                if (result != RewardedAdResult.Rewarded)
                {
                    return;
                }

                lastAdWatchTime = Time.unscaledTime;
                CurrencyService.Add(adRewardAmount);
                UpdateBalance();
                RefreshCards();
                UpdateAdButton();
            });
        }

        private void UpdateAdButton()
        {
            if (!rewardedAdService.CanShowAd)
            {
                adButtonView.ShowUnavailable();
                return;
            }

            float remaining = adCooldownSeconds - (Time.unscaledTime - lastAdWatchTime);
            if (remaining > 0f)
            {
                adButtonView.ShowCooldown(remaining);
                return;
            }

            adButtonView.ShowAvailable();
        }

        private bool IsAdAvailable()
        {
            if (!rewardedAdService.CanShowAd)
            {
                return false;
            }

            float elapsed = Time.unscaledTime - lastAdWatchTime;
            return elapsed >= adCooldownSeconds;
        }
    }
}
