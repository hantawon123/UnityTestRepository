# 네트워크 QA 작업 기록 (2026-09-10)

브랜치: `feature/client/qa-network-contract`

## 범위

QA 목록의 체크되지 않은 항목만 확인. 사용자 승인에 따라 Client UI·캐릭터·애니메이션 코드는 읽기만 하고 Bootstrap/Network 연결 및 테스트 수정.

## 커밋별 변경

- `cb14aeaf`: Jenkins #56 네트워크 계약 테스트의 카탈로그 초기화 격리. WebGL 대상 로컬 EditMode 106/106 통과. Jenkins 전체 파이프라인 재실행 결과는 아직 없음.
- `c51f8aac`: 로컬 하이라이트 스킵 뒤 로비가 보이는 동안 이전 경기 맵의 렌더러·콜라이더 숨김 유지. 관련 135/135 통과.
- `8b740e13`: 숨기기/찾기 안내 UI가 실제로 표시되는 동안 로컬 이동·상호작용 입력과 물건 HUD 차단. 관련 203/203 통과.
- `20513bdd`: 결과 씬이 표시되는 동안 네트워크 들기 애니메이션 값 전달 억제. 물건 소유 상태와 리플레이 데이터는 유지.
- `f2d4cee6`: 앉기/엎드리기 상태에서 점프로 서기 전환 제거, 서 있고 접지한 경우에만 점프. 회귀 케이스 포함 관련 223/223 통과.
- 인게임 ESC: 기존 SettingsView/SettingsPresenter를 Bootstrap에서 조립. 열린 동안 로컬 입력과 채팅 차단, 뒤로가기 시 복원, 나가기 확인은 기존 RoomUiCommands 경로 사용. 설정 화면 내부 UI는 변경하지 않음.

## 하이라이트 맵 잔존 원인과 확인 한계

하이라이트 카메라의 가림 복구가 Renderer.forceRenderingOff를 false로 되돌리는 반면, 기존 로비 코드는 보이기 상태가 바뀔 때 한 번만 이전 맵을 숨겼다. 따라서 스킵 후 정리/재생 데이터 처리가 렌더러를 복원하면 공통 하이라이트 종료까지 맵이 다시 보일 수 있다. 로비 표시 중 숨김을 계속 유지하도록 보완했다.

이는 코드상 가능한 재노출 경로를 차단한 것이다. 실제 팀원 한 명에게 발생한 원인과 완전히 동일한지는 아직 확정하지 못했다. 실제 다중 플레이로 재현/재검증해야 한다.

## 남은 확인 및 담당자 연결

- 3명 각각 다른 PC에서 호스트 먼저 스킵, 참가자 먼저 스킵, 거의 동시에 스킵, 늦은 참가자 스킵 확인. 로비 진입부터 공통 하이라이트 종료까지 맵 재노출 여부 확인.
- 배정/찾기 안내 중 클릭·이동 차단과 안내 종료 후 정상 복구 확인.
- 결과 화면의 승자 들기 자세, 로비 복귀 시 정상 상호작용 확인.
- 앉기/엎드리기 상태 점프 금지와 서기 전환 후 점프 확인. 변경 대상은 네트워크 플레이.
- 인게임 ESC 설정 열기, 화면의 뒤로가기, 변경사항 취소/적용, 나가기 확인(호스트/참가자 각각) 실플레이 확인. 다른 참가자의 경기 시간을 정지하지 않음.
- 배정 물건/카테고리 상시 표시: Client/Match/NetworkMatchHudView.cs의 SetAssignedItem()이 비어 있음. UI 담당자 구현 필요.
- 홈 모달·닉네임/친구 상태 표시·음소거 표시 등 UI 담당 영역은 직접 수정하지 않음.
- WebGL 프레임은 이번 코드 테스트로 검증할 수 없음. 새 카탈로그 포함 배포 빌드에서 별도 측정 필요.

## 테스트 범위 주의

넓은 필터 테스트에서 333개 중 332개 통과, PlaySettingsPresenterTests.RealView_GameStartMatchesLeaveGamePlate 1개 실패: 테스트가 GameStartButton을 찾지 못함. 해당 Client UI는 이번에 수정하지 않았으며 실패를 기록한다. 이 결과를 전체 통과로 취급하지 않는다.

