#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SecondChancePanelCreator
{
    // Figma source dimensions
    private const float PopupWidth = 820f;
    private const float PopupHeight = 1124f;
    private const float BorderThickness = 7f;
    private const float HeaderWidth = 806f;
    private const float HeaderHeight = 180f;
    private const float HeaderTopMargin = 7f;
    private const float CardMargin = 30f;
    private const float CardTopOffset = 218f;
    private const float TimerSize = 440f;
    private const float TimerInnerRatio = 0.77f; // 340/440
    private const float HeartBgSize = 329f;
    private const float HeartIconSize = 262f;
    private const float CloseBtnWidth = 128f;
    private const float CloseBtnHeight = 136f;
    private const float ButtonWidth = 720f;
    private const float ButtonHeight = 182f;
    private const float ButtonBottomMargin = 20f;
    private const float CoinSize = 125f;
    private const float ChanceTitleTopOffset = 47f;
    private const float ChanceTitleHeight = 76f;

    [MenuItem("Tools/Create SecondChance Panel")]
    public static void CreatePanel()
    {
        var hudRoot = GameObject.Find("HUD");
        if (hudRoot == null)
        {
            EditorUtility.DisplayDialog("Error", "No HUD GameObject found in the scene.", "OK");
            return;
        }

        var canvas = hudRoot.GetComponent<Canvas>();
        if (canvas == null)
            canvas = hudRoot.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error", "No Canvas found on HUD.", "OK");
            return;
        }

        var roundedRectSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/GAME/Art/Sprite/Rect/Sp_RoundedRect.png");
        var heartSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/GAME/Art/Sprite/Bonus/Sp_Booster_AddHeart.png");
        var close3dSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/GAME/Art/Sprite/Button/Close/Sp_Close_3d.png");
        var closeDawnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/GAME/Art/Sprite/Button/Close/Sp_Close_button_dawn.png");
        var shopButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/GAME/Art/Sprite/Button/Sp_Button_Shop.png");
        var coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/GAME/Art/Sprite/Coin/Sp_coin.png");
        var circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/GAME/Art/Sprite/Rect/Circle.png");
        var font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(
            "Assets/GAME/Fonts/Rubik-Bold SDF.asset");

        var oldPanel = hudRoot.transform.Find("SecondChancePanel");
        if (oldPanel != null)
            Undo.DestroyObjectImmediate(oldPanel.gameObject);

        // ── Full-screen overlay ──
        var panel = CreateUI("SecondChancePanel", hudRoot.transform);
        StretchFull(panel.GetComponent<RectTransform>());

        var panelCanvas = panel.AddComponent<Canvas>();
        panelCanvas.overrideSorting = true;
        panelCanvas.sortingOrder = 100;
        panel.AddComponent<GraphicRaycaster>();

        var dimBg = panel.AddComponent<Image>();
        dimBg.color = new Color(0f, 0f, 0f, 0.6f);
        dimBg.raycastTarget = true;

        // ── Popup container (border layer) ──
        var popup = CreateUI("Popup", panel.transform);
        var popupRect = popup.GetComponent<RectTransform>();
        popupRect.anchorMin = new Vector2(0.5f, 0.5f);
        popupRect.anchorMax = new Vector2(0.5f, 0.5f);
        popupRect.sizeDelta = new Vector2(PopupWidth, PopupHeight);

        var popupBorderImg = popup.AddComponent<Image>();
        popupBorderImg.sprite = roundedRectSprite;
        popupBorderImg.type = Image.Type.Sliced;
        popupBorderImg.color = new Color(0.596f, 0.667f, 0.796f, 1f); // border ~#98AACB

        // ── Popup fill ──
        var popupFill = CreateUI("PopupFill", popup.transform);
        var popupFillRect = popupFill.GetComponent<RectTransform>();
        popupFillRect.anchorMin = Vector2.zero;
        popupFillRect.anchorMax = Vector2.one;
        popupFillRect.offsetMin = new Vector2(BorderThickness, BorderThickness);
        popupFillRect.offsetMax = new Vector2(-BorderThickness, -BorderThickness);

        var popupFillImg = popupFill.AddComponent<Image>();
        popupFillImg.sprite = roundedRectSprite;
        popupFillImg.type = Image.Type.Sliced;
        popupFillImg.color = new Color(0.71f, 0.82f, 1f, 1f); // ~#B5D1FF

        // ── Header bar ──
        var header = CreateUI("Header", popup.transform);
        var headerRect = header.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = new Vector2(0f, -HeaderTopMargin);
        headerRect.sizeDelta = new Vector2(-(PopupWidth - HeaderWidth), HeaderHeight);

        var headerImg = header.AddComponent<Image>();
        headerImg.sprite = roundedRectSprite;
        headerImg.type = Image.Type.Sliced;
        headerImg.color = new Color(0.875f, 0.914f, 0.961f, 1f); // #DFE9F5

        var headerShadow = header.AddComponent<Shadow>();
        headerShadow.effectColor = new Color(0f, 0f, 0f, 0.25f);
        headerShadow.effectDistance = new Vector2(0f, -6f);

        // "YOU LOSE"
        var titleGo = CreateUI("TitleLabel", header.transform);
        var titleRect = titleGo.GetComponent<RectTransform>();
        StretchFull(titleRect);
        titleRect.offsetMax = new Vector2(-CloseBtnWidth - 20f, 0f);

        var titleText = titleGo.AddComponent<TMPro.TextMeshProUGUI>();
        titleText.text = "YOU LOSE";
        titleText.fontSize = 64;
        titleText.fontStyle = TMPro.FontStyles.Bold;
        titleText.alignment = TMPro.TextAlignmentOptions.Center;
        titleText.color = new Color(0.263f, 0.337f, 0.471f, 0.8f); // #435678 @ 80%
        if (font != null) titleText.font = font;

        // Close (X) button — 3D style from Figma
        var closeBtnGo = CreateUI("QuitButton", header.transform);
        var closeBtnRect = closeBtnGo.GetComponent<RectTransform>();
        closeBtnRect.anchorMin = new Vector2(1f, 0.5f);
        closeBtnRect.anchorMax = new Vector2(1f, 0.5f);
        closeBtnRect.pivot = new Vector2(1f, 0.5f);
        closeBtnRect.sizeDelta = new Vector2(CloseBtnWidth, CloseBtnHeight);
        closeBtnRect.anchoredPosition = new Vector2(-18f, 0f);

        var closeBtnImg = closeBtnGo.AddComponent<Image>();
        closeBtnImg.sprite = close3dSprite;
        closeBtnImg.preserveAspect = true;
        closeBtnImg.raycastTarget = true;

        var closeBtn = closeBtnGo.AddComponent<Button>();
        closeBtn.targetGraphic = closeBtnImg;
        if (closeDawnSprite != null)
        {
            closeBtn.transition = Selectable.Transition.SpriteSwap;
            closeBtn.spriteState = new SpriteState { pressedSprite = closeDawnSprite };
        }

        // ── Inner card ──
        var card = CreateUI("Card", popup.transform);
        var cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = Vector2.zero;
        cardRect.anchorMax = Vector2.one;
        cardRect.offsetMin = new Vector2(CardMargin, CardMargin);
        cardRect.offsetMax = new Vector2(-CardMargin, -CardTopOffset);

        var cardImg = card.AddComponent<Image>();
        cardImg.sprite = roundedRectSprite;
        cardImg.type = Image.Type.Sliced;
        cardImg.color = new Color(0.875f, 0.914f, 0.961f, 1f); // #DFE9F5

        var cardOutline = card.AddComponent<Outline>();
        cardOutline.effectColor = Color.white;
        cardOutline.effectDistance = new Vector2(5f, 5f);

        // ── "ВТОРОЙ ШАНС?" ──
        var chanceTitleGo = CreateUI("ChanceTitleLabel", card.transform);
        var chanceTitleRect = chanceTitleGo.GetComponent<RectTransform>();
        chanceTitleRect.anchorMin = new Vector2(0f, 1f);
        chanceTitleRect.anchorMax = new Vector2(1f, 1f);
        chanceTitleRect.pivot = new Vector2(0.5f, 1f);
        chanceTitleRect.sizeDelta = new Vector2(0f, ChanceTitleHeight);
        chanceTitleRect.anchoredPosition = new Vector2(0f, -ChanceTitleTopOffset);

        var chanceText = chanceTitleGo.AddComponent<TMPro.TextMeshProUGUI>();
        chanceText.text = "\u0412\u0422\u041E\u0420\u041E\u0419 \u0428\u0410\u041D\u0421?";
        chanceText.fontSize = 64;
        chanceText.fontStyle = TMPro.FontStyles.Bold;
        chanceText.alignment = TMPro.TextAlignmentOptions.Center;
        chanceText.color = new Color(0.263f, 0.337f, 0.471f, 1f); // #435678
        if (font != null) chanceText.font = font;

        // ── Timer ring ──
        // Timer center is 51px above card center
        var timerContainer = CreateUI("TimerContainer", card.transform);
        var timerContainerRect = timerContainer.GetComponent<RectTransform>();
        timerContainerRect.anchorMin = new Vector2(0.5f, 0.5f);
        timerContainerRect.anchorMax = new Vector2(0.5f, 0.5f);
        timerContainerRect.sizeDelta = new Vector2(TimerSize, TimerSize);
        timerContainerRect.anchoredPosition = new Vector2(0f, 51f);

        // Timer background (gray)
        var timerBgGo = CreateUI("TimerBackground", timerContainer.transform);
        StretchFull(timerBgGo.GetComponent<RectTransform>());
        var timerBgImg = timerBgGo.AddComponent<Image>();
        timerBgImg.sprite = circleSprite;
        timerBgImg.color = new Color(0.749f, 0.780f, 0.839f, 1f); // #BFC7D6
        timerBgImg.type = Image.Type.Filled;
        timerBgImg.fillMethod = Image.FillMethod.Radial360;
        timerBgImg.fillOrigin = (int)Image.Origin360.Top;
        timerBgImg.fillClockwise = true;
        timerBgImg.fillAmount = 1f;

        // Timer fill (green)
        var timerFillGo = CreateUI("TimerFill", timerContainer.transform);
        StretchFull(timerFillGo.GetComponent<RectTransform>());
        var timerFillImg = timerFillGo.AddComponent<Image>();
        timerFillImg.sprite = circleSprite;
        timerFillImg.color = new Color(0.34f, 0.64f, 0.03f, 1f); // green ~#57A408
        timerFillImg.type = Image.Type.Filled;
        timerFillImg.fillMethod = Image.FillMethod.Radial360;
        timerFillImg.fillOrigin = (int)Image.Origin360.Top;
        timerFillImg.fillClockwise = true;
        timerFillImg.fillAmount = 1f;

        // Inner circle (ring hole, matches card bg)
        var innerCircle = CreateUI("InnerCircle", timerContainer.transform);
        var innerCircleRect = innerCircle.GetComponent<RectTransform>();
        innerCircleRect.anchorMin = new Vector2(0.5f, 0.5f);
        innerCircleRect.anchorMax = new Vector2(0.5f, 0.5f);
        innerCircleRect.sizeDelta = Vector2.one * (TimerSize * TimerInnerRatio);
        var innerCircleImg = innerCircle.AddComponent<Image>();
        innerCircleImg.sprite = circleSprite;
        innerCircleImg.color = new Color(0.875f, 0.914f, 0.961f, 1f); // #DFE9F5

        // Semi-transparent circle behind heart
        var heartBg = CreateUI("HeartBackground", timerContainer.transform);
        var heartBgRect = heartBg.GetComponent<RectTransform>();
        heartBgRect.anchorMin = new Vector2(0.5f, 0.5f);
        heartBgRect.anchorMax = new Vector2(0.5f, 0.5f);
        heartBgRect.sizeDelta = new Vector2(HeartBgSize, HeartBgSize);
        var heartBgImg = heartBg.AddComponent<Image>();
        heartBgImg.sprite = circleSprite;
        heartBgImg.color = new Color(0.384f, 0.451f, 0.569f, 0.15f);
        heartBgImg.raycastTarget = false;

        // Heart icon
        var heartGo = CreateUI("HeartIcon", timerContainer.transform);
        var heartRect = heartGo.GetComponent<RectTransform>();
        heartRect.anchorMin = new Vector2(0.5f, 0.5f);
        heartRect.anchorMax = new Vector2(0.5f, 0.5f);
        heartRect.sizeDelta = new Vector2(HeartIconSize, HeartIconSize);
        var heartImg = heartGo.AddComponent<Image>();
        heartImg.sprite = heartSprite;
        heartImg.preserveAspect = true;
        heartImg.raycastTarget = false;

        // ── Continue button ──
        var continueBtnGo = CreateUI("ContinueButton", card.transform);
        var continueBtnRect = continueBtnGo.GetComponent<RectTransform>();
        continueBtnRect.anchorMin = new Vector2(0.5f, 0f);
        continueBtnRect.anchorMax = new Vector2(0.5f, 0f);
        continueBtnRect.pivot = new Vector2(0.5f, 0f);
        continueBtnRect.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);
        continueBtnRect.anchoredPosition = new Vector2(0f, ButtonBottomMargin);

        var continueBtnImg = continueBtnGo.AddComponent<Image>();
        continueBtnImg.sprite = shopButtonSprite;
        continueBtnImg.type = Image.Type.Sliced;

        var continueBtn = continueBtnGo.AddComponent<Button>();
        continueBtn.targetGraphic = continueBtnImg;

        // Coin icon on button
        var coinGo = CreateUI("CoinIcon", continueBtnGo.transform);
        var coinRect = coinGo.GetComponent<RectTransform>();
        coinRect.anchorMin = new Vector2(0f, 0.5f);
        coinRect.anchorMax = new Vector2(0f, 0.5f);
        coinRect.pivot = new Vector2(0f, 0.5f);
        coinRect.sizeDelta = new Vector2(CoinSize, CoinSize);
        coinRect.anchoredPosition = new Vector2(65f, 5f);
        var coinImg = coinGo.AddComponent<Image>();
        coinImg.sprite = coinSprite;
        coinImg.preserveAspect = true;
        coinImg.raycastTarget = false;

        // "Получить" label with gradient + outline
        var labelGo = CreateUI("Label", continueBtnGo.transform);
        var labelRect = labelGo.GetComponent<RectTransform>();
        StretchFull(labelRect);
        labelRect.offsetMin = new Vector2(120f, 9f);

        var labelText = labelGo.AddComponent<TMPro.TextMeshProUGUI>();
        labelText.text = "\u041F\u043E\u043B\u0443\u0447\u0438\u0442\u044C";
        labelText.fontSize = 78;
        labelText.fontStyle = TMPro.FontStyles.Bold;
        labelText.alignment = TMPro.TextAlignmentOptions.Center;
        labelText.enableVertexGradient = true;
        labelText.colorGradient = new TMPro.VertexGradient(
            new Color(1f, 1f, 0f, 1f),
            new Color(1f, 1f, 0f, 1f),
            new Color(1f, 0.5f, 0.21f, 1f),
            new Color(1f, 0.5f, 0.21f, 1f)
        );
        labelText.outlineColor = new Color32(124, 52, 0, 255); // #7C3400
        labelText.outlineWidth = 0.25f;
        if (font != null) labelText.font = font;

        // ── Wire up presenter ──
        var presenters = Object.FindObjectsByType<JumpRing.Game.UI.SecondChancePresenter>(
            FindObjectsSortMode.None);
        if (presenters.Length > 0)
        {
            var presenter = presenters[0];
            var so = new SerializedObject(presenter);
            so.FindProperty("secondChancePanel").objectReferenceValue = panel;
            so.FindProperty("continueButton").objectReferenceValue = continueBtn;
            so.FindProperty("quitButton").objectReferenceValue = closeBtn;
            so.FindProperty("timerFill").objectReferenceValue = timerFillImg;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(presenter);
        }

        panel.transform.SetAsLastSibling();
        panel.SetActive(false);

        Undo.RegisterCreatedObjectUndo(panel, "Create SecondChance Panel");
        Selection.activeGameObject = panel;
        Debug.Log("SecondChance Panel created (Figma v2).");
    }

    [MenuItem("Tools/Create Hearts HUD")]
    public static void CreateHeartsHud()
    {
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error", "No Canvas found in the scene.", "OK");
            return;
        }

        var oldHearts = canvas.transform.Find("HeartsContainer");
        if (oldHearts != null)
            Undo.DestroyObjectImmediate(oldHearts.gameObject);

        var container = CreateUI("HeartsContainer", canvas.transform);
        var containerRect = container.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0f, 1f);
        containerRect.anchorMax = new Vector2(0f, 1f);
        containerRect.pivot = new Vector2(0f, 1f);
        containerRect.anchoredPosition = new Vector2(20f, -100f);
        containerRect.sizeDelta = new Vector2(150f, 40f);

        var layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var heartsPresenter = container.AddComponent<JumpRing.Game.UI.HeartsHudPresenter>();
        var heartImages = new Image[3];
        var inactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.4f);

        for (var i = 0; i < 3; i++)
        {
            var heart = CreateUI($"Heart_{i}", container.transform);
            var heartRect = heart.GetComponent<RectTransform>();
            heartRect.sizeDelta = new Vector2(36f, 36f);

            var img = heart.AddComponent<Image>();
            img.color = inactiveColor;
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            heartImages[i] = img;
        }

        var so = new SerializedObject(heartsPresenter);

        var bonusManager = Object.FindFirstObjectByType<JumpRing.Game.Gameplay.BonusEffectManager>();
        if (bonusManager != null)
            so.FindProperty("bonusEffectManager").objectReferenceValue = bonusManager;

        var heartsProp = so.FindProperty("heartImages");
        heartsProp.arraySize = 3;
        for (var i = 0; i < 3; i++)
            heartsProp.GetArrayElementAtIndex(i).objectReferenceValue = heartImages[i];

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(heartsPresenter);

        Undo.RegisterCreatedObjectUndo(container, "Create Hearts HUD");
        Selection.activeGameObject = container;
        Debug.Log("Hearts HUD created and wired up.");
    }

    private static GameObject CreateUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
#endif
