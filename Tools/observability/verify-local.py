"""Check the running local stack without writing synthetic performance samples."""
import base64
import json
import sys
from pathlib import Path
from urllib.error import HTTPError
from urllib.parse import urlencode
from urllib.request import Request, urlopen

runtime = Path(sys.argv[1])
credentials = json.loads((runtime/'credentials.json').read_text(encoding='utf-8-sig'))
auth = base64.b64encode(('admin:'+credentials['adminPassword']).encode()).decode()


def get(url, headers=None):
    with urlopen(Request(url, headers=headers or {}), timeout=5) as response:
        return json.load(response)


assert get('http://127.0.0.1:9464/health')['status'] == 'ok'
health = get('http://127.0.0.1:3300/api/health')
assert health['database'] == 'ok'
dashboard = get('http://127.0.0.1:3300/api/dashboards/uid/game-performance', {'Authorization':'Basic '+auth})['dashboard']
targets = get('http://127.0.0.1:9090/api/v1/targets')['data']['activeTargets']
assert targets and all(t['health']=='up' for t in targets)
for panel in dashboard['panels']:
    query = panel['targets'][0]['expr']
    for name in ['build','phase','players']:
        query = query.replace('$'+name, '.*')
    assert get('http://127.0.0.1:9090/api/v1/query?'+urlencode({'query':query}))['status']=='success', panel['title']
for headers, body, expected in [({}, b'{}', 401), ({'Authorization':'Bearer '+credentials['token']}, b'{}', 400), ({'Authorization':'Bearer '+credentials['token']}, b' '*17000, 413)]:
    try:
        urlopen(Request('http://127.0.0.1:9464/ingest', data=body, headers=headers), timeout=5)
        raise AssertionError('Invalid request accepted')
    except HTTPError as error:
        assert error.code == expected, error.code
report = {'grafanaVersion':health['version'], 'panels':len(dashboard['panels']), 'targetsUp':len(targets), 'invalidRequestsRejected':3}
(runtime/'verification.json').write_text(json.dumps(report, indent=2),encoding='utf-8')
print(json.dumps(report))
