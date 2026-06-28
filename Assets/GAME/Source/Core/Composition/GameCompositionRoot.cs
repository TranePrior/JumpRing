using System;
using UnityEngine;
using JumpRing.Game.Core.Services;
using JumpRing.Game.Core.State;
using JumpRing.Game.Gameplay;
using JumpRing.Game.Theming;
using JumpRing.Game.UI;
using PlatformLink;

namespace JumpRing.Game.Core.Composition
{
    public sealed class GameCompositionRoot : MonoBehaviour
    {
        [Header("Core")]
        [SerializeField]
        private MonoBehaviour gameStateMachineComponent;

        [SerializeField]
        private MonoBehaviour scoreServiceComponent;

        [SerializeField]
        private MonoBehaviour currencyServiceComponent;

        [Header("Feature Entry Points")]
        [SerializeField]
        private RunSessionController runSessionController;

        [SerializeField]
        private HudPresenter hudPresenter;

        [SerializeField]
        private WorldTapCounterPresenter worldTapCounterPresenter;

        [Header("Theming")]
        [SerializeField]
        private ThemeManager themeManager;

        [SerializeField]
        private SkinShopService skinShopService;

        [Header("Difficulty Systems")]
        [SerializeField]
        private DifficultyManager difficultyManager;

        [SerializeField]
        private MicroEventSystem microEventSystem;

        [SerializeField]
        private RiskRewardSystem riskRewardSystem;

        [Header("Bonus System")]
        [SerializeField]
        private BonusEffectManager bonusEffectManager;

        [Header("No Ads")]
        [SerializeField]
        private NoAdsService noAdsService;

        [Header("Audio Settings")]
        [SerializeField]
        private AudioSettingsService audioSettingsService;

        [Header("Ring Upgrade")]
        [SerializeField]
        private RingSizeUpgradeService ringSizeUpgradeService;

        [SerializeField]
        private PlayerJumpController playerJumpController;

        [Header("Platform")]
        [SerializeField]
        private PlatformStorageService platformStorageService;

        [SerializeField]
        private GameplayApiService gameplayApiService;

        [SerializeField]
        private VibrationFeedbackService vibrationFeedbackService;

        private IGameStateMachine GameStateMachine => (IGameStateMachine)gameStateMachineComponent;

        private IScoreService ScoreService => (IScoreService)scoreServiceComponent;

        private ICurrencyService CurrencyService => (ICurrencyService)currencyServiceComponent;

        private static readonly string[] StorageIntKeys =
        {
            "DiamondBalance", "BestScore", "BonusSpawner_ConsecutiveDeaths", "NoAdsPurchased",
            "Settings_Music", "Settings_Effects", "Settings_Vibration"
        };

        private static readonly string[] StorageStringKeys =
        {
            "OwnedSkins", "ActiveSkinId", "SkinUpgrades", "SelectedLanguage"
        };

        private bool _storageReady;
        private bool _plinkReady;
        private bool _loadingFinished;

        private void Awake()
        {
            try
            {
                PLink.Initialize(OnPlinkInitialized);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameCompositionRoot] PLink init failed: {e.Message}");
                _plinkReady = true;
            }

            // Always kick off storage loading so the game initializes even if the
            // platform never signals readiness. PlatformStorageService waits for PLink
            // internally (with a timeout) before choosing cloud vs local data.
            if (platformStorageService != null)
            {
                platformStorageService.Initialize(StorageIntKeys, StorageStringKeys, OnStorageReady);
            }
            else
            {
                OnStorageReady();
            }
        }

        private void OnPlinkInitialized()
        {
            _plinkReady = true;
            TryFinishLoading();
        }

        private void TryFinishLoading()
        {
            if (_loadingFinished || !_storageReady || !_plinkReady)
            {
                return;
            }

            _loadingFinished = true;
            PLink.Analytics.SendGameReady();
            PLink.Environment.CloseLoadingScreen();
        }

