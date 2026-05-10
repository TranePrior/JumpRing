using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using JumpRing.Game.UI;
using JumpRing.Game.Core.Services;
using JumpRing.Game.Theming;

namespace JumpRing.Game.Editor
{
    public static class ShopFieldsWirer
    {
        [MenuItem("Tools/Wire Shop Fields")]
        public static void Wire()
        {
            // --- Wire ShopPresenter on ShopPanel ---
            var shopPresenter = Object.FindFirstObjectByType<ShopPresenter>(FindObjectsInactive.Include);
            if (shopPresenter == null)
            {
                Debug.LogError("[ShopFieldsWirer] ShopPresenter not found in scene!");
                return;
            }

            var so = new SerializedObject(shopPresenter);

            // skinShopService
            var skinShopService = Object.FindFirstObjectByType<SkinShopService>(FindObjectsInactive.Include);
            if (skinShopService != null)
                so.FindProperty("skinShopService").objectReferenceValue = skinShopService;

            // currencyServiceComponent
            var allMono = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var m in allMono)
            {
                if (m is ICurrencyService)
                {
                    so.FindProperty("currencyServiceComponent").objectReferenceValue = m;
                    break;
                }
            }

            // shopPanel (CanvasGroup on same object)
            var cg = shopPresenter.GetComponent<CanvasGroup>();
            if (cg != null)
                so.FindProperty("shopPanel").objectReferenceValue = cg;

            // closeButton
            var closeBtn = FindChildByName<Button>(shopPresenter.transform, "CloseButton");
            if (closeBtn != null)
                so.FindProperty("closeButton").objectReferenceValue = closeBtn;

            // gridContent
            var gridContent = FindChildTransformByName(shopPresenter.transform, "GridContent");
            if (gridContent != null)
                so.FindProperty("gridContent").objectReferenceValue = gridContent;

            // cardPrefab
            var cardGuids = AssetDatabase.FindAssets("SkinCard t:Prefab");
            foreach (var guid in cardGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("UI/SkinCard"))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    var cardView = prefab.GetComponent<ShopSkinCardView>();
                    if (cardView != null)
                    {
                        so.FindProperty("cardPrefab").objectReferenceValue = cardView;
                        break;
                    }
                }
            }

            // balanceLabel
            var balanceLabel = FindChildByName<TMP_Text>(shopPresenter.transform, "BalanceLabel");
            if (balanceLabel != null)
                so.FindProperty("balanceLabel").objectReferenceValue = balanceLabel;

            // iconBar
            var iconBar = GameObject.Find("IconBar");
            if (iconBar == null)
            {
                // Try finding inactive
                var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var t in allTransforms)
                {
                    if (t.name == "IconBar")
                    {
                        iconBar = t.gameObject;
                        break;
                    }
                }
            }
            if (iconBar != null)
                so.FindProperty("iconBar").objectReferenceValue = iconBar;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(shopPresenter);
            Debug.Log("[ShopFieldsWirer] ShopPresenter fields wired successfully!");

            // --- Wire MainMenuPresenter.shopPresenter ---
            var mainMenuPresenter = Object.FindFirstObjectByType<MainMenuPresenter>(FindObjectsInactive.Include);
            if (mainMenuPresenter != null)
            {
                var mso = new SerializedObject(mainMenuPresenter);
                var spProp = mso.FindProperty("shopPresenter");
                if (spProp != null && spProp.objectReferenceValue == null)
                {
                    spProp.objectReferenceValue = shopPresenter;
                    mso.ApplyModifiedProperties();
                    EditorUtility.SetDirty(mainMenuPresenter);
                    Debug.Log("[ShopFieldsWirer] MainMenuPresenter.shopPresenter wired!");
                }
            }

            Debug.Log("[ShopFieldsWirer] All done! Save the scene (Ctrl+S).");
        }

        static T FindChildByName<T>(Transform root, string name) where T : Component
        {
            var all = root.GetComponentsInChildren<T>(true);
            foreach (var c in all)
            {
                if (c.gameObject.name == name)
                    return c;
            }
            return null;
        }

        static Transform FindChildTransformByName(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                    return child;
            }
            return null;
        }
    }
}
