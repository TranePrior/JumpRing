using DG.Tweening;
using JumpRing.Game.Core.Services;
using JumpRing.Game.Core.State;
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
        private TMP_Text bestScoreLabel;

        [SerializeField]
        private GameObject newBestRoot;

        [SerializeField]
        private TMP_Text earningsLabel;

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

        private int pendingEarnings;
        private bool rewardDoubled;
        private Sequence panelSequence;

        private void OnEnable()
        {
            gameStateMachine.StateChanged += OnStateChanged;
            retryButton.onClick.AddListener(OnRetryClicked);
            menuButton.onClick.AddListener(OnMenuClicked);

            if (doubleRewardButton != null)
            {
                doubleRewardButton.onClick.AddListener(OnDoubleRewardClicked);
            }

            panel.SetActive(false);
        }

        private void OnDisable()
        {
            gameStateMachine.StateChanged -= OnStateChanged;
            retryButton.onClick.RemoveListener(OnRetryClicked);
            menuButton.onClick.RemoveListener(OnMenuClicked);

            if (doubleRewardButton != null)
            {
                doubleRewardButton.onClick.RemoveListener(OnDoubleRewardClicked);
            }
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

            scoreLabel.text = currentScore.ToString();
            if (bestScoreLabel != null) bestScoreLabel.text = bestScore.ToString();
            if (newBestRoot != null) newBestRoot.SetActive(isNewBest);

            pendingEarnings = CurrencyService.RunEarnings;
            rewardDoubled = false;
            UpdateEarningsUI();

            if (doubleRewardButton != null)
            {
                doubleRewardButton.gameObject.SetActive(true);
            }

            if (dimOverlay != null)
            {
                dimOverlay.Show();
            }

            panel.SetActive(true);
            panelSequence?.Kill();

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
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                UnityEngine.SceneManagement.SceneManager.LoadScene(scene.buildIndex, UnityEngine.SceneManagement.LoadSceneMode.Single);
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
            if (rewardDoubled)
            {
                return;
            }

            if (rewardedAdService != null && rewardedAdService.CanShowAd)
            {
                rewardedAdService.ShowAd(onReward: ApplyDoubleReward);
            }
            else
            {
                ApplyDoubleReward();
            }
        }

        private void ApplyDoubleReward()
        {
            rewardDoubled = true;
            CurrencyService.Add(pendingEarnings);
            UpdateEarningsUI();

            if (doubleRewardButton != null)
            {
                doubleRewardButton.gameObject.SetActive(false);
            }
        }

        private void UpdateEarningsUI()
        {
            int displayEarnings = rewardDoubled ? pendingEarnings * 2 : pendingEarnings;

            if (earningsLabel != null)
            {
                earningsLabel.text = $"+{displayEarnings}";
            }
        }
    }
}
