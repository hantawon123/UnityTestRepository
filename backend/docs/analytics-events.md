# 플레이 로그 이벤트 명세 v1

Unity 클라이언트와 백엔드가 주고받는 행동 이벤트의 규약입니다.

**여기 있는 것** — 어떤 이벤트를 언제 누가 보내는지, 왜 그 이벤트가 필요한지.
**여기 없는 것** — 엔드포인트 스펙. 그건 [`openapi.json`](openapi.json)에 있고 코드에서 자동 생성됩니다.

저장 구조는 `d205_analytics.game_event` 한 테이블입니다. 게임 DB(`d205`)와 같은 MySQL
인스턴스를 쓰지만 스키마도 커넥션 풀도 분리되어 있어서, 이벤트 수집이 막혀도 게임 API는
영향을 받지 않습니다. 수집 코드는 같은 Spring 앱의 `domain/analytics` 패키지에 있습니다.
별도 서비스로 나누지 않은 이유와 나눌 시점은 지라 에픽 S15P21D205-780 에 있습니다.

---

## 1. 이 로그로 답하려는 질문

**이벤트는 이 목록에서 역산해서 만들었습니다.** 여기에 답하지 않는 이벤트는 넣지 않습니다.
"일단 다 찍어두고 나중에 보자"는 거의 항상 쓰이지 않습니다.

| # | 질문 | 왜 알아야 하나 |
| --- | --- | --- |
| 1 | 숨는 시간 기본 30초가 맞나 | 짧으면 못 숨긴 채 시작하고, 길면 지루하다 |
| 2 | 맵의 어느 구역이 안 쓰이나 | 안 쓰이는 구역은 만든 값을 못 한다 |
| 3 | 너무 좋거나 나쁜 은신처는 어디인가 | 아무도 못 찾는 자리가 있으면 게임이 성립하지 않는다 |
| 4 | 찾는 시간 기본 5분이 맞나 | 전체 플레이의 83%다. 여기가 비면 게임이 비는 것이다 |
| 5 | 기절 펀치 3회가 맞나 | 전투가 너무 빨리 끝나거나 너무 안 끝난다 |
| 6 | 조작이 학습되나 | 키가 12개다. 파티게임치고 많다 |
| 7 | 어느 단계에서 이탈하나 | 이탈 지점이 곧 가장 재미없는 구간이다 |

---

## 2. 공통 봉투

모든 이벤트가 아래 필드를 갖습니다. 각 이벤트가 따로 갖는 것은 `params`에만 넣습니다.

| 필드 | 타입 | 설명 |
| --- | --- | --- |
| `occurred_at` | epoch ms | **클라이언트 시각.** 조작될 수 있다. 3절 시각 규칙 참고 |
| `client_session_id` | uuid | **보낸 쪽**의 앱 실행 단위. 재시작하면 바뀐다. 호스트가 대신 보낸 이벤트면 호스트의 세션이다 |
| `client_seq` | int | 세션 안에서 0부터 단조 증가. **재전송 중복 제거의 키**다 |
| `room_code` | char(6) | Photon 방 코드. 방에 없으면 null. 매치 시작 전 로비 이탈을 묶는 유일한 열쇠다 |
| `match_id` | uuid | 경기 단위. 호스트가 만들어 전원에게 공유한다. 인게임 밖이면 null |
| `match_time_ms` | int | Fusion `ServerTime` 기준 경기 시각. **호스트 이벤트만** 채운다 |
| `user_public_id` | uuid | **행동의 주체.** `users.public_id`. 계정 발급 응답의 `userId`와 같은 값 |
| `event_name` | string | 5절 목록 중 하나 |
| `phase` | enum | `Waiting` / `Hiding` / `Searching` / `Highlight` / `Result`. 코드의 `MatchPhase` 이름 그대로. 인게임 밖이면 null |
| `map_id` | string | |
| `pos_x` `pos_y` `pos_z` | float | 이벤트가 일어난 좌표 |
| `from_host` | bool | 호스트(StateAuthority)가 보냈는지 |
| `schema_ver` | short | 현재 `1` |
| `params` | json | 이벤트별 추가 값 |

**`user_public_id`는 `users_seq`가 아닙니다.** 클라이언트는 `public_id`만 알고,
`users_seq`는 가입자 수가 드러나서 외부로 내보내지 않는 값입니다. FK 는 걸지 않습니다.

