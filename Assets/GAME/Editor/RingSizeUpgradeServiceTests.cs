using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using JumpRing.Game.Theming;
using JumpRing.Game.Core.Services;

namespace JumpRing.Tests.EditMode
{
    [TestFixture]
    public sealed class RingSizeUpgradeServiceTests
    {
        private const string UpgradesKey = "SkinUpgrades";
        private const string TestSkinId = "TestRing";
        private const int TestSkinPrice = 100;
        private const int MaxLevel = 10;
        private const float ScaleStep = 0.1f;
        private const float MaxTotalScale = 1.3f;

        private static readonly float[] PriceMultipliers = { 1f, 1.5f, 2f, 3f, 4f, 5.5f };

        private RingSizeUpgradeService service;
        private GameObject serviceObject;
        private FakeCurrencyService fakeCurrency;
        private SkinItem testSkin;

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(UpgradesKey);

            serviceObject = new GameObject("UpgradeService");
            fakeCurrency = serviceObject.AddComponent<FakeCurrencyService>();
            PlatformStorageService storage = serviceObject.AddComponent<PlatformStorageService>();
            service = serviceObject.AddComponent<RingSizeUpgradeService>();

            SetPrivateField(service, "currencyServiceComponent", fakeCurrency);
            SetPrivateField(service, "storageService", storage);
            SetPrivateField(service, "maxLevel", MaxLevel);
            SetPrivateField(service, "scaleStep", ScaleStep);
            SetPrivateField(service, "levelPriceMultipliers", PriceMultipliers);

            testSkin = CreateSkin(TestSkinId, TestSkinPrice);

            service.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(UpgradesKey);
            UnityEngine.Object.DestroyImmediate(serviceObject);
            UnityEngine.Object.DestroyImmediate(testSkin);
        }

        [Test]
        public void GetLevel_NoUpgrades_ReturnsZero()
        {
            Assert.AreEqual(0, service.GetLevel(TestSkinId));
        }

        [Test]
        public void TryUpgrade_WithEnoughMoney_IncreasesLevel()
        {
            fakeCurrency.SetBalance(1000);

            bool result = service.TryUpgrade(testSkin);

            Assert.IsTrue(result);
            Assert.AreEqual(1, service.GetLevel(TestSkinId));
        }

        [Test]
        public void TryUpgrade_NotEnoughMoney_ReturnsFalse()
        {
            fakeCurrency.SetBalance(0);

            bool result = service.TryUpgrade(testSkin);

            Assert.IsFalse(result);
            Assert.AreEqual(0, service.GetLevel(TestSkinId));
        }

        [Test]
        public void TryUpgrade_AtMaxLevel_ReturnsFalse()
        {
            UpgradeToMaxLevel();

            bool result = service.TryUpgrade(testSkin);

            Assert.IsFalse(result);
            Assert.AreEqual(MaxLevel, service.GetLevel(TestSkinId));
        }

        [Test]
        public void IsMaxed_AtMaxLevel_ReturnsTrue()
        {
            UpgradeToMaxLevel();

            Assert.IsTrue(service.IsMaxed(TestSkinId));
        }

        [Test]
        public void IsMaxed_BelowMaxLevel_ReturnsFalse()
        {
            Assert.IsFalse(service.IsMaxed(TestSkinId));
        }

        [Test]
        public void GetBonusScale_ReturnsLevelTimesStep()
        {
            fakeCurrency.SetBalance(999999);

            service.TryUpgrade(testSkin);
            service.TryUpgrade(testSkin);
            service.TryUpgrade(testSkin);

            Assert.AreEqual(0.3f, service.GetBonusScale(TestSkinId), 0.001f);
        }

        [Test]
        public void GetTotalScale_BelowCap_IsBasePlusBonus()
        {
            fakeCurrency.SetBalance(999999);

            service.TryUpgrade(testSkin);
            service.TryUpgrade(testSkin);

            Assert.AreEqual(1.2f, service.GetTotalScale(TestSkinId), 0.001f);
        }

        [Test]
        public void GetTotalScale_CappedAtMaxScale()
        {
            UpgradeToMaxLevel();

            // Ten levels of 0.1 would reach 2.0, but the ring is clamped so it cannot
            // outgrow the playfield.
            Assert.AreEqual(MaxTotalScale, service.GetTotalScale(TestSkinId), 0.001f);
        }

        [Test]
        public void GetUpgradePrice_FollowsPerLevelMultipliers()
        {
            fakeCurrency.SetBalance(999999);

            int price0 = service.GetUpgradePrice(testSkin);
            service.TryUpgrade(testSkin);
            int price1 = service.GetUpgradePrice(testSkin);
            service.TryUpgrade(testSkin);
            int price2 = service.GetUpgradePrice(testSkin);

            Assert.AreEqual(100, price0);
            Assert.AreEqual(150, price1);
            Assert.AreEqual(200, price2);
        }

        [Test]
        public void GetUpgradePrice_BeyondLastMultiplier_KeepsUsingTheLastOne()
        {
            fakeCurrency.SetBalance(999999);

            for (int i = 0; i < PriceMultipliers.Length; i++)
            {
                service.TryUpgrade(testSkin);
            }

            int expected = Mathf.RoundToInt(TestSkinPrice * PriceMultipliers[PriceMultipliers.Length - 1]);
            Assert.AreEqual(expected, service.GetUpgradePrice(testSkin));
        }

        [Test]
        public void GetUpgradePrice_FreeSkin_UsesFallbackBasePrice()
        {
            SkinItem freeSkin = CreateSkin("FreeRing", 0);

            try
            {
                // A free skin has no price to scale, so upgrades are priced off a flat base.
                Assert.AreEqual(150, service.GetUpgradePrice(freeSkin));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(freeSkin);
            }
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
        public void TryUpgrade_SpendsTheUpgradePrice()
        {
            fakeCurrency.SetBalance(1000);

            service.TryUpgrade(testSkin);

            Assert.AreEqual(900, fakeCurrency.Balance);
        }

        [Test]
        public void Persistence_SaveAndLoad_PreservesLevels()
        {
            fakeCurrency.SetBalance(999999);

            service.TryUpgrade(testSkin);
            service.TryUpgrade(testSkin);

            service.Initialize();

            Assert.AreEqual(2, service.GetLevel(TestSkinId));
        }

        private void UpgradeToMaxLevel()
        {
            fakeCurrency.SetBalance(999999);

            for (int i = 0; i < MaxLevel; i++)
            {
                service.TryUpgrade(testSkin);
            }
        }

        private static SkinItem CreateSkin(string skinId, int price)
        {
            SkinItem skin = ScriptableObject.CreateInstance<SkinItem>();
            SetPrivateField(skin, "skinId", skinId);
            SetPrivateField(skin, "price", price);
            return skin;
        }

        // Serialized fields are private by design, so the fixture reaches them by name.
        // Failing loudly here keeps a renamed field from surfacing as a bare NullReferenceException.
        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType()
                .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(
                field,
                $"{target.GetType().Name} has no private field '{fieldName}' — this fixture is out of date with the type.");

            field.SetValue(target, value);
        }

        private sealed class FakeCurrencyService : MonoBehaviour, ICurrencyService
        {
            public event Action<int> BalanceChanged;

            private int balance;

            public int Balance => balance;

            public int RunEarnings => 0;

            public void ResetRunEarnings() { }

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
