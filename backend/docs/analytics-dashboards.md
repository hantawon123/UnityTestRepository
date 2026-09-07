# 플레이 로그 대시보드 쿼리 (812)

Metabase 의 "우리의 분석" 컬렉션에 들어갈 다섯 화면의 SQL 입니다. 데이터베이스는 "플레이 로그"
(`d205_analytics`)를 고르고 **SQL 질문**으로 붙여 넣습니다. 각 질문이 답하는 것은
[`analytics-events.md`](analytics-events.md) 1절의 번호를 따릅니다.

**여기 있는 것** — 쿼리와 읽는 법. **여기 없는 것** — 맵 그림 위에 겹치는 히트맵. 그건 Metabase 로
안 되고 데이터가 며칠 쌓인 뒤 따로 만듭니다(812 "다음 주").

쿼리는 수집 부하 테스트가 남긴 합성 데이터(`docs/analytics-load-test.md`)로 문법과 결과 형태를
확인했습니다. 합성 데이터에는 `match_start`·`match_end`·`client_quit` 이 없어서 그 셋에 기대는
화면은 결과가 비는 것까지만 확인했습니다. 실제 데이터가 들어오면 값의 뜻을 다시 봐야 합니다.

---

## 공통: 정상 종료된 경기만

경기 단위 집계는 전부 이 필터를 깔고 갑니다. `match_end` 가 없는 경기는 호스트 마이그레이션이
실패해 중간에 끊긴 것이라 섞으면 "찾는 시간이 짧다" 같은 잘못된 결론이 나옵니다.

```sql
-- 다른 쿼리에서 WITH 절로 재사용합니다.
SELECT DISTINCT match_id
FROM game_event
WHERE event_name = 'match_end'
```

호스트가 바뀐 경기(`host_migrated`)는 경계에서 `phase_change` 가 중복될 수 있습니다. 경기당 1건이어야
하는 이벤트를 세는 쿼리는 그 경기를 따로 봅니다(`analytics-events.md` 6절).

---

## 1. 숨는 시간 적정성 (질문 1)

숨기기 단계가 끝날 때 **아직 물건을 안 숨긴 인원의 비율**을 방 설정(`hide_sec`)별로 봅니다.
비율이 높으면 30초가 짧은 것이고, 0 에 가까우면 줄여도 됩니다.

```sql
WITH finished AS (
    SELECT DISTINCT match_id FROM game_event WHERE event_name = 'match_end'
),
setup AS (
    SELECT match_id,
           CAST(JSON_EXTRACT(params, '$.player_count') AS UNSIGNED) AS player_count,
           CAST(JSON_EXTRACT(params, '$.hide_sec')     AS UNSIGNED) AS hide_sec
    FROM game_event
    WHERE event_name = 'match_start'
),
hid AS (
    -- Hiding 단계 안에서 한 번이라도 숨긴 사람. Searching 중 재배치는 세지 않습니다.
    SELECT match_id, COUNT(DISTINCT user_public_id) AS hidden_players
    FROM game_event
    WHERE event_name = 'item_hidden' AND phase = 'Hiding'
    GROUP BY match_id
)
SELECT s.hide_sec                                                        AS `숨는 시간(초)`,
       COUNT(*)                                                          AS `경기 수`,
       ROUND(AVG(1 - COALESCE(h.hidden_players, 0) / s.player_count) * 100, 1) AS `못 숨긴 인원 %`
FROM setup s
JOIN finished f USING (match_id)
LEFT JOIN hid h USING (match_id)
GROUP BY s.hide_sec
ORDER BY s.hide_sec
```

**시각화**: 막대. x = 숨는 시간, y = 못 숨긴 인원 %. 경기 수가 5 미만인 막대는 믿지 마세요.

---

## 2. 찾는 시간 적정성 (질문 4)

남의 물건을 **찾은 시각의 분포**입니다. `match_time_ms` 는 경기 시작 기준이므로 Searching 시작
시각(`hide_sec × player_count`)을 빼서 "찾기 시작 후 몇 분"으로 바꿉니다. 분포가 `seek_sec` 끝에
몰려 있으면 5분이 짧고, 앞쪽에 몰려 있고 뒤가 비면 길다는 뜻입니다.

