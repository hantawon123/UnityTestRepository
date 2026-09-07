"""Local playtest collector. Bind only to loopback; publish through an authenticated proxy."""
import hmac
import json
import math
import os
import re
import time
from http.server import BaseHTTPRequestHandler, HTTPServer

from prometheus_client import CollectorRegistry, generate_latest
from prometheus_client.core import CounterMetricFamily, GaugeMetricFamily, HistogramMetricFamily

BOUNDS = [1, 2, 4, 8, 12, 16.667, 20, 25, 33.333, 50, 100, 250, 1000]
LABELS = ['build', 'role', 'players', 'phase']
COUNTERS = ['rx_bytes', 'tx_bytes', 'resim_ticks', 'forward_ticks', 'gc_collections']
PHASES = ['Waiting', 'Hiding', 'Searching', 'Highlight', 'Result', 'Unknown']


def number(value, maximum):
    if isinstance(value, bool) or not isinstance(value, (int, float)) or not math.isfinite(value) or not 0 <= value <= maximum:
        raise ValueError('invalid number')
    return value


def validate(data):
    if not isinstance(data, dict):
        raise ValueError('object required')
    if not re.fullmatch(r'[a-f0-9]{32}', data['session']):
        raise ValueError('session')
    if not re.fullmatch(r'[A-Za-z0-9_.-]{1,32}', data['build']):
        raise ValueError('build')
    if data['role'] not in ['host', 'client', 'server'] or data['phase'] not in PHASES:
        raise ValueError('role or phase')
    if type(data['players']) is not int or not 1 <= data['players'] <= 6:
        raise ValueError('players')
    if type(data['seq']) is not int or not 0 <= data['seq'] <= 2147483647:
        raise ValueError('sequence')
    number(data['sent_at'], 1e13)
    if abs(time.time() - data['sent_at']) > 30:
        raise ValueError('stale batch')
    for name in ['frame', 'fusion', 'rtt']:
        counts = data[name + '_buckets']
        if not isinstance(counts, list) or len(counts) != len(BOUNDS) + 1:
            raise ValueError('buckets')
        for v in counts:
            if type(v) is not int:
                raise ValueError('integer buckets required')
            number(v, 10000)
        if sum(counts) > 10000:
            raise ValueError('too many samples')
        total = number(data[name + '_sum'], 1e8)
        lower = sum(n * edge for n, edge in zip(counts[1:], BOUNDS))
        upper = sum(n * edge for n, edge in zip(counts[:-1], BOUNDS)) + counts[-1] * 10000
        if not lower <= total <= upper + 0.001:
            raise ValueError('sum inconsistent with buckets')
    for name in COUNTERS:
        number(data[name], 1e8)
    number(data['managed_bytes'], 1e12)
    if type(data['fusion_available']) is not bool:
        raise ValueError('availability')
    if not data['fusion_available'] and (sum(data['rtt_buckets']) or any(data[n] for n in COUNTERS[:4])):
        raise ValueError('unsupported statistics must be absent')
    return data


class Store:
    def __init__(self):
        self.sessions = {}
        self.groups = {}

    def ingest(self, data, now=None):
        now = time.monotonic() if now is None else now
        self.sessions = {k: v for k, v in self.sessions.items() if now - v[1] < 600}
        old = self.sessions.get(data['session'])
        if old and data['seq'] <= old[0]:
            return 202  # Retrying a batch must not count it twice.
        if old and now - old[1] < 0.5:
            return 429
        if not old and len(self.sessions) >= 256:
            return 503
        key = (data['build'], data['role'], str(data['players']), data['phase'])
        if key not in self.groups:
            if len(self.groups) >= 256:
                return 503
            self.groups[key] = {n: [0] * (len(BOUNDS) + 1) for n in ['frame', 'fusion', 'rtt']}
            self.groups[key].update({n + '_sum': 0 for n in ['frame', 'fusion', 'rtt']})
            self.groups[key].update({n: 0 for n in COUNTERS})
        group = self.groups[key]
        for n in ['frame', 'fusion', 'rtt']:
            group[n] = [a + b for a, b in zip(group[n], data[n + '_buckets'])]
            group[n + '_sum'] += data[n + '_sum']
        for n in COUNTERS:
            group[n] += data[n]
        self.sessions[data['session']] = (data['seq'], now, key, data['managed_bytes'], data['fusion_available'])
        return 202

    def collect(self):
        for n in ['frame', 'fusion', 'rtt']:
            metric = HistogramMetricFamily('game_' + n + '_milliseconds', n + ' duration distribution', labels=LABELS)
            for key, group in self.groups.items():
                if not sum(group[n]):
                    continue  # Unavailable RTT is not zero latency.
                count = 0
                buckets = []
                for edge, value in zip([*BOUNDS, '+Inf'], group[n]):
                    count += value
                    buckets.append((str(edge), count))
                metric.add_metric(key, buckets, group[n + '_sum'])
            yield metric
        for n in COUNTERS:
            metric = CounterMetricFamily('game_' + n, n, labels=LABELS)
            for key, group in self.groups.items():
                metric.add_metric(key, group[n])
            yield metric
        now = time.monotonic()
        live = {}
        for _, seen, key, memory, available in self.sessions.values():
            if now - seen < 20:
                values = live.setdefault(key, [0, 0, 0])
                values[0] += 1
                values[1] = max(values[1], memory)
                values[2] += int(available)
        for name, index in [('reporting_peers', 0), ('managed_bytes_max', 1), ('fusion_supported_peers', 2)]:
            metric = GaugeMetricFamily('game_' + name, name, labels=LABELS)
            for key in self.groups:
                metric.add_metric(key, live.get(key, [0, 0, 0])[index])
            yield metric


def serve():
    token = os.environ.get('GAME_METRICS_TOKEN', '')
    if len(token) < 24:
        raise SystemExit('Set GAME_METRICS_TOKEN to a random value of at least 24 characters')
    store = Store()
    registry = CollectorRegistry()
    registry.register(store)

    class Handler(BaseHTTPRequestHandler):
        def setup(self):
            super().setup()
            self.connection.settimeout(3)

        def respond(self, code, body=b'', content_type='application/json'):
            self.send_response(code)
            self.send_header('Content-Type', content_type)
            self.send_header('Content-Length', str(len(body)))
            self.end_headers()
            self.wfile.write(body)

        def do_GET(self):
            if self.path == '/metrics':
                self.respond(200, generate_latest(registry), 'text/plain; version=0.0.4; charset=utf-8')
            elif self.path == '/health':
                self.respond(200, b'{"status":"ok"}')
            else:
                self.respond(404)

        def do_POST(self):
            if self.path != '/ingest':
                return self.respond(404)
            if not hmac.compare_digest(self.headers.get('Authorization', ''), 'Bearer ' + token):
                return self.respond(401)
            if self.headers.get('Transfer-Encoding') or self.headers.get('Content-Encoding'):
                return self.respond(400)
            try:
                size = int(self.headers.get('Content-Length', '0'))
                if not 0 < size <= 16384:
                    return self.respond(413)
                data = validate(json.loads(self.rfile.read(size)))
                self.respond(store.ingest(data))
            except (ValueError, KeyError, TypeError, TimeoutError):
                self.respond(400)

        def log_message(self, *args):
            pass  # Never print credentials or per-batch request logs.

    HTTPServer(('127.0.0.1', 9464), Handler).serve_forever()


if __name__ == '__main__':
    serve()
