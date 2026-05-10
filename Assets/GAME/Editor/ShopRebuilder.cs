using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace JumpRing.Game.Editor
{
    public static class ShopRebuilder
    {
        [MenuItem("Tools/Rebuild Shop (Full)")]
        public static void RebuildAll()
        {
            RebuildSkinCard();
            RebuildShopPanel();
            AssetDatabase.SaveAssets();
            Debug.Log("[ShopRebuilder] Done! Now re-open the scene to update prefab instances.");
        }

        static GameObject UI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static void RebuildSkinCard()
        {
            string path = "Assets/GAME/Prefab/UI/SkinCard.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);

            // Clear
            for (int i = root.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            foreach (var c in root.GetComponents<Component>())
                if (!(c is RectTransform) && !(c is CanvasRenderer))
                    Object.DestroyImmediate(c);
            foreach (var c in root.GetComponents<CanvasRenderer>())
                Object.DestroyImmediate(c);

            var rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(428, 518);

            // Background
            var bg = root.AddComponent<Image>();
            bg.color = new Color(0.875f, 0.914f, 0.961f); // #DFE9F5
            var cardBtn = root.AddComponent<Button>();
            cardBtn.targetGraphic = bg;
            var nav = cardBtn.navigation;
            nav.mode = Navigation.Mode.None;
            cardBtn.navigation = nav;

            // --- Name Label (top) ---
            var nameGO = UI("NameLabel", root.transform);
            var nameRT = nameGO.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0, 0.75f);
            nameRT.anchorMax = new Vector2(1, 0.95f);
            nameRT.offsetMin = new Vector2(8, 0);
            nameRT.offsetMax = new Vector2(-8, 0);
            var nameLabel = nameGO.AddComponent<TextMeshProUGUI>();
            nameLabel.text = "Skin";
            nameLabel.fontSize = 40;
            nameLabel.fontStyle = FontStyles.Bold;
            nameLabel.color = new Color(0.478f, 0.537f, 0.639f); // #7A89A3
            nameLabel.alignment = TextAlignmentOptions.Center;
            nameLabel.raycastTarget = false;

            // --- Icon (center) ---
            var iconGO = UI("IconImage", root.transform);
            var iconRT = iconGO.GetComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.15f, 0.28f);
            iconRT.anchorMax = new Vector2(0.85f, 0.75f);
            iconRT.offsetMin = Vector2.zero;
            iconRT.offsetMax = Vector2.zero;
            var iconImg = iconGO.AddComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            // --- Selection Frame ---
            var frameGO = UI("SelectionFrame", root.transform);
            var frameRT = frameGO.GetComponent<RectTransform>();
            frameRT.anchorMin = Vector2.zero;
            frameRT.anchorMax = Vector2.one;
            frameRT.offsetMin = new Vector2(4, 4);
            frameRT.offsetMax = new Vector2(-4, -4);
            var selFrame = frameGO.AddComponent<Image>();
            selFrame.color = new Color(0.39f, 0.58f, 0.97f, 0.3f);
            selFrame.raycastTarget = false;
            selFrame.enabled = false;

            // --- Action Button ---
            var actionGO = UI("ActionButton", root.transform);
            var actionRT = actionGO.GetComponent<RectTransform>();
            actionRT.anchorMin = new Vector2(0.04f, 0.01f);
            actionRT.anchorMax = new Vector2(0.96f, 0.25f);
            actionRT.offsetMin = Vector2.zero;
            actionRT.offsetMax = Vector2.zero;
            var actionBg = actionGO.AddComponent<Image>();
            var buyBtnSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GAME/Art/Sprite/Sp_Purchase_Button_no_activ.png");
            if (buyBtnSpr) actionBg.sprite = buyBtnSpr;
            actionBg.type = Image.Type.Sliced;
            actionBg.color = Color.white;
            var actionBtn = actionGO.AddComponent<Button>();
            actionBtn.targetGraphic = actionBg;
            var bnav = actionBtn.navigation;
            bnav.mode = Navigation.Mode.None;
            actionBtn.navigation = bnav;

            // Coin icon
            var coinGO = UI("CoinIcon", actionGO.transform);
            var coinRT = coinGO.GetComponent<RectTransform>();
            coinRT.anchorMin = new Vector2(0, 0.1f);
            coinRT.anchorMax = new Vector2(0, 0.9f);
            coinRT.sizeDelta = new Vector2(55, 0);
            coinRT.anchoredPosition = new Vector2(40, 0);
            var coinImg = coinGO.AddComponent<Image>();
            var coinSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GAME/Art/UI/Icon_Coin.png");
            if (coinSpr) coinImg.sprite = coinSpr;
            coinImg.preserveAspect = true;
            coinImg.raycastTarget = false;

            // Price label
            var priceGO = UI("PriceLabel", actionGO.transform);
            var priceRT = priceGO.GetComponent<RectTransform>();
            priceRT.anchorMin = new Vector2(0.25f, 0);
            priceRT.anchorMax = Vector2.one;
            priceRT.offsetMin = Vector2.zero;
            priceRT.offsetMax = Vector2.zero;
            var priceLabel = priceGO.AddComponent<TextMeshProUGUI>();
            priceLabel.text = "500";
            priceLabel.fontSize = 36;
            priceLabel.fontStyle = FontStyles.Bold;
            priceLabel.color = Color.white;
            priceLabel.alignment = TextAlignmentOptions.Center;
            priceLabel.raycastTarget = false;

            // Action label (Активен/Выбрать)
            var actLabelGO = UI("ActionLabel", actionGO.transform);
            var actLabelRT = actLabelGO.GetComponent<RectTransform>();
            actLabelRT.anchorMin = Vector2.zero;
            actLabelRT.anchorMax = Vector2.one;
            actLabelRT.offsetMin = Vector2.zero;
            actLabelRT.offsetMax = Vector2.zero;
            var actLabel = actLabelGO.AddComponent<TextMeshProUGUI>();
            actLabel.text = "\u0410\u043a\u0442\u0438\u0432\u0435\u043d";
            actLabel.fontSize = 36;
            actLabel.fontStyle = FontStyles.Bold;
            actLabel.color = Color.white;
            actLabel.alignment = TextAlignmentOptions.Center;
            actLabel.raycastTarget = false;
            actLabel.gameObject.SetActive(false);

            // --- Wire ShopSkinCardView ---
            var view = root.AddComponent<UI.ShopSkinCardView>();
            var so = new SerializedObject(view);
            so.FindProperty("iconImage").objectReferenceValue = iconImg;
            so.FindProperty("nameLabel").objectReferenceValue = nameLabel;
            so.FindProperty("priceLabel").objectReferenceValue = priceLabel;
            so.FindProperty("selectionFrame").objectReferenceValue = selFrame;
            so.FindProperty("cardButton").objectReferenceValue = cardBtn;
            so.FindProperty("actionButton").objectReferenceValue = actionBtn;
            so.FindProperty("actionButtonLabel").objectReferenceValue = actLabel;
            so.FindProperty("actionButtonImage").objectReferenceValue = actionBg;
            so.FindProperty("coinIcon").objectReferenceValue = coinImg;

            var activeBtnSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GAME/Art/Sprite/Sp_Purchase_Button_activ.png");
            so.FindProperty("buyButtonSprite").objectReferenceValue = buyBtnSpr;
            so.FindProperty("activeButtonSprite").objectReferenceValue = activeBtnSpr;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("[ShopRebuilder] SkinCard rebuilt.");
        }

        static void RebuildShopPanel()
        {
            string path = "Assets/GAME/Prefab/ShopPanel.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);

            // Clear
            for (int i = root.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            foreach (var c in root.GetComponents<Component>())
                if (!(c is RectTransform) && !(c is CanvasRenderer))
                    Object.DestroyImmediate(c);
            foreach (var c in root.GetComponents<CanvasRenderer>())
                Object.DestroyImmediate(c);

            // Root = full screen
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var cg = root.AddComponent<CanvasGroup>();
            var rootBg = root.AddComponent<Image>();
            rootBg.color = new Color(0.706f, 0.82f, 0.976f); // #B4D1F9

            // === HEADER ===
            var header = UI("Header", root.transform);
            var hRT = header.GetComponent<RectTransform>();
            hRT.anchorMin = new Vector2(0, 0.87f);
            hRT.anchorMax = Vector2.one;
            hRT.offsetMin = new Vector2(8, 0);
            hRT.offsetMax = new Vector2(-8, -8);
            var hBg = header.AddComponent<Image>();
            hBg.color = new Color(0.875f, 0.914f, 0.961f); // #DFE9F5
            hBg.raycastTarget = false;

            // Title
            var title = UI("Title", header.transform);
            var tRT = title.GetComponent<RectTransform>();
            tRT.anchorMin = Vector2.zero;
            tRT.anchorMax = Vector2.one;
            tRT.offsetMin = Vector2.zero;
            tRT.offsetMax = Vector2.zero;
            var titleTMP = title.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "\u041C\u0430\u0433\u0430\u0437\u0438\u043D";
            titleTMP.fontSize = 52;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.color = new Color(0.263f, 0.337f, 0.471f); // #435678
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.raycastTarget = false;

            // Close button
            var closeGO = UI("CloseButton", header.transform);
            var clRT = closeGO.GetComponent<RectTransform>();
            clRT.anchorMin = new Vector2(1, 0.5f);
            clRT.anchorMax = new Vector2(1, 0.5f);
            clRT.sizeDelta = new Vector2(100, 100);
            clRT.anchoredPosition = new Vector2(-60, 0);
            var clShadow = closeGO.AddComponent<Image>();
            clShadow.color = new Color(0.349f, 0.392f, 0.463f); // #596476

            var clFace = UI("Face", closeGO.transform);
            var cfRT = clFace.GetComponent<RectTransform>();
            cfRT.anchorMin = Vector2.zero;
            cfRT.anchorMax = Vector2.one;
            cfRT.offsetMin = Vector2.zero;
            cfRT.offsetMax = new Vector2(0, -9);
            var cfImg = clFace.AddComponent<Image>();
            cfImg.color = new Color(0.875f, 0.914f, 0.961f);
            cfImg.raycastTarget = false;

            var clIcon = UI("Icon", clFace.transform);
            var ciRT = clIcon.GetComponent<RectTransform>();
            ciRT.anchorMin = new Vector2(0.18f, 0.18f);
            ciRT.anchorMax = new Vector2(0.82f, 0.82f);
            ciRT.offsetMin = Vector2.zero;
            ciRT.offsetMax = Vector2.zero;
            var ciImg = clIcon.AddComponent<Image>();
            var exitSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GAME/Art/UI/Icon_Exit.png");
            if (exitSpr) ciImg.sprite = exitSpr;
            ciImg.preserveAspect = true;
            ciImg.color = new Color(0.263f, 0.337f, 0.471f);
            ciImg.raycastTarget = false;

            var closeBtn = closeGO.AddComponent<Button>();
            closeBtn.targetGraphic = clShadow;
            var cnav = closeBtn.navigation;
            cnav.mode = Navigation.Mode.None;
            closeBtn.navigation = cnav;

            // === SCROLL + GRID ===
            var scroll = UI("SkinGrid", root.transform);
            var sRT = scroll.GetComponent<RectTransform>();
            sRT.anchorMin = new Vector2(0, 0.1f);
            sRT.anchorMax = new Vector2(1, 0.87f);
            sRT.offsetMin = new Vector2(15, 8);
            sRT.offsetMax = new Vector2(-15, -8);
            var sBg = scroll.AddComponent<Image>();
            sBg.color = new Color(1, 1, 1, 0.005f);
            var mask = scroll.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            var sr = scroll.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Elastic;

            var grid = UI("GridContent", scroll.transform);
            var gRT = grid.GetComponent<RectTransform>();
            gRT.anchorMin = new Vector2(0, 1);
            gRT.anchorMax = new Vector2(1, 1);
            gRT.pivot = new Vector2(0.5f, 1);
            gRT.offsetMin = Vector2.zero;
            gRT.offsetMax = Vector2.zero;
            var gl = grid.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(428, 518);
            gl.spacing = new Vector2(25, 25);
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gl.constraintCount = 2;
            gl.childAlignment = TextAnchor.UpperCenter;
            gl.padding = new RectOffset(15, 15, 15, 15);
            var csf = grid.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = gRT;

            // === BALANCE BAR ===
            var balBar = UI("BalanceBar", root.transform);
            var bRT = balBar.GetComponent<RectTransform>();
            bRT.anchorMin = new Vector2(0.3f, 0.015f);
            bRT.anchorMax = new Vector2(0.7f, 0.075f);
            bRT.offsetMin = Vector2.zero;
            bRT.offsetMax = Vector2.zero;
            var bBg = balBar.AddComponent<Image>();
            bBg.color = new Color(0.875f, 0.914f, 0.961f);
            bBg.raycastTarget = false;

            var bCoin = UI("CoinIcon", balBar.transform);
            var bcRT = bCoin.GetComponent<RectTransform>();
            bcRT.anchorMin = new Vector2(0, 0.1f);
            bcRT.anchorMax = new Vector2(0, 0.9f);
            bcRT.sizeDelta = new Vector2(55, 0);
            bcRT.anchoredPosition = new Vector2(40, 0);
            var bcImg = bCoin.AddComponent<Image>();
            var coinSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GAME/Art/UI/Icon_Coin.png");
            if (coinSpr) bcImg.sprite = coinSpr;
            bcImg.preserveAspect = true;
            bcImg.raycastTarget = false;

            var bText = UI("BalanceLabel", balBar.transform);
            var btRT = bText.GetComponent<RectTransform>();
            btRT.anchorMin = new Vector2(0.25f, 0);
            btRT.anchorMax = Vector2.one;
            btRT.offsetMin = Vector2.zero;
            btRT.offsetMax = Vector2.zero;
            var balLabel = bText.AddComponent<TextMeshProUGUI>();
            balLabel.text = "0";
            balLabel.fontSize = 40;
            balLabel.fontStyle = FontStyles.Bold;
            balLabel.color = new Color(0.263f, 0.337f, 0.471f);
            balLabel.alignment = TextAlignmentOptions.Center;
            balLabel.raycastTarget = false;

            // === WIRE ShopPresenter ===
            var presenter = root.AddComponent<UI.ShopPresenter>();
            var so = new SerializedObject(presenter);
            so.FindProperty("shopPanel").objectReferenceValue = cg;
            so.FindProperty("closeButton").objectReferenceValue = closeBtn;
            so.FindProperty("gridContent").objectReferenceValue = grid.transform;
            so.FindProperty("balanceLabel").objectReferenceValue = balLabel;

            var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GAME/Prefab/UI/SkinCard.prefab");
            if (cardPrefab)
            {
                var cardView = cardPrefab.GetComponent<UI.ShopSkinCardView>();
                so.FindProperty("cardPrefab").objectReferenceValue = cardView;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("[ShopRebuilder] ShopPanel rebuilt.");
        }
    }
}
