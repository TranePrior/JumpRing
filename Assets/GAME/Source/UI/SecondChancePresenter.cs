using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using JumpRing.Game.Core;
using JumpRing.Game.Core.Services;
using JumpRing.Game.Gameplay;

namespace JumpRing.Game.UI
{
    public sealed class SecondChancePresenter : PopupWindow
    {
        [SerializeField]
        private RunSessionController runSessionController;

        [SerializeField]
        private ReviveService reviveService;

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

        private float countdown;
        // Was a plain field overwritten with a literal on every show, while BonusEffectManager
        // carried an inspector-editable duration that nothing read — the inspector said 7s and the
        // game ran 5s. Serialized here at the value that was actually in effect.
        [SerializeField, Min(1f)]
        private float countdownDuration = 5f;
        private bool isCountingDown;
        private bool adReviveUsedThisRun;
        private bool holdsDialogPause;
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

            // Disabling this object silently strands the pause taken in ShowPanel, which would
            // hold the whole game at timeScale 0 for the rest of the session.
            ReleaseDialogPause();
        }

        private void Update()
        {
            if (!isCountingDown)
            {
                return;
            }

            // The countdown deliberately runs on unscaled time, so the Dialog pause this window
            // takes can't freeze it. Any other pause must stop it dead: an ad covers this window
            // for 15-30s against a 5s countdown, and a lost focus means nobody is looking at the
            // screen at all. Ticking through either finished the run and opened the game over card
            // behind the ad — the reward then revived into an already-finished session.
            if (PauseService.HasAnyReasonExcept(PauseReason.Dialog))
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
            bool hasHearts = reviveService.CanReviveWithHeart;
            bool canAdRevive = !adReviveUsedThisRun && rewardedAdService.CanShowAd;

            if (!hasHearts && !canAdRevive)
            {
                runSessionController.ForceFinishRun();
                return;
            }

            continueButton.gameObject.SetActive(hasHearts);
            adContinueButton.gameObject.SetActive(canAdRevive && !hasHearts);
            adContinueButton.interactable = true;

            countdown = countdownDuration;
            isCountingDown = true;

            // The ring is only written from Update, so without this the window opens on whatever
            // fill the scene was authored with and snaps to full a frame later.
            timerFill.fillAmount = 1f;

            dimOverlay.Show();
            ShowPanel();
        }

        private void OnContinueClicked()
        {
            if (!reviveService.CanReviveWithHeart)
            {
                return;
            }

            reviveService.ReviveWithHeart();
            CloseAfterRevive();
        }

        private void OnAdContinueClicked()
        {
            if (adReviveUsedThisRun)
            {
                return;
            }

            // The video takes seconds to appear and this button sits under it the whole time.
            adContinueButton.interactable = false;

            bool adStarted = rewardedAdService.ShowAd(OnAdFinished);

            if (adStarted)
            {
                return;
            }

            // The platform had nothing to show — that is not the player giving up on the run.
            // The offer is withdrawn, the countdown keeps running and the run ends on its own
            // terms. Killing it here made a dead ad slot look exactly like a lost run.
            WithdrawAdOffer();
        }

        private void OnAdFinished(RewardedAdResult result)
        {
            switch (result)
            {
                case RewardedAdResult.Rewarded:
                    adReviveUsedThisRun = true;
                    reviveService.ReviveWithAd();
                    CloseAfterRevive();
                    break;

                // The player watched the offer and turned it down. Same answer as the quit button.
                case RewardedAdResult.Skipped:
                    OnQuitClicked();
                    break;

                // The ad broke on the way out. The player never got to decide, so the run keeps
                // whatever is left of its countdown instead of being ended on the platform's behalf.
                case RewardedAdResult.Failed:
                    WithdrawAdOffer();
                    break;
            }
        }

        private void WithdrawAdOffer()
        {
            adContinueButton.gameObject.SetActive(false);
        }

        private void CloseAfterRevive()
        {
            // The dim goes first: the window's own close animation plays over the live game, and
            // fading the two together read as a flash.
            dimOverlay.HideImmediate();
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
            // GameState.Paused only stops the player from being controlled — everything driven by
            // Time.deltaTime kept running underneath this window, so an active bonus quietly burned
            // through its remaining seconds while the player was deciding, and the game stayed
            // audible. The window's own animations all run unscaled and are unaffected.
            HoldDialogPause();

            OpenWindow();

            heartbeatSequence?.Kill();
            heartbeatSequence = WindowAnimations.Heartbeat(heartIcon);
        }

        private void HidePanel()
        {
            heartbeatSequence?.Kill();
            heartIcon.localScale = Vector3.one;
            isCountingDown = false;

            ReleaseDialogPause();
            CloseWindow();
        }

        private void HoldDialogPause()
        {
            if (holdsDialogPause)
            {
                return;
            }

            holdsDialogPause = true;
            PauseService.Add(PauseReason.Dialog);
        }

        private void ReleaseDialogPause()
        {
            if (!holdsDialogPause)
            {
                return;
            }

            holdsDialogPause = false;
            PauseService.Remove(PauseReason.Dialog);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            heartbeatSequence?.Kill();
        }
    }
}
