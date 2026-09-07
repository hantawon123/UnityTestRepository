using System;

namespace Game.Core
{
    /// <summary>One bounded telemetry window. Bucket counts are non-cumulative.</summary>
    [Serializable]
    public sealed class PerformanceBatch
    {
        public static readonly double[] Bounds = { 1, 2, 4, 8, 12, 16.667, 20, 25, 33.333, 50, 100, 250, 1000 };
        public string session, build, role, phase;
        public int seq, players;
        public double sent_at;
        public bool fusion_available;
        public int[] frame_buckets = new int[14], fusion_buckets = new int[14], rtt_buckets = new int[14];
        public double frame_sum, fusion_sum, rtt_sum;
        public double rx_bytes, tx_bytes, resim_ticks, forward_ticks, gc_collections, managed_bytes;

        public static void Observe(int[] buckets, ref double sum, double milliseconds)
        {
            if (double.IsNaN(milliseconds) || double.IsInfinity(milliseconds) || milliseconds < 0) return;
            milliseconds = Math.Min(milliseconds, 10000);
            int index = 0;
            while (index < Bounds.Length && milliseconds > Bounds[index]) index++;
            buckets[index]++;
            sum += milliseconds;
        }
    }

    // Network owns sampling; Backend owns HTTP. The port preserves assembly boundaries.
    public interface IPerformanceSink
    {
        void Submit(PerformanceBatch batch);
    }
}
