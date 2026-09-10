# First 캐릭터 애니메이션 미리보기

`Assets/Scenes/CharacterTest.unity`를 열고 Play 하면 `FirstPlayerCapsule`에
아래 69개 동작이 연결됩니다. 화면 버튼, 숫자키, 좌우 화살표로 선택합니다. 목록이 길면 왼쪽 패널을 스크롤합니다.

| 번호 | 상태 | 반복 | 재생 구간 |
| --- | --- | --- | --- |
| 1 | `Idle` | O | 2초 |
| 2 | `Walk_Forward` | O | 0~24프레임 |
| 3 | `Walk_Back` | O | 0~24프레임 |
| 4 | `Walk_Left` | O | 0~24프레임 |
| 5 | `Walk_Right` | O | 0~24프레임 |
| 6 | `Run_Forward` | O | 0~19프레임 |
| 7 | `Run_Back` | O | 0~19프레임 |
| 8 | `Run_Left` | O | 0~19프레임 |
| 9 | `Run_Right` | O | 0~19프레임 |
| 10 | `Jump` | X | 0~32프레임 |
| 11 | `Fall` | O | 0~48프레임 |
| 12 | `Land` | X | 0~20프레임 |
| 13 | `Crouch_Idle` | O | 0~60프레임 |
| 14 | `Crouch_Walk_Forward` | O | 0~36프레임 |
| 15 | `Crouch_Start` | X | 0~24프레임 |
| 16 | `Crouch_End` | X | 0~24프레임 |
| 17 | `Crouch_Walk_Left` | O | 0~36프레임 |
| 18 | `Crouch_Walk_Right` | O | 0~36프레임 |
| 19 | `Crouch_Walk_Back` | O | 0~36프레임 |
| 20 | `Pickup_Low` | X | 0~60프레임 |
| 21 | `Pickup_Crouch` | X | 0~48프레임 |
| 22 | `Pickup_Prone` | X | 0~48프레임 |
| 23 | `Carry_Idle` | O | 0~60프레임 |
| 24 | `Carry_Walk_Forward` | O | 0~24프레임 |
| 25 | `Carry_Walk_Back` | O | 0~24프레임 |
| 26 | `Carry_Walk_Left` | O | 0~24프레임 |
| 27 | `Carry_Walk_Right` | O | 0~24프레임 |
| 28 | `Carry_Run_Forward` | O | 0~19프레임 |
| 29 | `Carry_Run_Back` | O | 0~19프레임 |
| 30 | `Carry_Run_Left` | O | 0~19프레임 |
| 31 | `Carry_Run_Right` | O | 0~19프레임 |
| 32 | `Carry_Crouch_Idle` | O | 0~60프레임 |
| 33 | `Carry_Crouch_Walk_Forward` | O | 0~36프레임 |
| 34 | `Carry_Crouch_Walk_Back` | O | 0~36프레임 |
| 35 | `Carry_Crouch_Walk_Left` | O | 0~36프레임 |
| 36 | `Carry_Crouch_Walk_Right` | O | 0~36프레임 |
| 37 | `Carry_Prone_Idle` | O | 0~60프레임 |
| 38 | `Carry_Crawl_Forward` | O | 0~36프레임 |
| 39 | `Carry_Crawl_Back` | O | 0~36프레임 |
| 40 | `Carry_Crawl_Left` | O | 0~36프레임 |
| 41 | `Carry_Crawl_Right` | O | 0~36프레임 |
| 42 | `PutDown_Low` | X | 0~60프레임 |
| 43 | `PutDown_Crouch` | X | 0~48프레임 |
| 44 | `PutDown_Prone` | X | 0~48프레임 |
| 45 | `Throw` | X | 0~24프레임 |
| 46 | `Prone_Start` | X | 0~24프레임 |
| 47 | `Prone_Idle` | O | 0~60프레임 |
| 48 | `Crawl_Forward` | O | 0~36프레임 |
| 49 | `Crawl_Back` | O | 0~36프레임 |
| 50 | `Crawl_Left` | O | 0~36프레임 |
| 51 | `Crawl_Right` | O | 0~36프레임 |
| 52 | `Prone_End` | X | 0~24프레임 |
| 53 | `Crouch_To_Prone` | X | 0~36프레임 |
| 54 | `Prone_To_Crouch` | X | 0~36프레임 |
| 55 | `Punch` | X | 0~24프레임 |
| 56 | `Punch_Walk` | X | 0~24프레임 |
| 57 | `Punch_Run` | X | 0~24프레임 |
| 58 | `Punch_Crouch` | X | 0~24프레임 |
| 59 | `Punch_Crouch_Walk` | X | 0~24프레임 |
| 60 | `Hit` | X | 0~30프레임 |
| 61 | `Hit_Walk` | X | 0~30프레임 |
| 62 | `Hit_Run` | X | 0~30프레임 |
| 63 | `Hit_Crouch` | X | 0~30프레임 |
| 64 | `Hit_Crouch_Walk` | X | 0~30프레임 |
| 65 | `Stun_Start` | X | 0~66프레임 |
| 66 | `Stun_Idle` | O | 0~60프레임 |
| 67 | `Stun_End` | X | 0~36프레임 |
| 68 | `Carry_Jump` | X | 0~32프레임 |
| 69 | `Carry_Land` | X | 0~20프레임 |

