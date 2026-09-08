# 엔딩 맵 (Ending) — 경찰서 유치장

매치 종료 후 결과를 보여주는 엔딩 장면의 제작 문서. 컨셉은 [concept-guide.md](../concept/concept-guide.md)의 `[GAME END] — 탈출 성공 / 체포`.
컨셉 이미지: [game-ending-concept.png](../concept/game-ending-concept.png) — **한 장면**에 탈출 성공자는 철창 앞 복도에서 환호하고, 체포된 사람은 철창 안에 서 있다.

- 브랜치: `feature/client/ending-map`
- Jira: 724(에픽) · 725 컨셉 기획·에셋 · 726 임포트·URP · 727 모듈 조립 · 728 소품 · 729 조명 · 730 상호작용 소품 · 731 씬 통합·스폰 · 732 문서화 · 733 콜리전·플레이테스트

## 1. 컨셉 기획 (725, 2026-09-07 초안)

### 현재 결과 흐름 (이미 구현)
1. 매치 종료 → `Result.unity`가 인게임 위에 **화면 전체 오버레이**로 올라옴. 카메라·조명은 스코프가 꺼서 인게임 화면이 그대로 뒤에 보임.
2. `ResultPresenter`가 문구를 표시: 탈출 성공 / 체포 (`MatchTimerView.WinHeadline` 등). `ResultDisplaySeconds = 5`.
3. 5초 뒤 하이라이트 리플레이(클립 3개 × 10초) → 로비 복귀.

승패 데이터: `MatchResult.WinnerPlayerIndices`(권한자 발행) → `NetworkResultLobbyReturnController.IsLocalWinner`. 즉 **누가 탈출/체포인지 전원 명단을 클라이언트가 알고 있다.**

### 엔딩 장면 목표
결과 5초 구간을 텍스트 오버레이에서 **3D 연출 장면**으로 바꾼다.
- 배경: 경찰서 유치장(복도 + 철창 + 잠긴 문 + 열린 출구).
- 탈출 성공자: 철창 **앞 복도**에 서서 환호(컨셉의 팔 들기·점프 포즈).
- 체포된 사람: 철창 **안**에 서서 철창을 붙잡은 낙담 포즈.
- 카메라: 복도 끝에서 철창을 정면으로 보는 고정 카메라(에셋 제공값 위치 (0, 1.85, 7.6), 회전 (4.5, 180, 0), FOV 40.4).
- 문구는 기존 `ResultView`를 그대로 위에 얹는다(탈출 성공! / 체포되었습니다).

### 베이스 에셋 — 이미 확보됨
`Assets/PoliceHoldingStylized/` (2026-09-07 develop에 추가, `e70c86ec`, hantawon123). Unity 6 URP용, 단일 프리팹 `Police_Holding_Stylized.prefab`.
- 크기 7.5 × 5.2 × 13.9 m. 유치장(z -3~0, 내부 깊이 2.63 m) + 복도(z 0~10) + 출구(z 10, 흰빛 배경). 철창 폭 7.17 m.
- 렌더러 171개, URP Lit/Unlit 머티리얼 35개, BoxCollider 12개(바닥·벽·철창·출구 차단). 라이트 오브젝트 그룹 `Lighting` 포함(펜던트 전구 3개).
- 컨셉 이미지와의 차이: 컨셉은 형광등 3개 + 벽걸이 소화기 + 자물쇠 클로즈업. 에셋은 펜던트 전구 + 바닥 소화기 + 경찰 방패 장식. 분위기(회청색 벽돌·차가운 빛)는 조명으로 맞춘다.
- 추가 구매 불필요. 필요한 것은 **에셋이 아니라 애니메이션**.

### 필요한 애니메이션 (없음 → 제작 필요)
현재 클립: Idle·Walk·Run·Jump·Fall·Land·Crouch·Crawl·Punch·Stunned. 엔딩용 없음.
- `Celebrate`(환호: 팔 들기 + 제자리 점프 반복) — 탈출자 최대 6명이 같은 클립을 쓰되 시작 시간을 어긋나게.
- `Dejected`(낙담: 철창 붙잡고 고개 숙임 또는 무릎 꿇기) — 체포자.
- 제작 경로: Blender 리그(`PlayerCapsule_*.blend`)에서 액션 2개 추가 → FBX → `Assets/_Game/Content/Animations/`. Mixamo 리타겟(Humanoid)도 가능(Cheering, Defeated 계열).

