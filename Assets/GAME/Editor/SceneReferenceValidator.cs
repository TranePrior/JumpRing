using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JumpRing.Editor
{
    /// <summary>
    /// Reports serialized object references that were left empty on the project's own components.
    /// Runs on entering play mode and from Tools/JumpRing/Validate Scene References.
    /// </summary>
    /// <remarks>
    /// An unassigned reference used to mean a silently disabled feature: the game booted, the
    /// console stayed clean, and nobody noticed that vibration never fired, that Yandex was never
    /// told gameplay had started, or that the dim overlay never animated. Those three sat broken
    /// for a long time and only surfaced during a frame-by-frame review of a screen recording.
    /// This turns that class of bug back into something the editor tells you about up front.
    /// </remarks>
    public static class SceneReferenceValidator
    {
        private const string Menu = "Tools/JumpRing/Validate Scene References";
        private const string ProjectNamespace = "JumpRing";

        /// <summary>
        /// Fields that are legitimately empty and would otherwise be permanent noise.
        /// Keyed by "ComponentTypeName.fieldName".
        /// </summary>
        private static readonly HashSet<string> Ignored = new()
        {
            // uGUI ScrollRect exposes scrollbar slots that this project deliberately does not use.
            "TunedScrollRect.m_HorizontalScrollbar",
            "TunedScrollRect.m_VerticalScrollbar",
        };

        [InitializeOnLoadMethod]
        private static void Hook()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode)
            {
                Validate(logWhenClean: false);
            }
        }

        [MenuItem(Menu)]
        private static void ValidateFromMenu()
        {
            Validate(logWhenClean: true);
        }

        private static void Validate(bool logWhenClean)
        {
            var report = new StringBuilder();
            int holes = 0;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                {
                    continue;
                }

                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        holes += Inspect(behaviour, report);
                    }
                }
            }

            if (holes > 0)
            {
                Debug.LogError($"[SceneReferenceValidator] {holes} unassigned reference(s):\n{report}");
                return;
            }

            if (logWhenClean)
            {
                Debug.Log("[SceneReferenceValidator] All references assigned.");
            }
        }

        private static int Inspect(MonoBehaviour behaviour, StringBuilder report)
        {
            if (behaviour == null)
            {
                return 0;
            }

            var type = behaviour.GetType();
            if (type.Namespace == null || !type.Namespace.StartsWith(ProjectNamespace))
            {
                return 0;
            }

            int holes = 0;
            var serialized = new SerializedObject(behaviour);
            var property = serialized.GetIterator();
            bool descend = true;

            while (property.NextVisible(descend))
            {
                descend = false;

                if (property.propertyType != SerializedPropertyType.ObjectReference)
                {
                    continue;
                }

                if (property.objectReferenceValue != null || property.objectReferenceInstanceIDValue != 0)
                {
                    continue;
                }

                if (Ignored.Contains($"{type.Name}.{property.name}"))
                {
                    continue;
                }

                holes++;
                report.AppendLine($"  {type.Name}.{property.name} on {Path(behaviour.transform)}");
            }

            return holes;
        }

        private static string Path(Transform transform)
        {
            var path = transform.name;
            for (var parent = transform.parent; parent != null; parent = parent.parent)
            {
                path = parent.name + "/" + path;
            }

            return path;
        }
    }
}