`Pickup_Crouch` / `Pickup_Prone` / `PutDown_Crouch` / `PutDown_Prone`은 해당 자세를 유지한 채 팔만 집기·내려놓기로 움직인다. `Tools/make_first_posture_pickup.py`로 베이크한다.

`Punch_*` / `Hit_*` 이동·자세 변형은 기본 펀치/피격 상체를 걷기·달리기·웅크리기에 올린 클립이다. `Tools/make_first_punch_locomotion.py`로 베이크한다.

`Carry_Land`는 `Land`에 `Carry_Idle` 오른손 홀드를 올린 클립이다. `Tools/make_first_carry_land.py`로 베이크한다.

동작명은 `{자세/모드}_{동작}_{방향?}`로 통일했다. `Stun_Idle`은 `Stun_Start` 마지막 자세에
`Belly_Breath`와 미세한 상체 들림으로 숨쉬기를 넣은 루프이다.
`Tools/make_first_stun_idle.py` / `Tools/make_first_stun_end.py`로 베이크한다.

```text
blender --background --python Tools/make_first_stun_idle.py -- Assets/Scenes/CharacterTest/First
blender --background --python Tools/make_first_stun_end.py -- Assets/Scenes/CharacterTest/First
blender --background --python Tools/make_first_carry_land.py -- Assets/Scenes/CharacterTest/First
blender --background --python Tools/make_first_punch_locomotion.py -- Assets/Scenes/CharacterTest/First
```

우클릭 드래그 또는 Q/E로 카메라를 회전하고, 휠로 줌, R로 카메라를 초기화합니다.

각 FBX는 First 캐릭터의 28-bone Generic 리그를 사용합니다. `Idle`에는
`Belly_Breath` BlendShape 애니메이션도 포함합니다. `CharacterTestPreviewSetup`이
압축 없는 Generic 임포트, 클립 루프, 컨트롤러 상태, 미리보기 버튼 목록을 설정합니다.

`Pickup_Low`는 제공된 Mixamo FBX의 손 경로만 사용합니다. 몸통과 하체는 기본 대기
포즈에 고정하고, 양팔·손가락만 앞쪽 물건을 잡는 자세로 움직입니다. 동작 끝은
`Carry_Idle`의 전방 들기 포즈와 이어집니다. 실제 물건은 `PlayerInteractor`의
`HoldPoint`에 붙으며, 배치·드롭·던질 때는 해당 부모 연결을 해제해 물리 상태로 돌립니다.
든 채 걷기·뛰기는 `Tools/make_first_carry_locomotion.py`로 만든다. 하체와 왼팔은
기존 이동 클립을 유지하고, `Shoulder.R` 이하만 `Carry_Idle`의 오른손 들기 포즈로 고정한다.
웅크리기·엎드리기 들기는 `Tools/make_first_carry_postures.py`다. 웅크리기는 기본 팔보다
오른팔을 조금 더 들고, 엎드리기는 왼손을 바닥에 둔 채 오른팔을 살짝 올린 다음 손을 옆으로 세운다.

```text
blender --background --python Tools/make_first_carry_locomotion.py -- CARRY.fbx OUT_DIR
blender --background --python Tools/make_first_carry_postures.py -- CARRY.fbx OUT_DIR
```

`PutDown_Low`는 `Pickup_Low`의 역동작이다. 게임 상호작용 연결 시 물건은 이 동작의
접촉 프레임에 HoldPoint에서 분리한다.

몸통 후속 보정은 `Tools/refine_throw_torso.py -- INPUT_DIR OUT_DIR`로 생성한다.
몸통 중앙의 팔뼈 가중치를 Spine으로 옮기고 상완 방향을 조금 위로 올린다.
공용 Idle와 Throw FBX를 함께 반영하며, 재실행 입력은 보정 전 파일을 사용한다.

손목 후속 보정은 `Tools/align_throw_wrist.py`로 베이크한다. 아래팔과 손의 축을
일치시키며 처음·마지막 3프레임만 기존 연결 자세로 보간한다. 다른 뼈의 동작과
메시 가중치는 유지한다. 실행 인자는 `-- INPUT_THROW.fbx OUT_DIR`이다.

