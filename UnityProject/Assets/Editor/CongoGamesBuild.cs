#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CongoGames.EditorTools
{
    public static class CongoGamesBuild
    {
        private const string MainScene = "Assets/Scenes/Main.unity";
        private const string OutputPath = "../Builds/Windows/CongoGames.exe";

        [MenuItem("CongoGames/Build/Windows 64-bit")]
        public static void BuildWindows64Menu()
        {
            BuildWindows64();
        }

        public static void BuildWindows64()
        {
            string outputOverride = Environment.GetEnvironmentVariable("CONGOGAMES_BUILD_OUTPUT");
            string output = string.IsNullOrWhiteSpace(outputOverride)
                ? Path.GetFullPath(Path.Combine(Application.dataPath, OutputPath))
                : Path.GetFullPath(outputOverride);
            string dir = Path.GetDirectoryName(output);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (!File.Exists(Path.Combine(Application.dataPath, "Scenes/Main.unity")))
            {
                throw new FileNotFoundException("Scene introuvable", MainScene);
            }

            var options = new BuildPlayerOptions
            {
                scenes = new[] { MainScene },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Build Windows echoue: " + summary.result + " (" + summary.totalErrors + " erreur(s), "
                    + summary.totalWarnings + " avertissement(s)).");
            }

            Debug.Log("[CongoGames] Build Windows OK: " + output + " (" + summary.totalSize + " bytes)");
        }
    }
}
#endif
