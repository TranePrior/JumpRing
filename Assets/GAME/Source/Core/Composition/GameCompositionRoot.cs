using UnityEngine;
using JumpRing.Game.Core.Services;
using JumpRing.Game.Core.State;
using JumpRing.Game.Gameplay;
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

        private IGameStateMachine GameStateMachine => (IGameStateMachine)gameStateMachineComponent;

        private IScoreService ScoreService => (IScoreService)scoreServiceComponent;

        private ICurrencyService CurrencyService => (ICurrencyService)currencyServiceComponent;

        private void Awake()
        {
            runSessionController.Construct(GameStateMachine, ScoreService);
            hudPresenter.Construct(ScoreService, CurrencyService);

            GameStateMachine.Enter(GameState.MainMenu);
        }
    }
}
