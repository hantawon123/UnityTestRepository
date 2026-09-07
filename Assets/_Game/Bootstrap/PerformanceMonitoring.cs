using System;
using System.Text.RegularExpressions;
using Game.Backend;
using Game.Network;
using Game.Network.Session;
using UnityEngine;
using VContainer.Unity;

namespace Game.Bootstrap
{
    public sealed class PerformanceMonitoring : ITickable, IDisposable
    {
        private readonly NetworkRunnerService network;
        private readonly PerformanceUploader uploader;
        private readonly string build;
        private PerformanceMonitor monitor;
        private double nextCheck;

        public PerformanceMonitoring(NetworkRunnerService network, PerformanceUploader uploader, string build)
        {
            this.network = network;
            this.uploader = uploader;
            this.build = Regex.IsMatch(build ?? "", "^[A-Za-z0-9_.-]{1,32}$") ? build : "local";
        }

        public void Tick()
        {
            if (monitor != null || Time.realtimeSinceStartupAsDouble < nextCheck) return;
            nextCheck = Time.realtimeSinceStartupAsDouble + 1;
            foreach (var avatar in network.PlayerAvatars)
            {
                var runner = avatar.Runner;
                if (runner == null || !runner.IsRunning) continue;
                monitor = runner.gameObject.AddComponent<PerformanceMonitor>();
                monitor.Initialize(uploader, build);
                runner.AddGlobal(monitor);
                break;
            }
        }

        public void Dispose()
        {
            if (monitor != null)
            {
                if (monitor.Runner != null) monitor.Runner.RemoveGlobal(monitor);
                UnityEngine.Object.Destroy(monitor);
            }
            uploader.Dispose();
        }
    }
}
