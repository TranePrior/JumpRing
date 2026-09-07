using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using JumpRing.Game.Core.State;
using JumpRing.Game.UI;

namespace JumpRing.Tests.EditMode
{
    /// <summary>
    /// The in-run pause button (Yandex publishing requirement 6.3). The menu icon bar cannot cover
    /// it: its canvas is faded out and non-interactive for the whole run.
    /// </summary>
    [TestFixture]
    public sealed class PauseButtonPresenterTests
    {
        private GameObject hudObject;
        private GameObject buttonObject;
        private GameStateMachine stateMachine;
        private PauseButtonPresenter presenter;

        [SetUp]
        public void SetUp()
        {
            var systemsObject = new GameObject("GameSystems");
            stateMachine = systemsObject.AddComponent<GameStateMachine>();

            buttonObject = new GameObject("Icon_Pause", typeof(RectTransform), typeof(Image), typeof(Button));

            hudObject = new GameObject("HUD");
            presenter = hudObject.AddComponent<PauseButtonPresenter>();

            SetField("_button", buttonObject.GetComponent<Button>());
            SetField("_gameStateMachine", stateMachine);

            // Outside play mode Unity does not run the enable/disable callbacks, so the state
            // subscription the presenter takes there has to be driven by hand.
            Invoke("OnEnable");
        }

        [TearDown]
        public void TearDown()
        {
            Invoke("OnDisable");
            Object.DestroyImmediate(hudObject);
            Object.DestroyImmediate(buttonObject);
            Object.DestroyImmediate(stateMachine.gameObject);
        }

        private void SetField(string name, Object value)
        {
            var field = typeof(PauseButtonPresenter).GetField(
                name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"{name} not found");
            field.SetValue(presenter, value);
        }

        private void Invoke(string methodName)
        {
            var method = typeof(PauseButtonPresenter).GetMethod(
                methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, $"{methodName} not found");
            method.Invoke(presenter, null);
        }

        [Test]
        public void Bootstrap_ButtonIsHidden()
        {
            Assert.IsFalse(buttonObject.activeSelf, "Nothing to pause before the game starts.");
        }

        [Test]
        public void MainMenu_ButtonIsHidden()
        {
            stateMachine.Enter(GameState.MainMenu);

            Assert.IsFalse(buttonObject.activeSelf, "The menu icon bar already carries the settings window.");
        }

        [Test]
        public void Ready_ButtonIsVisible()
        {
            stateMachine.Enter(GameState.Ready);

            Assert.IsTrue(buttonObject.activeSelf, "The button must be up from the first frame the player can act.");
        }

        [Test]
        public void Gameplay_ButtonIsVisible()
        {
            stateMachine.Enter(GameState.Gameplay);

            Assert.IsTrue(buttonObject.activeSelf, "A run must be pausable — Yandex requirement 6.3.");
        }

        [Test]
        public void GameOver_ButtonIsHidden()
        {
            stateMachine.Enter(GameState.Gameplay);
            stateMachine.Enter(GameState.GameOver);

            Assert.IsFalse(buttonObject.activeSelf, "The game-over panel owns the screen.");
        }

        [Test]
        public void Paused_ButtonIsHidden()
        {
            stateMachine.Enter(GameState.Gameplay);
            stateMachine.Enter(GameState.Paused);

            Assert.IsFalse(buttonObject.activeSelf, "The second-chance dialog owns the screen.");
        }

        [Test]
        public void PresenterTornDown_StopsFollowingTheState()
        {
            stateMachine.Enter(GameState.Gameplay);

            Invoke("OnDisable");
            stateMachine.Enter(GameState.MainMenu);

            Assert.IsTrue(buttonObject.activeSelf, "A torn-down presenter must not keep driving the button.");

            // TearDown invokes OnDisable again; the presenter has to survive that.
            Invoke("OnEnable");
        }
    }
}
