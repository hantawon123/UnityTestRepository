# 매치 맵 (마트) — 제작 경과

메인 매치 맵을 마트 테마 로우폴리로 새로 만든다. Jira 에픽 **S15P21D205-901 마트 맵 세팅**, 태스크 902~915.
태스크 상세·순서는 [../../planning/match-map-mart-jira-tasks.md](../../planning/match-map-mart-jira-tasks.md).

- 파일명은 영문 소문자와 하이픈. 예: `synty-shops-demo-market-aisles.png`
- 작업 브랜치 `feature/client/match-map-mart` (develop 기준)

## 이미지 목록

- `synty-shops-demo-mall-overview.png` — Shops Pack 데모(쇼핑몰) 기본 카메라 시점. 2층 몰·에스컬레이터·분수
- `synty-shops-demo-market-aisles.png` — 데모 안 슈퍼마켓 구역 눈높이. 진열대(Aisle) 5열·계산대·천장 안내판
- `synty-shops-demo-market-high.png` — 같은 구역을 위에서. 진열대 배열·카트·냉장 진열장 위치 참고
- `synty-plaza-demo-overview.png` — Shopping Plaza Map 데모 전경. 도심 블록 위 매장 7개 + 주차장(사전 조립 맵)

## 제작 경과

### 1. 에셋 선정·구매 (2026-09-10)

- 후보 검색 기준: 컨셉(도둑 아지트·경찰 도착·탈출), 숨길 곳 많은 실내, URP 지원, 기존 캡슐 캐릭터와 어울리는 스타일라이즈드.
- **결정**: Synty **Value Bundle — POLYGON Shops Pack + POLYGON Shopping Plaza Map**. 마트(슈퍼마켓) 테마.
  - 번들 상품을 임포트하면 `Assets/ShopsValueBundle/ReadMe.txt`만 들어오고, 실제 팩 2개는 Package Manager > My Assets에서 각각 임포트해야 한다.
  - Shops Pack: 228 MB, v1.6.6(2026-08-18). 프리팹 1,933(스토어 표기) / 실제 임포트 1,957. 슈퍼마켓 통로·계산대·냉장/냉동 진열장·청과대, 모듈러 건물, 간판 295, 캐릭터 14, 데모 쇼핑몰 씬.
  - Shopping Plaza Map: 126 MB. Shops Pack에 의존하는 사전 조립 도심 블록 맵(매장 7개·화장실 2·주차장).
- **라이선스**: Restricted Single Entity. 추가 제한 2개 — (1) 사용자 제작 콘텐츠(UGC)가 주목적인 앱에서의 수익화 금지, (2) NFT 발행·거래 상품 금지. 우리 게임은 정해진 규칙의 파티게임이라 해당 없음. 팀 내 같은 프로젝트 사용은 단일 조직 사용으로 봄. 원본 .unitypackage는 저장소 제외(.gitignore `*.unitypackage`).

### 2. 임포트·URP 검증 (2026-09-10, 902)

- 임포트 결과: `Assets/Synty/` 427 MB — `PolygonShops`(프리팹 1,957·재질 62), `PolygonMapsPlaza`(프리팹 34·재질 14), `PolygonGeneric`(공용, 프리팹 495·재질 128), `SyntyPackageHelper`(에디터 도구).
- **URP**: 재질은 Synty 자체 셰이더 그래프(`Synty/Generic_Basic` 대부분, `Generic_Decals`·`Generic_Standard`·파티클용)를 쓰고 프로젝트 URP 에셋(`PC_RPAsset`)에서 **핑크 재질 0개**. 변환 작업 불필요.
- **텍스처**: 팩 공용 팔레트/아틀라스 2048² 다수(예: `PolygonShops_Building_Brick_Coloured_01`). 소품은 팔레트 한 장을 공유하는 전형적 Synty 방식 → 로비 톤 통일(912)에서 로비 쪽을 팔레트 방식으로 맞추는 근거.
- **프리팹 분류(Shops Pack)**: Buildings 167 · Characters 34 · Environments 39 · FX 5 · Food 298 · Generic 28 · Products 93 · Props 977 · Signs 295 · Vehicles 3 · Weapons 18.
  - 마트 핵심 소품 `SM_Prop_Market_*` 77개: 진열대 Aisle 01~05(+End·Preset 조합), 계산대 Checkout(Large·Shelf·Light·Divider), Deli Fridge 01~02(+Insert), Drinks Fridge 01~03(+Insert), Freezer 01~03(+Insert·Shelf), Food Display 01~05(+Insert), Fruit Basket/Bowl, PriceTag.
  - 진열 상품은 `Insert` 프리팹으로 분리돼 있어 빈 진열대·채운 진열대를 선택 가능 → 숨김 장소(빈 칸) 설계에 유리.
  - 그 외 접두사: Prop_Food 297 · Product 94 · Cafe 89 · Clothes 88 · Computer 76 · Shop 56 · Kitchen 51 · Warehouse 23(창고·팔레트·상자) · Poster 20 · Lighting 38.
