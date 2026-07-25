using System;
using System.Collections;
using System.Collections.Generic;
using PlatformLink;
using UnityEngine;

namespace JumpRing.Game.Core.Services
{
    public sealed class PlatformStorageService : MonoBehaviour
    {
        // One deadline for the whole load, measured from Initialize. Splitting it into a
        // "wait for PLink" window plus a "wait for storage" window let the two stack, so a platform
        // that became ready just before the first window expired pushed the loading screen out to
        // the sum of both. Realtime, because a focus loss during boot pauses the game clock and a
        // scaled wait would then never expire.
        private const float LoadTimeoutSeconds = 10f;
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

            // Armed before either path starts, so the loading screen is bounded no matter where the
            // load stalls — waiting for the platform, or waiting for a storage callback.
            StartCoroutine(LoadDeadline());

            if (PLink.IsInitialized)
            {
                LoadFromCloud();
            }
            else
            {
                // Wait for the platform to become ready so cloud data can be read,
                // but never block the game forever: the deadline falls back to local.
                PLink.Initilized += OnPlinkReady;
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

        private IEnumerator LoadDeadline()
        {
            yield return new WaitForSecondsRealtime(LoadTimeoutSeconds);

            if (callbackFired)
            {
                yield break;
            }

            PLink.Initilized -= OnPlinkReady;

            Debug.LogWarning("[PlatformStorageService] Load timed out — falling back to PlayerPrefs");
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

            if (pendingIntKeys.Length + pendingStringKeys.Length == 0)
            {
                cloudWritable = true;
                Complete();
                return;
            }

            // Load keys one at a time instead of firing them all in a single frame.
            // The underlying WebGL storage keeps only one pending load callback, so parallel
            // loads would clobber each other. Sequential loads are cheap because the JS layer
            // fetches the whole player data object on the first request and serves the rest
            // from its cache — so this stays a single network round-trip overall.
            LoadNextKey(0);
        }

        private void LoadNextKey(int index)
        {
            // A fallback Complete may have already resolved every key from PlayerPrefs; late
            // callbacks must not mutate the cache after services have read their values.
            if (callbackFired)
            {
                return;
            }

            if (index < pendingIntKeys.Length)
            {
                string k = pendingIntKeys[index];
                PLink.Storage.LoadInt(k, (success, value) =>
                {
                    if (callbackFired) return;
                    // A local write made while this load was in flight wins — overwriting it here
                    // would silently discard what the player just did.
                    if (!dirtyInts.Contains(k))
                    {
                        intCache[k] = success ? value : PlayerPrefs.GetInt(k, 0);
                    }
                    LoadNextKey(index + 1);
                });
                return;
            }

            int stringIndex = index - pendingIntKeys.Length;
            if (stringIndex < pendingStringKeys.Length)
            {
                string k = pendingStringKeys[stringIndex];
                PLink.Storage.LoadString(k, (success, value) =>
                {
                    if (callbackFired) return;
                    // Same as above: a local write made mid-load must not be clobbered.
                    if (!dirtyStrings.Contains(k))
                    {
                        stringCache[k] = success ? value : PlayerPrefs.GetString(k, "");
                    }
                    LoadNextKey(index + 1);
                });
                return;
            }

            cloudWritable = true;
            Complete();
        }

        private void Complete()
        {
            if (callbackFired)
            {
                return;
            }

            callbackFired = true;
            isLoaded = true;
            StartCoroutine(NotifyLoaded());
        }

        // Initialize() runs from Awake. A platform that answers synchronously — or an empty key set
        // — would otherwise deliver this inline, before the other components' Awake/OnEnable had
        // run, and every presenter that subscribes in OnEnable would miss the first state change.
        private IEnumerator NotifyLoaded()
        {
            yield return null;

            pendingOnComplete?.Invoke();
            Loaded?.Invoke();
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

        // Writing an unchanged value would still queue a cloud save, and the platform rejects
        // a payload identical to the stored one — so unchanged writes are dropped here.
        public void SetInt(string key, int value)
        {
            if (intCache.TryGetValue(key, out var current) && current == value)
            {
                return;
            }

            intCache[key] = value;
            PlayerPrefs.SetInt(key, value);
            dirtyInts.Add(key);
            ScheduleFlush();
        }

        public void SetString(string key, string value)
        {
            if (stringCache.TryGetValue(key, out var current) && current == value)
            {
                return;
            }

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
