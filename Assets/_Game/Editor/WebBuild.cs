using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Game.Editor
{
    public static class WebBuild
    {
        // Run with -batchmode -quit -buildTarget WebGL -executeMethod Game.Editor.WebBuild.Build.
        public static void Build()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
                throw new BuildFailedException("Install WebGL Build Support for this Unity version.");

            var output = Environment.GetEnvironmentVariable("WEBGL_OUTPUT") ?? "Builds/WebGL";
            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled)
                .Select(scene => scene.path).ToArray();
            if (scenes.Length == 0) throw new BuildFailedException("No enabled build scenes.");
            if (!File.Exists("Assets/Photon/Fusion/Resources/PhotonAppSettings.asset"))
                throw new BuildFailedException("Restore PhotonAppSettings.asset before building.");

            var compression = PlayerSettings.WebGL.compressionFormat;
            var fallback = PlayerSettings.WebGL.decompressionFallback;
            var hashes = PlayerSettings.WebGL.nameFilesAsHashes;
            var version = PlayerSettings.bundleVersion;
            try
            {
                PlayerSettings.bundleVersion = Environment.GetEnvironmentVariable("WEBGL_REVISION") ?? version;
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
                PlayerSettings.WebGL.decompressionFallback = false;
                PlayerSettings.WebGL.nameFilesAsHashes = true;
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = output,
                    target = BuildTarget.WebGL,
                    options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new BuildFailedException($"WebGL build failed: {report.summary.totalErrors} errors.");
                File.WriteAllText(Path.Combine(output, "version.txt"), PlayerSettings.bundleVersion);
            }
            finally
            {
                PlayerSettings.WebGL.compressionFormat = compression;
                PlayerSettings.WebGL.decompressionFallback = fallback;
                PlayerSettings.WebGL.nameFilesAsHashes = hashes;
                PlayerSettings.bundleVersion = version;
            }
        }
    }
}
