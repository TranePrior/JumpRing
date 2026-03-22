using System;

namespace JumpRing.Game.Core.State
{
    public interface IGameStateMachine
    {
        event Action<GameState> StateChanged;

        GameState CurrentState { get; }

        void Enter(GameState state);
    }
}
