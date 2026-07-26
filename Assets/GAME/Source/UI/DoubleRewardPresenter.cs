using DG.Tweening;
using JumpRing.Game.Core.Services;
using JumpRing.Game.Core.State;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JumpRing.Game.UI
{
    public sealed class DoubleRewardPresenter : PopupWindow
    {
        [Header("Dependencies")]
        [SerializeField]
        private GameStateMachine gameStateMachine;

        [SerializeField]
        private MonoBehaviour currencyServiceComponent;

        [SerializeField]
        private RewardedAdService rewardedAdService;

        [Header("UI")]
        [SerializeField]
        private TMP_Text earningsLabel;

        [SerializeField]
        private Button doubleRewardButton;

        [SerializeField]
        private Button continueButton;

        [Header("Overlay")]
        [SerializeField]
        private DimOverlay dimOverlay;

        [Header("Feature Toggle")]
        [SerializeField]
        private bool featureEnabled;

        private ICurrencyService CurrencyService => (ICurrencyService)currencyServiceComponent;

        private int pendingEarnings;
        private bool rewardDoubled;

        private void OnEnable()
        {
            doubleRewardButton.onClick.AddListener(OnDoubleRewardClicked);
            continueButton.onClick.AddListener(OnContinueClicked);
            CloseWindowImmediate();

            // Only own the game-over flow when this feature is on. When off, GameOverPresenter
            // is the sole owner — staying unsubscribed avoids both double-reward and the
            // previous bug where this presenter force-entered MainMenu on top of it.
            if (featureEnabled)
            {
                gameStateMachine.StateChanged += OnStateChanged;
            }
        }

        private void OnDisable()
        {
            gameStateMachine.StateChanged -= OnStateChanged;
            doubleRewardButton.onClick.RemoveListener(OnDoubleRewardClicked);
            continueButton.onClick.RemoveListener(OnContinueClicked);
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
            pendingEarnings = CurrencyService.RunEarnings;
            rewardDoubled = false;

            UpdateUI();

            doubleRewardButton.gameObject.SetActive(rewardedAdService.CanShowAd);

            dimOverlay.Show();
            OpenWindow();
        }

        private void Hide()
        {
            dimOverlay.Hide();
            CloseWindow(() => gameStateMachine.Enter(GameState.MainMenu));
        }

        private void OnDoubleRewardClicked()
        {
            if (rewardDoubled)
            {
                return;
            }

            if (rewardedAdService.CanShowAd)
            {
                rewardedAdService.ShowAd(onReward: ApplyDoubleReward);
            }
        }

        private void ApplyDoubleReward()
        {
            rewardDoubled = true;
            CurrencyService.Add(pendingEarnings);
            UpdateUI();
            doubleRewardButton.gameObject.SetActive(false);
        }

        private void OnContinueClicked()
        {
            Hide();
        }

        private void UpdateUI()
        {
            int displayEarnings = rewardDoubled ? pendingEarnings * 2 : pendingEarnings;
            earningsLabel.SetText("+{0}", displayEarnings);
        }
    }
}
