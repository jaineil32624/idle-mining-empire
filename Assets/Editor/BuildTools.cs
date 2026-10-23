using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace IdleMiningEmpire.EditorTools
{
    /// <summary>
    /// One-click builds from the "Idle Mining Empire/Build" menu, plus static entry points for CI:
    ///   Unity -batchmode -quit -projectPath . -executeMethod IdleMiningEmpire.EditorTools.BuildTools.BuildWebGL
    /// WebGL goes to /docs so GitHub Pages can serve it straight from the main branch.
    /// </summary>
    public static class BuildTools
    {
        public const string WebGLPath = "docs";
        public const string WindowsPath = "Builds/Windows/IdleMiningEmpire.exe";
        public const string AndroidPath = "Builds/Android/IdleMiningEmpire.apk";

        [MenuItem("Idle Mining Empire/Build/WebGL (docs folder - GitHub Pages and itch.io)", priority = 1)]
        public static void BuildWebGL() => Build(BuildTarget.WebGL, WebGLPath);

        [MenuItem("Idle Mining Empire/Build/Windows", priority = 2)]
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, WindowsPath);

        [MenuItem("Idle Mining Empire/Build/Android APK", priority = 3)]
        public static void BuildAndroid()
        {
            EditorUserBuildSettings.buildAppBundle = false;
            Build(BuildTarget.Android, AndroidPath);
        }

        [MenuItem("Idle Mining Empire/Build/Android App Bundle (Google Play)", priority = 4)]
        public static void BuildAndroidBundle()
        {
            EditorUserBuildSettings.buildAppBundle = true;
            Build(BuildTarget.Android, "Builds/Android/IdleMiningEmpire.aab");
            EditorUserBuildSettings.buildAppBundle = false;
        }

        [MenuItem("Idle Mining Empire/Build/Open Builds Folder", priority = 20)]
        public static void OpenBuilds()
        {
            Directory.CreateDirectory("Builds");
            EditorUtility.RevealInFinder("Builds");
        }

        static void Build(BuildTarget target, string path)
        {
            ProjectSetup.EnsureScene();
            ProjectSetup.ApplyPlayerSettings();

            if (!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(target), target))
            {
                string msg = "The " + target + " build module isn't installed.\nOpen Unity Hub > Installs > (your Unity version) > Add Modules, and add it.";
                Fail(msg);
                return;
            }

            string full = Path.GetFullPath(path);
            if (target == BuildTarget.WebGL && Directory.Exists(full))
            {
                // Clean the old WebGL build but keep the folder.
                foreach (var f in Directory.GetFiles(full)) File.Delete(f);
                foreach (var d in Directory.GetDirectories(full)) Directory.Delete(d, true);
            }
            Directory.CreateDirectory(target == BuildTarget.WebGL ? full : Path.GetDirectoryName(full));

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.ScenePath },
                locationPathName = path,
                target = target,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                if (target == BuildTarget.WebGL)
                {
                    File.WriteAllText(Path.Combine(full, ".nojekyll"), ""); // GitHub Pages: serve files as-is
                }
                string ok = target + " build succeeded (" + (summary.totalSize / (1024f * 1024f)).ToString("0.0") + " MB) -> " + full;
                Debug.Log("[Idle Mining Empire] " + ok);
                if (!Application.isBatchMode)
                {
                    if (target == BuildTarget.WebGL)
                        EditorUtility.DisplayDialog("WebGL build ready",
                            ok + "\n\nCommit and push the docs folder, then in GitHub: Settings > Pages > Deploy from branch > main /docs.\n" +
                            "For itch.io: zip the CONTENTS of the docs folder and upload as an HTML game.", "OK");
                    else EditorUtility.RevealInFinder(full);
                }
            }
            else Fail(target + " build failed: " + summary.result + " (" + summary.totalErrors + " errors). See the Console.");
        }

        static void Fail(string msg)
        {
            Debug.LogError("[Idle Mining Empire] " + msg);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else EditorUtility.DisplayDialog("Build problem", msg, "OK");
        }
    }
}
