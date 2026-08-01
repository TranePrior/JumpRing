using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using JumpRing.Game.Gameplay;

namespace JumpRing.Tests.EditMode
{
    /// <summary>
    /// The revive sequence used to live inside SecondChancePresenter, where it could only be
    /// exercised with a full scene behind it. These are the cases that were never covered.
    /// </summary>
    [TestFixture]
    public sealed class ReviveServiceTests
    {
        private const float ReviveOffset = 2f;

        private GameObject serviceObject;
        private ReviveService service;
        private FakePlayer player;
        private FakeCoinSpawner coins;
        private FakeCamera gameplayCamera;
        private FakeRunSession runSession;
        private FakeSecondChanceStock stock;
        private List<string> callOrder;

        [SetUp]
        public void SetUp()
        {
            callOrder = new List<string>();
            player = new FakePlayer(callOrder);
            coins = new FakeCoinSpawner(callOrder);
            gameplayCamera = new FakeCamera(callOrder);
            runSession = new FakeRunSession(callOrder);
            stock = new FakeSecondChanceStock(callOrder);

            serviceObject = new GameObject("ReviveService");
            service = serviceObject.AddComponent<ReviveService>();
            SetReviveOffset(ReviveOffset);
            service.Construct(player, coins, gameplayCamera, runSession, stock);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(serviceObject);
        }

        private void SetReviveOffset(float value)
        {
            var field = typeof(ReviveService).GetField(
                "reviveOffset",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(field, "reviveOffset not found");
            field.SetValue(service, value);
        }

        [Test]
        public void ReviveWithHeart_SpendsExactlyOneHeart()
        {
            stock.Count = 2;

            service.ReviveWithHeart();

            Assert.AreEqual(1, stock.ConsumeCalls, "A revive costs one heart, no more and no less.");
            Assert.AreEqual(0, stock.InvincibilityCalls,
                "Consuming a heart already arms the grace period — granting it twice would stack it.");
        }

        [Test]
        public void ReviveWithAd_GrantsGraceWithoutSpendingAHeart()
        {
            stock.Count = 1;

            service.ReviveWithAd();

            Assert.AreEqual(0, stock.ConsumeCalls, "An ad revive must not also eat a heart.");
            Assert.AreEqual(1, stock.InvincibilityCalls, "An ad revive gets the same grace period.");
        }

        [Test]
        public void Revive_PutsThePlayerBackBehindWhereItDied()
        {
            player.LastDeathPosition = new Vector2(41.5f, 7f);

            service.ReviveWithAd();

            Assert.AreEqual(41.5f - ReviveOffset, player.ReviveX, 0.0001f,
                "Coming back exactly on the death position drops the player onto what killed them.");
        }

        [Test]
        public void Revive_RebuildsTheWorldBeforeHandingTheRunBack()
        {
            player.LastDeathPosition = new Vector2(10f, 0f);

            service.ReviveWithAd();

            // The coin field lays itself out around the player and the camera cuts to it, so both
            // have to read the teleport rather than the position the run died at. Handing the
            // session back first would let a frame run against the old world.
            CollectionAssert.AreEqual(
                new[] { "invincibility", "teleport", "coins", "camera", "ready" },
                callOrder);
        }

        [Test]
        public void CanReviveWithHeart_FollowsTheHeartStock()
        {
            stock.Count = 0;
            Assert.IsFalse(service.CanReviveWithHeart, "No hearts means no heart revive.");

            stock.Count = 1;
            Assert.IsTrue(service.CanReviveWithHeart, "One heart is enough.");
        }

        private sealed class FakePlayer : IRevivablePlayer
        {
            private readonly List<string> calls;

            public FakePlayer(List<string> calls) => this.calls = calls;

            public Vector2 LastDeathPosition { get; set; }
            public float ReviveX { get; private set; }

            public void RevivePlayer(float reviveX)
            {
                ReviveX = reviveX;
                calls.Add("teleport");
            }
        }

        private sealed class FakeCoinSpawner : ICoinSpawner
        {
            private readonly List<string> calls;

            public FakeCoinSpawner(List<string> calls) => this.calls = calls;

            public void RespawnFromCurrentPosition() => calls.Add("coins");
        }

        private sealed class FakeCamera : ICameraFollow
        {
            private readonly List<string> calls;

            public FakeCamera(List<string> calls) => this.calls = calls;

            public void SnapImmediate() => calls.Add("camera");
        }

        private sealed class FakeSecondChanceStock : ISecondChanceStock
        {
            private readonly List<string> calls;

            public FakeSecondChanceStock(List<string> calls) => this.calls = calls;

            public int Count { get; set; }
            public int ConsumeCalls { get; private set; }
            public int InvincibilityCalls { get; private set; }

            public int SecondChanceCount => Count;

            public void ConsumeSecondChance()
            {
                ConsumeCalls++;
                Count--;
                calls.Add("consume");
            }

            public void StartInvincibility()
            {
                InvincibilityCalls++;
                calls.Add("invincibility");
            }
        }

        private sealed class FakeRunSession : IRunSessionController
        {
            private readonly List<string> calls;

            public FakeRunSession(List<string> calls) => this.calls = calls;

            public event Action RunStarted;
            public event Action RunFinished;
            public event Action DeathRequested;
            public event Action<int> TapCountChanged;

            public bool CanControlPlayer => false;
            public bool CanStartRun => false;
            public bool HasActiveRun => true;
            public bool IsInReadyState => false;
            public bool IsReadyAfterRevive => false;
            public int TapCount => 0;

            public void ReviveToReady() => calls.Add("ready");

            public void StartRun() => Unused();
            public void BeginGameplay() { }
            public void FinishRun() { }
            public void ForceFinishRun() { }
            public void PauseRun() { }
            public void ResumeRun() { }
            public void OpenMainMenu() { }
            public void ToggleMainMenu() { }
            public void RestartFromScratch() { }
            public int RegisterTap() => 0;

            // Keeps the compiler from warning about events this fake never raises.
            private void Unused()
            {
                RunStarted?.Invoke();
                RunFinished?.Invoke();
                DeathRequested?.Invoke();
                TapCountChanged?.Invoke(0);
            }
        }
    }
}