**`client_session_id`는 `user_presence.session_id`와 다른 것입니다.** 그쪽은 Photon 방
코드입니다. 이름을 다르게 둔 이유가 그것이고, 방 코드는 여기서 `room_code`입니다.

**`phase`에 FINAL 이 없습니다.** 코드에서 마지막 30초는 단계가 아니라 `Searching` 안의 구간
(`FinalWarningStartedEvent`)입니다. 그 경계는 `final_warning` 이벤트가 찍습니다.
`Highlight`와 `Result`를 빼지 않은 것은 질문 7(이탈 지점)이 그 구간에서도 일어나기 때문입니다.

**좌표는 `params`가 아니라 최상위 컬럼입니다.** JSON 안에 넣으면 히트맵 집계마다
`JSON_EXTRACT`가 붙어 인덱스를 못 씁니다. 이 로그의 핵심이 공간 분석이므로 여기서 타협하지 않습니다.

`received_at`은 클라이언트가 보내지 않습니다. **서버가 받은 시각을 따로 찍습니다.**

---

## 3. 시각 규칙

시각이 세 개 있고 셋은 쓰는 곳이 다릅니다.

| 시각 | 누가 찍나 | 어디에 쓰나 |
| --- | --- | --- |
| `occurred_at` | 클라이언트 벽시계 | 날짜별 집계, 파티션 키. **믿지 않는다** |
| `received_at` | 서버 | 수집 상태 확인. `occurred_at`과 크게 벌어지면 스풀 재전송이거나 시계가 틀린 것 |
| `match_time_ms` | 호스트의 Fusion `ServerTime` | **경기 안 시간 계산 전부.** "숨기기 시작 후 몇 초에 숨겼나"는 이 값으로 구한다 |

`occurred_at`은 UTC epoch ms 로 보내고 서버가 UTC `DATETIME(3)`으로 넣습니다. 기존 게임
테이블은 CHAR(14) UTC 문자열이고 JDBC URL 은 `serverTimezone=Asia/Seoul`이라, 규칙을 적어두지
않으면 KST 로 들어가는 행과 UTC 로 들어가는 행이 섞입니다. **`game_event`의 DATETIME 은 전부 UTC 입니다.**

서버는 `occurred_at`이 `지금 - 7일` 보다 이르거나 `지금 + 5분` 보다 늦은 이벤트가 하나라도
있으면 **그 배치를 400 으로 거부합니다.** 파티션 키가 클라이언트 값이라, 1970년 시각 하나가
들어오면 파티션이 없어 insert 가 실패하고 그 배치 전체가 죽습니다. 7일은 스풀이 묵을 수 있는
최대 기간을 넉넉히 잡은 것입니다.

경기 안 분석에 `occurred_at`을 쓰지 않는 이유가 하나 더 있습니다. `MatchSessionCoordinator`가
모든 판정을 `ServerTime` 기준으로 하므로, 같은 축으로 기록해야 "규칙상 30초"와 "로그상 30초"가
같은 30초가 됩니다.

---

## 4. 누가 보내는가 — 가장 중요한 규칙

Photon Fusion 이라 **이 규칙을 어기면 데이터가 통째로 못 쓰게 됩니다.**
6명이 각자 "라운드 종료"를 보내면 이벤트가 6개 생기고, 중간 이탈자가 있으면
6으로 나누는 보정조차 맞지 않습니다.

### StateAuthority(호스트)만 보낸다 — 경기 사실

단계 전환, 물건 배치·집기·파괴, 명중, 기절, 최종 판정, 위치 샘플.

호스트는 전원의 상태를 알고 있으므로 **다른 플레이어를 대신해서 보냅니다.**
이때 `user_public_id`는 그 행동의 주체이고, `from_host`는 `true`, `client_session_id`는
호스트의 세션입니다.

**호스트가 대신 보내려면 호스트가 전원의 `userId`를 알아야 합니다.** 지금은 모릅니다.
`PlayerRegistry.IdOf`는 Photon `PlayerRef` 기반의 `"P3"` 같은 값을 돌려주고, 연결 토큰에는
비밀번호와 닉네임만 실립니다. `PlayerAvatar`의 `Nickname`처럼 `UserId`를 `[Networked]`로
복제하는 작업(지라 S15P21D205-864)이 선행 조건입니다. 이게 없으면 호스트 발행
이벤트 전부의 `user_public_id`가 비어서 유저 단위 분석이 하나도 안 됩니다.

