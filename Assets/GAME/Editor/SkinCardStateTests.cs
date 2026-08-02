using NUnit.Framework;
using JumpRing.Game.UI;

namespace JumpRing.Tests.EditMode
{
    [TestFixture]
    public sealed class SkinCardStateTests
    {
        private const int MaxLevel = 6;
        private const int UpgradePrice = 150;

        [Test]
        public void ResolveAction_NotOwned_IsBuy()
        {
            var state = Locked(canAfford: true);

            Assert.AreEqual(SkinCardAction.Buy, state.ResolveAction());
            Assert.IsTrue(state.IsActionAvailable());
        }

        [Test]
        public void ResolveAction_NotOwnedAndBroke_ButtonAndCardAreDead()
        {
            var state = Locked(canAfford: false);

            Assert.AreEqual(SkinCardAction.Buy, state.ResolveAction());
            Assert.IsFalse(state.IsActionAvailable());
            Assert.IsFalse(state.IsCardClickable());
        }

        [Test]
        public void ResolveAction_OwnedBelowMaxLevel_IsUpgrade()
        {
            var state = Owned(isActive: false, upgradeLevel: 0, canAffordUpgrade: true);

            Assert.AreEqual(SkinCardAction.Upgrade, state.ResolveAction());
            Assert.IsTrue(state.IsActionAvailable());
        }

        // The bug this whole flow was built around: an owned skin used to be unreachable while its
        // upgrade was unaffordable, which locked the player into whatever skin was active.
        [Test]
        public void OwnedSkin_WithUnaffordableUpgrade_StaysSelectableFromCard()
        {
            var state = Owned(isActive: false, upgradeLevel: 2, canAffordUpgrade: false);

            Assert.AreEqual(SkinCardAction.Upgrade, state.ResolveAction());
            Assert.IsFalse(state.IsActionAvailable());
            Assert.IsTrue(state.IsCardClickable());
        }

        [Test]
        public void OwnedSkin_IsAlwaysClickable_EvenWhenBroke()
        {
            var state = new SkinCardState(true, false, false, 0, MaxLevel, UpgradePrice, false);

            Assert.IsTrue(state.IsCardClickable());
        }

        [Test]
        public void ResolveAction_OwnedAtMaxLevel_IsSelect()
        {
            var state = Owned(isActive: false, upgradeLevel: MaxLevel, canAffordUpgrade: false);

            Assert.AreEqual(SkinCardAction.Select, state.ResolveAction());
            Assert.IsTrue(state.IsActionAvailable());
        }

        [Test]
        public void ResolveAction_ActiveAtMaxLevel_ButtonIsInert()
        {
            var state = Owned(isActive: true, upgradeLevel: MaxLevel, canAffordUpgrade: false);

            Assert.AreEqual(SkinCardAction.Select, state.ResolveAction());
            Assert.IsFalse(state.IsActionAvailable());
        }

        [Test]
        public void ActiveSkin_BelowMaxLevel_StillSellsUpgrades()
        {
            var state = Owned(isActive: true, upgradeLevel: 3, canAffordUpgrade: true);

            Assert.AreEqual(SkinCardAction.Upgrade, state.ResolveAction());
            Assert.IsTrue(state.IsActionAvailable());
        }

        [Test]
        public void IsMaxUpgraded_AtAndAboveMaxLevel_IsTrue()
        {
            Assert.IsFalse(Owned(false, MaxLevel - 1, true).IsMaxUpgraded);
            Assert.IsTrue(Owned(false, MaxLevel, true).IsMaxUpgraded);
            Assert.IsTrue(Owned(false, MaxLevel + 1, true).IsMaxUpgraded);
        }

        private static SkinCardState Locked(bool canAfford)
        {
            return new SkinCardState(false, false, canAfford, 0, MaxLevel, UpgradePrice, false);
        }

        private static SkinCardState Owned(bool isActive, int upgradeLevel, bool canAffordUpgrade)
        {
            return new SkinCardState(true, isActive, true, upgradeLevel, MaxLevel, UpgradePrice, canAffordUpgrade);
        }
    }
}
