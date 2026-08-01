using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using JumpRing.Game.Gameplay;

namespace JumpRing.Tests.EditMode
{
    /// <summary>
    /// A revive drops the player two units behind where they died — back into the stretch of line
    /// that killed them, a third of a second away at full speed. Invincibility does not cover it:
    /// a ring clipped by the line dies regardless. The safe zone is what makes the ground ahead
    /// survivable, and it has to be armed on both revive paths.
    /// </summary>
    [TestFixture]
    public sealed class RevivalGraceTests
    {
        private const float ReviveSafeZone = 2f;
        private const float InvincibilityDuration = 3f;

        private GameObject managerObject;
        private BonusEffectManager manager;

        [SetUp]
        public void SetUp()
        {
            managerObject = new GameObject("BonusEffectManager");
            manager = managerObject.AddComponent<BonusEffectManager>();

            SetField("reviveSafeZoneDuration", ReviveSafeZone);
            SetField("invincibilityDuration", InvincibilityDuration);
            SetField("safeZoneRemaining", 0f);
            SetField("maxSecondChances", 3);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(managerObject);
        }

        private void SetField(string name, object value)
        {
            var field = typeof(BonusEffectManager).GetField(
                name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"{name} not found");
            field.SetValue(manager, value);
        }

        private T GetField<T>(string name)
        {
            var field = typeof(BonusEffectManager).GetField(
                name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"{name} not found");
            return (T)field.GetValue(manager);
        }

        private float SafeZone => GetField<float>("safeZoneRemaining");

        [Test]
        public void AdRevive_FlattensTheLineAhead()
        {
            manager.StartInvincibility();

            Assert.AreEqual(ReviveSafeZone, SafeZone, 0.0001f,
                "An ad revive must not drop the player back onto the geometry that killed them.");
        }

        [Test]
        public void HeartRevive_FlattensTheLineAhead()
        {
            SetField("secondChanceCount", 1);

            manager.ConsumeSecondChance();

            Assert.AreEqual(ReviveSafeZone, SafeZone, 0.0001f,
                "Both revive paths land in the same place and need the same protection.");
        }

        [Test]
        public void SpendingNothing_ArmsNothing()
        {
            SetField("secondChanceCount", 0);

            manager.ConsumeSecondChance();

            Assert.AreEqual(0f, SafeZone, 0.0001f, "No heart, no revive, no safe zone.");
        }

        [Test]
        public void TheFirstTap_RestartsTheZoneAndGrantsInvincibility()
        {
            manager.StartInvincibility();

            // The player looked at the window for a while before tapping; most of the zone armed
            // at revive time is gone by now.
            SetField("safeZoneRemaining", 0.2f);

            manager.ActivatePendingInvincibility();

            Assert.AreEqual(ReviveSafeZone, SafeZone, 0.0001f,
                "The full stretch is measured from the first tap, not from the revive.");
            Assert.IsTrue(manager.IsInvincible, "The grace period starts when the player moves.");
        }

        [Test]
        public void ARevive_DoesNotCutShortTheStartOfRunZone()
        {
            // Dying inside the opening seconds of a run: the start zone is longer than the revive
            // one, and overwriting it would hand the player back a harder line than they had.
            SetField("safeZoneRemaining", 7f);

            manager.StartInvincibility();

            Assert.AreEqual(7f, SafeZone, 0.0001f, "A running safe zone must never be shortened.");
        }

        [Test]
        public void ActivatingWithNothingPending_ChangesNothing()
        {
            manager.ActivatePendingInvincibility();

            Assert.AreEqual(0f, SafeZone, 0.0001f, "No revive happened.");
            Assert.IsFalse(manager.IsInvincible, "No revive happened.");
        }
    }
}
