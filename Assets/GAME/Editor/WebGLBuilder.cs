using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace JumpRing.Game.Editor
{
    /// <summary>
    /// Builds the WebGL player with the project's own settings.
    /// </summary>
    /// <remarks>
    /// Exists for headless runs: <c>Unity -batchmode -quit -executeMethod
    /// JumpRing.Game.Editor.WebGLBuilder.BuildFromCommandLine -buildOutput Builds/Name</c>.
    /// </remarks>
    public static class WebGLBuilder
    {
        private const string OutputArgument = "-buildOutput";
        private const string DefaultOutput = "Builds/WebGL";

        [MenuItem("Tools/JumpRing/Build WebGL")]
        public static void BuildFromMenu()
        {
            Build(DefaultOutput);
        }

        public static void BuildFromCommandLine()
        {
            Build(ReadOutputPath());
        }

        private static void Build(string outputPath)
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[WebGLBuilder] Build {summary.result}: {summary.totalErrors} error(s).");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"[WebGLBuilder] Built to '{outputPath}' in {summary.totalTime}, " +
                      $"{summary.totalSize / (1024 * 1024)} MB.");
        }

        private static string ReadOutputPath()
        {
            string[] args = Environment.GetCommandLineArgs();

            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == OutputArgument)
                {
                    return args[i + 1];
                }
            }

            return DefaultOutput;
        }
    }
}
