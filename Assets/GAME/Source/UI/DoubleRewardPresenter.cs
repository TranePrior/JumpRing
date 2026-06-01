using System.Collections;
using DG.Tweening;
using JumpRing.Game.Core.Services;
using JumpRing.Game.Core.State;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JumpRing.Game.UI
{
    public sealed class DoubleRewardPresenter : MonoBehaviour
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
        private GameObject panel;

        [SerializeField]
        private CanvasGroup panelCanvasGroup;

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
        private Sequence panelSequence;

        private void OnEnable()
        {
            gameStateMachine.StateChanged += OnStateChanged;
            doubleRewardButton.onClick.AddListener(OnDoubleRewardClicked);
            continueButton.onClick.AddListener(OnContinueClicked);
            panel.SetActive(false);
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

            if (!featureEnabled)
            {
                StartCoroutine(EnterMainMenuDeferred());
                return;
            }

            Show();
        }

        private IEnumerator EnterMainMenuDeferred()
        {
            yield return null;
            gameStateMachine.Enter(GameState.MainMenu);
        }

        private void Show()
        {
            pendingEarnings = CurrencyService.RunEarnings;
            rewardDoubled = false;

            UpdateUI();

            bool canShowAd = rewardedAdService != null && rewardedAdService.CanShowAd;
            doubleRewardButton.gameObject.SetActive(canShowAd);

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

        private void Hide()
        {
            panelSequence?.Kill();

            if (dimOverlay != null)
            {
                dimOverlay.Hide();
            }

            if (panelCanvasGroup != null)
            {
                panelSequence = WindowAnimations.AnimateClose(panelCanvasGroup, panel.transform, panel);
                panelSequence.AppendCallback(() => gameStateMachine.Enter(GameState.MainMenu));
            }
            else
            {
                panel.SetActive(false);
                gameStateMachine.Enter(GameState.MainMenu);
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
                rewardedAdService.ShowAd(
                    onReward: ApplyDoubleReward
                );
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

            if (earningsLabel != null)
            {
                earningsLabel.text = $"+{displayEarnings}";
            }
        }
    }
}
