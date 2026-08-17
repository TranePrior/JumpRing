using NUnit.Framework;
using RetroCat.Modules.FlexibleUI.Runtime.Canvases;
using UnityEngine;

namespace JumpRing.Tests.EditMode
{
    [TestFixture]
    public sealed class SafeAreaPaddingTests
    {
        // left, top, right, bottom
        private static readonly Vector4 Insets = new Vector4(12f, 59f, 8f, 34f);

        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        private static readonly Vector2 TopRight = new Vector2(1f, 1f);
        private static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);
        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        [Test]
        public void TopLeftAnchor_MovesRightAndDown()
        {
            var offset = SafeAreaPadding.CalculateOffset(TopLeft, TopLeft, Insets);

            Assert.AreEqual(12f, offset.x);
            Assert.AreEqual(-59f, offset.y);
        }

        [Test]
        public void TopRightAnchor_MovesLeftAndDown()
        {
            var offset = SafeAreaPadding.CalculateOffset(TopRight, TopRight, Insets);

            Assert.AreEqual(-8f, offset.x);
            Assert.AreEqual(-59f, offset.y);
        }

        [Test]
        public void BottomCenterAnchor_MovesUpOnly()
        {
            var offset = SafeAreaPadding.CalculateOffset(BottomCenter, BottomCenter, Insets);

            Assert.AreEqual(0f, offset.x);
            Assert.AreEqual(34f, offset.y);
        }

        [Test]
        public void CenteredAnchor_IsNotMoved()
        {
            var offset = SafeAreaPadding.CalculateOffset(Center, Center, Insets);

            Assert.AreEqual(Vector2.zero, offset);
        }

        [Test]
        public void StretchedAnchors_AreTreatedAsCentered()
        {
            var offset = SafeAreaPadding.CalculateOffset(Vector2.zero, Vector2.one, Insets);

            Assert.AreEqual(Vector2.zero, offset);
        }

        [Test]
        public void ZeroInsets_ProduceNoOffset()
        {
            var offset = SafeAreaPadding.CalculateOffset(TopLeft, TopLeft, Vector4.zero);

            Assert.AreEqual(Vector2.zero, offset);
        }
    }
}
