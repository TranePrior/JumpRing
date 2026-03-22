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
        bool HasActiveRun { get; }

        void StartRun();
        void FinishRun();
        void PauseRun();
        void ResumeRun();
        void OpenMainMenu();
        void ToggleMainMenu();
    }

    public sealed class RunSessionController : MonoBehaviour, IRunSessionController
    {
        public event Action RunStarted;
        public event Action RunFinished;

        private IGameStateMachine gameStateMachine;
        private IScoreService scoreService;
        private bool isConstructed;
        private bool hasActiveRun;

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

        public bool HasActiveRun => hasActiveRun;

        public void StartRun()
        {
            scoreService.Reset();
            gameStateMachine.Enter(GameState.Gameplay);
            hasActiveRun = true;
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

        public void OpenMainMenu()
        {
            gameStateMachine.Enter(GameState.MainMenu);
        }

        public void ToggleMainMenu()
        {
            if (gameStateMachine.CurrentState == GameState.Gameplay)
            {
                OpenMainMenu();
                return;
            }

            if (gameStateMachine.CurrentState == GameState.MainMenu && hasActiveRun)
            {
                ResumeRun();
            }
        }
    }
}
