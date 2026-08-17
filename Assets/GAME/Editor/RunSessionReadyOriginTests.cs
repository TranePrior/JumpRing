using System;
using NUnit.Framework;
using UnityEngine;
using JumpRing.Game.Core.Services;
using JumpRing.Game.Core.State;
using JumpRing.Game.Gameplay;

namespace JumpRing.Tests.EditMode
{
    /// <summary>
    /// Ready is entered both by starting a run and by reviving one, and the next tap means opposite
    /// things in the two cases: a revive has to swallow it, a fresh run has to play it. Telling them
    /// apart is what lets one tap on "tap to start" begin the run and jump.
    /// </summary>
    [TestFixture]
    public sealed class RunSessionReadyOriginTests
    {
        private GameObject sessionObject;
        private RunSessionController session;
        private FakeStateMachine stateMachine;

        [SetUp]
        public void SetUp()
        {
            stateMachine = new FakeStateMachine();
            sessionObject = new GameObject("RunSessionController");
            session = sessionObject.AddComponent<RunSessionController>();
            session.Construct(stateMachine, new FakeScoreService());
            stateMachine.Enter(GameState.MainMenu);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(sessionObject);
        }

        [Test]
        public void StartingARun_LeavesTheTapAlone()
        {
            session.StartRun();

            Assert.IsTrue(session.IsInReadyState, "A started run waits in Ready.");
            Assert.IsFalse(session.IsReadyAfterRevive,
                "The tap that started the run must go straight on to the first jump.");
        }

        [Test]
        public void AReviveClaimsTheTap()
        {
            session.StartRun();
            session.BeginGameplay();
            session.FinishRun();

            session.ReviveToReady();

            Assert.IsTrue(session.IsReadyAfterRevive,
                "The click that bought the revive must not also restart the run.");
        }

        [Test]
        public void LeavingReady_ReleasesTheRevivedTap()
        {
            session.StartRun();
            session.BeginGameplay();
            session.FinishRun();
            session.ReviveToReady();

            session.BeginGameplay();

            Assert.IsFalse(session.IsReadyAfterRevive, "The run is moving again; nothing to guard.");
        }

        [Test]
        public void AFreshRunAfterARevive_LeavesTheTapAlone()
        {
            session.StartRun();
            session.BeginGameplay();
            session.FinishRun();
            session.ReviveToReady();
            session.ForceFinishRun();

            session.StartRun();

            Assert.IsFalse(session.IsReadyAfterRevive,
                "A revive earlier in the session must not cost the next run its opening tap.");
        }

        private sealed class FakeStateMachine : IGameStateMachine
        {
            public event Action<GameState> StateChanged;

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

        private sealed class FakeScoreService : IScoreService
        {
            public event Action<int> ScoreChanged;

            // Not exercised by these tests: no backing field, so no unused-event warning.
            public event Action RecordBeaten
            {
                add { }
                remove { }
            }

            public int CurrentScore { get; private set; }

            public int BestScore { get; private set; }

            public void Reset()
            {
                CurrentScore = 0;
                ScoreChanged?.Invoke(CurrentScore);
            }

            public void Add(int points)
            {
                CurrentScore += points;
                BestScore = Mathf.Max(BestScore, CurrentScore);
                ScoreChanged?.Invoke(CurrentScore);
            }
        }
    }
}
