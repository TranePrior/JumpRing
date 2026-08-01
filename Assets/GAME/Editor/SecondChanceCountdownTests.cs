using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using JumpRing.Game.Core;
using JumpRing.Game.UI;

namespace JumpRing.Tests.EditMode
{
    /// <summary>
    /// Covers the countdown gate only. The rest of the window needs its full set of serialized
    /// scene references, which is play mode territory.
    /// </summary>
    [TestFixture]
    public sealed class SecondChanceCountdownTests
    {
        private const PauseReason AllReasons = PauseReason.Ad | PauseReason.FocusLost | PauseReason.Dialog | PauseReason.Popup;
        private const float CountdownDuration = 5f;

        private GameObject presenterObject;
        private SecondChancePresenter presenter;
        private Image timerFill;

        [SetUp]
        public void SetUp()
        {
            PauseService.Remove(AllReasons);

            presenterObject = new GameObject("SecondChancePresenter");
            presenter = presenterObject.AddComponent<SecondChancePresenter>();
            timerFill = presenterObject.AddComponent<Image>();

            SetField("timerFill", timerFill);
            SetField("countdownDuration", CountdownDuration);
            SetField("countdown", CountdownDuration * 0.5f);
            SetField("isCountingDown", true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(presenterObject);
            PauseService.Remove(AllReasons);
        }

        private void SetField(string name, object value)
        {
            var field = typeof(SecondChancePresenter).GetField(
                name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"{name} not found");
            field.SetValue(presenter, value);
        }

        private void Tick()
        {
            var method = typeof(SecondChancePresenter).GetMethod(
                "Update", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, "Update not found");
            method.Invoke(presenter, null);
        }

        [Test]
        public void CountdownDoesNotRunWhileAnAdIsShown()
        {
            // The ad the player just started covers this window for 15-30s against a 5s countdown.
            // Ticking through it finished the run and opened the game over card behind the ad, so
            // the reward revived into an already-finished session.
            PauseService.Add(PauseReason.Ad);

            Tick();

            Assert.AreEqual(1f, timerFill.fillAmount,
                "The countdown must be frozen for the whole duration of an ad.");
        }

        [Test]
        public void CountdownDoesNotRunWhileTheWindowIsOutOfFocus()
        {
            // Nobody is looking at the screen. The countdown runs on unscaled time, so without
            // this the run quietly finished itself in a backgrounded tab.
            PauseService.Add(PauseReason.FocusLost);

            Tick();

            Assert.AreEqual(1f, timerFill.fillAmount,
                "The countdown must be frozen while the game is out of focus.");
        }

        [Test]
        public void CountdownRunsUnderTheWindowsOwnDialogPause()
        {
            // The window pauses the game itself, which is exactly why the countdown runs on
            // unscaled time — that must keep working.
            PauseService.Add(PauseReason.Dialog);

            Tick();

            Assert.AreEqual(0.5f, timerFill.fillAmount, 0.05f,
                "The countdown must keep running under its own dialog pause.");
        }

        [Test]
        public void CountdownStaysFrozenWhenAPopupCoversItsOwnDialogPause()
        {
            // This window used to raise Popup itself — its panel carried a PopupTracker on top of
            // the Dialog pause the presenter takes — so the gate below saw the window's own second
            // pause as an external one and froze the countdown for the whole time the card was up.
            // The ring sat on its authored fill and the run never ended on its own. Popup here now
            // means what it says: a window stacked over this one.
            PauseService.Add(PauseReason.Dialog);
            PauseService.Add(PauseReason.Popup);

            Tick();

            Assert.AreEqual(1f, timerFill.fillAmount,
                "A window stacked over this one must freeze the countdown.");
        }

        [Test]
        public void CountdownStaysFrozenWhenAnAdCoversItsOwnDialogPause()
        {
            // The real combination once the window takes a pause of its own: both reasons are
            // active at the same time, and the ad has to win.
            PauseService.Add(PauseReason.Dialog);
            PauseService.Add(PauseReason.Ad);

            Tick();

            Assert.AreEqual(1f, timerFill.fillAmount,
                "An ad must freeze the countdown even while the window holds its own pause.");
        }
    }
}
