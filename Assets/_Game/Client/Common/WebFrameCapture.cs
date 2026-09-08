using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

namespace Game.Client.Common
{
    /// <summary>Opt-in, local-only frame timing capture for WebGL playtests (?perf=1).</summary>
    public sealed class WebFrameCapture : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void GamePerfInstall(string target);
        [DllImport("__Internal")] private static extern void GamePerfReport(string json);

        [Serializable] private sealed class Report
        {
            public string revision, scene, quality, graphicsDevice;
            public int width, height, frames, slow50ms, gcCollections;
            public double seconds, averageFps, p50ms, p95ms, p99ms, maxMs;
            public long managedHeapBytes;
            public bool interrupted;
        }

        private float[] samples;
        private int count, slow, initialGc;
        private double startedAt, previousFrame;
        private string scene;
        private bool interrupted;

        private void Start() => GamePerfInstall(gameObject.name);

        [Preserve]
        public void BeginCapture()
        {
            samples ??= new float[32768];
            count = slow = 0;
            initialGc = GC.CollectionCount(0);
            startedAt = previousFrame = Time.realtimeSinceStartupAsDouble;
            scene = SceneManager.GetActiveScene().name;
            interrupted = !Application.isFocused;
        }

        private void Update()
        {
            if (startedAt <= 0d) return;
            var now = Time.realtimeSinceStartupAsDouble;
            var milliseconds = (float)((now - previousFrame) * 1000d);
            previousFrame = now;
            samples[count++] = milliseconds;
            if (milliseconds > 50f) slow++;
            interrupted |= !Application.isFocused || SceneManager.GetActiveScene().name != scene;
            if (now - startedAt < 30d && count < samples.Length) return;

            Array.Sort(samples, 0, count);
            var seconds = now - startedAt;
            startedAt = 0d;
            GamePerfReport(JsonUtility.ToJson(new Report
            {
                revision = Application.version, scene = scene,
                quality = QualitySettings.names[QualitySettings.GetQualityLevel()],
                graphicsDevice = SystemInfo.graphicsDeviceName,
                width = Screen.width, height = Screen.height, frames = count,
                seconds = seconds, averageFps = count / seconds,
                p50ms = Percentile(.50), p95ms = Percentile(.95), p99ms = Percentile(.99),
                maxMs = samples[count - 1], slow50ms = slow,
                gcCollections = GC.CollectionCount(0) - initialGc,
                managedHeapBytes = GC.GetTotalMemory(false), interrupted = interrupted
            }, true));
        }

        private double Percentile(double fraction) => samples[Math.Max(0, (int)Math.Ceiling(count * fraction) - 1)];

        private void OnApplicationFocus(bool focused)
        {
            if (startedAt > 0d && !focused) interrupted = true;
        }
#endif
    }
}
