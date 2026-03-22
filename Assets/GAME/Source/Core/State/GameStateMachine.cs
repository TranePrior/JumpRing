using System;
using UnityEngine;

namespace JumpRing.Game.Core.State
{
    public sealed class GameStateMachine : MonoBehaviour, IGameStateMachine
    {
        public event Action<GameState> StateChanged;

        [field: SerializeField]
        public GameState CurrentState { get; private set; } = GameState.Bootstrap;

        public void Enter(GameState state)
        {
            if (CurrentState == state)
            {
                return;
            }

            CurrentState = state;
            StateChanged?.Invoke(CurrentState);
        }
    }
}
