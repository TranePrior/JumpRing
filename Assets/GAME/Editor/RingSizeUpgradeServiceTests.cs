using System;
using NUnit.Framework;
using UnityEngine;
using JumpRing.Game.Theming;
using JumpRing.Game.Core.Services;

namespace JumpRing.Tests.EditMode
{
    [TestFixture]
    public sealed class RingSizeUpgradeServiceTests
    {
        private RingSizeUpgradeService service;
        private GameObject serviceObject;
        private FakeCurrencyService fakeCurrency;
        private SkinItem testSkin;

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey("SkinUpgrades");

            serviceObject = new GameObject("UpgradeService");
            fakeCurrency = serviceObject.AddComponent<FakeCurrencyService>();
            service = serviceObject.AddComponent<RingSizeUpgradeService>();

            var currencyField = typeof(RingSizeUpgradeService)
                .GetField("currencyServiceComponent",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            currencyField.SetValue(service, fakeCurrency);

            var maxLevelField = typeof(RingSizeUpgradeService)
                .GetField("maxLevel",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            maxLevelField.SetValue(service, 10);

            var stepField = typeof(RingSizeUpgradeService)
                .GetField("scaleStep",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            stepField.SetValue(service, 0.1f);

            var multiplierField = typeof(RingSizeUpgradeService)
                .GetField("priceMultiplier",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            multiplierField.SetValue(service, 1.5f);

            testSkin = ScriptableObject.CreateInstance<SkinItem>();
            var skinIdField = typeof(SkinItem)
                .GetField("skinId",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            skinIdField.SetValue(testSkin, "TestRing");

            var priceField = typeof(SkinItem)
                .GetField("price",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            priceField.SetValue(testSkin, 100);

            service.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey("SkinUpgrades");
            UnityEngine.Object.DestroyImmediate(serviceObject);
            UnityEngine.Object.DestroyImmediate(testSkin);
        }

        [Test]
        public void GetLevel_NoUpgrades_ReturnsZero()
        {
            Assert.AreEqual(0, service.GetLevel("TestRing"));
        }

        [Test]
        public void TryUpgrade_WithEnoughMoney_IncreasesLevel()
        {
            fakeCurrency.SetBalance(1000);

            bool result = service.TryUpgrade(testSkin);

            Assert.IsTrue(result);
            Assert.AreEqual(1, service.GetLevel("TestRing"));
        }

        [Test]
        public void TryUpgrade_NotEnoughMoney_ReturnsFalse()
        {
            fakeCurrency.SetBalance(0);

            bool result = service.TryUpgrade(testSkin);

            Assert.IsFalse(result);
            Assert.AreEqual(0, service.GetLevel("TestRing"));
        }

        [Test]
        public void TryUpgrade_AtMaxLevel_ReturnsFalse()
        {
            fakeCurrency.SetBalance(999999);

            for (int i = 0; i < 10; i++)
            {
                service.TryUpgrade(testSkin);
            }

            bool result = service.TryUpgrade(testSkin);

            Assert.IsFalse(result);
            Assert.AreEqual(10, service.GetLevel("TestRing"));
        }

        [Test]
        public void IsMaxed_AtMaxLevel_ReturnsTrue()
        {
            fakeCurrency.SetBalance(999999);

            for (int i = 0; i < 10; i++)
            {
                service.TryUpgrade(testSkin);
            }

            Assert.IsTrue(service.IsMaxed("TestRing"));
        }

        [Test]
        public void IsMaxed_BelowMaxLevel_ReturnsFalse()
        {
            Assert.IsFalse(service.IsMaxed("TestRing"));
        }

        [Test]
        public void GetBonusScale_ReturnsLevelTimesStep()
        {
            fakeCurrency.SetBalance(999999);

            service.TryUpgrade(testSkin);
            service.TryUpgrade(testSkin);
            service.TryUpgrade(testSkin);

            Assert.AreEqual(0.3f, service.GetBonusScale("TestRing"), 0.001f);
        }

        [Test]
        public void GetTotalScale_CappedAtTwo()
        {
            fakeCurrency.SetBalance(999999);

            for (int i = 0; i < 10; i++)
            {
                service.TryUpgrade(testSkin);
            }

            Assert.AreEqual(2f, service.GetTotalScale("TestRing"), 0.001f);
        }

        [Test]
        public void GetUpgradePrice_IncreasesWithLevel()
        {
            fakeCurrency.SetBalance(999999);

            int price0 = service.GetUpgradePrice(testSkin);
            service.TryUpgrade(testSkin);
            int price1 = service.GetUpgradePrice(testSkin);

            Assert.AreEqual(100, price0);
            Assert.AreEqual(150, price1);
        }

        [Test]
        public void TryUpgrade_FiresSkinUpgradedEvent()
        {
            fakeCurrency.SetBalance(1000);
            SkinItem eventSkin = null;
            int eventLevel = -1;

            service.SkinUpgraded += (skin, level) =>
            {
                eventSkin = skin;
                eventLevel = level;
            };

            service.TryUpgrade(testSkin);

            Assert.AreEqual(testSkin, eventSkin);
            Assert.AreEqual(1, eventLevel);
        }

        [Test]
        public void Persistence_SaveAndLoad_PreservesLevels()
        {
            fakeCurrency.SetBalance(999999);

            service.TryUpgrade(testSkin);
            service.TryUpgrade(testSkin);

            service.Initialize();

            Assert.AreEqual(2, service.GetLevel("TestRing"));
        }

        private sealed class FakeCurrencyService : MonoBehaviour, ICurrencyService
        {
            public event Action<int> BalanceChanged;

            private int balance;

            public int Balance => balance;

            public void SetBalance(int value)
            {
                balance = value;
            }

            public void Add(int amount)
            {
                balance += amount;
                BalanceChanged?.Invoke(balance);
            }

            public bool Spend(int amount)
            {
                if (amount > balance)
                {
                    return false;
                }

                balance -= amount;
                BalanceChanged?.Invoke(balance);
                return true;
            }
        }
    }
}