`Throw`는 `Carry_Idle`에서 상완을 옆으로 열고, 상완·전완을 같이 올린 뒤
팔을 직선으로 펴서 던진다. 물건은 26프레임에서 놓으며 끝은 기본 대기다.

```text
blender --background --python Tools/make_first_throw.py -- IDLE.fbx CARRY.fbx OUT_DIR
```

엎드리기는 배를 바닥에 붙이고 손을 짚은 포복이다. Mixamo 소총 엎드리기 포즈는
쓰지 않는다. 다시 만들 때는 다음을 사용한다.

2026-09-08 수정: `Prone Forward.fbx`에서 확인한 무릎 로컬 X 굽힘축을 사용하고,
고관절의 벌림·바깥 회전과 분리했다. 좌우 다리가 번갈아 접히며, 뻗는 다리는
비틀림을 푼다. 17~23번은 동일한 엎드리기 기본 자세를 공유한다.
각지게 부풀던 `Crawl_Waist_Round`는 0으로 고정한다.
공용 Idle FBX의 몸통 아래쪽 허벅지 가중치는 골반으로 완만하게 분배했다
(`Tools/fix_crawl_hip_weights.py`, 원본에 한 번만 적용).
`Tools/check_crawl_pose.py`로 뼈 길이, 무릎 단일 굽힘축, 루프 연결을 검사한다.

서서 걷기 좌·우·후진은 `Walk_Forward`의 발 스윙을 방향 벡터로 재배치해
`Tools/make_first_walk_directions.py`로 만든다. 달리기는 `Run_Forward`를 같은 방식으로
`Tools/make_first_run_directions.py`에 넣는다. 얼굴과 발끝은 정면을 유지하고,
좌우 이동의 무릎은 바깥으로 밀지 않고 앞으로만 접는다. 좌우 걷기·달리기는 발이 가운데로
모이지 않게 각자 쪽 폭을 유지한다. `Run_Forward` 루프는
끝 프레임이 첫 프레임과 같아 Unity에서 한 프레임이 두 번 보이던 부분을 빼 0~19로 돈다.

```text
blender --background --python Tools/make_first_walk_directions.py -- WALK.fbx OUT_DIR
```

쭈그리기 가랑이 보정과 오리걸음은 `Tools/make_first_duck_walk.py`로 생성한다.
골반을 높인 가벼운 쭈그리기에서 발바닥 위치를 유지한다. 공용 Idle 메시의
`Crouch_Groin_Flat`은 호환용 이름이며, 평평한 바닥 보정 대신 볼륨 보존 스키닝을
역산해 둥근 엉덩이와 다리 두께를 보존한다. 네 방향 걷기는 두 다리 길이를 유지하며
좌우 체중 이동과 발 들기를 반복한다. 출력 Idle, Crouch Idle, 네 방향 Walk,
Stand 전환 FBX를 함께 교체하고 Prone 양방향 전환도 새 Crouch Idle로 재생성한다.
FBX 시간은 0부터 시작하며 1.2초 걷기 루프의 첫·끝 포즈를 일치시킨다.

`Crouch_To_Prone`과 `Prone_To_Crouch`는 각각 기존 `Crouch_Idle`과
`Prone_Idle`의 끝 포즈를 정확히 이어 주는 1.2초 전환 동작이다. `Tools/make_first_crouch_prone_transitions.py`로 베이크한다.

허벅지 양감/옆구리 후속 보정: `Crawl_Follow_L/R` BlendShape가 당기는 다리와
동기화되어 허벅지 단면의 눌림을 완화하고 같은 쪽 옆구리·배를 위로 이동시킨다.
뼈 애니메이션은 그대로 유지한다. 보정은 선형 스키닝을 역산해 FBX에 베이크하며,
`make_first_prone.py`가 출력하는 공용 Idle FBX도 함께 교체해야 한다.
`FirstCrawlCorrectivePostprocessor`는 비포복 클립에 0 곡선을 추가해 전환 후
보정이 남지 않게 한다. `render_crawl_cycle.py`는 보정 범위와 루프 초기화를 검사한다.

```text
blender --background --python Tools/make_first_prone.py -- IDLE.fbx "Prone Forward.fbx" OUT_DIR
```

승인된 First bake를 Unity용 FBX로 내보낼 때는 다음 스크립트를 사용합니다.

```text
blender --background --python Tools/export_first_idle_to_unity.py -- INPUT.blend OUTPUT.fbx
```

동작 원본과 제작 체크리스트는
[`docs/design/character/animation/animation-checklist.md`](../../../../docs/design/character/animation/animation-checklist.md)를 확인합니다.
