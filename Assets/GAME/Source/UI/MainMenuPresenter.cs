using JumpRing.Game.Core.State;
using UnityEngine;
using JumpRing.Game.Gameplay;

namespace JumpRing.Game.UI
{
    public sealed class MainMenuPresenter : MonoBehaviour
    {
        [SerializeField]
        private CanvasGroup canvasGroup;

        [SerializeField]
        private GameStateMachine gameStateMachine;

        [SerializeField]
        private RunSessionController runSessionController;

        [SerializeField]
        private bool showOnGameOver = true;

        private void OnEnable()
        {
            gameStateMachine.StateChanged += OnStateChanged;
            OnStateChanged(gameStateMachine.CurrentState);
        }

        private void OnDisable()
        {
            gameStateMachine.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(GameState state)
        {
            var isVisible = state == GameState.MainMenu || (showOnGameOver && state == GameState.GameOver);

            canvasGroup.alpha = isVisible ? 1f : 0f;
            canvasGroup.interactable = isVisible;
            canvasGroup.blocksRaycasts = isVisible;
        }

        public void StartRun()
        {
            runSessionController.StartRun();
        }

        public void RestartRun()
        {
            runSessionController.StartRun();
        }
    }
}
