using System;
using System.Collections;
using System.Collections.Generic;
using PlatformLink;
using UnityEngine;

namespace JumpRing.Game.Core.Services
{
    public sealed class PlatformStorageService : MonoBehaviour
    {
        private const float FallbackTimeoutSeconds = 3f;

        private readonly Dictionary<string, int> intCache = new();
        private readonly Dictionary<string, string> stringCache = new();
        private bool isLoaded;
        private bool callbackFired;

        public bool IsLoaded => isLoaded;
        public event Action Loaded;

        public void Initialize(string[] intKeys, string[] stringKeys, Action onComplete)
        {
            if (!PLink.IsInitialized)
            {
                LoadFromPlayerPrefs(intKeys, stringKeys);
                isLoaded = true;
                onComplete?.Invoke();
                Loaded?.Invoke();
                return;
            }

            callbackFired = false;
            int remaining = intKeys.Length + stringKeys.Length;

            if (remaining == 0)
            {
                isLoaded = true;
                callbackFired = true;
                onComplete?.Invoke();
                Loaded?.Invoke();
                return;
            }

            StartCoroutine(FallbackTimeout(intKeys, stringKeys, onComplete));

            foreach (var key in intKeys)
            {
                string k = key;
                PLink.Storage.LoadInt(k, (success, value) =>
                {
                    intCache[k] = success ? value : PlayerPrefs.GetInt(k, 0);
                    remaining--;
                    if (remaining <= 0) OnAllLoaded(onComplete);
                });
            }

            foreach (var key in stringKeys)
            {
                string k = key;
                PLink.Storage.LoadString(k, (success, value) =>
                {
                    stringCache[k] = success ? value : PlayerPrefs.GetString(k, "");
                    remaining--;
                    if (remaining <= 0) OnAllLoaded(onComplete);
                });
            }
        }

        private void OnAllLoaded(Action onComplete)
        {
            if (callbackFired)
            {
                return;
            }

            callbackFired = true;
            isLoaded = true;
            onComplete?.Invoke();
            Loaded?.Invoke();
        }

        private IEnumerator FallbackTimeout(string[] intKeys, string[] stringKeys, Action onComplete)
        {
            yield return new WaitForSeconds(FallbackTimeoutSeconds);

            if (callbackFired)
            {
                yield break;
            }

            Debug.LogWarning("[PlatformStorageService] PLink.Storage timeout — falling back to PlayerPrefs");
            LoadFromPlayerPrefs(intKeys, stringKeys);
            OnAllLoaded(onComplete);
        }

        private void LoadFromPlayerPrefs(string[] intKeys, string[] stringKeys)
        {
            foreach (var key in intKeys)
            {
                intCache[key] = PlayerPrefs.GetInt(key, 0);
            }

            foreach (var key in stringKeys)
            {
                stringCache[key] = PlayerPrefs.GetString(key, "");
            }
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            return intCache.TryGetValue(key, out var value) ? value : defaultValue;
        }

        public string GetString(string key, string defaultValue = "")
        {
            return stringCache.TryGetValue(key, out var value) ? value : defaultValue;
        }

        public void SetInt(string key, int value)
        {
            intCache[key] = value;
            PlayerPrefs.SetInt(key, value);
            PlayerPrefs.Save();

            if (PLink.IsInitialized)
            {
                PLink.Storage.SaveInt(key, value);
            }
        }

        public void SetString(string key, string value)
        {
            stringCache[key] = value;
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();

            if (PLink.IsInitialized)
            {
                PLink.Storage.SaveString(key, value);
            }
        }
    }
}
