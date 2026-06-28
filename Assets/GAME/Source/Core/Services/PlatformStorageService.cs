using System;
using System.Collections;
using System.Collections.Generic;
using PlatformLink;
using UnityEngine;

namespace JumpRing.Game.Core.Services
{
    public sealed class PlatformStorageService : MonoBehaviour
    {
        private const float FallbackTimeoutSeconds = 10f;
        private const float PlinkWaitSeconds = 8f;
        private const float SaveFlushSeconds = 2f;

        private readonly Dictionary<string, int> intCache = new();
        private readonly Dictionary<string, string> stringCache = new();
        private readonly HashSet<string> dirtyInts = new();
        private readonly HashSet<string> dirtyStrings = new();
        private bool isLoaded;
        private bool callbackFired;
        private bool loadStarted;
        private bool flushScheduled;

        // True only when the cloud load finished cleanly (all keys resolved via PLink).
        // If we fell back to PlayerPrefs (timeout / PLink not ready) this stays false and
        // Flush refuses to push to the cloud, so a possibly-stale local state can never
        // overwrite real cloud progress.
        private bool cloudWritable;

        private string[] pendingIntKeys;
        private string[] pendingStringKeys;
        private Action pendingOnComplete;

        public bool IsLoaded => isLoaded;
        public event Action Loaded;

        public void Initialize(string[] intKeys, string[] stringKeys, Action onComplete)
        {
            pendingIntKeys = intKeys;
            pendingStringKeys = stringKeys;
            pendingOnComplete = onComplete;

            if (PLink.IsInitialized)
            {
                LoadFromCloud();
            }
            else
            {
                // Wait for the platform to become ready so cloud data can be read,
                // but never block the game forever: fall back to local after a timeout.
                PLink.Initilized += OnPlinkReady;
                StartCoroutine(PlinkWaitTimeout());
            }
        }

        private void OnDestroy()
        {
            Flush();
            PLink.Initilized -= OnPlinkReady;
        }

        private void OnPlinkReady()
        {
            PLink.Initilized -= OnPlinkReady;
            LoadFromCloud();
        }

        private IEnumerator PlinkWaitTimeout()
        {
            yield return new WaitForSeconds(PlinkWaitSeconds);

            PLink.Initilized -= OnPlinkReady;

            if (loadStarted)
            {
                yield break;
            }

            Debug.LogWarning("[PlatformStorageService] PLink not ready in time — falling back to PlayerPrefs");
            loadStarted = true;
            LoadFromPlayerPrefs(pendingIntKeys, pendingStringKeys);
            Complete();
        }

        private void LoadFromCloud()
        {
            if (loadStarted)
            {
                return;
            }

            loadStarted = true;

            int remaining = pendingIntKeys.Length + pendingStringKeys.Length;

            if (remaining == 0)
            {
                cloudWritable = true;
                Complete();
                return;
            }

            StartCoroutine(FallbackTimeout());

            foreach (var key in pendingIntKeys)
            {
                string k = key;
                PLink.Storage.LoadInt(k, (success, value) =>
                {
                    // Late callbacks that arrive after a fallback Complete must not mutate
                    // the cache — services already read their values and a silent overwrite
                    // would desync them.
                    if (callbackFired) return;
                    intCache[k] = success ? value : PlayerPrefs.GetInt(k, 0);
                    remaining--;
                    if (remaining <= 0)
                    {
                        cloudWritable = true;
                        Complete();
                    }
                });
            }

            foreach (var key in pendingStringKeys)
            {
                string k = key;
                PLink.Storage.LoadString(k, (success, value) =>
                {
                    if (callbackFired) return;
                    stringCache[k] = success ? value : PlayerPrefs.GetString(k, "");
                    remaining--;
                    if (remaining <= 0)
                    {
                        cloudWritable = true;
                        Complete();
                    }
                });
            }
        }

        private void Complete()
        {
            if (callbackFired)
            {
                return;
            }

            callbackFired = true;
            isLoaded = true;
            pendingOnComplete?.Invoke();
            Loaded?.Invoke();
        }

        private IEnumerator FallbackTimeout()
        {
            yield return new WaitForSeconds(FallbackTimeoutSeconds);

            if (callbackFired)
            {
                yield break;
            }

            Debug.LogWarning("[PlatformStorageService] PLink.Storage timeout — falling back to PlayerPrefs");
            LoadFromPlayerPrefs(pendingIntKeys, pendingStringKeys);
            Complete();
        }

        private void LoadFromPlayerPrefs(string[] intKeys, string[] stringKeys)
        {
            // Only fill keys that haven't already been resolved (e.g. from the cloud).
            // The fallback timeout must not clobber values that did arrive.
            foreach (var key in intKeys)
            {
                if (!intCache.ContainsKey(key))
                {
                    intCache[key] = PlayerPrefs.GetInt(key, 0);
                }
            }

            foreach (var key in stringKeys)
            {
                if (!stringCache.ContainsKey(key))
                {
                    stringCache[key] = PlayerPrefs.GetString(key, "");
                }
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
            dirtyInts.Add(key);
            ScheduleFlush();
        }

        public void SetString(string key, string value)
        {
            stringCache[key] = value;
            PlayerPrefs.SetString(key, value);
            dirtyStrings.Add(key);
            ScheduleFlush();
        }

        private void ScheduleFlush()
        {
            if (flushScheduled)
            {
                return;
            }

            flushScheduled = true;
            StartCoroutine(FlushAfterDelay());
        }

        private IEnumerator FlushAfterDelay()
        {
            yield return new WaitForSecondsRealtime(SaveFlushSeconds);
            flushScheduled = false;
            Flush();
        }

        // Batches the expensive PlayerPrefs.Save() (synchronous IndexedDB flush on WebGL)
        // and cloud writes instead of doing them on every coin/score change.
        private void Flush()
        {
            if (dirtyInts.Count == 0 && dirtyStrings.Count == 0)
            {
                return;
            }

            PlayerPrefs.Save();

            if (PLink.IsInitialized && cloudWritable)
            {
                foreach (var key in dirtyInts)
                {
                    PLink.Storage.SaveInt(key, intCache[key]);
                }

                foreach (var key in dirtyStrings)
                {
                    PLink.Storage.SaveString(key, stringCache[key]);
                }
            }

            dirtyInts.Clear();
            dirtyStrings.Clear();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                Flush();
            }
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused)
            {
                Flush();
            }
        }
    }
}
