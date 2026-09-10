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
