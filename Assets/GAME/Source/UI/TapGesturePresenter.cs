using JumpRing.Game.Core.State;
using UnityEngine;
using UnityEngine.UI;

namespace JumpRing.Game.UI
{
    public sealed class TapGesturePresenter : MonoBehaviour
    {
        [SerializeField]
        private Image tapGestureImage;

        [SerializeField]
        private MonoBehaviour gameStateMachineComponent;

        private IGameStateMachine GameStateMachine => (IGameStateMachine)gameStateMachineComponent;

        private void OnEnable()
        {
            GameStateMachine.StateChanged += OnStateChanged;
            OnStateChanged(GameStateMachine.CurrentState);
        }

        private void OnDisable()
        {
            GameStateMachine.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(GameState state)
        {
            tapGestureImage.enabled = state == GameState.Ready;
        }
    }
}
