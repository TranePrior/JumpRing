using System;
using System.Collections.Generic;
using JumpRing.Game.Core.Services;
using JumpRing.Game.Core.State;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JumpRing.Game.Gameplay
{
    public interface IRunStartGate
    {
        bool CanStartRun();
    }

    public interface IRunSessionController
    {
        event Action RunStarted;
        event Action RunFinished;
        event Action DeathRequested;
        event Action<int> TapCountChanged;

        bool CanControlPlayer { get; }
        bool CanStartRun { get; }
        bool HasActiveRun { get; }
        bool IsInReadyState { get; }
        bool IsReadyAfterRevive { get; }
        int TapCount { get; }

        void StartRun();
        void BeginGameplay();
        void FinishRun();
        void ForceFinishRun();
        void ReviveToReady();
        void PauseRun();
        void ResumeRun();
        void OpenMainMenu();
        void ToggleMainMenu();
        void RestartFromScratch();
        int RegisterTap();
    }

    public sealed class RunSessionController : MonoBehaviour, IRunSessionController
    {
        public event Action RunStarted;
        public event Action RunFinished;
        public event Action DeathRequested;
        public event Action<int> TapCountChanged;

        private readonly List<IRunStartGate> runStartGates = new();
        private IGameStateMachine gameStateMachine;
        private IScoreService scoreService;
        private bool isConstructed;
        private bool hasActiveRun;
        private bool enteredReadyFromRevive;

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

        public bool CanControlPlayer => isConstructed && gameStateMachine.CurrentState == GameState.Gameplay;

        public bool CanStartRun => isConstructed && (gameStateMachine.CurrentState == GameState.MainMenu ||
            gameStateMachine.CurrentState == GameState.GameOver);

        public bool HasActiveRun => hasActiveRun;

        public int TapCount { get; private set; }

        public bool IsInReadyState => isConstructed && gameStateMachine.CurrentState == GameState.Ready;

        /// <summary>
        /// True only while waiting in Ready for the tap that resumes a revived run.
        /// </summary>
        /// <remarks>
        /// Ready is entered from two places and they want opposite things from the next tap. A revive
        /// must swallow it — the click that bought the revive would otherwise fall through and restart
        /// the run before the player sees they are alive. A run started from the menu must consume it
        /// — that tap is the player asking to play, and making them tap a second time reads as the
        /// game ignoring the first.
        /// </remarks>
        public bool IsReadyAfterRevive => IsInReadyState && enteredReadyFromRevive;

        public void RegisterStartGate(IRunStartGate gate)
        {
            if (runStartGates.Contains(gate))
            {
                return;
            }

            runStartGates.Add(gate);
        }

        public void UnregisterStartGate(IRunStartGate gate)
        {
            runStartGates.Remove(gate);
        }

        public void StartRun()
        {
            for (var gateIndex = 0; gateIndex < runStartGates.Count; gateIndex++)
            {
                if (runStartGates[gateIndex].CanStartRun())
                {
                    continue;
                }

                hasActiveRun = false;
                OpenMainMenu();
                return;
            }

            scoreService.Reset();
            ScorePerTap = 1;
            TapCount = 0;
            TapCountChanged?.Invoke(TapCount);
            enteredReadyFromRevive = false;
            gameStateMachine.Enter(GameState.Ready);
            hasActiveRun = true;
            RunStarted?.Invoke();
        }

        public void BeginGameplay()
        {
            if (gameStateMachine.CurrentState != GameState.Ready)
            {
                return;
            }

            gameStateMachine.Enter(GameState.Gameplay);
        }

        public void FinishRun()
        {
            if (gameStateMachine.CurrentState == GameState.GameOver ||
                gameStateMachine.CurrentState == GameState.Paused)
            {
                return;
            }

            PauseRun();
            DeathRequested?.Invoke();
        }

        public void ForceFinishRun()
        {
            if (gameStateMachine.CurrentState == GameState.GameOver)
            {
                return;
            }

            hasActiveRun = false;
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

        public void ReviveToReady()
        {
            enteredReadyFromRevive = true;
            gameStateMachine.Enter(GameState.Ready);
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

        public void RestartFromScratch()
        {
            var activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.buildIndex, LoadSceneMode.Single);
        }

        /// <summary>
        /// Score multiplier per tap. Set by BonusEffectManager for ScoreBoost (x2).
        /// </summary>
        public int ScorePerTap { get; set; } = 1;

        public int RegisterTap()
        {
            if (!CanControlPlayer)
            {
                return scoreService.CurrentScore;
            }

            TapCount++;
            TapCountChanged?.Invoke(TapCount);
            scoreService.Add(ScorePerTap);
            return scoreService.CurrentScore;
        }
    }
}
