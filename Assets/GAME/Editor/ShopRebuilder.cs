using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.IO;

namespace JumpRing.Game.Editor
{
    public static class ShopRebuilder
    {
        [MenuItem("Tools/Rebuild Shop (Full)")]
        public static void RebuildAll()
        {
            EnsureRoundedRectSprite();
            RebuildSkinCard();
            RebuildShopPanel();
            AssetDatabase.SaveAssets();
            Debug.Log("[ShopRebuilder] Done! Now re-open the scene to update prefab instances.");
        }

        static readonly string FontPath = "Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Bangers SDF.asset";
        static readonly string BuyBtnPath = "Assets/GAME/Art/Sprite/Sp_Purchase_Button_no_activ.png";
        static readonly string ActiveBtnPath = "Assets/GAME/Art/Sprite/Sp_Purchase_Button_activ.png";
        static readonly string CoinIconPath = "Assets/GAME/Art/UI/Icon_Coin.png";
        static readonly string ExitIconPath = "Assets/GAME/Art/UI/Icon_Exit.png";
        static readonly string RoundedRectPath = "Assets/GAME/Art/Sprite/Sp_RoundedRect.png";

        static readonly Color DarkText = new Color(0.176f, 0.216f, 0.282f); // #2D3748
        static readonly Color PanelBg = new Color(0.706f, 0.82f, 0.976f);   // #B4D1F9
        static readonly Color CardBg = new Color(0.91f, 0.93f, 0.98f);      // #E8EDFA
        static readonly Color HeaderBg = new Color(0.875f, 0.914f, 0.961f);  // #DFE9F5

        static void EnsureRoundedRectSprite()
        {
            string fullPath = Path.Combine(Application.dataPath, "..", RoundedRectPath);
            if (File.Exists(fullPath)) return;

            int w = 64, h = 64, r = 20;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = 0, dy = 0;
                    if (x < r && y < r) { dx = r - x - 0.5f; dy = r - y - 0.5f; }
                    else if (x >= w - r && y < r) { dx = x - (w - r) + 0.5f; dy = r - y - 0.5f; }
                    else if (x < r && y >= h - r) { dx = r - x - 0.5f; dy = y - (h - r) + 0.5f; }
                    else if (x >= w - r && y >= h - r) { dx = x - (w - r) + 0.5f; dy = y - (h - r) + 0.5f; }

                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    byte a = dist > r ? (byte)0 : (dist > r - 1.5f ? (byte)(255 * (r - dist) / 1.5f) : (byte)255);
                    pixels[y * w + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.Refresh();

            // Set meta: sprite mode Single, 9-slice borders
            var importer = AssetImporter.GetAtPath(RoundedRectPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = new Vector4(r, r, r, r); // 9-slice
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }

        static GameObject UI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static void SetFont(TMP_Text tmp)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font != null)
            {
                tmp.font = font;
                tmp.material = font.material;
            }
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

            // Background (rounded via white 9-slice sprite, tinted to card color)
            var roundedSpr = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedRectPath);
            var buyBtnSpr = AssetDatabase.LoadAssetAtPath<Sprite>(BuyBtnPath);
            var bg = root.AddComponent<Image>();
            if (roundedSpr) bg.sprite = roundedSpr;
            bg.type = Image.Type.Sliced;
            bg.color = CardBg;
            var cardBtn = root.AddComponent<Button>();
            cardBtn.targetGraphic = bg;
            var nav = cardBtn.navigation;
            nav.mode = Navigation.Mode.None;
            cardBtn.navigation = nav;

            // --- Name Label (top) ---
            var nameGO = UI("NameLabel", root.transform);
            var nameRT = nameGO.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0, 0.78f);
            nameRT.anchorMax = new Vector2(1, 0.98f);
            nameRT.offsetMin = new Vector2(8, 0);
            nameRT.offsetMax = new Vector2(-8, 0);
            var nameLabel = nameGO.AddComponent<TextMeshProUGUI>();
            nameLabel.text = "Skin";
            nameLabel.fontSize = 50;
            nameLabel.fontStyle = FontStyles.Bold;
            nameLabel.color = DarkText;
            nameLabel.alignment = TextAlignmentOptions.Center;
            nameLabel.raycastTarget = false;
            SetFont(nameLabel);

