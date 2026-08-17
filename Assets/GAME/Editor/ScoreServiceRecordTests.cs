using System.Reflection;
using JumpRing.Game.Core.Services;
using NUnit.Framework;
using UnityEngine;

namespace JumpRing.Tests.EditMode
{
    [TestFixture]
    public sealed class ScoreServiceRecordTests
    {
        private const string BestScoreKey = "BestScore";
        private const int OldRecord = 10;

        private GameObject serviceObject;
        private PlatformStorageService storage;
        private ScoreService service;
        private int recordBeatenCount;

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(BestScoreKey);

            serviceObject = new GameObject("ScoreService");
            storage = serviceObject.AddComponent<PlatformStorageService>();
            service = serviceObject.AddComponent<ScoreService>();

            SetPrivateField(service, "storageService", storage);

            // AddComponent does not run Awake outside play mode, so the leaderboard submitter
            // the score path uses would stay null.
            InvokePrivate(service, "Awake");

            recordBeatenCount = 0;
            service.RecordBeaten += OnRecordBeaten;
        }

        [TearDown]
        public void TearDown()
        {
            service.RecordBeaten -= OnRecordBeaten;
            Object.DestroyImmediate(serviceObject);
            PlayerPrefs.DeleteKey(BestScoreKey);
        }

        [Test]
        public void FirstEverRun_StaysSilent()
        {
            AddPoints(5);

            Assert.AreEqual(0, recordBeatenCount);
        }

        [Test]
        public void PassingOldRecord_RaisesOnce()
        {
            GiveOldRecord();

            AddPoints(OldRecord + 1);

            Assert.AreEqual(1, recordBeatenCount);
        }

        [Test]
        public void ScoringBelowOldRecord_StaysSilent()
        {
            GiveOldRecord();

            AddPoints(OldRecord);

            Assert.AreEqual(0, recordBeatenCount);
        }

        [Test]
        public void EveryPointPastTheRecord_DoesNotRaiseAgain()
        {
            GiveOldRecord();

            AddPoints(OldRecord + 5);

            Assert.AreEqual(1, recordBeatenCount);
        }

        [Test]
        public void NextRun_RaisesAgainOnTheNewRecord()
        {
            GiveOldRecord();
            AddPoints(OldRecord + 1);

            service.Reset();
            AddPoints(OldRecord + 2);

            Assert.AreEqual(2, recordBeatenCount);
        }

        // The storage keeps an in-memory cache and only reads PlayerPrefs while loading,
        // so the seed has to go through the service itself.
        private void GiveOldRecord()
        {
            storage.SetInt(BestScoreKey, OldRecord);
        }

        private void AddPoints(int points)
        {
            for (int i = 0; i < points; i++)
            {
                service.Add(1);
            }
        }

        private void OnRecordBeaten()
        {
            recordBeatenCount++;
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(target, null);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }
    }
}