        private void OnStorageReady()
        {
            runSessionController.Construct(GameStateMachine, ScoreService);
            hudPresenter.Construct(ScoreService, CurrencyService);

            if (worldTapCounterPresenter != null)
            {
                worldTapCounterPresenter.Construct(runSessionController);
            }

            if (difficultyManager != null)
            {
                runSessionController.RunStarted += difficultyManager.OnRunStarted;
                runSessionController.RunFinished += difficultyManager.OnRunFinished;
            }

            if (microEventSystem != null)
            {
                runSessionController.RunStarted += microEventSystem.OnRunStarted;
                runSessionController.RunFinished += microEventSystem.OnRunFinished;
            }

            if (riskRewardSystem != null)
            {
                runSessionController.RunStarted += riskRewardSystem.OnRunStarted;
                runSessionController.RunFinished += riskRewardSystem.OnRunFinished;
            }

            if (bonusEffectManager != null)
            {
                runSessionController.RunStarted += bonusEffectManager.OnRunStarted;
                runSessionController.RunFinished += bonusEffectManager.OnRunFinished;
            }

            runSessionController.RunStarted += OnRunStartedResetEarnings;

            if (noAdsService != null)
            {
                noAdsService.Initialize();
            }

            if (audioSettingsService != null)
            {
                audioSettingsService.Initialize();
            }

            if (skinShopService != null)
            {
                skinShopService.Initialize();
            }

            if (ringSizeUpgradeService != null)
            {
                ringSizeUpgradeService.Initialize();
            }

            if (themeManager != null)
            {
                if (skinShopService != null && skinShopService.ActiveSkin != null)
                {
                    var pack = skinShopService.Catalog.FindPackForSkin(skinShopService.ActiveSkin);
                    themeManager.ApplyTheme(skinShopService.ActiveSkin.ThemeData, pack);
                }
                else
                {
                    themeManager.Initialize();
                }
            }

            ApplyRingSizeBonus();

            if (skinShopService != null)
            {
                skinShopService.SkinSelected += OnSkinSelectedForSizeBonus;
            }

            if (ringSizeUpgradeService != null)
            {
                ringSizeUpgradeService.SkinUpgraded += OnSkinUpgradedForSizeBonus;
            }

            if (gameplayApiService != null)
            {
                GameStateMachine.StateChanged += gameplayApiService.OnStateChanged;
            }

            if (vibrationFeedbackService != null)
            {
                runSessionController.DeathRequested += vibrationFeedbackService.OnDeath;

                if (playerJumpController != null)
                {
                    playerJumpController.Jumped += vibrationFeedbackService.OnJump;
                }
            }

            GameStateMachine.Enter(GameState.MainMenu);

            _storageReady = true;
            TryFinishLoading();
        }

        private void OnDestroy()
        {
            runSessionController.RunStarted -= OnRunStartedResetEarnings;

            if (gameplayApiService != null)
            {
                GameStateMachine.StateChanged -= gameplayApiService.OnStateChanged;
            }

            if (vibrationFeedbackService != null)
            {
                runSessionController.DeathRequested -= vibrationFeedbackService.OnDeath;

                if (playerJumpController != null)
                {
                    playerJumpController.Jumped -= vibrationFeedbackService.OnJump;
                }
            }

            if (difficultyManager != null)
            {
                runSessionController.RunStarted -= difficultyManager.OnRunStarted;
                runSessionController.RunFinished -= difficultyManager.OnRunFinished;
            }

            if (microEventSystem != null)
            {
                runSessionController.RunStarted -= microEventSystem.OnRunStarted;
                runSessionController.RunFinished -= microEventSystem.OnRunFinished;
            }

            if (riskRewardSystem != null)
            {
                runSessionController.RunStarted -= riskRewardSystem.OnRunStarted;
                runSessionController.RunFinished -= riskRewardSystem.OnRunFinished;
            }

            if (bonusEffectManager != null)
            {
                runSessionController.RunStarted -= bonusEffectManager.OnRunStarted;
                runSessionController.RunFinished -= bonusEffectManager.OnRunFinished;
            }

            if (skinShopService != null)
            {
                skinShopService.SkinSelected -= OnSkinSelectedForSizeBonus;
            }

            if (ringSizeUpgradeService != null)
            {
                ringSizeUpgradeService.SkinUpgraded -= OnSkinUpgradedForSizeBonus;
            }
        }

        private void OnRunStartedResetEarnings()
        {
            CurrencyService.ResetRunEarnings();
        }

        private void OnSkinSelectedForSizeBonus(SkinItem skin)
        {
            ApplyRingSizeBonus();
        }

        private void OnSkinUpgradedForSizeBonus(SkinItem skin, int level)
        {
            ApplyRingSizeBonus();
        }

        private void ApplyRingSizeBonus()
        {
            if (playerJumpController == null || ringSizeUpgradeService == null || skinShopService == null)
            {
                return;
            }

            var activeSkin = skinShopService.ActiveSkin;
            if (activeSkin == null)
            {
                return;
            }

            float bonus = ringSizeUpgradeService.GetBonusScale(activeSkin.SkinId);
            playerJumpController.SetPermanentSizeBonus(bonus);
        }
    }
}
