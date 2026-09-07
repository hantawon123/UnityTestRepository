using System;
using System.Diagnostics;
using Fusion;
using Fusion.Statistics;
using Game.Core;
using Game.Network.Match;
using UnityEngine;

namespace Game.Network
{
    /// <summary>Opt-in runtime counters. No networked state or game authority changes.</summary>
    public sealed class PerformanceMonitor : SimulationBehaviour, IBeforeUpdate, IAfterUpdate
    {
        private static readonly string[] Phases = { "Waiting", "Hiding", "Searching", "Highlight", "Result" };
        private IPerformanceSink sink;
        private string build;
        private readonly string session = Guid.NewGuid().ToString("N");
        private PerformanceBatch batch;
        private MatchSessionState match;
        private FusionStatisticsManager statistics;
        private long before;
        private double nextFlush, nextFind;
        private int sequence, previousGc;

        public void Initialize(IPerformanceSink target, string version)
        {
            sink = target;
            build = version;
            previousGc = GC.CollectionCount(0);
        }

        public void BeforeUpdate() => before = Stopwatch.GetTimestamp();

        public void AfterUpdate()
        {
            if (sink == null || Runner == null || !Runner.IsRunning) return;
            double fusionMs = (Stopwatch.GetTimestamp() - before) * 1000.0 / Stopwatch.Frequency;
            double now = Time.realtimeSinceStartupAsDouble;
            if (match == null && now >= nextFind)
            {
                match = FindFirstObjectByType<MatchSessionState>();
                nextFind = now + 1;
            }
            int phaseIndex = match != null && match.Object != null && match.Object.IsValid ? (int)match.Phase : -1;
            string phase = phaseIndex >= 0 && phaseIndex < Phases.Length ? Phases[phaseIndex] : "Unknown";
            int players = 0;
            foreach (var unused in Runner.ActivePlayers) players++;
            if (players < 1 || players > 6) return;
            string role = Runner.GameMode == GameMode.Server ? "server" : Runner.IsServer ? "host" : "client";
            if (batch == null || batch.phase != phase || batch.players != players || batch.role != role)
            {
                Flush();
                batch = new PerformanceBatch { session = session, build = build, role = role, phase = phase, players = players };
                nextFlush = now + 5;
            }
            PerformanceBatch.Observe(batch.frame_buckets, ref batch.frame_sum, Time.unscaledDeltaTime * 1000);
            PerformanceBatch.Observe(batch.fusion_buckets, ref batch.fusion_sum, fusionMs);
            int gc = GC.CollectionCount(0);
            batch.gc_collections += Math.Max(0, gc - previousGc);
            previousGc = gc;
            if (statistics == null) Runner.TryGetFusionStatistics(out statistics);
            batch.fusion_available = statistics != null;
            if (statistics != null)
            {
                var values = statistics.SimulationSnapshot.Stats;
                if (values.TryGetValue(FusionStatType.RoundTripTime, out float rtt) && rtt > 0)
                    PerformanceBatch.Observe(batch.rtt_buckets, ref batch.rtt_sum, rtt * 1000);
                if (values.TryGetValue(FusionStatType.InBandwidth, out float rx)) batch.rx_bytes += rx;
                if (values.TryGetValue(FusionStatType.OutBandwidth, out float tx)) batch.tx_bytes += tx;
                if (values.TryGetValue(FusionStatType.Resimulations, out float resim)) batch.resim_ticks += resim;
                if (values.TryGetValue(FusionStatType.ForwardTicks, out float forward)) batch.forward_ticks += forward;
            }
            if (now >= nextFlush) Flush();
        }

        private void Flush()
        {
            if (batch == null) return;
            batch.seq = sequence++;
            batch.sent_at = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
            batch.managed_bytes = GC.GetTotalMemory(false);
            sink.Submit(batch);
            batch = null;
        }

        private void OnDestroy()
        {
            if (sink != null) Flush();
        }
    }
}
