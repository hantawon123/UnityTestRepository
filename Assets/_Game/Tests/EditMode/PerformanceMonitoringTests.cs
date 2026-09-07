using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Backend;
using Game.Core;
using NUnit.Framework;

namespace Game.Architecture.Tests
{
    public sealed class PerformanceMonitoringTests
    {
        [Test]
        public void HistogramPreservesBoundaryAndRejectsNonFiniteSamples()
        {
            var batch = new PerformanceBatch();
            PerformanceBatch.Observe(batch.frame_buckets, ref batch.frame_sum, 16.667);
            PerformanceBatch.Observe(batch.frame_buckets, ref batch.frame_sum, 50.01);
            PerformanceBatch.Observe(batch.frame_buckets, ref batch.frame_sum, double.NaN);
            Assert.That(batch.frame_buckets[5], Is.EqualTo(1));
            Assert.That(batch.frame_buckets[10], Is.EqualTo(1));
            Assert.That(batch.frame_sum, Is.EqualTo(66.677).Within(0.0001));
        }

        [Test]
        public void UploaderDropsConcurrentWindowsAndCancelsOnDispose()
        {
            var transport = new PendingTransport();
            var uploader = new PerformanceUploader(transport, "http://127.0.0.1:9464/ingest", new string('a', 32));
            uploader.Submit(new PerformanceBatch());
            uploader.Submit(new PerformanceBatch());
            Assert.That(transport.Calls, Is.EqualTo(1));
            Assert.That(transport.LastCall.TimeoutSeconds, Is.EqualTo(2));
            Assert.That(transport.LastCall.Headers[0].Name, Is.EqualTo("Authorization"));
            uploader.Dispose();
            Assert.That(transport.Token.IsCancellationRequested, Is.True);
            transport.Completion.TrySetResult(HttpCallResult.Failed(HttpOutcome.Cancelled));
        }

        [Test]
        public void UploaderRejectsPlaintextRemoteEndpoint()
        {
            Assert.Throws<ArgumentException>(() => new PerformanceUploader(new PendingTransport(),
                "http://example.com/ingest", new string('a', 32)));
        }

        private sealed class PendingTransport : IHttpTransport
        {
            public readonly UniTaskCompletionSource<HttpCallResult> Completion = new();
            public int Calls;
            public HttpCall LastCall;
            public CancellationToken Token;
            public UniTask<HttpCallResult> SendAsync(HttpCall call, CancellationToken cancellation)
            {
                Calls++;
                LastCall = call;
                Token = cancellation;
                return Completion.Task;
            }
        }
    }
}