Unity 테스트 로그와 XML은 작업용 문서 저장소의 `.build/lobby-profiler/qa-*-tests.*`에 보관. 이번 작업은 아직 MR/배포하지 않음.

최종 수정 범위의 정규화된 테스트 필터 실행: 242/242 통과 (`qa-scoped-tests.xml`). 실제 ESC 화면과 다중 플레이 검증은 위 목록대로 남아 있음.

## 후속 플레이 오류 보완

- `34334767`: WebGL 대상 Editor에서도 Host 모드 허용. 기존 조건의 UNITY_EDITOR 제외 제거.
- `8a109430`: Playground의 NetworkInteractionSceneBridge에 AsSelf 등록 추가. 실제 씬 조립에서 발생한 VContainer Resolve 실패 보완.
- `70d31917`: 소지 좌우 오프셋을 코드 기본값과 PlayerCharacter 프리팹에서 0으로 변경. 이 위치 설정에 한해 사용자에게 Client 수정 예외 승인받음. 결과 화면에서 CarriedItem 참조를 해제하고 이전 경기 스냅샷의 재부착 차단.
- 위 후속 수정은 자동 테스트를 재실행하지 않았으며, 이전 242개 통과 결과에 포함되지 않음. 열린 사용자 Unity의 재컴파일과 실플레이 확인 필요.
- Fusion RejoinSession NullReferenceException 직전에 Game does not exist 오류 확인. 재입장 시도 원인은 아직 미확정이며 해결 완료로 처리하지 않음.
- 사용자 요청에 따라 QA 브랜치를 origin에 push하며 MR은 생성하지 않음.

## 설정 모달 및 Space/Space/Tab 스킵 후속

- Bootstrap 설정 조립에서 기존 1920x1080 UI를 중앙 정렬한 80% 크기로 축소. CanvasScaler Expand로 화면비가 달라도 전체가 들어오도록 처리하고 게임 HUD 위로 표시.
- 열린 설정은 ESC로 기존 RequestBack 경로 호출. 확인/피드백 창에서 소비한 ESC가 같은 프레임에 설정까지 닫지 않도록 처리.
- 이전 경기 SceneRoots 배열에 남은 재사용 PlayerCameraController 루트를 로비 전환의 숨김 대상에서 제외. 프레임 후반에도 이전 맵 숨김 유지. 실제 재현 해결 여부는 플레이 검증 필요.
- 사용자 오류 두 개는 동일한 RPC_RequestThrow 거절 응답의 Player:None 대상 오류. 던지기/놓기/떨어뜨리기 RPC에 SourceIsHostPlayer를 지정해 호스트 발신자를 명시.
- 카메라 루트가 이전 씬 배열에 남은 경우의 회귀 테스트 추가. 사용자 Unity 세션을 종료하지 않았으므로 이번 자동 테스트는 미실행.

## ESC 메뉴 재현 수정 및 재검증

- 메뉴가 검은 배경에 가려진 원인: Bootstrap에서 UI 자식들을 역순으로 옮겨 배경이 맨 위로 이동. 원래 형제 순서를 유지하도록 수정하고 회귀 테스트 추가.
- ESC 닫기 후 카메라가 동일 ESC로 커서를 다시 해제하지 않도록 경기 설정에서 EscapeReleasesCursor 관리. 크기는 100% 유지.
- 스킵 후 이전 맵 숨김은 Renderer.enabled까지 비활성화하며 원래 활성 상태를 보관/복원. 다른 참가자가 재생 중일 때 Playground 씬 자체의 존재는 유지. 실제 다중 플레이의 맵 재노출 해결 여부는 미확정.
- 사용자가 Editor를 닫은 뒤 WebGL 대상 EditMode 검증 실행: NetworkContractTests, ResultPresentationTests, LobbyHighlightHandoffTests, NetworkMatchHudPresenterTests 총 154/154 통과. 로그: qa-menu-regression.xml / qa-menu-regression.log.