- **콜라이더 점검**
  - Props 977개 중 962개에 콜라이더, 대부분 **MeshCollider(convex)**, BoxCollider는 20개. Buildings는 Box 124·Mesh 44. 비균등 스케일 프리팹은 Buildings 1개만(로비 계단 교훈 대비 양호). LODGroup 없음.
  - FBX 임포터 `Read/Write`가 꺼져 있음(`isReadable=False`). 로비에서 확인한 대로 Read/Write 꺼진 convex 메시 콜라이더는 플레이 중 조준 광선에 잡히지 않을 수 있다 → **상호작용 대상(Carryable·파쇄기·열리는 문)은 BoxCollider로 대체**(로비 Carryable 메뉴 3번 방식) 또는 해당 FBX만 Read/Write 켜기.
  - 라이트맵 UV 생성(`generateSecondaryUV`)이 꺼져 있음 → 베이크(911) 전에 모듈·가구 FBX에 켜거나, Synty 방식대로 라이트맵 없이 실시간+프로브로 갈지 결정.
- **문짝 분리 여부(열리는 가구 가능성)**
  - `SM_Prop_Market_Drinks_Fridge_01~03`: `..._Door_01`(+Glass) 자식으로 문이 **별도 메시** → 열기 상호작용 가능(피벗 확인 필요).
  - `SM_Prop_Market_Deli_Fridge`, `Freezer`, `Hunting_Cabinet`, `Mall_Kiosk_Cabinet`: 유리만 별도, 문 없음(개방형).
  - → 열리는 가구 태스크(문서 T7, 미발급)는 음료 냉장고 3종을 대상으로 범위를 잡을 수 있다.
- **데모 씬**: `PolygonShops/Scenes/Demo.unity`(쇼핑몰·소형 매장·버거·사냥용품점·주차장, 렌더러 10,709), `Demo_Lite`, `Overview`; `PolygonMapsPlaza/Scenes/Demo.unity`(도심 블록, 렌더러 11,424). 슈퍼마켓 구역은 Shops 데모의 `Mall_Downstairs_Props` 아래 (18, 1, -5) 부근, 진열대 25개 밀집.
- **다음(903)**: 데모의 슈퍼마켓 구역(약 8×9 m 진열대 블록 + 계산대)을 참고해 우리 규모(로비 2~3배, 6인)로 레이아웃 도면 작성. 창고(`Prop_Warehouse`)·하역장·사무실·화장실(Plaza의 Toilet 프리팹 세트) 존을 붙인다.

### 3. 조립 씬과 에디터 도구 (2026-09-10, 904)

- 조립 씬 `Assets/_Game/Content/Scenes/MartBuild.unity`. 데모 씬(쇼핑몰·플라자)에서 필요한 매장을 복사해 와 조립(루트 오브젝트 약 1만 개, 팩 프리팹 인스턴스). 씬 안에서 필요한 구역만 골라 온 것이라 원본 데모 씬은 그대로.
- **합쳐진 소품 분해** `Game/Match Map/Explode Merged Props…` (`MergedPropExplodeMenu.cs`): Synty의 Preset/Insert/Stacked 프리팹은 진열대+상품, 상자 더미가 한 메시라 플레이어가 개별 상품과 상호작용할 수 없다. 메시를 정점 위치 용접→연결 조각으로 나눠 팩의 개별 프리팹과 형태 매칭해 프리팹 인스턴스로 세운다.
  - 매칭: 회전 불변 지표(정점 수·중심 거리 분포)로 후보를 고르고 Y회전 탐색 → 실패 시 임의 3D 회전(PCA 초기값 + ICP). 여러 조각 프리팹(꽃다발=화분+꽃, 파인애플=과육+잎, 팔레트)은 가장 큰 조각으로 위치·회전을 잡고 나머지 조각이 예측 위치에 있는지 검증해 통째로 잡는다.
  - 팩에 없는 모양(인서트 전용 병·캔·상자)은 조각 지오메트리로 생성 프리팹을 만들어 `Assets/_Game/Content/MatchMap/GeneratedProps/`에 저장·재사용(BoxCollider 포함).
  - 결과는 `<원본>_Exploded/{Structure, Products}`. 구조물 판정: Products/Food 폴더는 상품, 생성 프리팹은 크기(0.6 m/0.03 m³), 팩 Props는 키워드(Stand·Shelf·Rack·Aisle·Checkout·Fridge·Freezer·Display·Pallet·Trolley·Cart·Counter·Table·Cabinet·Kiosk) 또는 1.2 m.
  - 일괄 처리는 `(Background)` 메뉴(에디터 틱마다 하나씩, 에디터가 멈추지 않음). `Revert All Exploded`로 되돌리기, `Reclassify`로 분류만 재적용.
  - **필수 설정**: Synty 모델 FBX의 Read/Write를 켰다(1,965개). 꺼진 상태에서 대량 처리하면 Unity가 CPU 메시 데이터를 해제해 카탈로그를 못 읽고 전부 생성 프리팹이 되는 사고가 있었다. 메시 콜라이더 조준(907)에도 필요.
  - 결과(씬 전체 243개): 상품 10,607개(팩 프리팹 1,684·생성 8,923), 구조물 1,287개, 생성 프리팹 511종. 꽃다발 54·파인애플 32·창고 상자 46은 팩 프리팹. 팔레트 스택은 판재로 부서져 제외(`Pallet` 이름). 양배추 일부는 크기 변형으로 미매칭 → 수동 처리.
  - 성능 주의: 활성 렌더러 약 2.4만 개. 906/907에서 상호작용 물건만 남기고 나머지는 Static 배칭.
