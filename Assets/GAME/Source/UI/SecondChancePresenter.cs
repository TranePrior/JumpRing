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

        [Header("UI References")]
        [SerializeField]
        private GameObject secondChancePanel;

        [SerializeField]
        private Button continueButton;

        [SerializeField]
        private Button quitButton;

        [SerializeField]
        private Image timerFill;

        [Header("Revival")]
        [SerializeField, Min(0.1f)]
        private float reviveOffset = 2f;

        private float countdown;
        private float countdownDuration;
        private bool isCountingDown;

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
            countdownDuration = bonusEffectManager.SecondChanceTimerDuration;
            countdown = countdownDuration;
            isCountingDown = true;

            var hasHearts = bonusEffectManager.SecondChanceCount > 0;
            continueButton.gameObject.SetActive(hasHearts);
            secondChancePanel.SetActive(true);
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
            runSessionController.ReviveToReady();
            HidePanel();
        }

        private void OnQuitClicked()
        {
            HidePanel();
            runSessionController.ForceFinishRun();
        }

        private void HidePanel()
        {
            isCountingDown = false;
            secondChancePanel.SetActive(false);
        }
    }
}