### 각 클라이언트가 보낸다 — 그 클라이언트만 아는 것

UI 클릭, 설정 변경, FPS·핑, 이탈, 조작 학습 지표, **펀치 휘두름**.

호스트가 알 수 없는 정보입니다. `from_host`는 `false`입니다.

펀치 휘두름이 여기 있는 이유: [PlayerCombatant](../../Assets/_Game/Client/Combat/PlayerCombatant.cs)는
레이캐스트가 사람을 맞췄을 때만 호스트에 `TryRequestHit`을 보냅니다. **빗나간 주먹은 호스트에
도달하지 않습니다.** 그래서 명중률은 클라이언트의 `punch_swung`과 호스트의 `punch_hit`을
유저·경기로 조인해서 구합니다. 빗나감을 호스트로 올리는 RPC 를 새로 만들지 않습니다.
분석 때문에 네트워크 트래픽을 늘리는 것은 순서가 뒤바뀐 것입니다.

### `from_host`를 왜 저장하나

규칙이 나중에 어긋나도 중복 제거가 가능하도록 남깁니다. 경기 사실 이벤트에
`from_host = false`가 섞여 있으면 그건 버그입니다.

### 호스트가 바뀌면 — 호스트 마이그레이션

**호스트가 나가도 경기는 끊기지 않습니다.** `MatchMigrationCheckpoint`로 새 호스트가
이어받습니다. 그래서 다음이 일어납니다.

- 한 `match_id` 안에서 `from_host = true`인 이벤트의 `client_session_id`가 중간에 바뀝니다.
- `match_id`가 체크포인트에 실려 있어야 새 호스트가 같은 값으로 이어 씁니다. **체크포인트에
  안 실으면 새 호스트가 새 경기를 시작한 것처럼 보입니다.**
- 경계에서 이벤트가 겹칠 수 있습니다. 옛 호스트가 `phase_change`를 보낸 직후 나갔고 새 호스트가
  복원하면서 같은 전환을 다시 찍는 경우입니다. 그래서 새 호스트는 복원 직후 `host_migrated`를
  한 번 찍고, 집계는 그 경기의 경계 전후를 의심합니다(6절).

마이그레이션 자체가 실패해 경기가 정말로 끊기면 `match_end`가 없는 `match_id`가 남습니다.
집계할 때 제외합니다.

---

## 5. 이벤트 목록 (19개)

### 5.1 호스트 발행 — 경기 사실 (11)

| 이벤트 | `params` | 비고 |
| --- | --- | --- |
| `match_start` | `map_id, player_count, hide_sec, seek_sec, run_speed, stun_hits, destroy_limit, players[]` | **방 설정을 전부 박는다.** 이게 없으면 "30초일 때 vs 60초일 때"를 비교할 수 없다. `players`는 `{seat, user_public_id}` 목록. 좌석↔유저 대응의 원본 |
| `phase_change` | `from, to` | 모든 단계 계산의 기준점. `to`는 `MatchPhase` 이름 |
| `final_warning` | `ends_in_ms` | Searching 마지막 30초 진입. 단계가 아니라 이벤트로 찍는다 |
| `host_migrated` | `previous_host_user_public_id` | 새 호스트가 복원 직후 한 번. 이 경기는 경계 중복을 의심한다 |
| `match_end` | `end_reason, duration_ms, escaped_count` | 이게 없는 경기는 분석 제외. `end_reason`은 `MatchEndReason` 이름 |
| `player_result` | `result, held_own_item` | 좌표 = 종료 시점 위치 |
| `item_hidden` | `item_id, owner_id` | 좌표 = 숨긴 위치. **질문 3의 핵심.** `TryRecordItemPlacement`가 신호 |
| `item_picked_up` | `item_id, owner_id, holder_id, hidden_ago_ms` | 좌표 = 집은 위치. `holder_id ≠ owner_id`면 발견/탈취, 같으면 되찾음. `TryHoldObject`가 신호 |
| `item_destroyed` | `item_id, owner_id, destroyer_id` | `PlayerItemDestroyed`가 신호 |
| `punch_hit` | `attacker_id, target_id, hits_so_far` | **명중만.** 빗나감은 클라이언트의 `punch_swung`. `RegisterHit`이 `Ignored`가 아닐 때 |
| `player_stunned` | `attacker_id, combat_ms, punches_landed` | `PlayerStunned`가 신호 |

