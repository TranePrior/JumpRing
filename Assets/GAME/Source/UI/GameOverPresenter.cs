using DG.Tweening;
using JumpRing.Game.Core.Services;
using JumpRing.Game.Core.State;
using JumpRing.Game.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JumpRing.Game.UI
{
    public sealed class GameOverPresenter : MonoBehaviour
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
        private GameObject panel;

        [SerializeField]
        private CanvasGroup panelCanvasGroup;

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

        private IScoreService ScoreService => (IScoreService)scoreServiceComponent;
        private ICurrencyService CurrencyService => (ICurrencyService)currencyServiceComponent;

        private const float ScoreCountDuration = 0.55f;

        private int pendingEarnings;
        private bool rewardDoubled;
        private Sequence panelSequence;
        private Tween scoreTween;
        private Sequence doubleRewardPulse;

        private void OnEnable()
        {
            gameStateMachine.StateChanged += OnStateChanged;
            retryButton.onClick.AddListener(OnRetryClicked);
            menuButton.onClick.AddListener(OnMenuClicked);
            doubleRewardButton.onClick.AddListener(OnDoubleRewardClicked);

            panel.SetActive(false);
        }

        private void OnDisable()
        {
            gameStateMachine.StateChanged -= OnStateChanged;
            retryButton.onClick.RemoveListener(OnRetryClicked);
            menuButton.onClick.RemoveListener(OnMenuClicked);
            doubleRewardButton.onClick.RemoveListener(OnDoubleRewardClicked);
        }

        private void OnDestroy()
        {
            panelSequence?.Kill();
            KillContentTweens();
        }

        private void OnStateChanged(GameState state)
        {
            if (state != GameState.GameOver)
            {
                return;
            }

            Show();
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

            if (dimOverlay != null)
            {
                dimOverlay.Show();
            }

            panel.SetActive(true);
            panelSequence?.Kill();
            KillContentTweens();

            scoreTween = NumberTween.Play(scoreLabel, 0, currentScore, ScoreCountDuration, "{0}");
            bestScoreBadge.Show(bestScore, isNewBest);
            rewardChip.Show(pendingEarnings);

            if (canDoubleReward)
            {
                doubleRewardPulse = WindowAnimations.Heartbeat(doubleRewardButton.transform);
            }

            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.interactable = true;
                panelCanvasGroup.blocksRaycasts = true;
                panelSequence = WindowAnimations.AnimateOpen(panelCanvasGroup, panel.transform);
            }
        }

        private void Hide(System.Action onComplete)
        {
            panelSequence?.Kill();
            KillContentTweens();

            if (dimOverlay != null)
            {
                dimOverlay.Hide();
            }

            if (panelCanvasGroup != null)
            {
                panelSequence = WindowAnimations.AnimateClose(panelCanvasGroup, panel.transform, panel);
                panelSequence.AppendCallback(() => onComplete?.Invoke());
            }
            else
            {
                panel.SetActive(false);
                onComplete?.Invoke();
            }
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
            if (interstitialAdService != null)
            {
                interstitialAdService.TryShow(continuation);
            }
            else
            {
                continuation?.Invoke();
            }
        }

        private void OnDoubleRewardClicked()
        {
            if (rewardDoubled || !rewardedAdService.CanShowAd)
            {
                return;
            }

            rewardedAdService.ShowAd(onReward: ApplyDoubleReward);
        }

        private void ApplyDoubleReward()
        {
            rewardDoubled = true;
            CurrencyService.Add(pendingEarnings);
            rewardChip.ShowDoubled(pendingEarnings * 2);

            StopDoubleRewardPulse();
            doubleRewardButton.gameObject.SetActive(false);
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
