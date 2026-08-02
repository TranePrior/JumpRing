using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using JumpRing.Game.Core.Services;
using JumpRing.Game.Theming;
using JumpRing.Game.UI;

namespace JumpRing.Game.Editor
{
    /// <summary>
    /// Play Mode cheats and save wiping. Lives under Assets/GAME/Editor, so none of it is compiled
    /// into a player build — there is nothing here that can leak into production.
    /// </summary>
    public static class DevCheats
    {
        private const int CoinGrant = 100000;
        private const int UnlockBudget = 1000000;

        [MenuItem("Tools/Dev/Add 100000 Coins", true)]
        [MenuItem("Tools/Dev/Unlock All Skins", true)]
        private static bool ValidatePlaying()
        {
            return EditorApplication.isPlaying;
        }

        [MenuItem("Tools/Dev/Add 100000 Coins")]
        private static void AddCoins()
        {
            var currency = FindCurrencyService();
            if (currency == null)
            {
                Debug.LogError("[DevCheats] No ICurrencyService in the loaded scenes.");
                return;
            }

            currency.Add(CoinGrant);
            RefreshOpenShop();
            Debug.Log($"[DevCheats] Balance: {currency.Balance}");
        }

        // Buys every skin through the real shop API instead of poking at storage, so ownership is
        // persisted and every listener reacts exactly as it would for a genuine purchase.
        [MenuItem("Tools/Dev/Unlock All Skins")]
        private static void UnlockAllSkins()
        {
            var currency = FindCurrencyService();
            var shop = Object.FindFirstObjectByType<SkinShopService>(FindObjectsInactive.Include);

            if (currency == null || shop == null)
            {
                Debug.LogError("[DevCheats] Currency or skin shop service is missing from the loaded scenes.");
                return;
            }

            currency.Add(UnlockBudget);

            int unlocked = 0;
            foreach (var pack in shop.Catalog.Packs)
            {
                foreach (var skin in pack.Skins)
                {
                    if (shop.TryPurchase(skin))
                    {
                        unlocked++;
                    }
                }
            }

            RefreshOpenShop();
            Debug.Log($"[DevCheats] Unlocked {unlocked} skin(s).");
        }

        [MenuItem("Tools/Dev/Wipe Save", true)]
        private static bool ValidateNotPlaying()
        {
            return !EditorApplication.isPlaying;
        }

        // Play Mode is blocked on purpose: PlatformStorageService serves reads from its in-memory
        // cache and flushes it back, so a wipe during a session is silently undone on the next save.
        [MenuItem("Tools/Dev/Wipe Save")]
        private static void WipeSave()
        {
            var saveDirectories = GetEditorSaveDirectories();

            bool confirmed = EditorUtility.DisplayDialog(
                "Wipe save",
                "Delete this project's local save?\n\n"
                + $"PlayerPrefs + PlatformLink editor files in: {string.Join(", ", saveDirectories)}\n\n"
                + "Cloud saves on Yandex are not touched — a published build will pull that progress back.",
                "Wipe",
                "Cancel");

            if (!confirmed)
            {
                return;
            }

            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            int files = 0;
            foreach (var directory in saveDirectories)
            {
                files += DeleteSaveFiles(directory);
            }

            AssetDatabase.Refresh();
            Debug.Log($"[DevCheats] Save wiped: PlayerPrefs + {files} PlatformLink file(s).");
        }

        // In the editor PLink stores every key as its own .txt under the configured folder, and that
        // is what the game actually reads — PlayerPrefs is only the fallback when a key is missing
        // there. Wiping one without the other leaves the old progress in place.
        private static int DeleteSaveFiles(string directory)
        {
            if (!Directory.Exists(directory))
            {
                return 0;
            }

            int deleted = 0;
            foreach (var file in Directory.GetFiles(directory, "*.txt"))
            {
                if (AssetDatabase.DeleteAsset(file.Replace('\\', '/')))
                {
                    deleted++;
                }
            }

            return deleted;
        }

        // Read from the PlatformLink config rather than hardcoding the path, so a folder moved in
        // the config does not silently turn this into a no-op.
        private static List<string> GetEditorSaveDirectories()
        {
            var directories = new List<string>();
            var guids = AssetDatabase.FindAssets("PlatformLinkConfig t:ScriptableObject");

            foreach (var guid in guids)
            {
                var config = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid));
                var iterator = new SerializedObject(config).GetIterator();

                while (iterator.NextVisible(true))
                {
                    if (iterator.propertyType != SerializedPropertyType.String
                        || iterator.name != "_saveFilePath"
                        || string.IsNullOrEmpty(iterator.stringValue)
                        || directories.Contains(iterator.stringValue))
                    {
                        continue;
                    }

                    directories.Add(iterator.stringValue);
                }
            }

            return directories;
        }

        private static ICurrencyService FindCurrencyService()
        {
            var behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var behaviour in behaviours)
            {
                if (behaviour is ICurrencyService currency)
                {
                    return currency;
                }
            }

            return null;
        }

        // The shop grid is only rebuilt on open, so a cheat applied while it is on screen would
        // otherwise leave stale prices and dead buttons until the player closes and reopens it.
        private static void RefreshOpenShop()
        {
            if (!ShopPresenter.IsOpen)
            {
                return;
            }

            var presenter = Object.FindFirstObjectByType<ShopPresenter>(FindObjectsInactive.Include);
            presenter.Open();
        }
    }
}
