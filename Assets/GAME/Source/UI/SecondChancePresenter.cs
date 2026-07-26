using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using JumpRing.Game.Core.Services;
using JumpRing.Game.Gameplay;

namespace JumpRing.Game.UI
{
    public sealed class SecondChancePresenter : PopupWindow
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
        private Button continueButton;

        [SerializeField]
        private Button adContinueButton;

        [SerializeField]
        private Button quitButton;

        [SerializeField]
        private Image timerFill;

        [SerializeField]
        private Transform heartIcon;

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
        // Was a plain field overwritten with a literal on every show, while BonusEffectManager
        // carried an inspector-editable duration that nothing read — the inspector said 7s and the
        // game ran 5s. Serialized here at the value that was actually in effect.
        [SerializeField, Min(1f)]
        private float countdownDuration = 5f;
        private bool isCountingDown;
        private bool adReviveUsedThisRun;
        private bool isAdReviveMode;
        private Sequence heartbeatSequence;

        private void OnEnable()
        {
            runSessionController.DeathRequested += OnDeathRequested;
            runSessionController.RunStarted += OnRunStarted;
            continueButton.onClick.AddListener(OnContinueClicked);
            quitButton.onClick.AddListener(OnQuitClicked);
            adContinueButton.onClick.AddListener(OnAdContinueClicked);

            CloseWindowImmediate();
        }

        private void OnDisable()
        {
            runSessionController.DeathRequested -= OnDeathRequested;
            runSessionController.RunStarted -= OnRunStarted;
            continueButton.onClick.RemoveListener(OnContinueClicked);
            quitButton.onClick.RemoveListener(OnQuitClicked);
            adContinueButton.onClick.RemoveListener(OnAdContinueClicked);
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
            bool canAdRevive = !adReviveUsedThisRun && rewardedAdService.CanShowAd;

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

            countdown = countdownDuration;
            isCountingDown = true;

            dimOverlay.Show();
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

            coinStepSpawner.RespawnFromCurrentPosition();
            cameraFollowTarget.SnapImmediate();

            dimOverlay.HideImmediate();
            runSessionController.ReviveToReady();
            HidePanel();
        }

        private void OnQuitClicked()
        {
            // The dim stays up: ForceFinishRun hands straight over to the game over card, which
            // shares this overlay. Hiding it here started a fade-out that the card's Show()
            // immediately reversed, flashing the whole screen bright for a fifth of a second.
            HidePanel();
            runSessionController.ForceFinishRun();
        }

        private void ShowPanel()
        {
            OpenWindow();

            heartbeatSequence?.Kill();
            heartbeatSequence = WindowAnimations.Heartbeat(heartIcon);
        }

        private void HidePanel()
        {
            heartbeatSequence?.Kill();
            heartIcon.localScale = Vector3.one;
            isCountingDown = false;

            CloseWindow();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            heartbeatSequence?.Kill();
        }
    }
}
