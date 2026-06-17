using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

public static class WebGLBuild
{
    private static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);
    private static string OutputPath => Path.Combine(ProjectRoot, "Builds", "WebGL");
    private static string OutputPathDev => Path.Combine(ProjectRoot, "Builds", "WebGL_Dev");
    private static string DocsPath => Path.Combine(ProjectRoot, "docs");

    [MenuItem("MSRPG/Build WebGL")]
    public static void Build()
    {
        ConfigurePlayerSettings();
        var path = OutputPath;
        Directory.CreateDirectory(path);
        RunBuild(path, BuildOptions.None);
    }

    [MenuItem("MSRPG/Build WebGL (Development)")]
    public static void BuildDev()
    {
        ConfigurePlayerSettings();
        var path = OutputPathDev;
        Directory.CreateDirectory(path);
        RunBuild(path, BuildOptions.Development | BuildOptions.AllowDebugging);
    }

    [MenuItem("MSRPG/Build + Deploy WebGL")]
    public static void BuildAndDeploy()
    {
        ConfigurePlayerSettings();
        var path = OutputPath;
        Directory.CreateDirectory(path);
        if (RunBuild(path, BuildOptions.None))
            Deploy();
    }

    [MenuItem("MSRPG/Deploy WebGL")]
    public static void Deploy()
    {
        var src = OutputPath;
        if (!Directory.Exists(src))
        {
            UnityEngine.Debug.LogError("[WebGL Deploy] 빌드 폴더가 없습니다. 먼저 Build WebGL을 실행하세요.");
            return;
        }

        if (Directory.Exists(DocsPath))
            Directory.Delete(DocsPath, true);
        CopyDirectory(src, DocsPath);
        UnityEngine.Debug.Log($"[WebGL Deploy] docs/ 복사 완료");

        var timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        RunGit("add docs/");
        RunGit($"commit -m \"Deploy WebGL build ({timestamp})\"");
        RunGit("push origin main");

        UnityEngine.Debug.Log("[WebGL Deploy] GitHub Pages 배포 완료! 1~2분 후 반영됩니다.");
    }

    private static void ConfigurePlayerSettings()
    {
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.WebGL, ManagedStrippingLevel.Minimal);
    }

    private static bool RunBuild(string outputPath, BuildOptions buildOptions)
    {
        var urpAssets = CollectAllURPAssets();
        var hdrBackup = new Dictionary<UniversalRenderPipelineAsset, bool>();
        foreach (var asset in urpAssets)
        {
            hdrBackup[asset] = asset.supportsHDR;
            asset.supportsHDR = false;
        }

        try
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
            {
                UnityEngine.Debug.Log($"[WebGL Build] 성공 ({report.summary.totalSize / 1024 / 1024} MB) → {Path.GetFullPath(outputPath)}");
                return true;
            }
            else
            {
                UnityEngine.Debug.LogError($"[WebGL Build] 실패: {report.summary.totalErrors}개 오류");
                return false;
            }
        }
        finally
        {
            foreach (var kv in hdrBackup)
                kv.Key.supportsHDR = kv.Value;
        }
    }

    private static void RunGit(string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = args,
            WorkingDirectory = ProjectRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        using var proc = Process.Start(psi);
        proc.WaitForExit();
        var output = proc.StandardOutput.ReadToEnd().Trim();
        var error = proc.StandardError.ReadToEnd().Trim();
        if (proc.ExitCode != 0)
            UnityEngine.Debug.LogError($"[Git] {args} 실패:\n{error}");
        else
            UnityEngine.Debug.Log($"[Git] {args}\n{(output.Length > 0 ? output : error)}");
    }

    private static void CopyDirectory(string src, string dst)
    {
        Directory.CreateDirectory(dst);
        foreach (var file in Directory.GetFiles(src))
            File.Copy(file, Path.Combine(dst, Path.GetFileName(file)), true);
        foreach (var dir in Directory.GetDirectories(src))
            CopyDirectory(dir, Path.Combine(dst, Path.GetFileName(dir)));
    }

    private static List<UniversalRenderPipelineAsset> CollectAllURPAssets()
    {
        var result = new List<UniversalRenderPipelineAsset>();
        int count = QualitySettings.count;
        for (int i = 0; i < count; i++)
        {
            if (QualitySettings.GetRenderPipelineAssetAt(i) is UniversalRenderPipelineAsset urp)
                if (!result.Contains(urp)) result.Add(urp);
        }
        if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset def && !result.Contains(def))
            result.Add(def);
        return result;
    }
}
