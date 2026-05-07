using UnityEngine;
using UnityEditor;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;

public class BuildWindow : EditorWindow
{
    private string gameName = "PunchNMunch";
    private string version = "1.0.0";
    private bool buildWindows = true;
    private bool buildWeb = true;

    [MenuItem("Build/Open Build Window")]
    public static void ShowWindow()
    {
        GetWindow<BuildWindow>("Auto Builder");
    }

    void OnGUI()
    {
        GUILayout.Label("Build Settings", EditorStyles.boldLabel);

        gameName = EditorGUILayout.TextField("Game Name", gameName);
        version = EditorGUILayout.TextField("Version", version);

        EditorGUILayout.Space();
        GUILayout.Label("Platforms to Build", EditorStyles.boldLabel);
        buildWindows = EditorGUILayout.Toggle("Windows (x64)", buildWindows);
        buildWeb = EditorGUILayout.Toggle("WebGL", buildWeb);

        EditorGUILayout.Space();

        if (GUILayout.Button("Start Build, Zip, and Run", GUILayout.Height(40)))
        {
            ExecuteBuild();
        }
    }

    private void ExecuteBuild()
    {
        // Changed folder name from "Builds" to "Build" per request
        string buildRoot = "Build";

        string windowsFolderName = $"{gameName}_Windows_v{version}";
        string webFolderName = $"{gameName}_Web_v{version}";

        string windowsPath = Path.Combine(buildRoot, windowsFolderName);
        string webPath = Path.Combine(buildRoot, webFolderName);

        // 1. Clean/Prep Folder
        if (Directory.Exists(buildRoot)) Directory.Delete(buildRoot, true);
        Directory.CreateDirectory(buildRoot);

        // 2. Build Windows
        if (buildWindows)
        {
            Debug.Log($"Switching to Windows and Building v{version}...");
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);

            Directory.CreateDirectory(windowsPath);
            BuildPlayerOptions winOptions = new BuildPlayerOptions();
            winOptions.scenes = GetScenePaths();
            winOptions.locationPathName = Path.Combine(windowsPath, $"{gameName}.exe");
            winOptions.target = BuildTarget.StandaloneWindows64;

            // BuildOptions.AutoRunPlayer launches the game after build
            winOptions.options = BuildOptions.AutoRunPlayer;

            BuildPipeline.BuildPlayer(winOptions);

            ZipFile.CreateFromDirectory(windowsPath, Path.Combine(buildRoot, $"{windowsFolderName}.zip"));
        }

        // 3. Build WebGL
        if (buildWeb)
        {
            Debug.Log($"Switching to WebGL and Building v{version}...");
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

            // FIX: Force WebGL to use WebGL 2.0 (OpenGLES3) to prevent pink decals
            PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new UnityEngine.Rendering.GraphicsDeviceType[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });

            Directory.CreateDirectory(webPath);
            BuildPlayerOptions webOptions = new BuildPlayerOptions();
            webOptions.scenes = GetScenePaths();
            webOptions.locationPathName = webPath;
            webOptions.target = BuildTarget.WebGL;

            // This will open your default browser to host the game
            webOptions.options = BuildOptions.AutoRunPlayer;

            BuildPipeline.BuildPlayer(webOptions);

            ZipFile.CreateFromDirectory(webPath, Path.Combine(buildRoot, $"{webFolderName}.zip"));
        }

        Debug.Log("All processes finished.");
        EditorUtility.RevealInFinder(buildRoot);
    }

    private string[] GetScenePaths()
    {
        string[] scenes = new string[EditorBuildSettings.scenes.Length];
        for (int i = 0; i < scenes.Length; i++)
        {
            scenes[i] = EditorBuildSettings.scenes[i].path;
        }
        return scenes;
    }
}