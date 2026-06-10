using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

public static class WebGLBuild
{
    private const string OutputPath = "Builds/WebGL";
    private const string OutputPathDev = "Builds/WebGL_Dev";

    [MenuItem("MSRPG/Build WebGL")]
    public static void Build()
    {
        ConfigurePlayerSettings();
        RunBuild(OutputPath, BuildOptions.None);
    }

    [MenuItem("MSRPG/Build WebGL (Development)")]
    public static void BuildDev()
    {
        ConfigurePlayerSettings();
        RunBuild(OutputPathDev, BuildOptions.Development | BuildOptions.AllowDebugging);
    }

    private static void ConfigurePlayerSettings()
    {
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.WebGL, ManagedStrippingLevel.Minimal);
    }

    private static void RunBuild(string outputPath, BuildOptions buildOptions)
    {
        var scenes = new[]
        {
            "Assets/_Game/Scenes/Login.unity",
            "Assets/_Game/Scenes/Mesoria.unity"
        };

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.WebGL,
            options = buildOptions
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == BuildResult.Succeeded)
            Debug.Log($"[WebGL Build] 성공 ({report.summary.totalSize / 1024 / 1024} MB) → {Path.GetFullPath(outputPath)}");
        else
            Debug.LogError($"[WebGL Build] 실패: {report.summary.totalErrors}개 오류");
    }
}
