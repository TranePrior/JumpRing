using DG.Tweening;
using JumpRing.Game.Core.Services;
using JumpRing.Game.Core.State;
using JumpRing.Game.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JumpRing.Game.UI
{
    public sealed class GameOverPresenter : PopupWindow
    {
        [Header("Dependencies")]
        [SerializeField]
        private GameStateMachine gameStateMachine;

        [SerializeField]
        private RunSessionController runSessionController;

        [SerializeField]
        private MonoBehaviour scoreServiceComponent;

        [SerializeField]
        private MonoBehaviour currencyServiceComponent;

        [SerializeField]
        private RewardedAdService rewardedAdService;

        [SerializeField]
        private InterstitialAdService interstitialAdService;

        [Header("UI")]
        [SerializeField]
        private TMP_Text scoreLabel;

        [SerializeField]
        private BestScoreBadge bestScoreBadge;

        [SerializeField]
        private RewardChip rewardChip;

        [SerializeField]
        private Button doubleRewardButton;

        [SerializeField]
        private Button retryButton;

        [SerializeField]
        private Button menuButton;

        [Header("Overlay")]
        [SerializeField]
        private DimOverlay dimOverlay;

        [Header("Layout")]
        [SerializeField]
        private LayoutFreezer layoutFreezer;

        private IScoreService ScoreService => (IScoreService)scoreServiceComponent;
        private ICurrencyService CurrencyService => (ICurrencyService)currencyServiceComponent;

        private const float ScoreCountDuration = 0.55f;

        private int pendingEarnings;
        private bool rewardDoubled;
        private Tween scoreTween;
        private Sequence doubleRewardPulse;

        private void OnEnable()
        {
            gameStateMachine.StateChanged += OnStateChanged;
            retryButton.onClick.AddListener(OnRetryClicked);
            menuButton.onClick.AddListener(OnMenuClicked);
            doubleRewardButton.onClick.AddListener(OnDoubleRewardClicked);

            CloseWindowImmediate();
        }

        private void OnDisable()
        {
            gameStateMachine.StateChanged -= OnStateChanged;
            retryButton.onClick.RemoveListener(OnRetryClicked);
            menuButton.onClick.RemoveListener(OnMenuClicked);
            doubleRewardButton.onClick.RemoveListener(OnDoubleRewardClicked);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            KillContentTweens();
        }

        private void OnStateChanged(GameState state)
        {
            if (state == GameState.GameOver)
            {
                Show();
                return;
            }

            // Anything that leaves GameOver takes the card with it. Only the Retry and Menu buttons
            // used to close it, so any other exit — a revive, a tap that restarted the run — left
            // the card on screen and the next death re-opened it on top of itself, which read as a
            // hard cut with no close animation.
            HideImmediate();
        }

        private void Show()
        {
            int currentScore = ScoreService.CurrentScore;
            int bestScore = ScoreService.BestScore;
            bool isNewBest = currentScore >= bestScore && currentScore > 0;

            pendingEarnings = CurrencyService.RunEarnings;
            rewardDoubled = false;

            bool canDoubleReward = rewardedAdService.CanShowAd && pendingEarnings > 0;
            doubleRewardButton.gameObject.SetActive(canDoubleReward);

            dimOverlay.Show();

            ActivateWindow();
            KillContentTweens();

            bestScoreBadge.Show(bestScore, isNewBest);

            // Fill in the values the count-ups will land on, lay the card out once at that size,
            // then freeze the layout. Without this every tweened SetText rebuilt eight nested
            // layout groups per frame and the fitters resized the card as the digit count grew.
            scoreLabel.SetText("{0}", currentScore);
            rewardChip.PrepareLayout(pendingEarnings);
            layoutFreezer.Rebuild();

            scoreTween = NumberTween.Play(scoreLabel, 0, currentScore, ScoreCountDuration, "{0}");
            rewardChip.Show(pendingEarnings);

            if (canDoubleReward)
            {
                doubleRewardPulse = WindowAnimations.Heartbeat(doubleRewardButton.transform);
            }

            PlayOpenAnimation();
        }

        private void HideImmediate()
        {
            if (!IsWindowOpen)
            {
                return;
            }

            KillContentTweens();
            dimOverlay.HideImmediate();
            CloseWindowImmediate();
        }

        private void Hide(System.Action onComplete)
        {
            KillContentTweens();
            dimOverlay.Hide();
            CloseWindow(onComplete);
        }

        private void OnRetryClicked()
        {
            Hide(() => ShowInterstitialThen(() =>
            {
                runSessionController.StartRun();
            }));
        }

        private void OnMenuClicked()
        {
            Hide(() => ShowInterstitialThen(() => gameStateMachine.Enter(GameState.MainMenu)));
        }

        private void ShowInterstitialThen(System.Action continuation)
        {
            interstitialAdService.TryShow(continuation);
        }

        private void OnDoubleRewardClicked()
        {
            if (rewardDoubled || !rewardedAdService.CanShowAd)
            {
                return;
            }

            // A skipped or broken ad simply leaves the card as it was — the offer stays on screen
            // and the player keeps the single reward they already earned.
            rewardedAdService.ShowAd(result =>
            {
                if (result == RewardedAdResult.Rewarded)
                {
                    ApplyDoubleReward();
                }
            });
        }

        private void ApplyDoubleReward()
        {
            rewardDoubled = true;
            CurrencyService.Add(pendingEarnings);

            StopDoubleRewardPulse();
            doubleRewardButton.gameObject.SetActive(false);

            // The doubled amount is wider and the button just left the card, so re-lay the card out
            // at its new final size before freezing it again for the count-up.
            rewardChip.PrepareLayout(pendingEarnings * 2);
            layoutFreezer.Rebuild();

            rewardChip.ShowDoubled(pendingEarnings * 2);
        }

        private void KillContentTweens()
        {
            scoreTween?.Kill();
            scoreTween = null;
            StopDoubleRewardPulse();
        }

        private void StopDoubleRewardPulse()
        {
            doubleRewardPulse?.Kill();
            doubleRewardPulse = null;
            doubleRewardButton.transform.localScale = Vector3.one;
        }
    }
}