```sql
WITH finished AS (
    SELECT DISTINCT match_id FROM game_event WHERE event_name = 'match_end'
),
setup AS (
    SELECT match_id,
           CAST(JSON_EXTRACT(params, '$.player_count') AS UNSIGNED) AS player_count,
           CAST(JSON_EXTRACT(params, '$.hide_sec')     AS UNSIGNED) AS hide_sec,
           CAST(JSON_EXTRACT(params, '$.seek_sec')     AS UNSIGNED) AS seek_sec
    FROM game_event
    WHERE event_name = 'match_start'
),
found AS (
    SELECT e.match_id,
           (e.match_time_ms / 1000 - s.hide_sec * s.player_count) AS seek_elapsed_sec,
           s.seek_sec
    FROM game_event e
    JOIN setup s USING (match_id)
    JOIN finished f USING (match_id)
    WHERE e.event_name = 'item_picked_up'
      AND JSON_UNQUOTE(JSON_EXTRACT(e.params, '$.holder_id')) <> JSON_UNQUOTE(JSON_EXTRACT(e.params, '$.owner_id'))
)
SELECT FLOOR(seek_elapsed_sec / 30) * 30 AS `찾기 시작 후(초)`,
       COUNT(*)                          AS `발견 건수`,
       MIN(seek_sec)                     AS `설정된 찾는 시간(초)`
FROM found
WHERE seek_elapsed_sec >= 0
GROUP BY FLOOR(seek_elapsed_sec / 30) * 30
ORDER BY 1
```

**시각화**: 막대(히스토그램). 30초 구간. 방 설정이 여러 종류면 `seek_sec` 로 필터를 하나 두세요.

---

## 3. 은신처 분포 (질문 2·3)

숨긴 위치를 **2m 격자**로 묶어 건수와, 그 자리에 숨긴 물건이 **발견되기까지 걸린 시간**을 붙입니다.
건수가 많고 발견까지 오래 걸리는 칸이 "너무 좋은 자리", 아무도 안 쓰는 칸이 "죽은 구역"입니다.

```sql
WITH finished AS (
    SELECT DISTINCT match_id FROM game_event WHERE event_name = 'match_end'
),
hidden AS (
    SELECT e.match_id, e.map_id,
           JSON_UNQUOTE(JSON_EXTRACT(e.params, '$.item_id')) AS item_id,
           FLOOR(e.pos_x / 2) * 2 AS gx,
           FLOOR(e.pos_z / 2) * 2 AS gz
    FROM game_event e
    JOIN finished f USING (match_id)
    WHERE e.event_name = 'item_hidden'
),
found AS (
    SELECT match_id,
           JSON_UNQUOTE(JSON_EXTRACT(params, '$.item_id')) AS item_id,
           CAST(JSON_EXTRACT(params, '$.hidden_ago_ms') AS UNSIGNED) / 1000 AS hidden_for_sec
    FROM game_event
    WHERE event_name = 'item_picked_up'
      AND JSON_UNQUOTE(JSON_EXTRACT(params, '$.holder_id')) <> JSON_UNQUOTE(JSON_EXTRACT(params, '$.owner_id'))
)
SELECT h.map_id                             AS `맵`,
       h.gx                                 AS `x(2m)`,
       h.gz                                 AS `z(2m)`,
       COUNT(*)                             AS `숨긴 횟수`,
       COUNT(f.item_id)                     AS `발견된 횟수`,
       ROUND(AVG(f.hidden_for_sec), 0)      AS `발견까지 평균(초)`,
       ROUND(100 * (1 - COUNT(f.item_id) / COUNT(*)), 0) AS `끝까지 안 들킨 %`
FROM hidden h
LEFT JOIN found f USING (match_id, item_id)
GROUP BY h.map_id, h.gx, h.gz
ORDER BY `숨긴 횟수` DESC
```

