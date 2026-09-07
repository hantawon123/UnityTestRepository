# 로비 맵 (Lobby) — 지하실 아지트

대기실(로비) 맵의 제작 과정과 결정 사항, 중간 결과 이미지를 둡니다.
컨셉은 [concept-guide.md](../concept/concept-guide.md)의 `[LOBBY] — 작전실`: 도둑들이 작전을 준비하는 지하 아지트.

- 파일명은 영문 소문자와 하이픈. 예: `basement-overview-topdown.png`
- Unity 조립 씬: `Assets/PolyWorkshop_BasementWorkshop/Scenes/LobbyBuild.unity` (실제 로비 씬 `Assets/_Game/Content/Scenes/Lobby.unity`에는 아직 미투입)

## 목록

- `basement-overview-topdown.png` — 지하실 내부 전경(위에서). 작업대 존·잡동사니 존·바닥 전단 배치 확인용
- `basement-wanted-posters.png` — 서벽의 탈 종류별 WANTED 포스터 4장(Rabbit/Bear/Cat/Dog)
- `basement-ceiling-lights.png` — 천장 장선과 조명 배치, 동벽 계단·초록 문
- `basement-boundary-colliders.png` — 벽에 붙인 보이지 않는 경계 콜라이더 6면(연두색 와이어)
- `posters/` — WANTED 포스터 원본 이미지 4종(1024×1536). 데칼 아틀라스 합성에 사용

## 제작 경과

### 1. 컨셉과 레이아웃 (2026-09-04)

- 로비를 "도둑 아지트 = 지하실 작업실"로 정의. Blender로 14×12×5 m 블록아웃을 먼저 만들어 레이아웃 설계도로 사용
  (서벽 작업대 존, 북벽 선반 존, 동벽 금고·계획판·닫힌 문, 남벽 높은 창 3개, 천장 목재 장선, 잡동사니 존).
- 에셋: Unity Asset Store **Basement Workshop Environment**(PolyWorkshop) 구매. 모듈러 벽·천장·기둥과 소품 프리팹 제공.
  Built-in 렌더 파이프라인 에셋이라 URP 머티리얼 변환 후 사용.

### 2. Unity 조립

- `LobbyBasementEnvironment` 루트 아래에 역할별로 분리:
  `BasementModules`(BigRoom·SmallRoom 벽/천장/기둥) · `StaticProps`(고정 소품 79) · `Items`(공구 15) · `Decals`(종이류 20) · `Lights`(20) · `Boundary`(경계 6)
- 설계 원칙: 상자·콘·타이어·가스통처럼 들고 옮길 수 있는 소품은 벽과 합치지 않고 **개별 오브젝트**로 둔다.
  이후 물건 들기/놓기 시스템(Carryable)에 연결할 대상이기 때문.

### 3. WANTED 포스터 데칼 (2026-09-07)

- 팩의 데칼 10종은 아틀라스 텍스처 1장을 공유한다. 원본을 건드리지 않고 복제본 텍스처의 포스터 칸(Decal04 세로 포스터, Decal08 바닥 전단)에만 우리 그림을 합성.
- 탈 종류(Rabbit/Bear/Cat/Dog)마다 아틀라스 1장 + 머티리얼 1개, 총 4세트:
  `Assets/_Game/Content/Textures/TEX_LobbyDecals_{탈}.png`, `Assets/_Game/Content/Materials/MAT_LobbyDecals_{탈}.mat`
- 벽의 세로 포스터 4장에 세트를 하나씩, 바닥 전단은 세트를 섞어서 배치.
- **결정**: 바닥 신문·전단은 고정 배치(들 수 없음). 그림자에 사각 프레임이 생기는 문제는 머티리얼을 Transparent → **Opaque + Alpha Clipping**으로 바꿔 해결.

### 4. 경계 콜라이더 (2026-09-07)

- 벽 모듈 콜라이더가 있어도 물리 버그로 맵 밖에 나가는 것을 막는 안전망. 에디터 메뉴 `Game/Lobby/Build Boundary Colliders`(`LobbyBoundsBuilderMenu.cs`)로 자동 생성.
- 측정 기준은 **벽·문 모듈(Basement_Wall/Door)의 최외곽 면**, 높이는 벽 상단/천장 모듈. 바닥 판(Ground)과 천장 위 램프 메시를 포함하면 경계가 벽에서 수 m 떨어지므로 제외.
- 결과: 내부 13.3 × 6.4 × 12.5 m (x -3.15~10.10, z -10.35~2.15, y -0.09~6.32), 판 두께 2 m, 벽 면에서 5 cm 여유.

## 다음 단계

1. `LobbyBasementEnvironment`를 프리팹으로 저장하고 `Lobby.unity`의 창고 환경(LobbyWarehouseEnvironment)과 교체. 스폰 포인트 6개·조명·Reflection Probe·Post-process Volume 정리.
2. Lobby 씬에서 실제 플레이 테스트(이동, 벽·경계 충돌, 시작·나가기).
3. 상자·콘·타이어·가스통을 Carryable 프리팹 변형으로 전환(로비 네트워크 동기화 여부 확인 후).
4. (선택) 방장 전용 상호작용 "작전 계획판"으로 설정/시작 화면 열기.
