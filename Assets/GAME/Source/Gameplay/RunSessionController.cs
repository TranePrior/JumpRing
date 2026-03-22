using System;
using JumpRing.Game.Core.Services;
using JumpRing.Game.Core.State;
using UnityEngine;

namespace JumpRing.Game.Gameplay
{
    public interface IRunSessionController
    {
        event Action RunStarted;
        event Action RunFinished;

        bool CanControlPlayer { get; }
        bool CanStartRun { get; }

        void StartRun();
        void FinishRun();
        void PauseRun();
        void ResumeRun();
    }

    public sealed class RunSessionController : MonoBehaviour, IRunSessionController
    {
        public event Action RunStarted;
        public event Action RunFinished;

        private IGameStateMachine gameStateMachine;
        private IScoreService scoreService;
        private bool isConstructed;

        public void Construct(IGameStateMachine stateMachine, IScoreService score)
        {
            if (isConstructed)
            {
                return;
            }

            gameStateMachine = stateMachine;
            scoreService = score;
            isConstructed = true;
        }

        public bool CanControlPlayer
        {
            get
            {
                return gameStateMachine.CurrentState == GameState.Gameplay;
            }
        }

        public bool CanStartRun
        {
            get
            {
                return gameStateMachine.CurrentState == GameState.MainMenu ||
                    gameStateMachine.CurrentState == GameState.GameOver;
            }
        }

        public void StartRun()
        {
            scoreService.Reset();
            gameStateMachine.Enter(GameState.Gameplay);
            RunStarted?.Invoke();
        }

        public void FinishRun()
        {
            if (gameStateMachine.CurrentState == GameState.GameOver)
            {
                return;
            }

            gameStateMachine.Enter(GameState.GameOver);
            RunFinished?.Invoke();
        }

        public void PauseRun()
        {
            gameStateMachine.Enter(GameState.Paused);
        }

        public void ResumeRun()
        {
            gameStateMachine.Enter(GameState.Gameplay);
        }
    }
}