**시각화**: 표로 시작. Metabase 의 피벗(행 z, 열 x, 값 숨긴 횟수)이 맵 위 히트맵의 임시 대용입니다.
`끝까지 안 들킨 %` 가 높으면서 `숨긴 횟수` 도 많은 칸을 먼저 보세요. MySQL 에 중앙값 함수가 없어
평균을 썼습니다. 극단값이 걱정되면 Metabase 의 필터로 `hidden_for_sec` 상한을 두세요.

---

## 4. 이탈 지점 (질문 7)

**정상 종료가 아닌 이탈**(`FORCED`)이 어느 단계·어느 화면에서 일어나는지 셉니다. 매치 전 로비에서의
이탈은 `phase` 가 NULL 이고 `scene` 으로 구분합니다.

```sql
SELECT COALESCE(phase, JSON_UNQUOTE(JSON_EXTRACT(params, '$.scene')), '(모름)') AS `어디서`,
       JSON_UNQUOTE(JSON_EXTRACT(params, '$.reason'))                         AS `이유`,
       COUNT(*)                                                                AS `건수`,
       COUNT(DISTINCT user_public_id)                                          AS `사람 수`
FROM game_event
WHERE event_name = 'client_quit'
  AND occurred_at >= NOW() - INTERVAL 7 DAY
GROUP BY 1, 2
ORDER BY `건수` DESC
```

**시각화**: 막대(`어디서` × `이유`). `FORCED` 가 `Searching` 에 몰리면 찾는 단계가 지루한 것이고,
`Lobby` 에 몰리면 매치가 안 잡혀 나가는 것입니다. `건수` 와 `사람 수` 를 나눠 보는 이유는 한 명이
반복해서 튕기는 것과 여러 명이 나가는 것이 다른 문제이기 때문입니다.

`scene_enter` 와 짝지어 단계별 도달 인원 대비 이탈률을 내는 것이 다음 단계인데, 그건 실제
데이터를 보고 만듭니다.

---

## 5. 수집 상태

파이프라인이 살아 있는지 보는 화면입니다. 값이 아니라 **끊김**을 보는 것이라 대시보드 맨 위에 둡니다.

```sql
SELECT DATE_FORMAT(received_at, '%Y-%m-%d %H:00') AS `시각(UTC)`,
       COUNT(*)                                    AS `이벤트`,
       COUNT(DISTINCT client_session_id)           AS `세션`,
       SUM(event_name = 'position_sample')         AS `위치 샘플`,
       COUNT(*) - SUM(event_name = 'position_sample') AS `그 외`
FROM game_event
WHERE received_at >= NOW() - INTERVAL 24 HOUR
GROUP BY 1
ORDER BY 1
```

**시각화**: 선(`이벤트`, `그 외`). 플레이테스트 시간대에 봉우리가 없으면 클라이언트가 안 보내고 있는
것입니다. `위치 샘플` 이 전체의 85% 안팎이면 정상입니다.

`received_at` 은 UTC 라 화면 시각이 KST 보다 9시간 이릅니다. Metabase 의 보고 시간대 설정
(관리자 → 설정 → 지역화)을 Asia/Seoul 로 두면 Metabase 가 변환해 보여 줍니다.

큐에서 버린 개수는 DB 에 없습니다. 앱 로그의 "플레이 로그 지난 1분" WARN 이 유일한 창입니다.

```
ssh d205 "docker logs d205-app --since 24h 2>&1 | grep '플레이 로그 지난 1분' | tail -20"
```

---

## 만들 때 순서

1. Metabase 홈 → "새로운" → "SQL 질문" → 데이터베이스 "플레이 로그".
2. 위 쿼리를 붙이고 실행. 결과 형태가 맞으면 시각화를 고르고 "우리의 분석" 컬렉션에 저장.
3. 다섯 개를 저장한 뒤 "새로운" → "대시보드" 로 묶습니다. 5번(수집 상태)을 맨 위에.
4. 무거운 쿼리는 게임과 같은 MySQL 을 때립니다. 대시보드 자동 새로 고침은 켜지 않습니다.
