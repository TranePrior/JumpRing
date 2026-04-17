using UnityEngine;
using JumpRing.Game.Core.Services;
using JumpRing.Game.Core.State;
using JumpRing.Game.Gameplay;
using JumpRing.Game.Theming;
using JumpRing.Game.UI;

namespace JumpRing.Game.Core.Composition
{
    public sealed class GameCompositionRoot : MonoBehaviour
    {
        [Header("Core")]
        [SerializeField]
        private MonoBehaviour gameStateMachineComponent;

        [SerializeField]
        private MonoBehaviour scoreServiceComponent;

        [SerializeField]
        private MonoBehaviour currencyServiceComponent;

        [Header("Feature Entry Points")]
        [SerializeField]
        private RunSessionController runSessionController;

        [SerializeField]
        private HudPresenter hudPresenter;

        [Header("Theming")]
        [SerializeField]
        private ThemeManager themeManager;

        [Header("Difficulty Systems")]
        [SerializeField]
        private DifficultyManager difficultyManager;

        [SerializeField]
        private MicroEventSystem microEventSystem;

        [SerializeField]
        private RiskRewardSystem riskRewardSystem;

        private IGameStateMachine GameStateMachine => (IGameStateMachine)gameStateMachineComponent;

        private IScoreService ScoreService => (IScoreService)scoreServiceComponent;

        private ICurrencyService CurrencyService => (ICurrencyService)currencyServiceComponent;

        private void Awake()
        {
            runSessionController.Construct(GameStateMachine, ScoreService);
            hudPresenter.Construct(ScoreService, CurrencyService);

            if (difficultyManager != null)
            {
                runSessionController.RunStarted += difficultyManager.OnRunStarted;
                runSessionController.RunFinished += difficultyManager.OnRunFinished;
            }

            if (microEventSystem != null)
            {
                runSessionController.RunStarted += microEventSystem.OnRunStarted;
                runSessionController.RunFinished += microEventSystem.OnRunFinished;
            }

            if (riskRewardSystem != null)
            {
                runSessionController.RunStarted += riskRewardSystem.OnRunStarted;
                runSessionController.RunFinished += riskRewardSystem.OnRunFinished;
            }

            if (themeManager != null)
            {
                themeManager.Initialize();
            }

            GameStateMachine.Enter(GameState.MainMenu);
        }

        private void OnDestroy()
        {
            if (difficultyManager != null)
            {
                runSessionController.RunStarted -= difficultyManager.OnRunStarted;
                runSessionController.RunFinished -= difficultyManager.OnRunFinished;
            }

            if (microEventSystem != null)
            {
                runSessionController.RunStarted -= microEventSystem.OnRunStarted;
                runSessionController.RunFinished -= microEventSystem.OnRunFinished;
            }

            if (riskRewardSystem != null)
            {
                runSessionController.RunStarted -= riskRewardSystem.OnRunStarted;
                runSessionController.RunFinished -= riskRewardSystem.OnRunFinished;
            }
        }
    }
}
