using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class PlatformBuildTools
{
    [MenuItem("Build/PC/Windows 64-bit")]
    public static void BuildWindows64()
    {
        string buildPath = EditorUtility.SaveFolderPanel("Выберите папку для сборки Windows", "", "");
        if (string.IsNullOrEmpty(buildPath))
        {
            return;
        }

        ConfigureCommonPlayerSettings();
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, "PLATFORM_PC;ENABLE_PROFILER");
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11, GraphicsDeviceType.Vulkan });

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = GetEnabledScenePaths(),
            locationPathName = Path.Combine(buildPath, "GolfWHill_PC.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.ShowBuiltPlayer
        };

        BuildPipeline.BuildPlayer(options);
    }

    [MenuItem("Build/Android/APK")]
    public static void BuildAndroid()
    {
        string buildPath = EditorUtility.SaveFilePanel("Выберите место для APK", "", "GolfWHill_Android", "apk");
        if (string.IsNullOrEmpty(buildPath))
        {
            return;
        }

        ConfigureCommonPlayerSettings();
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, "PLATFORM_MOBILE;ANDROID_VERSION");
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = GetEnabledScenePaths(),
            locationPathName = buildPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildPipeline.BuildPlayer(options);
    }

    [MenuItem("Build/WebGL/Build")]
    public static void BuildWebGL()
    {
        string buildPath = EditorUtility.SaveFolderPanel("Выберите папку для сборки WebGL", "", "");
        if (string.IsNullOrEmpty(buildPath))
        {
            return;
        }

        ConfigureCommonPlayerSettings();
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.WebGL, "PLATFORM_WEBGL;WEB_BUILD");
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = GetEnabledScenePaths(),
            locationPathName = buildPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        BuildPipeline.BuildPlayer(options);
    }

    [MenuItem("Build/Tools/Оптимизация для PC")]
    public static void OptimizeForPC()
    {
        QualitySettings.SetQualityLevel(2, true);
        PlayerSettings.resizableWindow = true;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        Debug.Log("Применены настройки для PC.");
    }

    [MenuItem("Build/Tools/Оптимизация для Android")]
    public static void OptimizeForAndroid()
    {
        QualitySettings.SetQualityLevel(1, true);
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.MTRendering = true;
        Debug.Log("Применены настройки для Android.");
    }

    [MenuItem("Build/Tools/Оптимизация для WebGL")]
    public static void OptimizeForWebGL()
    {
        QualitySettings.SetQualityLevel(1, true);
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        Debug.Log("Применены настройки для WebGL.");
    }

    private static void ConfigureCommonPlayerSettings()
    {
        PlayerSettings.productName = "GolfWHill";
        PlayerSettings.companyName = "RTU MIREA";
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.runInBackground = true;
    }

    private static string[] GetEnabledScenePaths()
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        int enabledCount = 0;

        for (int i = 0; i < scenes.Length; i++)
        {
            if (scenes[i].enabled)
            {
                enabledCount++;
            }
        }

        string[] scenePaths = new string[enabledCount];
        int index = 0;
        for (int i = 0; i < scenes.Length; i++)
        {
            if (!scenes[i].enabled)
            {
                continue;
            }

            scenePaths[index++] = scenes[i].path;
        }

        return scenePaths;
    }
}
