using NUnit.Framework;
using JumpRing.Game.Core.Services;

namespace JumpRing.Tests.EditMode
{
    [TestFixture]
    public sealed class ThrottledScoreSubmitterTests
    {
        private const string LeaderboardId = "TopScore";
        private const float Cooldown = 1.5f;

        private FakeLeaderboardSubmitter fakeSubmitter;
        private ThrottledScoreSubmitter throttle;

        [SetUp]
        public void SetUp()
        {
            fakeSubmitter = new FakeLeaderboardSubmitter();
            throttle = new ThrottledScoreSubmitter(fakeSubmitter, LeaderboardId, Cooldown);
        }

        [Test]
        public void FirstSubmit_SendsImmediately()
        {
            throttle.Submit(10, 0f);

            Assert.AreEqual(1, fakeSubmitter.SubmitCount);
            Assert.AreEqual(10, fakeSubmitter.LastScore);
            Assert.AreEqual(LeaderboardId, fakeSubmitter.LastLeaderboardId);
            Assert.IsFalse(throttle.HasPendingScore);
        }

        [Test]
        public void SecondSubmitInsideCooldown_IsQueuedNotSent()
        {
            throttle.Submit(10, 0f);

            throttle.Submit(20, 0.4f);

            Assert.AreEqual(1, fakeSubmitter.SubmitCount, "A submit inside the cooldown must not reach the platform.");
            Assert.AreEqual(10, fakeSubmitter.LastScore);
            Assert.IsTrue(throttle.HasPendingScore);
        }

        [Test]
        public void QueuedScore_IsSentOnceCooldownElapses()
        {
            throttle.Submit(10, 0f);
            throttle.Submit(20, 0.4f);

            throttle.Tick(Cooldown);

            Assert.AreEqual(2, fakeSubmitter.SubmitCount);
            Assert.AreEqual(20, fakeSubmitter.LastScore);
            Assert.IsFalse(throttle.HasPendingScore);
        }

        [Test]
        public void TickInsideCooldown_SendsNothing()
        {
            throttle.Submit(10, 0f);
            throttle.Submit(20, 0.4f);

            throttle.Tick(0.5f);
            throttle.Tick(1.4f);

            Assert.AreEqual(1, fakeSubmitter.SubmitCount);
            Assert.IsTrue(throttle.HasPendingScore);
        }

        [Test]
        public void MultipleSubmitsInsideCooldown_SendOnlyTheLatestScore()
        {
            throttle.Submit(10, 0f);
            throttle.Submit(20, 0.2f);
            throttle.Submit(30, 0.6f);
            throttle.Submit(40, 1.1f);

            throttle.Tick(2f);

            Assert.AreEqual(2, fakeSubmitter.SubmitCount, "Everything queued inside one window collapses to a single send.");
            Assert.AreEqual(40, fakeSubmitter.LastScore, "Only the newest record is worth sending.");
        }

        [Test]
        public void TickWithoutPendingScore_SendsNothing()
        {
            throttle.Submit(10, 0f);

            throttle.Tick(100f);
            throttle.Tick(200f);

            Assert.AreEqual(1, fakeSubmitter.SubmitCount);
        }

        [Test]
        public void CooldownRestartsFromEachSend()
        {
            throttle.Submit(10, 0f);
            throttle.Submit(20, 1.6f);

            throttle.Submit(30, 2.5f);
            Assert.AreEqual(2, fakeSubmitter.SubmitCount, "2.5s is inside the window opened by the 1.6s send.");

            throttle.Tick(3.5f);
            Assert.AreEqual(3, fakeSubmitter.SubmitCount);
            Assert.AreEqual(30, fakeSubmitter.LastScore);
        }

        [Test]
        public void PlatformUnavailable_QueuesUntilItBecomesAvailable()
        {
            fakeSubmitter.IsAvailable = false;

            throttle.Submit(10, 0f);
            Assert.AreEqual(0, fakeSubmitter.SubmitCount);
            Assert.IsTrue(throttle.HasPendingScore, "A score set before the platform is ready must not be dropped.");

            fakeSubmitter.IsAvailable = true;
            throttle.Tick(0.1f);

            Assert.AreEqual(1, fakeSubmitter.SubmitCount);
            Assert.AreEqual(10, fakeSubmitter.LastScore);
        }

        [Test]
        public void EmptyLeaderboardId_NeverSends()
        {
            var unconfigured = new ThrottledScoreSubmitter(fakeSubmitter, string.Empty, Cooldown);

            unconfigured.Submit(10, 0f);
            unconfigured.Tick(100f);

            Assert.AreEqual(0, fakeSubmitter.SubmitCount);
        }

        [Test]
        public void SubmitPendingImmediately_IgnoresCooldown()
        {
            throttle.Submit(10, 0f);
            throttle.Submit(20, 0.4f);

            throttle.SubmitPendingImmediately(0.5f);

            Assert.AreEqual(2, fakeSubmitter.SubmitCount, "Teardown has no further frames, so the queued record goes out now.");
            Assert.AreEqual(20, fakeSubmitter.LastScore);
        }

        [Test]
        public void SubmitPendingImmediately_WithoutPendingScore_SendsNothing()
        {
            throttle.Submit(10, 0f);

            throttle.SubmitPendingImmediately(0.5f);

            Assert.AreEqual(1, fakeSubmitter.SubmitCount);
        }

        [Test]
        public void ScoringRunThenDeath_SendsAtMostOncePerCooldownWindow()
        {
            // The shape that produced the platform rate-limit error: a record is beaten on
            // consecutive scores and the run then ends inside the cooldown window.
            throttle.Submit(10, 10f);
            throttle.Submit(11, 10.3f);
            throttle.Submit(12, 10.5f);

            // Death, ad, popups — all inside the window.
            throttle.Tick(10.6f);
            throttle.Tick(10.9f);
            throttle.Tick(11.2f);

            Assert.AreEqual(1, fakeSubmitter.SubmitCount, "Nothing may go out before the window closes.");

            throttle.Tick(11.5f);

            Assert.AreEqual(2, fakeSubmitter.SubmitCount);
            Assert.AreEqual(12, fakeSubmitter.LastScore, "The final record must survive the throttling.");
        }

        private sealed class FakeLeaderboardSubmitter : ILeaderboardSubmitter
        {
            public bool IsAvailable { get; set; } = true;

            public int SubmitCount { get; private set; }

            public string LastLeaderboardId { get; private set; }

            public int LastScore { get; private set; }

            public void Submit(string leaderboardId, int score)
            {
                SubmitCount++;
                LastLeaderboardId = leaderboardId;
                LastScore = score;
            }
        }
    }
}
