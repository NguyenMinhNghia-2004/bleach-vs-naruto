using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BVN.EditorTools
{
    public static class BuildTools
    {
        const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("BVN/Ensure Main Scene")]
        public static void EnsureScene()
        {
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory("Assets/Scenes");
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        [MenuItem("BVN/Build Windows")]
        public static void BuildWindows()
        {
            EnsureScene();
            PlayerSettings.productName = "Bleach vs Naruto";
            PlayerSettings.companyName = "5DPLAY-Port";
            PlayerSettings.defaultScreenWidth = 1200;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            var outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Build/Windows"));
            Directory.CreateDirectory(outDir);
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = Path.Combine(outDir, "BleachVsNaruto.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log("[build] result=" + report.summary.result + " size=" + report.summary.totalSize + " errors=" + report.summary.totalErrors);
            if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
        }

        [MenuItem("BVN/Build WebGL")]
        public static void BuildWebGL()
        {
            EnsureScene();
            PlayerSettings.productName = "Bleach vs Naruto";
            PlayerSettings.companyName = "5DPLAY-Port";
            PlayerSettings.defaultWebScreenWidth = 800;
            PlayerSettings.defaultWebScreenHeight = 600;
            PlayerSettings.runInBackground = true;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;

            var outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Build/WebGL"));
            Directory.CreateDirectory(outDir);
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log("[build webgl] result=" + report.summary.result + " size=" + report.summary.totalSize + " errors=" + report.summary.totalErrors);
            if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
        }
    }
}