발견·탈취·되찾음을 이벤트 셋으로 나누지 않고 `item_picked_up` 하나로 둔 이유: 코드에
그 셋을 구분하는 신호가 없습니다. 호스트 쪽 신호는 `TryHoldObject` 하나이고 구분은
`owner_id`와 `holder_id`의 관계에서 나옵니다. 쿼리에서 갈리는 것을 발행 시점에 갈라 두면
발행 지점 셋이 각자 어긋날 여지만 생깁니다.

`item_hidden`은 Hiding 단계와 Searching 단계의 재배치 양쪽에서 발생합니다. `phase`가 구분해 줍니다.

호스트 발행 지점은 `Server`(순수 C#) 계층에 둡니다. 위 신호들을 이미 받고 있는
`HighlightEventRecorder` 옆이 자연스러운 자리입니다.

### 5.2 호스트 발행 — 공간 (1)

| 이벤트 | 빈도 | 비고 |
| --- | --- | --- |
| `position_sample` | 1Hz × 인원수 | 호스트가 전원 위치를 알므로 호스트가 인원수만큼 발행 |

**각 클라이언트가 자기 위치를 보내면 안 됩니다.** 호스트가 어차피 다 알고 있어서
중복이고, 클라이언트마다 시각이 달라 같은 순간의 스냅샷이 만들어지지 않습니다.

전체 행의 약 85%가 이 이벤트입니다. **설정으로 끌 수 있어야 합니다.**

### 5.3 클라이언트 발행 (7)

| 이벤트 | `params` | 비고 |
| --- | --- | --- |
| `client_session_start` | `build_ver, platform, resolution` | |
| `scene_enter` | `scene` | `AppFlowState` 이름. 이탈 분석의 기반 |
| `client_quit` | `scene, reason` | `reason`은 `NORMAL` / `FORCED`. **질문 7의 핵심** |
| `first_interact` | `elapsed_since_phase_ms` | F키 첫 상호작용 성공까지. 세션당 1회 |
| `punch_swung` | `had_target` | 주먹을 휘두른 사실. `punch_hit`과 조인해 명중률 |
| `settings_changed` | `category, key` | **값은 넣지 않는다.** 무엇을 바꿨는지만 알면 된다 |
| `perf_sample` | `fps_avg, fps_p1, ping_ms` | 30초마다 |

`settings_changed`에서 값을 빼는 이유는 두 가지입니다. 노이즈가 크고,
설정값 중에 닉네임 공개 범위처럼 개인 정보에 가까운 것이 섞여 있습니다.
**"감도를 자주 바꾼다"는 사실만으로 기본값이 나쁘다는 신호는 충분합니다.**

### 5.4 질문 ↔ 이벤트

| 질문 | 답을 내는 방법 |
| --- | --- |
| 1 숨는 시간 | `phase_change(Hiding→Searching)`의 `match_time_ms`까지 `item_hidden`이 없는 인원 비율 |
| 2 죽은 구역 | `position_sample` 좌표 히트맵 |
| 3 은신처 품질 | `item_hidden` 좌표 bin 별 건수 × `item_picked_up.hidden_ago_ms` 중앙값 |
| 4 찾는 시간 | `item_picked_up`(`holder ≠ owner`)의 `match_time_ms` 분포 vs `match_start.seek_sec` |
| 5 기절 3회 | `punch_hit` ÷ `punch_swung` 명중률, `player_stunned.combat_ms` |
| 6 조작 학습 | `first_interact` 소요시간 추이, `settings_changed` 빈도 |
| 7 이탈 지점 | `scene_enter` → `client_quit`을 `phase` 별로. 로비 이탈은 `room_code`로 묶는다 |

---

## 6. 집계할 때 반드시 넣을 필터

```sql
-- 정상 종료된 경기만
WHERE match_id IN (
  SELECT match_id FROM game_event WHERE event_name = 'match_end'
)
```

마이그레이션이 실패해 끊긴 경기는 이벤트가 중간에 멎어 있습니다. 이걸 섞으면
"찾는 시간이 짧다" 같은 잘못된 결론이 나옵니다.

```sql
-- 호스트가 바뀐 경기. 경계 전후의 phase_change 중복을 의심한다
SELECT DISTINCT match_id FROM game_event WHERE event_name = 'host_migrated'
```

발행 규칙이 지켜지고 있는지 주기적으로 확인합니다.

```sql
-- 경기당 1건이어야 하는 이벤트가 여러 건이면 발행 책임 분리가 깨진 것.
-- host_migrated 가 있는 경기는 경계 중복이라 여기서 제외하고 따로 본다
SELECT match_id, event_name, COUNT(*)
FROM game_event
WHERE event_name IN ('match_start', 'match_end')
  AND match_id NOT IN (SELECT match_id FROM game_event WHERE event_name = 'host_migrated')
GROUP BY match_id, event_name
HAVING COUNT(*) > 1
```

재전송으로 생기는 중복은 여기서 걱정하지 않습니다. `UNIQUE (client_session_id, client_seq,
occurred_at)`이 DB 에서 막습니다(7절).

---

## 7. 전송 규약

클라이언트는 이벤트를 **모아서** 보냅니다. `POST /api/v1/events`에 배열로 넣습니다.

| 시점 | 동작 |
| --- | --- |
| 10초 경과 | flush |
| 50건 누적 | flush |
| 단계 전환 | **강제 flush** |
| 경기 종료 | **강제 flush** |
| 앱 종료 | **강제 flush** |

**이벤트마다 HTTP를 치지 않습니다.** 프레임이 튑니다.

**경기 종료 시 한 번에 몰아 보내지도 않습니다.** 질문 7이 "어느 단계에서 이탈하나"인데,
몰아 보내는 방식은 이탈한 사람이 애초에 전송하지 않고 나가므로 이탈자 데이터만
정확히 사라집니다. 지금 방식은 최악의 경우에도 마지막 10초치만 잃습니다.

**직렬화는 이벤트 발행 시점에** 합니다. flush 시점에 50건을 한꺼번에 직렬화하면
그 프레임이 튑니다. 큐에는 객체가 아니라 직렬화된 문자열이 쌓입니다.

전송에 실패하면 디스크에 스풀하고 다음 실행 때 재전송합니다. 시연 중 네트워크가
한 번 튀어서 그날 데이터가 통째로 날아가는 것을 막습니다. **플레이테스트 기회는 유한합니다.**

### 재전송은 중복을 만든다

202 를 받지 못한 배치가 실제로는 저장됐을 수 있습니다. 서버가 넣고 나서 응답이 끊기면
클라이언트는 실패로 보고 스풀에서 다시 보냅니다. 그래서

- 클라이언트는 이벤트마다 `client_seq`를 붙입니다. 세션 안에서 0부터 하나씩 늘고, 재전송해도
  **같은 값**입니다. 스풀에 쓸 때 이미 붙어 있어야 합니다.
- 서버는 `UNIQUE (client_session_id, client_seq, occurred_at)`에 `INSERT IGNORE`로 넣습니다.
  두 번 온 이벤트는 조용히 버려집니다.
- 유니크 키에 `occurred_at`이 들어간 것은 파티션 규칙 때문입니다. MySQL 은 파티션 키가 모든
  유니크 키에 포함돼야 하고, `received_at`은 재전송마다 달라져서 쓸 수 없습니다.

### 계층 위치

Unity 쪽 전송 구현은 `Backend` 계층에 둡니다. `Client`가 아닙니다. `UnityWebRequest`를
만지는 계층은 `Backend` 하나라는 것이 `_Game/README.md`의 규칙이고, 계정·친구·접속 상태가
그렇게 되어 있습니다. `Core`에는 `IAnalyticsSink` 포트와 순수 이벤트 타입만 둡니다.
`Server`는 `Core`만 보므로 호스트 발행 지점이 HTTP 를 모른 채 포트에 밀어 넣을 수 있습니다.

### 서버는 기다리지 않는다

서버는 받자마자 상한 큐에 넣고 `202`로 응답합니다. DB 쓰기를 기다리지 않습니다.
큐가 가득 차면 이벤트를 **버립니다.** 절대 블로킹하지 않습니다.

한 IP 가 분당 600요청을 넘기면 `429 RATE_LIMITED` 입니다. 클라이언트 하나는 10초마다 한 번
(분당 6)이라 혼자서는 닿지 않습니다. 상한이 600 인 이유는 플레이테스트가 한 교실에서
이뤄져 수십 명이 NAT 뒤의 공용 IP 하나로 보이기 때문입니다. 100명이 한 IP 뒤에 있어도
닿지 않고, 한 IP 가 무한히 쏘는 것만 막습니다. 429 는 400 과 달리 **재전송합니다.** 배치를
스풀에 그대로 두고 다음 flush 에 다시 보냅니다.

서버가 배치를 거부하는 경우(400)는 `event_name`이 목록에 없을 때, 배열이 상한을 넘을 때,
`occurred_at`이 3절의 범위를 벗어날 때입니다. 클라이언트는 400 을 받은 배치를 재전송하지
않고 버립니다. 다시 보내도 같은 답이 옵니다.

---

## 8. 개인정보

- **닉네임, 채팅 내용, 음성은 로깅하지 않습니다.** `user_public_id`만 넣습니다
- 설정 이벤트에 설정 *값*을 넣지 않습니다
- 분석 계정은 `SELECT` 권한만 갖습니다

### 탈퇴하면 지운다

게임 DB 는 탈퇴 시 CASCADE 로 그 사람의 행을 전부 지우고, `room_invites` 주석은 그것을
"탈퇴는 흔적을 남기지 않는다"는 약속으로 적어 두었습니다. `game_event`는 FK 가 없어서
그냥 두면 `user_public_id`가 남습니다. **같은 약속을 여기에도 적용합니다.**

탈퇴 처리가 `UPDATE game_event SET user_public_id = NULL WHERE user_public_id = ?`를 분석
DataSource 로 실행합니다. 행은 남고 사람만 지워집니다. 경기 집계는 "누군가 여기 숨겼다"만
알면 되고 그게 누구였는지는 필요 없으므로, 행을 지우면 남은 다섯 명의 경기가 뒤틀리고
사람만 지우면 아무것도 뒤틀리지 않습니다.

`params` 안의 `owner_id`, `attacker_id` 같은 값도 같은 사람을 가리킵니다. 그것까지 훑어
바꾸는 것은 JSON 갱신이라 비싸고, 그 값들은 `match_start.players`로만 유저와 이어지므로
최상위 컬럼이 NULL 이 된 순간 이미 끊깁니다. 최상위 컬럼만 지웁니다.

**best-effort 입니다.** 분석 DB 가 죽어 있어도 탈퇴는 성공해야 합니다. 실패하면 로그만
남기고, 그 로그를 보고 사람이 나중에 같은 UPDATE 를 실행합니다. 탈퇴를 막는 것보다 그 편이
약속에 가깝습니다. 사용자 입장에서 탈퇴는 이미 됐고, 남은 것은 우리가 치울 일입니다.

`ix_game_event_match` 도 `ix_game_event_name` 도 `user_public_id`로 시작하지 않아 이 UPDATE 는
풀 스캔입니다. 탈퇴는 드물고 테이블이 수백만 행이라도 수 초라 인덱스를 따로 두지 않습니다.
탈퇴가 잦아지거나 행이 수천만이 되면 그때 `(user_public_id)` 인덱스를 추가합니다.

---

## 9. 버전과 미확정 항목

`schema_ver`는 현재 `1`입니다. 필드를 추가할 때는 올리고, 집계 쿼리에서 버전을
구분합니다. **이벤트를 지우기보다 새 이름으로 추가하는 편이 안전합니다.**

### 확정 대기

승리 조건이 절대평가(종료 시 자기 물건을 들고 있으면 성공)라 순위·점수 개념이 없습니다.
이 규칙이 바뀌면 아래 두 이벤트의 `params`가 바뀝니다.

- `player_result`의 `result`
- `match_end`의 `escaped_count`

**나머지 17개와 공통 봉투는 이 변경과 무관합니다.** 파이프라인 구현을 먼저 진행하고
이 둘만 나중에 확정합니다.

### 열려 있는 결정

- `position_sample`을 1Hz로 켤지. 켜면 행 수가 약 6배가 됩니다(경기당 450 → 2,600).
  질문 2(맵의 죽은 구역)는 이것 없이 답할 수 없습니다
