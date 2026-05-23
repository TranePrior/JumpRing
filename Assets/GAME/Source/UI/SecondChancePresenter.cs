using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
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
        private Button quitButton;

        [SerializeField]
        private Image timerFill;

        [Header("Overlay")]
        [SerializeField]
        private DimOverlay dimOverlay;

        [Header("Revival")]
        [SerializeField, Min(0.1f)]
        private float reviveOffset = 2f;
        private float countdown;
        private float countdownDuration;
        private bool isCountingDown;
        private Sequence panelSequence;

        private void OnEnable()
        {
            runSessionController.DeathRequested += OnDeathRequested;
            continueButton.onClick.AddListener(OnContinueClicked);
            quitButton.onClick.AddListener(OnQuitClicked);
            secondChancePanel.SetActive(false);
        }

        private void OnDisable()
        {
            runSessionController.DeathRequested -= OnDeathRequested;
            continueButton.onClick.RemoveListener(OnContinueClicked);
            quitButton.onClick.RemoveListener(OnQuitClicked);
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

        private void OnDeathRequested()
        {
            if (bonusEffectManager.SecondChanceCount <= 0)
            {
                runSessionController.ForceFinishRun();
                return;
            }

            countdownDuration = bonusEffectManager.SecondChanceTimerDuration;
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

            var deathPos = playerJumpController.LastDeathPosition;
            playerJumpController.RevivePlayer(deathPos.x - reviveOffset);

            if (coinStepSpawner != null)
            {
                coinStepSpawner.RespawnFromCurrentPosition();
            }

            runSessionController.ReviveToReady();
            HidePanel();
        }

        private void OnQuitClicked()
        {
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
            dimOverlay.Hide();

            if (secondChancePanelCanvasGroup != null)
            {
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
