# 6인 인게임 성능 작업

이번 주 스프린트: 55543 (2026-09-07~09-11). 담당 한태원.

| Task | 범위 | SP | Epic | Story |
|---|---|---:|---|---|
| S15P21D205-866 | Grafana 관제 | 8 | 413 | 865 |
| S15P21D205-867 | 6인 CPU 병목 특정 | 5 | 2 | 582 |
| S15P21D205-868 | 확인된 병목 최적화 | 8 | 2 | 582 |
| S15P21D205-869 | 전후 동등 조건 비교 | 3 | 2 | 582 |
| S15P21D205-870 | 호스트 분리/전용 서버 판단 | 5 | 2 | 582 |

Task와 Story는 같은 Epic 아래 '이슈 분할' 관계이며 Story에는 SP를 중복 합산하지 않는다. 기존 완료 574를 재개하거나 중복 완료 처리하지 않고 실제 인게임 측정 후속으로 진행한다.

## 867: CPU Profiler 결과

기준 코드는 9bc830eb 기반 12f6f51e(문서 후속), Unity 6000.3.22f1, Fusion 2.1.2.2279, Host 64Hz. 별도 복사 프로젝트의 Development 빌드로 6개 프로세스를 실행했다. 호스트와 추가 4개 클라이언트는 렌더링 없음, 클라이언트 1개는 1280×720/60fps. 자동 이동·회전·달리기·점프이며 경로 탐색 봇은 아니다.

Searching 진입 후 20초를 기다려 호스트 CPU를 20초 수집했다. 1,141 프레임 중 초기 5개를 제외한 1,136개 표본. Unity ProfilerRecorder의 TimeNanoseconds 단위를 ms로 변환했으며 바이트/개수 카운터는 시간 순위에서 제외한다. 기본 CPU 프로파일(Deep Profiling 아님) 원본도 저장했다.

| Marker | 평균 ms | p95 ms |
|---|---:|---:|
| Simulation.Update | 5.15 | 13.32 |
| Simulation.StepSimulation | 4.58 | 12.23 |
| SimulationBehaviourUpdater.InvokeFixedUpdateNetwork | 4.47 | 11.86 |
| KCC.FixedUpdate | 3.91 | 10.26 |
| Physics.Simulate | 0.58 | 1.81 |
| Physics.OverlapCapsuleNonAlloc | 0.54 | 1.27 |
| KCC.RenderUpdate | 0.48 | 1.01 |

KCC.FixedUpdate 평균은 Simulation.Update 평균의 약 76%다. **부모·자식 marker는 중첩되므로 위 시간을 더하면 안 된다.** KCC 이동/충돌 처리가 다음 분석 대상이라는 근거이며, 특정 KCC 내부 함수의 독점 CPU 시간이나 물리 엔진 오류를 확정한 것은 아니다.

470개 recorder와 raw 프로파일 수집 자체가 CPU·할당 오버헤드를 더한다. 프로파일 수집 중 GC 통계와 프레임 시간을 일반 실행의 성능 개선율 계산에 사용하지 않는다. 이전 비프로파일링 CSV 기준 6인 탐색 Host Fusion update p95 12.64ms, 클라이언트 frame p99 26.91ms와 구분한다.

## 868: 변경 판단

확인된 비용은 KCC 고정 업데이트에 집중된다. 다음 설정을 맹목적으로 변경하지 않는다.

- SDK KCCSettings.ForceSingleOverlapQuery 설명은 NPC용 최적화이며 이동 오류 가능성을 명시한다. 플레이어에 적용하지 않았다.
- MaxPenetrationSteps, CCD, step-up/snap-to-ground 기능 축소는 충돌·계단·접지 회귀 검증 없이 적용하지 않는다.
- MatchSessionState.PublishObjectStates의 배열·문자열 생성은 코드상 후보지만 이번 이동 프로파일에서 주요 비용으로 입증되지 않았다.

안전한 게임 동작 변경이 확인되기 전에는 868/869를 완료로 처리하지 않는다. 관제는 기능 추가이며 게임 성능 최적화와 구분한다.

## 870: 현재 PC 제한

사용자 확인: 외부 PC 없음. 같은 PC의 프로세스 분리/가능한 Server 모드 비교 및 전환 타당성을 검토한다. 현재 측정도 게임 시뮬레이션 호스트 프로세스는 이미 별도다. 따라서 별도 프로세스를 다시 띄우는 것만으로 물리 서버 분리 효과가 입증되지는 않는다. 원격 회선, 다른 CPU, 실제 호스팅 성능은 미검증이다.

## 원본 위치 및 재현

현재 측정 자료는 작업 워크스페이스 `.build/photon-baseline/`에 있다. `six-cpu/peer0-cpu.raw`(약 112MB, Unity Profiler에서 로드), `peer0-cpu.csv`, `peer0-markers.csv`, `peer*.log`, `manifest.json`을 함께 보존했다. 제품 저장소에 바이너리 측정 자료를 넣지 않는다. raw 파일을 사용자가 열어 볼 수 있으며 exporter 결과 표는 위와 같다.

별도 측정 프로젝트의 LocalPhotonBaseline은 임시 비공개 방을 만들고 6명 입장 후 실제 게임 시작 API를 호출한다. BackendSignIn만 개발 측정에서 생략하고 게임 권한/동기화 경로는 유지한다. BaselineCpuCapture는 `--cpu-capture true`일 때만 설치한다. 같은 PC 측정 명령은 작업 워크스페이스의 `.build/photon-baseline/run.ps1 -Count 6 -Seconds 290 -Run six-cpu`를 기반으로 한다. 해당 런처는 일반 통계 수집이며 CPU 캡처에는 추가 인자를 전달해야 한다.

종료 시 기존 NetworkMatchHudPresenter.Dispose → NetworkMatchHudView의 NullReferenceException이 1회 기록됐다. 캡처 저장 이후 종료 처리에서 발생했고 CPU 수집 실패는 아니지만, 별도 회귀 문제로 남긴다. 관련 Jira를 임의로 추가 생성하지 않았다.
