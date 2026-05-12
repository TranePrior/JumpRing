using TMPro;
using UnityEngine;
using JumpRing.Game.Core.State;
using JumpRing.Game.Gameplay;

namespace JumpRing.Game.UI
{
    public sealed class WorldTapCounterPresenter : MonoBehaviour
    {
        [SerializeField]
        private TextMeshPro tapCountText;

        [SerializeField]
        private MonoBehaviour gameStateMachineComponent;

        private IRunSessionController runSessionController;
        private IGameStateMachine gameStateMachine;
        private bool isConstructed;

        public void Construct(IRunSessionController session)
        {
            if (isConstructed)
            {
                return;
            }

            runSessionController = session;

            if (gameStateMachineComponent != null)
            {
                gameStateMachine = (IGameStateMachine)gameStateMachineComponent;
                gameStateMachine.StateChanged += OnStateChanged;
            }

            runSessionController.TapCountChanged += OnTapCountChanged;

            OnTapCountChanged(runSessionController.TapCount);

            if (gameStateMachine != null)
            {
                OnStateChanged(gameStateMachine.CurrentState);
            }

            isConstructed = true;
        }

        private void OnDestroy()
        {
            if (!isConstructed)
            {
                return;
            }

            runSessionController.TapCountChanged -= OnTapCountChanged;

            if (gameStateMachine != null)
            {
                gameStateMachine.StateChanged -= OnStateChanged;
            }
        }

        private void OnStateChanged(GameState state)
        {
            bool visible = state == GameState.Gameplay || state == GameState.Ready;
            tapCountText.gameObject.SetActive(visible);
        }

        private void OnTapCountChanged(int count)
        {
            tapCountText.text = count.ToString();
        }
    }
}
