# First 캐릭터 애니메이션 미리보기

`Assets/Scenes/CharacterTest.unity`를 열고 Play 하면 `FirstPlayerCapsule`에
아래 23개 동작이 연결됩니다. 화면 버튼, 숫자키, 좌우 화살표로 선택합니다. 목록이 길면 왼쪽 패널을 스크롤합니다.

| 번호 | 상태 | 반복 | 재생 구간 |
| --- | --- | --- | --- |
| 1 | `Idle_Breathing` | O | 2초 |
| 2 | `Walk_Wide_Clean` | O | 0~24프레임 |
| 3 | `Run_SideArms` | O | 0~20프레임 |
| 4 | `Jump_Cute` | X | 0~41프레임 |
| 5 | `Fall_Flutter` | O | 0~48프레임 |
| 6 | `Land_Matched` | X | 0~20프레임 |
| 7 | `Crouch_Idle_KneesUp` | O | 0~60프레임 |
| 8 | `Crouch_Walk_Forward_KneesUp` | O | 0~36프레임 |
| 9 | `Stand_To_Crouch_KneesUp` | X | 0~24프레임 |
| 10 | `Crouch_To_Stand_KneesUp` | X | 0~24프레임 |
| 11 | `Crouch_Walk_Left_KneesUp` | O | 0~36프레임 |
| 12 | `Crouch_Walk_Right_KneesUp` | O | 0~36프레임 |
| 13 | `Crouch_Walk_Back_KneesUp` | O | 0~36프레임 |
| 14 | `Pickup_Low` | X | 0~60프레임 |
| 15 | `Carry_Idle` | O | 0~60프레임 |
| 16 | `PutDown_Low` | X | 0~60프레임 |
| 17 | `Prone_Start` | X | 0~24프레임 |
| 18 | `Prone_Idle` | O | 0~60프레임 |
| 19 | `Crawl_Forward` | O | 0~36프레임 |
| 20 | `Crawl_Back` | O | 0~36프레임 |
| 21 | `Crawl_Left` | O | 0~36프레임 |
| 22 | `Crawl_Right` | O | 0~36프레임 |
| 23 | `Prone_End` | X | 0~24프레임 |

우클릭 드래그 또는 Q/E로 카메라를 회전하고, 휠로 줌, R로 카메라를 초기화합니다.

각 FBX는 First 캐릭터의 28-bone Generic 리그를 사용합니다. `Idle_Breathing`에는
`Belly_Breath` BlendShape 애니메이션도 포함합니다. `CharacterTestPreviewSetup`이
압축 없는 Generic 임포트, 클립 루프, 컨트롤러 상태, 미리보기 버튼 목록을 설정합니다.

`Pickup_Low`는 제공된 Mixamo FBX의 손 경로만 사용합니다. 몸통과 하체는 기본 대기
포즈에 고정하고, 양팔·손가락만 앞쪽 물건을 잡는 자세로 움직입니다. 동작 끝은
`Carry_Idle`의 전방 들기 포즈와 이어집니다. 실제 물건은 `PlayerInteractor`의
`HoldPoint`에 붙으며, 배치·드롭·던질 때는 해당 부모 연결을 해제해 물리 상태로 돌립니다.
`PutDown_Low`는 `Pickup_Low`의 역동작이다. 게임 상호작용 연결 시 물건은 이 동작의
접촉 프레임에 HoldPoint에서 분리한다.

엎드리기는 배를 바닥에 붙이고 손을 짚은 포복이다. Mixamo 소총 엎드리기 포즈는
쓰지 않는다. 다시 만들 때는 다음을 사용한다.

```text
blender --background --python Tools/make_first_prone.py -- IDLE.fbx "Prone Forward.fbx" OUT_DIR
```

승인된 First bake를 Unity용 FBX로 내보낼 때는 다음 스크립트를 사용합니다.

```text
blender --background --python Tools/export_first_idle_to_unity.py -- INPUT.blend OUTPUT.fbx
```

동작 원본과 제작 체크리스트는
[`docs/design/character/animation/animation-checklist.md`](../../../../docs/design/character/animation/animation-checklist.md)를 확인합니다.
