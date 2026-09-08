# WebGL 배포 마무리 검증 — 2026-09-08

기준 develop: `2b6fed00a29acb9c6aa9a8a9224ba1f4f307e227` (MR !221 병합).
후속 브랜치: `feature/server/webgl-finalize`. 이번 변경은 배포 검증 코드와 문서이며 Unity 화면·게임 규칙은 변경하지 않는다.

## 실제 배포 확인

- [Jenkins #11](https://j15d205.p.ssafy.io/jenkins/job/d205-unity-webgl/11/) SUCCESS, 전체 484.716초(8분 4.7초).
- 로그의 Publish 단계에서 기준 develop 게시 확인. 수동으로 시작한 빌드이며 SCM 자동 시작 성공으로 표현하지 않는다.
- [공개 포인터](https://j15d205.p.ssafy.io/play/current.json): revision은 위 SHA, sequence=11.
- 자동 SCM 폴링은 2026-09-08 00:28 UTC에 origin/develop을 조회하고 already built by 11 / No changes로 정상 종료(0.29초).
- 다음 develop 변경의 자동 시작·완료 확인은 병합 후 남은 검증이다. 이를 위해 develop에 직접 커밋하거나 시험용 변경을 넣지 않는다.

## HTTPS 및 브라우저

- 신규·기존 버전의 gzip 파일 HEAD 200, Content-Encoding=gzip, WASM Content-Type=application/wasm 확인.
- 공개 /play/ 접속이 현재 SHA의 releases 경로로 이동하고 홈·닉네임 표시 확인.
- Codex 내장 Chromium에서 비공개 검증방 생성 및 Lobby 씬/참가자 표시 확인. 재접속으로 검증방 종료.
- 캐시가 있는 같은 브라우저에서 /play/ 재접속 후 현재 SHA로 이동 확인.
- Chrome/Edge 별도 브라우저 매트릭스, 다른 참가자의 코드 입장, 6인 전체 경기·음성은 이번 검증에 포함하지 않는다(875 유지).

## 실제 롤백 및 복원

사용자의 공개 일시 롤백 승인 후 EC2의 별도 임시 경로에서 수정한 publish.py 실행.
Jenkins 작업 폴더의 스크립트는 바꾸지 않았으며 변경 코드는 이 MR 병합 후 반영된다.

1. 기존 포인터가 예상한 현재 SHA/sequence=11인지 검사.
2. 현재 버전과 이전 `4799b914bb6bbdb1107f2887875e047d793acc25`의 필수 파일·버전 및 HTTPS 헤더 검사.
3. 이전 버전으로 실제 포인터 전환, HTTPS current.json에서 이전 SHA/sequence=11 확인.
4. finally 경로로 현재 버전 복원, 로컬·HTTPS 포인터 모두 현재 SHA/sequence=11 확인.

기존 버전 디렉터리는 보존했고 순서 값은 낮추지 않았다. 기존 브라우저는 현재 버전 URL에 유지됐다.
실제 경기 중인 두 버전의 동시 접속 시험은 하지 않았다. 네트워크 분리는 기존 NetworkRunnerService의 WebGL `web-{Application.version}` 설정을 확인했다.

## 수정 및 회귀 검사

기존에는 롤백 대상에 index.html만 있으면 선택 가능했다. 새 게시·롤백·이미 게시된 버전 재선택 모두 동일한 검사로 version.txt 일치와 필수 파일의 존재·크기를 확인한다. 검사 실패 시 현재 포인터를 유지한다.

- EC2 Linux: `python3 /tmp/d205-webgl-finalize/test_publish.py` 통과. 테스트는 임시 합성 디렉터리에서 실행.
- 파일 누락·빈 WASM·버전 불일치 각각의 롤백/재게시 차단, 현재 포인터 보존, 정상 복원 검사 추가.
- 로컬: `node Tools/webgl/test_release_info.cjs` 통과. 새 버전/통신 실패 시 강제 이동 없음, 취소 시 유지, 명시적 확인 후에만 /play/ 이동.
- 로컬: `python Tools/webgl/test_changes.py` 3개 통과.
- Unity 코드는 변경하지 않아 추가 전체 Unity 빌드는 실행하지 않았다. #11은 이번 Python 수정 전의 게임 산출물이다.

## Jira 범위

| 이슈 | 확인 범위 / 남은 확인 |
| --- | --- |
| 876 | 실제 EC2 빌드·배포 성공 및 자동 폴링 확인. 후속 develop 변경에 따른 자동 시작·성공은 병합 후 확인 |
| 877 | 실제 HTTPS 게시·홈·게스트 프로필·호스트 방 생성/로비 진입 확인. 875의 다인 호환성 검증과 구분 |
| 878 | 실제 롤백·복원, 순서 값 유지, 손상 대상 차단, 캐시 재접속 및 알림 자동 검사 완료. 실제 구/신 빌드 혼합 참가 검증은 미실행 |
| 875 | 6인·음성·Chrome/Edge·탭 전환 검증 미완료 유지 |
| 575 / 579 | 이번 범위 제외. 최종 빌드 검증 범위 정리 / 사용자 요청 부하 테스트 보류 유지 |

MR은 사용자 요청대로 생성 직전 보고 후 진행한다. 미검증 조건을 충족한 것으로 처리하지 않는다.
