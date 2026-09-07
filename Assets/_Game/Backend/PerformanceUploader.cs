using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;
using UnityEngine;

namespace Game.Backend
{
    public sealed class PerformanceUploader : IPerformanceSink, IDisposable
    {
        private readonly IHttpTransport transport;
        private readonly string endpoint;
        private readonly HttpHeader[] headers;
        private readonly CancellationTokenSource lifetime = new();
        private bool sending;

        public PerformanceUploader(IHttpTransport transport, string endpoint, string token)
        {
            this.transport = transport;
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ||
                (uri.Scheme != "https" && !(uri.Scheme == "http" && uri.IsLoopback)))
                throw new ArgumentException("Metrics require HTTPS or a loopback URL.", nameof(endpoint));
            if (string.IsNullOrEmpty(token) || token.Length < 24)
                throw new ArgumentException("Metrics token is required.", nameof(token));
            this.endpoint = endpoint;
            headers = new[] { new HttpHeader("Authorization", "Bearer " + token) };
        }

        public void Submit(PerformanceBatch batch)
        {
            // Diagnostic windows may be dropped. Never grow a queue or block gameplay.
            if (sending || lifetime.IsCancellationRequested) return;
            SendAsync(batch).Forget();
        }

        private async UniTask SendAsync(PerformanceBatch batch)
        {
            sending = true;
            try
            {
                string json = JsonUtility.ToJson(batch);
                await transport.SendAsync(new HttpCall(HttpMethod.Post, endpoint, json, headers, 2), lifetime.Token);
            }
            catch (OperationCanceledException) { }
            catch (Exception) { /* Telemetry failures must not interrupt the match. */ }
            finally { sending = false; }
        }

        public void Dispose()
        {
            lifetime.Cancel();
            lifetime.Dispose();
        }
    }
}
