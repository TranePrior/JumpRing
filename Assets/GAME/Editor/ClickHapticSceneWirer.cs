using JumpRing.Game.Core.Composition;
using JumpRing.Game.Core.Services;
using JumpRing.Game.Core.Services.Haptics;
using JumpRing.Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JumpRing.Game.Editor
{
    /// <summary>
    /// Puts <see cref="ClickHapticService"/> into the open scene next to the other settings
    /// services and fills the references pointing at it.
    /// </summary>
    public static class ClickHapticSceneWirer
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";

        /// <summary>Entry point for a headless run: batch mode starts with no scene open.</summary>
        public static void WireFromBatch()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Wire();
        }

        [MenuItem("Tools/JumpRing/Wire Click Haptics")]
        public static void Wire()
        {
            var storage = Object.FindFirstObjectByType<PlatformStorageService>(FindObjectsInactive.Include);
            if (storage == null)
            {
                Debug.LogError("[ClickHapticSceneWirer] PlatformStorageService not found in scene!");
                return;
            }

            var haptics = Object.FindFirstObjectByType<ClickHapticService>(FindObjectsInactive.Include);
            if (haptics == null)
            {
                var audioSettings = Object.FindFirstObjectByType<AudioSettingsService>(FindObjectsInactive.Include);
                if (audioSettings == null)
                {
                    Debug.LogError("[ClickHapticSceneWirer] AudioSettingsService not found in scene!");
                    return;
                }

                haptics = Undo.AddComponent<ClickHapticService>(audioSettings.gameObject);
            }

            var hapticsObject = new SerializedObject(haptics);
            hapticsObject.FindProperty("storageService").objectReferenceValue = storage;
            hapticsObject.ApplyModifiedPropertiesWithoutUndo();

            var root = Object.FindFirstObjectByType<GameCompositionRoot>(FindObjectsInactive.Include);
            if (root == null)
            {
                Debug.LogError("[ClickHapticSceneWirer] GameCompositionRoot not found in scene!");
                return;
            }

            var rootObject = new SerializedObject(root);
            rootObject.FindProperty("clickHapticService").objectReferenceValue = haptics;
            rootObject.ApplyModifiedPropertiesWithoutUndo();

            // The settings window is opened from more than one place — the menu icon bar and the
            // in-run pause button each carry their own opener.
            var openers = Object.FindObjectsByType<SettingsPopupOpener>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var opener in openers)
            {
                var openerObject = new SerializedObject(opener);
                openerObject.FindProperty("_clickHapticService").objectReferenceValue = haptics;
                openerObject.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(haptics.gameObject.scene);
            EditorSceneManager.SaveOpenScenes();

            Debug.Log($"[ClickHapticSceneWirer] Wired haptics on '{haptics.gameObject.name}' " +
                      $"and {openers.Length} settings opener(s).");
        }
    }
}
