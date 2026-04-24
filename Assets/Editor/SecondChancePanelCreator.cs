#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SecondChancePanelCreator
{
    [MenuItem("Tools/Create SecondChance Panel")]
    public static void CreatePanel()
    {
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error", "No Canvas found in the scene.", "OK");
            return;
        }

        // Remove old panel if exists
        var oldPanel = canvas.transform.Find("SecondChancePanel");
        if (oldPanel != null)
        {
            Undo.DestroyObjectImmediate(oldPanel.gameObject);
        }

        // Panel root (full screen overlay)
        var panel = CreateUI("SecondChancePanel", canvas.transform);
        var panelRect = panel.GetComponent<RectTransform>();
        StretchFull(panelRect);

        // Override sorting to render on top of everything
        var panelCanvas = panel.AddComponent<Canvas>();
        panelCanvas.overrideSorting = true;
        panelCanvas.sortingOrder = 100;
        panel.AddComponent<GraphicRaycaster>();

        // Dimmed background
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.6f);
        bg.raycastTarget = true;

        // Timer fill bar at top
        var timerBg = CreateUI("TimerBackground", panel.transform);
        var timerBgRect = timerBg.GetComponent<RectTransform>();
        timerBgRect.anchorMin = new Vector2(0.2f, 0.72f);
        timerBgRect.anchorMax = new Vector2(0.8f, 0.75f);
        timerBgRect.offsetMin = Vector2.zero;
        timerBgRect.offsetMax = Vector2.zero;
        var timerBgImg = timerBg.AddComponent<Image>();
        timerBgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        var timerFill = CreateUI("TimerFill", timerBg.transform);
        var timerFillRect = timerFill.GetComponent<RectTransform>();
        StretchFull(timerFillRect);
        var timerFillImg = timerFill.AddComponent<Image>();
        timerFillImg.color = new Color(0.9f, 0.3f, 0.3f, 1f);
        timerFillImg.type = Image.Type.Filled;
        timerFillImg.fillMethod = Image.FillMethod.Horizontal;
        timerFillImg.fillAmount = 1f;

        // Heart button (left)
        var heartBtn = CreateButton("HeartButton", panel.transform,
            new Vector2(0.25f, 0.5f), new Vector2(160, 160),
            new Color(0.9f, 0.25f, 0.25f, 1f), "Heart");

        // Quit button (right)
        var quitBtn = CreateButton("QuitButton", panel.transform,
            new Vector2(0.75f, 0.5f), new Vector2(160, 160),
            new Color(0.4f, 0.4f, 0.4f, 1f), "Quit");

        // Ad button (bottom center)
        var adBtn = CreateButton("AdButton", panel.transform,
            new Vector2(0.5f, 0.3f), new Vector2(200, 80),
            new Color(0.2f, 0.7f, 0.3f, 1f), "Ad");

        // Wire up SecondChancePresenter
        var presenters = Object.FindObjectsByType<JumpRing.Game.UI.SecondChancePresenter>(FindObjectsSortMode.None);
        if (presenters.Length > 0)
        {
            var presenter = presenters[0];
            var so = new SerializedObject(presenter);
            so.FindProperty("secondChancePanel").objectReferenceValue = panel;
            so.FindProperty("heartButton").objectReferenceValue = heartBtn.GetComponent<Button>();
            so.FindProperty("quitButton").objectReferenceValue = quitBtn.GetComponent<Button>();
            so.FindProperty("adButton").objectReferenceValue = adBtn.GetComponent<Button>();
            so.FindProperty("timerFill").objectReferenceValue = timerFillImg;

            var jumpController = Object.FindFirstObjectByType<JumpRing.Game.Gameplay.PlayerJumpController>();
            if (jumpController != null)
            {
                so.FindProperty("playerJumpController").objectReferenceValue = jumpController;
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(presenter);
        }

        // Ensure panel renders on top of everything
        panel.transform.SetAsLastSibling();
        panel.SetActive(false);

        Undo.RegisterCreatedObjectUndo(panel, "Create SecondChance Panel");
        Selection.activeGameObject = panel;

        Debug.Log("SecondChance Panel created and wired up.");
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

        // Remove old hearts container if exists
        var oldHearts = canvas.transform.Find("HeartsContainer");
        if (oldHearts != null)
        {
            Undo.DestroyObjectImmediate(oldHearts.gameObject);
        }

        // Container anchored to top-left
        var container = CreateUI("HeartsContainer", canvas.transform);
        var containerRect = container.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0f, 1f);
        containerRect.anchorMax = new Vector2(0f, 1f);
        containerRect.pivot = new Vector2(0f, 1f);
        containerRect.anchoredPosition = new Vector2(20f, -100f);
        containerRect.sizeDelta = new Vector2(150f, 40f);

        // Add HorizontalLayoutGroup
        var layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        // Add HeartsHudPresenter
        var heartsPresenter = container.AddComponent<JumpRing.Game.UI.HeartsHudPresenter>();

        // Create 3 heart images
        var heartImages = new Image[3];
        var inactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.4f);

        for (var i = 0; i < 3; i++)
        {
            var heart = CreateUI($"Heart_{i}", container.transform);
            var heartRect = heart.GetComponent<RectTransform>();
            heartRect.sizeDelta = new Vector2(36f, 36f);

            var img = heart.AddComponent<Image>();
            img.color = inactiveColor;

            // Use Unity built-in knob sprite as placeholder heart shape
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            heartImages[i] = img;
        }

        // Wire up HeartsHudPresenter
        var so = new SerializedObject(heartsPresenter);

        var bonusManager = Object.FindFirstObjectByType<JumpRing.Game.Gameplay.BonusEffectManager>();
        if (bonusManager != null)
        {
            so.FindProperty("bonusEffectManager").objectReferenceValue = bonusManager;
        }

        var heartsProp = so.FindProperty("heartImages");
        heartsProp.arraySize = 3;
        for (var i = 0; i < 3; i++)
        {
            heartsProp.GetArrayElementAtIndex(i).objectReferenceValue = heartImages[i];
        }

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

    private static GameObject CreateButton(string name, Transform parent,
        Vector2 anchorCenter, Vector2 size, Color color, string label)
    {
        var btnGo = CreateUI(name, parent);
        var rect = btnGo.GetComponent<RectTransform>();
        rect.anchorMin = anchorCenter;
        rect.anchorMax = anchorCenter;
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;

        var img = btnGo.AddComponent<Image>();
        img.color = color;

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = img;

        // Label
        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(btnGo.transform, false);
        var labelRect = labelGo.GetComponent<RectTransform>();
        StretchFull(labelRect);

        var text = labelGo.AddComponent<TMPro.TextMeshProUGUI>();
        text.text = label;
        text.alignment = TMPro.TextAlignmentOptions.Center;
        text.fontSize = 28;
        text.color = Color.white;

        return btnGo;
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
