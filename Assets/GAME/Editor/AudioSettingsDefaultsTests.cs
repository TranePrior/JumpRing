using System.Reflection;
using JumpRing.Game.Core.Services;
using NUnit.Framework;
using UnityEngine;

namespace JumpRing.Tests.EditMode
{
    /// <summary>
    /// A brand new player used to start with music, effects and vibration switched off: the storage
    /// load wrote a cache entry even for keys nobody had ever saved, and that entry shadowed the
    /// default each reader passes to <see cref="PlatformStorageService.GetInt"/>.
    /// </summary>
    [TestFixture]
    public sealed class AudioSettingsDefaultsTests
    {
        private static readonly string[] SoundKeys =
        {
            StorageKeys.SettingsMusic,
            StorageKeys.SettingsEffects,
            StorageKeys.SettingsVibration
        };

        private GameObject serviceObject;
        private PlatformStorageService storage;
        private AudioSettingsService audioSettings;

        [SetUp]
        public void SetUp()
        {
            ForgetSoundKeys();

            serviceObject = new GameObject("AudioSettings");
            storage = serviceObject.AddComponent<PlatformStorageService>();
            audioSettings = serviceObject.AddComponent<AudioSettingsService>();

            SetPrivateField(audioSettings, "storageService", storage);

            // AddComponent leaves serialized arrays null outside play mode, and applying the loaded
            // settings walks the effects sources.
            SetPrivateField(audioSettings, "effectsSources", new AudioSource[0]);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(serviceObject);
            ForgetSoundKeys();
        }

        [Test]
        public void NewPlayer_StartsWithSoundAndVibrationOn()
        {
            LoadStorage();

            audioSettings.Initialize();

            Assert.IsTrue(audioSettings.IsMusicEnabled, "music");
            Assert.IsTrue(audioSettings.IsEffectsEnabled, "effects");
            Assert.IsTrue(audioSettings.IsVibrationEnabled, "vibration");
        }

        [Test]
        public void ReturningPlayer_KeepsEverythingSwitchedOff()
        {
            foreach (string key in SoundKeys)
            {
                PlayerPrefs.SetInt(key, 0);
            }

            LoadStorage();

            audioSettings.Initialize();

            Assert.IsFalse(audioSettings.IsMusicEnabled, "music");
            Assert.IsFalse(audioSettings.IsEffectsEnabled, "effects");
            Assert.IsFalse(audioSettings.IsVibrationEnabled, "vibration");
        }

        [Test]
        public void UnsetKey_LeavesTheReadersDefaultAlone()
        {
            LoadStorage();

            Assert.AreEqual(7, storage.GetInt(StorageKeys.SettingsMusic, 7));
            Assert.AreEqual("none", storage.GetString(StorageKeys.ActiveSkinId, "none"));
        }

        [Test]
        public void StoredZero_WinsOverTheReadersDefault()
        {
            PlayerPrefs.SetInt(StorageKeys.SettingsMusic, 0);

            LoadStorage();

            Assert.AreEqual(0, storage.GetInt(StorageKeys.SettingsMusic, 7));
        }

        /// <summary>
        /// Runs the local half of the load — the same path the service falls back to when the
        /// platform never answers. The cloud half needs a live platform and is verified on a build.
        /// </summary>
        private void LoadStorage()
        {
            InvokePrivate(storage, "LoadFromPlayerPrefs", StorageKeys.IntKeys, StorageKeys.StringKeys);
        }

        private static void ForgetSoundKeys()
        {
            foreach (string key in SoundKeys)
            {
                PlayerPrefs.DeleteKey(key);
            }

            PlayerPrefs.DeleteKey(StorageKeys.ActiveSkinId);
        }

        private static void InvokePrivate(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(target, arguments);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }
    }
}