### 씬 구성안 (727·728·731에서 구현)
- 새 씬 `Assets/_Game/Content/Scenes/Ending.unity` 또는 기존 `Result.unity`에 3D 배경 추가. **Result.unity 확장** 추천: 진입·타이밍·문구·하이라이트 전환이 이미 이 씬에 묶여 있어 흐름 변경이 없다. 스코프가 카메라·조명을 끄는 `DisableAdditiveSceneOutputs`는 엔딩용 카메라·조명을 예외로 두도록 수정.
- 배치: 유치장 프리팹 원점 고정. 스폰 포인트 두 세트 — `EscapeSpawn_1~6`(복도 z 3~6, 카메라 쪽에 가깝게 부채꼴) · `ArrestSpawn_1~6`(유치장 안 z -2~-0.6, 철창 바로 뒤 한 줄).
- 아바타: 네트워크 아바타를 옮기지 않고 **로컬 복제 비주얼**을 세운다(하이라이트 리플레이가 `Animator`가 달린 대상에 액션을 재생하는 방식과 같은 계열). 각 플레이어의 탈 종류·색은 로비/매치 외형 데이터로.
- 소품: 탈출자 손에 자기 물건(컨셉의 자루)을 들려 주는 것은 선택. 유치장 안 벤치는 에셋에서 제거된 상태.
- 조명: 컨셉의 차가운 형광 톤. 에셋 펜던트 전구는 색을 냉색으로, 상단에 긴 형광등 영역광 2~3개 추가 → 베이크(로비의 `Game/Lobby/Lighting` 메뉴를 엔딩용으로 복제).
- 상호작용 소품(730): 엔딩은 관람 장면이라 **들 수 있는 물건 없음**으로 제안. 이슈는 "해당 없음" 사유로 닫기.
- 콜리전(733): 아바타를 코드로 세우므로 이동 없음. 콜라이더는 에셋 12개로 충분. 플레이테스트는 6인 결과 조합(전원 탈출·전원 체포·혼합) 확인.

### 결정
- **담당 (2026-09-07)**: 엔딩 파트는 지환이 넘겨받아 전담한다. 유치장 에셋은 hantawon123이 추가한 것을 그대로 사용.

### 열어 둘 결정
1. 노출 시간: 3D 연출은 5초가 짧다. 7~8초로 늘릴지(`ResultDisplaySeconds`), 하이라이트 총 길이와 함께 결정.
2. 애니메이션 제작 주체(블렌더 캐릭터 담당 vs Mixamo 리타겟).
3. 씬 구성: Result.unity 확장(추천) vs 별도 Ending.unity.

## 2. 에셋 임포트·URP 확인 (726, 2026-09-07)

- 변환 작업 **불필요**: 머티리얼 35개 전부 URP Lit/Unlit, 텍스처 없이 플랫 컬러(`MapData.json`의 색·roughness·metallic 값), 이미시브 없음. FBX 스케일 1, Read/Write 꺼짐(콜라이더는 별도 BoxCollider라 문제 없음).
- 프리팹 안 `Lighting` 그룹에 실시간 스팟 6개: 창문 필(0.55, 온색), 펜던트 3개(2.8~3.4, 온색), 복도 바운스(1.4, 냉색, 그림자 없음), 출구 백광(7.6, 흰색). 전부 Realtime이라 베이크 전 상태. 데모 씬 환경광은 Flat #6B6E70, 스카이박스 기본, 안개 없음.
- 데모 카메라: (0, 1.85, 7.6) / (4.5, 180, 0) / FOV 40.4, Post Processing 켜짐, 배경 단색. 컨셉 구도와 같은 정면 샷.
- 컨셉(차가운 형광 톤)과 데모(온색 펜던트)의 차이는 729 조명에서 색온도·영역광 추가로 맞춘다.

## 참고 파일
- 결과 흐름: `Assets/_Game/Bootstrap/ResultLifetimeScope.cs`, `NetworkResultLobbyReturnController.cs`, `Assets/_Game/Content/Scenes/Result.unity`
- 리플레이 아바타 재생: `Assets/_Game/Bootstrap/HighlightReplayPlayer.cs`
- 와이어프레임: `docs/planning/wireframes/ending.md`, `highlight.md`
