# 캐릭터 애니메이션 (Animation)

리깅·리타겟·액션 제작 현황을 둡니다.

- 파일명은 영문 소문자와 하이픈. 예: `animation-checklist.md`
- `.blend`는 `source/blender/characters/`에 두고, 이 폴더에는 파일명·액션명만 적습니다.
- 스크린샷이 생기면 같은 폴더에 두고 상대경로로 링크합니다.
- 제작·검수 상태는 `animation-checklist.md`를 봅니다.

## 필요한 액션

할당된 Jira 스토리 기준으로 게임에 들어가야 하는 클립입니다.

### 기본 이동 — [S15P21D205-646](https://ssafy.atlassian.net/browse/S15P21D205-646)

제자리 대기, WASD 네 방향 걷기·달리기.

| 액션명 | 용도 |
| --- | --- |
| `Idle_Breathing` | 제자리 숨쉬기 대기 |
| `Walk_Wide_Clean` | 걷기 앞 |
| `Walk_Back` | 걷기 뒤 |
| `Walk_Left` | 걷기 왼쪽 |
| `Walk_Right` | 걷기 오른쪽 |
| `Run_SideArms` | 달리기 앞 |
| `Run_Back` | 달리기 뒤 |
| `Run_Left` | 달리기 왼쪽 |
| `Run_Right` | 달리기 오른쪽 |

### 점프·낙하 — [S15P21D205-652](https://ssafy.atlassian.net/browse/S15P21D205-652)

도약 → 공중·낙하 → 착지.

| 액션명 | 용도 |
| --- | --- |
| `Jump_Start` | 도약 |
| `Fall_Flutter` | 공중 유지·낙하 |
| `Land_Matched` | 착지 |

### 웅크리기 — [S15P21D205-661](https://ssafy.atlassian.net/browse/S15P21D205-661)

앉기 키로 몸을 낮추고, 웅크린 채 대기·네 방향 이동 후 일어서기.

| 액션명 | 용도 |
| --- | --- |
| `Stand_To_Crouch_KneesUp` | 웅크리기 진입 |
| `Crouch_Idle_KneesUp` | 웅크린 대기 |
| `Crouch_Walk_Forward_KneesUp` | 웅크린 전진 |
| `Crouch_Walk_Back_KneesUp` | 웅크린 후진 |
| `Crouch_Walk_Left_KneesUp` | 웅크린 좌측 |
| `Crouch_Walk_Right_KneesUp` | 웅크린 우측 |
| `Crouch_To_Stand_KneesUp` | 일어서기 |

### 엎드리기·기어가기 — [S15P21D205-677](https://ssafy.atlassian.net/browse/S15P21D205-677)

엎드리기 키로 눕고, 엎드린 채 대기·네 방향 기어가기 후 일어나기.
배는 바닥에 붙이고 손을 짚은 포복으로 만든다. 소총을 겨누는 Mixamo 엎드리기는 쓰지 않는다.

| 액션명 | 용도 |
| --- | --- |
| `Prone_Start` | 엎드리기 진입 |
| `Prone_Idle` | 엎드린 대기 |
| `Crawl_Forward` | 기어가기 앞 |
| `Crawl_Back` | 기어가기 뒤 |
| `Crawl_Left` | 기어가기 왼쪽 |
| `Crawl_Right` | 기어가기 오른쪽 |
| `Prone_End` | 일어나기 |

### 공격 — [S15P21D205-685](https://ssafy.atlassian.net/browse/S15P21D205-685)

서서, 점프 중, 웅크린 채로 때리기.

| 액션명 | 용도 |
| --- | --- |
| `Attack` | 서서 때리기 |
| `Attack_Air` | 점프 중 때리기 |
| `Attack_Crouch` | 웅크린 채 때리기 |

### 피격·기절 — [S15P21D205-697](https://ssafy.atlassian.net/browse/S15P21D205-697)

가벼운·강한 피격. 기절하면 쓰러진 채 유지되다가 다시 일어나기.

| 액션명 | 용도 |
| --- | --- |
| `Hit_Light` | 가벼운 피격 |
| `Hit_Heavy` | 강한 피격 |
| `Stun_Start` | 기절 진입 |
| `Stun_Idle` | 기절 유지 |
| `Stun_End` | 기절 회복·일어나기 |

### 물건 집기·내려놓기 — [S15P21D205-710](https://ssafy.atlassian.net/browse/S15P21D205-710)

낮은 곳, 허리 높이, 높은 곳에서 집어 들고 같은 높이로 내려놓기.

| 액션명 | 용도 |
| --- | --- |
| `Pickup_Low` | 낮은 곳 집기 |
| `Pickup` | 허리 높이 집기 |
| `Pickup_High` | 높은 곳 집기 |
| `PutDown_Low` | 낮은 곳 내려놓기 |
| `PutDown` | 허리 높이 내려놓기 |
| `PutDown_High` | 높은 곳 내려놓기 |

### 물건 들고 이동·던지기 — [S15P21D205-718](https://ssafy.atlassian.net/browse/S15P21D205-718)

든 채 대기·기존 이동과 조합, 좌클릭으로 던지기.

| 액션명 | 용도 |
| --- | --- |
| `Carry_Idle` | 물건 들고 대기 |
| `Carry_Upper` | 서서 이동할 때 상체 들기 자세 |
| `Carry_Crouch` | 웅크린 채 들기 |
| `Carry_Prone` | 엎드린 채 들기 |
| `Throw` | 던지기 |

### 물건 들기 연결 규칙

- `Pickup_Low`는 물건을 손에 붙이는 프레임을 기준으로, 앞뒤 몸통 흔들림을 작게 유지한다.
- 집기 완료 시 `Carry_Idle`로 전환한다. 서서 걷기·뛰기는 `Carry_Upper`를 상체 레이어로
  겹치고, 웅크리기·엎드리기는 각각 `Carry_Crouch`, `Carry_Prone`을 사용한다.
- 게임 연결 시 배치와 일반 드롭은 `PutDown_*`의 물건 접촉 프레임에서 HoldPoint 부모
  연결을 해제한다. 드롭은 해당 위치에서 물리를 켜며, 배치는 배치 미리보기에서 확정한
  위치·회전으로 해제한다.

## 목록

- `animation-checklist.md` — 플레이어 액션 제작·검수 체크리스트
