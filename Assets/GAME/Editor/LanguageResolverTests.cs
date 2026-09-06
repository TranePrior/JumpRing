using JumpRing.Game.Core.Localization;
using NUnit.Framework;

namespace JumpRing.Tests.EditMode
{
    /// <summary>
    /// Yandex requirement 2.14 — the game must pick its language from the platform SDK. These cases
    /// pin the mapping of every locale spelling the SDK is allowed to hand us.
    /// </summary>
    [TestFixture]
    public sealed class LanguageResolverTests
    {
        [TestCase("ru")]
        [TestCase("ru-RU")]
        [TestCase("RU")]
        [TestCase("Ru")]
        public void FromPlatformLocale_RussianSpellings_ResolveToRussian(string locale)
        {
            Assert.AreEqual(Language.RU, LanguageResolver.FromPlatformLocale(locale));
        }

        // Everything the game has no translation for must land on English, not on the developer's
        // own language — that is the platform's rule, and it is what moderation checks first.
        [TestCase("en")]
        [TestCase("En")]
        [TestCase("en-US")]
        [TestCase("tr")]
        [TestCase("kk")]
        [TestCase("uk")]
        [TestCase("")]
        [TestCase(null)]
        public void FromPlatformLocale_EverythingElse_ResolvesToEnglish(string locale)
        {
            Assert.AreEqual(Language.EN, LanguageResolver.FromPlatformLocale(locale));
        }

        // "russian" is a prefix match away from being read as Russian, and a locale that merely
        // starts with the same two letters must not be treated as a stored choice either.
        [TestCase("RU", Language.RU)]
        [TestCase("EN", Language.EN)]
        public void TryParseStored_ShippedLanguages_AreAccepted(string stored, Language expected)
        {
            Assert.IsTrue(LanguageResolver.TryParseStored(stored, out Language language));
            Assert.AreEqual(expected, language);
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("ru")]
        [TestCase("russian")]
        [TestCase("TR")]
        public void TryParseStored_AnythingElse_IsRejectedSoDetectionStillRuns(string stored)
        {
            Assert.IsFalse(LanguageResolver.TryParseStored(stored, out _));
        }

        // The regression this pins: switching the language once wrote it to the local mirror as well
        // as the cloud, and the mirror was then read as the player's choice forever. Clearing the
        // cloud save — or the SDK's language override moderation tests with — could no longer reach
        // the game, which kept serving the stale language.
        [Test]
        public void IsStoredChoiceTrustworthy_CloudAnsweredWithoutTheKey_DropsTheLocalLeftover()
        {
            Assert.IsFalse(LanguageResolver.IsStoredChoiceTrustworthy(
                cloudAuthoritative: true, storedInCloud: false));
        }

        [Test]
        public void IsStoredChoiceTrustworthy_CloudHoldsTheChoice_KeepsIt()
        {
            Assert.IsTrue(LanguageResolver.IsStoredChoiceTrustworthy(
                cloudAuthoritative: true, storedInCloud: true));
        }

        // Offline, or after a load timeout, the mirror is the only record of what the player picked
        // — discarding it there would reset the language of every launch the platform is slow on.
        [Test]
        public void IsStoredChoiceTrustworthy_CloudSilent_KeepsTheLocalChoice()
        {
            Assert.IsTrue(LanguageResolver.IsStoredChoiceTrustworthy(
                cloudAuthoritative: false, storedInCloud: false));
        }
    }
}