- **인접 복제** `Game/Match Map/Duplicate Adjacent/…` (`DuplicateAdjacentMenu.cs`): 선택 조각을 로컬 축 방향으로 자기 크기만큼 옆에 복제(Alt+Shift+방향키, PgUp/PgDn). 천장·바닥 타일 붙이기용, 프리팹 연결 유지.
- **선택을 프리팹으로** `Game/Match Map/Make Prefab From Selection…` (`MakePrefabFromSelectionMenu.cs`, Ctrl+Shift+Alt+P): 선택 묶음을 `Assets/_Game/Content/Prefabs/Mart/<이름>.prefab`으로 저장·연결. 피벗은 바닥 중앙, 종류별(Modules/Env/Props) 하위 그룹 옵션.
- 로비 로우폴리 변환기(`LobbyLowPolyTestMenu.cs`)도 912 대비로 함께 보관.

### 4. 콜라이더·경계 1차 검증 (2026-09-10, 905)

- 플레이 구역: 사용자가 마트 구역을 벽으로 막아 둔 상태. 구역 안 기준점(-106.8, -14.7)에서 플레이어 크기(폭 0.4 m·높이 1.1 m 상자, 0.5 m 격자)로 걸어갈 수 있는 1층 바닥을 BFS로 탐색한 결과 **x[-133.5, -87.5] × z[-27, 9.5] (약 46×37 m, 905 m²)로 닫혀 있고 밖으로 새는 곳 없음**. 구역 안에 계단·에스컬레이터·사다리는 없음.
- 모듈 콜라이더: 구역 안 벽 347개 중 2개(서쪽 끝 x -133.65/-136.15)만 콜라이더가 없어 BoxCollider를 붙였고, 바닥 431·유리벽 133·문 10·기둥 62는 모두 콜라이더 있음. 천장은 구역 안에 5개(서쪽 x -136, 구역 밖)만 콜라이더 없음.
- 천장: 대부분 5.5~5.9 m, 일부 3.0 m, 열린 곳(하늘) 몇 군데. 낮은 벽(상단 2.0~2.9 m) 10개가 있어 선반(2.1 m) 위에서 넘어갈 수 있으므로 경계 콜라이더가 필수.
- **경계 콜라이더(외곽선 추종)**: 구역이 직사각형이 아니라(꺾임·돌출 있음) 큰 6면 상자 대신, 도달 영역 R의 바깥 셀 중 R에 인접한 "껍질" 셀(0.5 m)마다 상자를 놓고 행 단위로 이어 붙였다 → `MartEnvironment/Boundary` 아래 벽 상자 254개(높이 y −0.5~7) + 천장·바닥 판 2개. 벽 안쪽 면 바로 뒤에 붙으므로 사용자가 세운 벽과 같은 모양으로 막힌다(`mart-boundary-shell.png`). 재검증: 도달 영역 3,618셀 동일, 경계가 도달 영역을 침범한 셀 0.
  - 만드는 법(execute_code 스크립트, 메뉴화 예정): ① 기준점에서 걷기 BFS로 R ② 바운딩 박스 모서리에서 R이 아닌 셀로 BFS → 바깥 O(선반 등 내부 장애물은 O에 안 들어감) ③ O 중 R에 8방향 인접한 셀 = 껍질 ④ 행별 연속 셀을 한 BoxCollider로. 구역을 바꾸면 같은 절차로 다시 생성.
  - BoxCollider는 두께 있는 상자라 씬 뷰에 선이 두 줄 보이며, 막는 면은 안쪽 선이다.
- 콜라이더 수: 구역 안 6,779개(convex 메시 1,356·박스 5,400). Read/Write를 켠 뒤 상품 메시 콜라이더가 조준 광선에 잡히는 것을 확인(가격표 레이캐스트 OK) → 907에서 BoxCollider로 굳이 바꾸지 않아도 됨. 단 convex 메시 콜라이더 수천 개는 물리 비용이 있어 상호작용 없는 장식은 콜라이더 제거 검토.
- 남은 것(사용자 직접): 스폰 6·대기 스폰 6·파쇄기·탈출 지점 배치(909·908), 진열대 위 올라가기 정책, 통로 폭 확인.
