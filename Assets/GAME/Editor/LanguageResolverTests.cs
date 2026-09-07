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
    }
}
