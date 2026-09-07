"""Generate local Prometheus/Grafana configuration, without installing machine services."""
import argparse
import json
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('runtime', type=Path)
args = parser.parse_args()
root = args.runtime.resolve()
root.mkdir(parents=True, exist_ok=True)
for directory in ['data/grafana', 'data/prometheus', 'logs', 'provisioning/datasources', 'provisioning/dashboards', 'dashboards']:
    (root / directory).mkdir(parents=True, exist_ok=True)
(root / 'prometheus.yml').write_text('''global:
  scrape_interval: 5s
scrape_configs:
  - job_name: game
    static_configs:
      - targets: ['127.0.0.1:9464']
''')
(root / 'grafana.ini').write_text(f'''[server]
http_addr = 127.0.0.1
http_port = 3300
[paths]
data = {root.as_posix()}/data/grafana
logs = {root.as_posix()}/logs
provisioning = {root.as_posix()}/provisioning
[analytics]
reporting_enabled = false
check_for_updates = false
[auth.anonymous]
enabled = false
[users]
allow_sign_up = false
''', encoding='utf-8')
(root / 'provisioning/datasources/prometheus.yml').write_text('''apiVersion: 1
datasources:
  - name: Game Prometheus
    uid: game-prometheus
    type: prometheus
    access: proxy
    url: http://127.0.0.1:9090
    isDefault: true
    editable: false
''')
(root / 'provisioning/dashboards/game.yml').write_text(f'''apiVersion: 1
providers:
  - name: game
    folder: In-game performance
    type: file
    disableDeletion: false
    editable: false
    options:
      path: {root.as_posix()}/dashboards
''', encoding='utf-8')
selector = '{build=~"$build",phase=~"$phase",players=~"$players"}'
group = 'role,players,build'
panels = []


def panel(title, expr, unit='short', kind='timeseries', legend='{{role}} / {{players}}p / {{build}}'):
    i = len(panels)
    panels.append({'id': i + 1, 'title': title, 'type': kind,
                   'gridPos': {'x': (i % 2) * 12, 'y': (i // 2) * 8, 'w': 12, 'h': 8},
                   'datasource': {'type': 'prometheus', 'uid': 'game-prometheus'},
                   'targets': [{'refId': 'A', 'expr': expr, 'legendFormat': legend}],
                   'fieldConfig': {'defaults': {'unit': unit}, 'overrides': []}})


panel('수집 API 상태', 'up{job="game"}', kind='stat', legend='collector')
panel('최근 20초 보고 참가자', f'sum by ({group}) (game_reporting_peers{selector})', kind='stat')
for metric, title in [('frame', '프레임'), ('fusion', 'Fusion update'), ('rtt', 'RTT (보고된 표본)')]:
    for quantile in [.95, .99]:
        panel(f'{title} p{int(quantile*100)}', f'histogram_quantile({quantile}, sum by (le,{group}) (rate(game_{metric}_milliseconds_bucket{selector}[1m])))', 'ms')
for edge in ['33.333', '50']:
    filt = selector[:-1] + f',le="{edge}"' + '}'
    panel(f'{edge}ms 초과 프레임 비율', f'1 - sum by ({group}) (rate(game_frame_milliseconds_bucket{filt}[1m])) / sum by ({group}) (rate(game_frame_milliseconds_count{selector}[1m]))', 'percentunit')
for n, title, unit in [('tx_bytes', '송신량', 'Bps'), ('rx_bytes', '수신량', 'Bps'), ('resim_ticks', '재시뮬레이션 tick/s', 'short'), ('gc_collections', 'GC0 수집/s', 'short')]:
    panel(title, f'sum by ({group}) (rate(game_{n}_total{selector}[1m]))', unit)
panel('관리 힙 최대 (최근 보고 참가자)', f'max by ({group}) (game_managed_bytes_max{selector})', 'bytes')
panel('Fusion 상세 통계 지원 참가자', f'sum by ({group}) (game_fusion_supported_peers{selector})', kind='stat')
variables = []
for name in ['build', 'phase', 'players']:
    variables.append({'name': name, 'type': 'query', 'datasource': {'type': 'prometheus', 'uid': 'game-prometheus'},
                      'query': f'label_values(game_reporting_peers, {name})', 'includeAll': True, 'allValue': '.*',
                      'multi': True, 'refresh': 1, 'current': {'text': 'All', 'value': '$__all'}})
dashboard = {'uid': 'game-performance', 'title': '인게임 성능 관제', 'schemaVersion': 39, 'version': 1,
             'refresh': '5s', 'timezone': 'browser', 'time': {'from': 'now-15m', 'to': 'now'},
             'tags': ['game', 'performance'], 'templating': {'list': variables}, 'panels': panels,
             'description': '로컬 Host/Client 관측. 히스토그램 분위수는 근삿값. Fusion update는 단일 tick CPU 비용이 아님. 지원 여부와 수집 상태를 함께 확인.'}
(root / 'dashboards/game.json').write_text(json.dumps(dashboard, ensure_ascii=False, indent=2), encoding='utf-8')
print('Configured:', root)