            // --- Icon (center, larger area) ---
            var iconGO = UI("IconImage", root.transform);
            var iconRT = iconGO.GetComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.05f, 0.18f);
            iconRT.anchorMax = new Vector2(0.95f, 0.82f);
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
            actionRT.anchorMin = new Vector2(0.04f, 0.02f);
            actionRT.anchorMax = new Vector2(0.96f, 0.22f);
            actionRT.offsetMin = Vector2.zero;
            actionRT.offsetMax = Vector2.zero;
            var actionBg = actionGO.AddComponent<Image>();
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
            var coinSpr = AssetDatabase.LoadAssetAtPath<Sprite>(CoinIconPath);
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
            priceLabel.fontSize = 40;
            priceLabel.fontStyle = FontStyles.Bold;
            priceLabel.color = Color.white;
            priceLabel.alignment = TextAlignmentOptions.Center;
            priceLabel.raycastTarget = false;
            SetFont(priceLabel);

            // Action label
            var actLabelGO = UI("ActionLabel", actionGO.transform);
            var actLabelRT = actLabelGO.GetComponent<RectTransform>();
            actLabelRT.anchorMin = Vector2.zero;
            actLabelRT.anchorMax = Vector2.one;
            actLabelRT.offsetMin = Vector2.zero;
            actLabelRT.offsetMax = Vector2.zero;
            var actLabel = actLabelGO.AddComponent<TextMeshProUGUI>();
            actLabel.text = "\u0410\u043a\u0442\u0438\u0432\u0435\u043d";
            actLabel.fontSize = 40;
            actLabel.fontStyle = FontStyles.Bold;
            actLabel.color = Color.white;
            actLabel.alignment = TextAlignmentOptions.Center;
            actLabel.raycastTarget = false;
            actLabel.gameObject.SetActive(false);
            SetFont(actLabel);

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

            // Button sprites
            var activeBtnSpr = AssetDatabase.LoadAssetAtPath<Sprite>(ActiveBtnPath);
            so.FindProperty("buyButtonSprite").objectReferenceValue = buyBtnSpr;
            so.FindProperty("activeButtonSprite").objectReferenceValue = activeBtnSpr;

            // Button colors (white = use sprite color as-is)
            so.FindProperty("buyButtonColor").colorValue = Color.white;
            so.FindProperty("activeButtonColor").colorValue = Color.white;
            so.FindProperty("disabledButtonColor").colorValue = new Color(0.6f, 0.6f, 0.6f, 1f);
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("[ShopRebuilder] SkinCard rebuilt.");
        }

        static void RebuildShopPanel()
        {
            string path = "Assets/GAME/Prefab/Shop.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);

            // Clear
            for (int i = root.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            foreach (var c in root.GetComponents<Component>())
                if (!(c is RectTransform) && !(c is CanvasRenderer))
                    Object.DestroyImmediate(c);
            foreach (var c in root.GetComponents<CanvasRenderer>())
                Object.DestroyImmediate(c);

            var roundedSpr = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedRectPath);

            // Root = full screen
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var cg = root.AddComponent<CanvasGroup>();
            var rootBg = root.AddComponent<Image>();
            rootBg.color = PanelBg;

            // === HEADER ===
            var header = UI("Header", root.transform);
            var hRT = header.GetComponent<RectTransform>();
            hRT.anchorMin = new Vector2(0, 0.87f);
            hRT.anchorMax = Vector2.one;
            hRT.offsetMin = new Vector2(7, 0);
            hRT.offsetMax = new Vector2(-7, -7);
            var hBg = header.AddComponent<Image>();
            if (roundedSpr) hBg.sprite = roundedSpr;
            hBg.type = Image.Type.Sliced;
            hBg.color = HeaderBg;
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
            titleTMP.color = DarkText;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.raycastTarget = false;
            SetFont(titleTMP);

            // Close button (rounded square)
            var closeGO = UI("CloseButton", header.transform);
            var clRT = closeGO.GetComponent<RectTransform>();
            clRT.anchorMin = new Vector2(1, 0.5f);
            clRT.anchorMax = new Vector2(1, 0.5f);
            clRT.sizeDelta = new Vector2(100, 100);
            clRT.anchoredPosition = new Vector2(-60, 0);
            var clBg = closeGO.AddComponent<Image>();
            if (roundedSpr) clBg.sprite = roundedSpr;
            clBg.type = Image.Type.Sliced;
            clBg.color = new Color(0.75f, 0.80f, 0.88f); // slightly darker than header

            var clIcon = UI("Icon", closeGO.transform);
            var ciRT = clIcon.GetComponent<RectTransform>();
            ciRT.anchorMin = new Vector2(0.2f, 0.2f);
            ciRT.anchorMax = new Vector2(0.8f, 0.8f);
            ciRT.offsetMin = Vector2.zero;
            ciRT.offsetMax = Vector2.zero;
            var ciImg = clIcon.AddComponent<Image>();
            var exitSpr = AssetDatabase.LoadAssetAtPath<Sprite>(ExitIconPath);
            if (exitSpr) ciImg.sprite = exitSpr;
            ciImg.preserveAspect = true;
            ciImg.color = DarkText;
            ciImg.raycastTarget = false;

            var closeBtn = closeGO.AddComponent<Button>();
            closeBtn.targetGraphic = clBg;
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

            // === BALANCE BAR (rounded, pill-shaped) ===
            var balBar = UI("BalanceBar", root.transform);
            var bRT = balBar.GetComponent<RectTransform>();
            bRT.anchorMin = new Vector2(0.3f, 0.015f);
            bRT.anchorMax = new Vector2(0.7f, 0.075f);
            bRT.offsetMin = Vector2.zero;
            bRT.offsetMax = Vector2.zero;
            var bBg = balBar.AddComponent<Image>();
            if (roundedSpr) bBg.sprite = roundedSpr;
            bBg.type = Image.Type.Sliced;
            bBg.color = HeaderBg;
            bBg.raycastTarget = false;

            var bCoin = UI("CoinIcon", balBar.transform);
            var bcRT = bCoin.GetComponent<RectTransform>();
            bcRT.anchorMin = new Vector2(0, 0.1f);
            bcRT.anchorMax = new Vector2(0, 0.9f);
            bcRT.sizeDelta = new Vector2(55, 0);
            bcRT.anchoredPosition = new Vector2(40, 0);
            var bcImg = bCoin.AddComponent<Image>();
            var coinSpr = AssetDatabase.LoadAssetAtPath<Sprite>(CoinIconPath);
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
            balLabel.fontSize = 44;
            balLabel.fontStyle = FontStyles.Bold;
            balLabel.color = DarkText;
            balLabel.alignment = TextAlignmentOptions.Center;
            balLabel.raycastTarget = false;
            SetFont(balLabel);

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
            Debug.Log("[ShopRebuilder] Shop rebuilt.");
        }
    }
}
