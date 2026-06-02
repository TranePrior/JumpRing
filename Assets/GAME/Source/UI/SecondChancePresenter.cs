using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using JumpRing.Game.Core.Services;
using JumpRing.Game.Gameplay;

namespace JumpRing.Game.UI
{
    public sealed class SecondChancePresenter : MonoBehaviour
    {
        [SerializeField]
        private BonusEffectManager bonusEffectManager;

        [SerializeField]
        private RunSessionController runSessionController;

        [SerializeField]
        private PlayerJumpController playerJumpController;

        [SerializeField]
        private CoinStepSpawner coinStepSpawner;

        [Header("UI References")]
        [SerializeField]
        private GameObject secondChancePanel;

        [SerializeField]
        private CanvasGroup secondChancePanelCanvasGroup;

        [SerializeField]
        private Button continueButton;

        [SerializeField]
        private Button adContinueButton;

        [SerializeField]
        private Button quitButton;

        [SerializeField]
        private Image timerFill;

        [Header("Overlay")]
        [SerializeField]
        private DimOverlay dimOverlay;

        [Header("Ad Revival")]
        [SerializeField]
        private RewardedAdService rewardedAdService;

        [Header("Revival")]
        [SerializeField]
        private CameraFollowTarget cameraFollowTarget;

        [SerializeField, Min(0.1f)]
        private float reviveOffset = 2f;

        private float countdown;
        private float countdownDuration;
        private bool isCountingDown;
        private bool adReviveUsedThisRun;
        private bool isAdReviveMode;
        private Sequence panelSequence;

        private void OnEnable()
        {
            runSessionController.DeathRequested += OnDeathRequested;
            runSessionController.RunStarted += OnRunStarted;
            continueButton.onClick.AddListener(OnContinueClicked);
            quitButton.onClick.AddListener(OnQuitClicked);

            if (adContinueButton != null)
            {
                adContinueButton.onClick.AddListener(OnAdContinueClicked);
            }

            secondChancePanel.SetActive(false);
        }

        private void OnDisable()
        {
            runSessionController.DeathRequested -= OnDeathRequested;
            runSessionController.RunStarted -= OnRunStarted;
            continueButton.onClick.RemoveListener(OnContinueClicked);
            quitButton.onClick.RemoveListener(OnQuitClicked);

            if (adContinueButton != null)
            {
                adContinueButton.onClick.RemoveListener(OnAdContinueClicked);
            }
        }

        private void Update()
        {
            if (!isCountingDown)
            {
                return;
            }

            countdown -= Time.unscaledDeltaTime;
            timerFill.fillAmount = countdown / countdownDuration;

            if (countdown <= 0f)
            {
                OnQuitClicked();
            }
        }

        private void OnRunStarted()
        {
            adReviveUsedThisRun = false;
        }

        private void OnDeathRequested()
        {
            bool hasHearts = bonusEffectManager.SecondChanceCount > 0;
            bool canAdRevive = !adReviveUsedThisRun
                               && rewardedAdService != null
                               && rewardedAdService.CanShowAd;

            if (!hasHearts && !canAdRevive)
            {
                runSessionController.ForceFinishRun();
                return;
            }

            isAdReviveMode = !hasHearts && canAdRevive;

            continueButton.gameObject.SetActive(hasHearts);
            if (adContinueButton != null)
            {
                adContinueButton.gameObject.SetActive(canAdRevive && !hasHearts);
            }

            countdownDuration = 5f;
            countdown = countdownDuration;
            isCountingDown = true;

            dimOverlay.Show();
            secondChancePanel.SetActive(true);
            ShowPanel();
        }

        private void OnContinueClicked()
        {
            if (bonusEffectManager.SecondChanceCount <= 0)
            {
                return;
            }

            bonusEffectManager.ConsumeSecondChance();
            Revive();
        }

        private void OnAdContinueClicked()
        {
            if (adReviveUsedThisRun)
            {
                return;
            }

            rewardedAdService.ShowAd(
                onReward: () =>
                {
                    adReviveUsedThisRun = true;
                    bonusEffectManager.StartInvincibility();
                    Revive();
                },
                onFail: () =>
                {
                    OnQuitClicked();
                }
            );
        }

        private void Revive()
        {
            var deathPos = playerJumpController.LastDeathPosition;
            playerJumpController.RevivePlayer(deathPos.x - reviveOffset);

            if (coinStepSpawner != null)
            {
                coinStepSpawner.RespawnFromCurrentPosition();
            }

            if (cameraFollowTarget != null)
            {
                cameraFollowTarget.SnapImmediate();
            }

            dimOverlay.HideImmediate();
            runSessionController.ReviveToReady();
            HidePanel();
        }

        private void OnQuitClicked()
        {
            dimOverlay.Hide();
            HidePanel();
            runSessionController.ForceFinishRun();
        }

        private void ShowPanel()
        {
            panelSequence?.Kill();
            if (secondChancePanelCanvasGroup != null)
            {
                secondChancePanelCanvasGroup.interactable = true;
                secondChancePanelCanvasGroup.blocksRaycasts = true;
                panelSequence = WindowAnimations.AnimateOpen(
                    secondChancePanelCanvasGroup, secondChancePanel.transform);
            }
        }

        private void HidePanel()
        {
            panelSequence?.Kill();
            isCountingDown = false;

            if (secondChancePanelCanvasGroup != null)
            {
                secondChancePanelCanvasGroup.interactable = false;
                secondChancePanelCanvasGroup.blocksRaycasts = false;

                panelSequence = WindowAnimations.AnimateClose(
                    secondChancePanelCanvasGroup, secondChancePanel.transform, secondChancePanel);
            }
            else
            {
                secondChancePanel.SetActive(false);
            }
        }
    }
}
