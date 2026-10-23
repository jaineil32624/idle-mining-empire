using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IdleMiningEmpire.EditorTools
{
    /// <summary>
    /// Runs automatically the first time the project is opened in Unity:
    /// creates the Main scene, registers it in Build Settings and applies Player Settings
    /// for Windows, Android and WebGL (GitHub Pages / itch.io friendly compression).
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        public const string ScenePath = "Assets/Scenes/Main.unity";
        const string MarkerPath = "ProjectSettings/IdleMiningEmpireSetup.txt";

        static ProjectSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (!File.Exists(ScenePath) || !File.Exists(MarkerPath)) RunSetup(false);
            };
        }

        [MenuItem("Idle Mining Empire/Re-run Project Setup", priority = 100)]
        public static void RunSetupMenu() => RunSetup(true);

        public static void RunSetup(bool verbose)
        {
            EnsureScene();
            ApplyPlayerSettings();
            File.WriteAllText(MarkerPath, "Idle Mining Empire project setup completed. Delete this file to run setup again.\n");
            AssetDatabase.SaveAssets();
            Debug.Log("[Idle Mining Empire] Project setup complete. Open Assets/Scenes/Main.unity and press Play!");
            if (verbose) EditorUtility.DisplayDialog("Idle Mining Empire", "Project setup complete.\n\nPress Play to start mining!", "OK");
        }

        public static void EnsureScene()
        {
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                var cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 6.6f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.43f, 0.76f, 1f);
                camGo.transform.position = new Vector3(0, 3.4f, -10);
                camGo.AddComponent<AudioListener>();
                // The game builds itself at runtime (GameManager auto-boots), so the scene only needs a camera.
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.Refresh();
            }
            else if (EditorSceneManager.GetActiveScene().path != ScenePath && string.IsNullOrEmpty(EditorSceneManager.GetActiveScene().path))
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == ScenePath))
            {
                scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }

        public static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "Jaineil Rana";
            PlayerSettings.productName = "Idle Mining Empire";
            if (string.IsNullOrEmpty(PlayerSettings.bundleVersion) || PlayerSettings.bundleVersion == "0.1" || PlayerSettings.bundleVersion == "1.0")
                PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.runInBackground = true;

            // Desktop
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;

            // Mobile: landscape only
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            // Android
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.jaineilrana.idleminingempire");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
            if (PlayerSettings.Android.bundleVersionCode < 1) PlayerSettings.Android.bundleVersionCode = 1;

            // WebGL: small and fast to load on GitHub Pages and itch.io
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true; // works on hosts that don't send Content-Encoding headers (GitHub Pages)
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
            PlayerSettings.stripEngineCode = true;
        }
    }
}
