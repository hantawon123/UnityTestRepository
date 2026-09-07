# 인게임 성능 관제 (866)

Grafana OSS 13.2.1 + Prometheus 3.13.2 + Python 3.10 이상. 게임 서버/DB 설정을 변경하지 않는 로컬 플레이테스트용 구성이다. Metabase 행동 분석(781/782)과 목적이 다르며, 최신 develop에는 해당 전송 구현이 없어 기존 `IHttpTransport` / `UnityWebRequestTransport`를 재사용한다.

## 실행

Windows PowerShell에서 저장소 루트 기준:

```powershell
Tools/observability/install-local.ps1 -Runtime Tools/observability/.runtime
Tools/observability/start-local.ps1 -Runtime Tools/observability/.runtime
```

Grafana: http://127.0.0.1:3300/d/game-performance . 사용자 `admin`, 비밀번호는 runtime의 `credentials.json`에 있다. 파일에는 수집 토큰도 있으므로 공유하거나 커밋하지 않는다. 외부 포트 개방, Windows 서비스 등록, 운영 배포는 하지 않는다.

Unity 실행 전에 해당 프로세스 환경에 설정한다. 이미 실행 중인 Editor에는 새 환경 변수가 전달되지 않는다.

```powershell
$credentials=Get-Content Tools/observability/.runtime/credentials.json -Raw | ConvertFrom-Json
$env:GAME_METRICS_URL='http://127.0.0.1:9464/ingest'
$env:GAME_METRICS_TOKEN=$credentials.token
$env:GAME_METRICS_BUILD='before-commit-id'
# 이 셸에서 Unity Editor 또는 게임 빌드 실행
```

URL이 없으면 수집 자체가 등록되지 않는다. 설정 오류는 경고 후 수집을 비활성화하며 게임 시작을 막지 않는다. 프로젝트에 토큰을 저장하거나 Networked 속성으로 복제하지 않는다. 일반 배포 클라이언트에 공용 장기 비밀을 내장하는 인증 설계가 아니며, 승인된 개발 플레이테스트 실행에만 사용한다.

종료:

```powershell
Tools/observability/stop-local.ps1 -Runtime Tools/observability/.runtime
```

PID·실행 경로·시작 시각이 일치하는 자체 프로세스만 종료한다. DB/게임 프로세스에는 관여하지 않는다. Prometheus 보존은 7일/1GB이며 Grafana 설정·DB는 runtime에 남는다.

## 지표 의미

- 프레임/Fusion update/RTT는 고정 구간 히스토그램. Grafana p95/p99는 구간 보간 **근삿값**이며 원본 CSV의 정확 분위수와 같지 않다. 백분위끼리 평균하지 않는다.
- Fusion update는 IBeforeUpdate~IAfterUpdate의 벽시계 시간이다. 단일 tick CPU 비용이 아니며 여러 tick과 대기 시간을 포함할 수 있다.
- RTT는 양수 보고 표본만 집계. 호스트 RTT 미보고는 0ms가 아니다.
- Fusion SDK 상세 통계는 빌드에 따라 없을 수 있다. 지원 참가자 수와 최근 보고 참가자 수를 함께 본다. frame/Fusion update는 자체 계측, GC는 `GC.CollectionCount(0)` 횟수, 메모리는 관리 힙 크기다. GC 정지 시간·프로세스 전체 RAM·GPU 시간은 아니다.
- 역할, 인원, 단계, 빌드로 비교한다. label에 방 코드·사용자 ID·세션 UUID를 넣지 않는다. 최근 보고 참가자 수는 20초 이후 0으로 만료된다. 메모리 패널은 같은 그룹의 최근 보고 최댓값이다.
- 5초 단위 비동기 배치, 단계 변경 시에도 flush. 동시 요청 하나/2초 제한이며 전송 지연 시 창을 버린다. 재전송/디스크 스풀 없이 메모리를 제한한다. 따라서 데이터는 손실 가능하고, 오류·이탈의 감사 로그 용도가 아니다. 서버는 동일 세션 sequence 재전송을 중복 합산하지 않는다.
- 서비스 재시작 시 수집기 누적 카운터는 초기화되고 Prometheus `rate`가 reset을 처리한다. 수집기 최대 256개 그룹/256개 최근 세션, 본문 16KB, 세션별 최소 0.5초, 30초 이내 시각만 허용. 포화 시 503. 빌드 label은 실험별 짧은 고정 이름 사용.
- 수집 API는 127.0.0.1 전용이며 `http.server` 기반 단일 요청 처리다. 승인된 로컬 플레이테스트 규모(6명/5초)에 한정한다. 원격 팀 관제로 확장할 때는 TLS·접근 제한·요청 제한을 갖춘 reverse proxy와 운영용 HTTP 서버로 배포 검증이 필요하다. Prometheus `/metrics`와 Grafana 관리 API를 인터넷에 직접 노출하지 않는다.

## 검증

```powershell
Tools/observability/.runtime/venv/Scripts/python.exe Tools/observability/test_collector.py
Tools/observability/.runtime/prometheus-3.13.2.windows-amd64/promtool.exe check config Tools/observability/.runtime/prometheus.yml
```

Unity EditMode `PerformanceMonitoringTests`: 히스토그램 경계·비유한 값, 전송 중복 억제·취소, 외부 평문 HTTP 거부. 수집기 테스트: 스키마 검증·중복 sequence·만료·용량 제한. `/health`, Grafana `/api/health`, Prometheus `/api/v1/targets`와 실제 게임 데이터 수신도 확인한다.

2026-09-07 검증: Unity Development 빌드 오류 0, 해당 EditMode 3/3 통과, Python 2/2 통과, promtool config 통과. 로컬 6인 실제 경기에서 host 1 / client 5 보고와 단계별 데이터 수집을 확인했다. `verify-local.py <runtime>`로 16개 패널 쿼리 성공, Grafana DB 정상, 수집 target up, 인증/스키마/본문 크기 거부 3개를 확인했다. 별도 외부 서버 배포나 원격 PC 성능 검증은 포함하지 않는다.

공식 배포 해시는 [Grafana 다운로드](https://grafana.com/grafana/download?edition=oss&platform=windows), [Prometheus 다운로드](https://prometheus.io/download/) 기준으로 고정했다. 업데이트 시 버전·해시와 로컬 실행 검증을 함께 갱신한다.
