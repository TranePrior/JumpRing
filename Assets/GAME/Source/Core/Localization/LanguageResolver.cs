using System;

namespace JumpRing.Game.Core.Localization
{
    /// <summary>
    /// Turns raw locale strings into a <see cref="Language"/>. Free of Unity and PlatformLink types
    /// so the rules Yandex moderation checks — auto-detection from the platform locale (requirement
    /// 2.14) and an English fallback for everything the game is not translated to — are covered by
    /// EditMode tests instead of a manual pass through the published build.
    /// </summary>
    public static class LanguageResolver
    {
        private const string RussianPrefix = "ru";

        /// <summary>
        /// Reads back a language the player explicitly chose. Returns false for anything that is not
        /// a language the game ships, so a stale or corrupted cloud value falls through to detection
        /// instead of pinning the game to one language.
        /// </summary>
        public static bool TryParseStored(string stored, out Language language)
        {
            if (stored == Language.RU.ToString())
            {
                language = Language.RU;
                return true;
            }

            if (stored == Language.EN.ToString())
            {
                language = Language.EN;
                return true;
            }

            language = Language.EN;
            return false;
        }

        /// <summary>
        /// Whether a stored language still counts as a choice the player made on this account.
        /// </summary>
        /// <remarks>
        /// A switched language is written twice: to the cloud save and to the local mirror the save
        /// is cached in. Wiping the cloud save — what the platform's "clear cloud data" does, and
        /// what a fresh account amounts to — leaves the mirror behind. Honouring that leftover pins
        /// the game to a language nobody picked here and makes the platform locale unreachable
        /// forever, which is precisely the input moderation overrides when it tests requirement
        /// 2.14. So the mirror only speaks for the player while the cloud stays silent.
        /// </remarks>
        public static bool IsStoredChoiceTrustworthy(bool cloudAuthoritative, bool storedInCloud)
        {
            return storedInCloud || !cloudAuthoritative;
        }

        /// <summary>
        /// Maps the locale the platform SDK reports for the current player.
        /// </summary>
        /// <remarks>
        /// Platform SDKs disagree on how they spell a locale: the editor stub reports "En", Yandex
        /// reports "ru", and a full tag like "ru-RU" is equally legal. An exact match against "ru"
        /// silently served English to Russian players on any of those spellings. Everything the game
        /// has no translation for resolves to English, which is what the platform requires.
        /// </remarks>
        public static Language FromPlatformLocale(string platformLocale)
        {
            return IsRussian(platformLocale) ? Language.RU : Language.EN;
        }

        private static bool IsRussian(string locale)
        {
            return !string.IsNullOrEmpty(locale)
                && locale.StartsWith(RussianPrefix, StringComparison.OrdinalIgnoreCase);
        }
    }
}
