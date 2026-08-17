using System;
using System.Collections.Generic;
using JumpRing.Game.Core.Localization;
using JumpRing.Game.Theming;
using NUnit.Framework;
using UnityEditor;

namespace JumpRing.Tests.EditMode
{
    /// <summary>
    /// Yandex requirements 2.10 and 8.2.3 — every string the player can read must exist in every
    /// shipped language. The game failed moderation with a fully working SDK detection because half
    /// the labels had no English text to switch to, so coverage is asserted here instead of by
    /// clicking through the published build.
    /// </summary>
    [TestFixture]
    public sealed class LocalizationCoverageTests
    {
        private const string RussianAssetPath = "Assets/GAME/Data/Localization/Localization_RU.asset";
        private const string EnglishAssetPath = "Assets/GAME/Data/Localization/Localization_EN.asset";
        private const string SkinNameKeyPrefix = "SkinName";

        private LocalizationData russian;
        private LocalizationData english;

        [SetUp]
        public void LoadAssets()
        {
            russian = AssetDatabase.LoadAssetAtPath<LocalizationData>(RussianAssetPath);
            english = AssetDatabase.LoadAssetAtPath<LocalizationData>(EnglishAssetPath);
        }

        [Test]
        public void EveryKey_HasTextInBothLanguages()
        {
            foreach (LocalizationKey key in Enum.GetValues(typeof(LocalizationKey)))
            {
                AssertTranslated(russian, key, "RU");
                AssertTranslated(english, key, "EN");
            }
        }

        // The failure mode that reached moderation: an entry copied into the English asset with the
        // Russian text still in it. A Cyrillic character in the English data is always that bug.
        [Test]
        public void EnglishData_ContainsNoCyrillic()
        {
            foreach (LocalizationKey key in Enum.GetValues(typeof(LocalizationKey)))
            {
                string text = english.GetText(key);

                foreach (char character in text)
                {
                    Assert.IsFalse(
                        character >= 'Ѐ' && character <= 'ӿ',
                        $"English text for {key} contains Cyrillic: '{text}'");
                }
            }
        }

        [Test]
        public void EverySkinItem_PointsAtASkinNameKey()
        {
            foreach (SkinItem skin in LoadAll<SkinItem>())
            {
                Assert.IsTrue(
                    skin.NameKey.ToString().StartsWith(SkinNameKeyPrefix, StringComparison.Ordinal),
                    $"{skin.name} has no skin name key assigned (resolved to {skin.NameKey})");
            }
        }

        [Test]
        public void SkinNameKeys_AreNotSharedBetweenSkins()
        {
            var used = new Dictionary<LocalizationKey, string>();

            foreach (SkinItem skin in LoadAll<SkinItem>())
            {
                Assert.IsFalse(
                    used.ContainsKey(skin.NameKey),
                    $"{skin.name} reuses the name key of {(used.TryGetValue(skin.NameKey, out string owner) ? owner : string.Empty)}");

                used.Add(skin.NameKey, skin.name);
            }
        }

        private static void AssertTranslated(LocalizationData data, LocalizationKey key, string language)
        {
            Assert.IsTrue(data.HasText(key), $"{language} text for {key} is missing");
            Assert.IsFalse(
                string.IsNullOrWhiteSpace(data.GetText(key)),
                $"{language} text for {key} is empty");
        }

        private static IEnumerable<T> LoadAll<T>() where T : UnityEngine.ScriptableObject
        {
            foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
            {
                yield return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            }
        }
    }
}
