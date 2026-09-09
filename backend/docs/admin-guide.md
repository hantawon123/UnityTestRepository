# 관리 API 가이드

관리 화면을 만드는 사람을 위한 문서입니다. 게임 클라이언트 담당자는
[client-guide.md](client-guide.md) 를 보세요.

관리 화면은 이 서버가 `/admin` 에서 직접 서빙합니다
(`src/main/resources/static/admin/index.html`, `global/web/AdminPageConfig`). 화면을 고치는
사람은 그 파일을 수정하고, 화면이 부르는 API 는 아래를 따릅니다.

---

## 1. 이 API 는 다른 API 와 규칙이 다릅니다

| | 게임 API (`/api/v1/...`) | 관리 API (`/api/v1/admin/...`) |
| --- | --- | --- |
| 신원 | `X-User-Id` 헤더 (식별일 뿐) | 로그인 세션 쿠키 |
| CSRF | 없음 | **필요합니다** |
| 상태 | 무상태 | 세션 |

**게임 API 는 아무것도 바뀌지 않았습니다.** 유니티 클라이언트는 지금까지처럼 헤더만
보냅니다.

---

## 2. 로그인

```
POST /api/v1/admin/session
Content-Type: application/x-www-form-urlencoded

username=admin&password=...
```

**`application/json` 이 아니라 폼 형식입니다.** Spring Security 의 필터가 직접 받기
때문입니다. 로그인 처리를 직접 짜면 세션에 인증 정보를 저장하는 단계를 빠뜨리기 쉽고,
그러면 로그인은 성공인데 다음 요청이 401 이 됩니다. 그 함정을 피하려고 필터에 맡겼고,
대신 요청 형식이 폼으로 고정됩니다.

| 응답 | 뜻 |
| --- | --- |
| `204` | 성공. `JSESSIONID` 와 `XSRF-TOKEN` 쿠키가 함께 옵니다 |
| `401 BAD_CREDENTIALS` | 아이디나 비밀번호가 틀림 |

**아이디가 틀린 것과 비밀번호가 틀린 것을 구분해 주지 않습니다.** 구분하면 아이디를
하나씩 찔러 볼 수 있습니다.

### 로그인이 항상 401 이라면

서버에 계정이 설정되지 않았을 수 있습니다. 계정은 DB 가 아니라 서버 환경변수
(`ADMIN_USERNAME`, `ADMIN_PASSWORD`) 에 있고, **없으면 아무 계정도 등록되지 않습니다.**
그 경우 서버 로그 첫머리에 경고가 남습니다.

---

## 3. 이후 요청

`JSESSIONID` 쿠키는 브라우저가 알아서 붙입니다. 따로 할 일이 없습니다.

**상태를 바꾸는 요청(POST, PATCH, DELETE)에는 CSRF 토큰이 하나 더 필요합니다.**

```js
// XSRF-TOKEN 쿠키를 읽어 X-XSRF-TOKEN 헤더에 그대로 넣습니다.
fetch('/api/v1/admin/...', {
  method: 'PATCH',
  headers: { 'X-XSRF-TOKEN': readCookie('XSRF-TOKEN') },
})
```

쿠키와 헤더 **둘 다** 가야 합니다. 서버는 그 둘이 같은지 봅니다. 다른 사이트에서 온
요청은 쿠키는 실려도 값을 읽을 수 없어 헤더를 채우지 못하고, 그래서 걸러집니다.

빠뜨리면 `403` 입니다. 로그인이 풀린 것이 아니니 다시 로그인시키지 마세요 —
`401` 과 `403` 을 구분해서 다뤄야 합니다.

---

## 4. 로그인 상태 확인

```
GET /api/v1/admin/session
```

```json
{ "username": "admin" }
```

살아 있지 않으면 `401 UNAUTHORIZED` 입니다. 화면을 새로 그릴 때 이것으로 확인하세요.

**리다이렉트가 오지 않습니다.** 로그인 페이지로 넘기는 대신 `401` 로 답합니다.
리다이렉트는 요청하는 쪽에서 성공처럼 보여서, 화면이 로그인 폼 HTML 을 데이터로
받게 됩니다.

---

## 5. 로그아웃

```
POST /api/v1/admin/logout
X-XSRF-TOKEN: ...
```

`204` 입니다. 세션이 서버에서 즉시 끊깁니다.

---

## 6. 신고 검토

로그인한 세션으로 부릅니다. 세션이 없으면 `401 UNAUTHORIZED` 이고, 상태를 바꾸는 PATCH 에는
3절의 CSRF 헤더가 필요합니다(없으면 `403`).

신고를 접수하는 `POST /api/v1/reports` 는 게임 클라이언트가 인증 없이 부르는 반대 성격의
API 이고, 여기서 다루지 않습니다.

### 신고당한 사람 목록

```
GET /api/v1/admin/reports?status=PENDING
```

`status` 는 생략하면 `PENDING` 입니다. `ACTIONED` 나 `DISMISSED` 를 주면 이미 검토한 것을
봅니다. 지난 판단을 확인할 방법이 없으면 "그때 왜 기각했지"를 DB 를 열어야 알게 됩니다.

신고 한 건씩이 아니라 **사람 단위로 묶어서** 옵니다. 한 건씩 나열하면 같은 사람에 대한
다섯 건이 흩어져 나와, 그 사람이 문제인지 신고한 사람이 문제인지 구분되지 않습니다.

```json
{
  "users": [
    {
      "userId": "...",
      "nickname": "...",
      "reportCount": 7,
      "reporterCount": 5,
      "fromDeletedAccounts": 0,
      "reasons": { "ABUSE": 5, "SPAM": 2 },
      "lastReportedAt": "20260909120000"
    }
  ]
}
```

| 필드 | 뜻 |
| --- | --- |
| `userId` | 신고당한 사람의 공개 식별자. 상세 조회와 마무리에 씁니다 |
| `nickname` | 지금 닉네임. 바뀔 수 있으니 표시에만 씁니다 |
| `reportCount` / `reporterCount` | 건수와 신고한 사람 수. **함께 봐야 합니다.** "3건 1명"은 신고한 쪽이, "7건 5명"은 신고당한 쪽이 의심스럽습니다 |
| `fromDeletedAccounts` | 신고자가 탈퇴해 누구인지 알 수 없는 건수. `reporterCount` 에 섞지 않습니다 |
| `reasons` | 사유별 건수. 키는 `ABUSE`, `CHEATING`, `SPAM`, `INAPPROPRIATE_NAME`, `OTHER` |
| `lastReportedAt` | 가장 최근 신고 시각. `yyyyMMddHHmmss`, UTC |

없으면 `users` 가 빈 배열입니다.

### 한 사람의 신고 상세

```
GET /api/v1/admin/reports/{userId}
GET /api/v1/admin/reports/{userId}?status=PENDING
```

`status` 를 주면 그 상태만, 비우면 전부 봅니다. 목록의 사유 분포로 부족할 때 펼쳐 봅니다.
최근 순입니다.

```json
{
  "reports": [
    { "reason": "ABUSE", "memo": "...", "createdAt": "20260909120000", "status": "PENDING" }
  ]
}
```

`memo` 는 신고자가 적은 한 줄이고 없으면 `null` 입니다. 신고가 없는 사람은 빈 배열이고,
없는 계정은 `404 TARGET_NOT_FOUND` 입니다. 구분하지 않으면 오타로 부른 것과 정상 조회가
같아 보입니다.

### 마무리

```
PATCH /api/v1/admin/reports/{userId}
Content-Type: application/json
X-XSRF-TOKEN: ...

{ "status": "ACTIONED" }
```

그 사람의 **미검토 신고를 한 번에** 마무리합니다. 경로가 신고 번호가 아니라 사용자인
이유입니다 — 다섯 건 쌓인 사람을 다섯 번 누르게 할 이유가 없습니다.

`status` 는 `ACTIONED`(실제 문제였음) 또는 `DISMISSED`(조치할 것 없음) 입니다. 이미 검토한
신고는 그대로 두고, 검토 뒤에 새로 들어온 것만 미검토로 남아 다음 차례에 다시 올라옵니다.
누가 마무리했는지는 로그인 아이디로 남습니다.

```json
{ "reviewed": 3 }
```

`reviewed` 는 이번 요청이 마무리한 건수입니다. 처리할 것이 없어도 `200` 이고 `reviewed` 가
`0` 입니다. 두 사람이 같은 화면을 보다가 둘 다 눌렀을 때 뒤에 누른 쪽에 오류를 주면 무엇이
잘못됐는지 알 수 없는데, 원하는 결과는 이미 이루어져 있습니다.

| 응답 | 뜻 |
| --- | --- |
| `400 INVALID_REQUEST` | `status` 가 없거나, `PENDING` 이거나, 알 수 없는 값. `PENDING` 으로 되돌리는 것은 검토 취소라 막습니다 |
| `401 UNAUTHORIZED` | 로그인 세션이 없음 |
| `403` | CSRF 헤더가 없거나 쿠키와 다름 |
| `404 TARGET_NOT_FOUND` | 그 `userId` 의 계정이 없음 |

---

## 7. 피드백 읽기

```
GET /api/v1/admin/feedback
GET /api/v1/admin/feedback?limit=100
```

플레이어가 설정 화면에서 보낸 글입니다. 신고와 달리 **한 건씩 그대로** 옵니다. 신고는
"이 사람이 몇 번 신고당했나"가 판단 단위지만 피드백은 한 건 한 건이 읽을 내용이라,
묶으면 정작 본문이 사라집니다. 최근 순입니다.

```json
{
  "feedback": [
    {
      "userId": "...",
      "nickname": "...",
      "message": "숨는 시간이 너무 짧아요",
      "buildVer": "1.4.2",
      "platform": "WebGL",
      "createdAt": "20260909120000"
    }
  ]
}
```

| 필드 | 뜻 |
| --- | --- |
| `userId`·`nickname` | 쓴 사람. **탈퇴했으면 둘 다 `null`** 입니다. 내용은 남기고 작성자만 비웁니다 |
| `message` | 본문. 500자까지입니다 |
| `buildVer`·`platform` | 어느 빌드·환경에서 왔나. 클라이언트가 안 보냈으면 `null` |
| `createdAt` | 보낸 시각. `yyyyMMddHHmmss`, UTC |

`limit` 은 생략하면 50, 최대 200 입니다. **200 을 넘겨도 400 이 아니라 200 으로 깎입니다.**
큰 값을 넣는 상황은 "다 보고 싶다" 이고 거기에 오류를 주면 답이 되지 않습니다. 상한 자체를
두는 이유는 피드백이 지워지지 않고 쌓이기만 해서, 상한이 없으면 응답이 시간에 비례해
자라기 때문입니다.

### 읽음 표시가 없습니다

신고에는 검토 상태가 있지만 피드백에는 없습니다. 판단할 것이 없고 읽을 것만 있어서입니다.
읽음 표시를 두면 "읽음"이 "처리했음"처럼 읽히고, 실제로는 아무것도 하지 않은 채 목록만
깨끗해집니다.

**답장할 방법도 없습니다.** 보낸 사람에게 메일이나 알림을 보낼 경로가 없고, 클라이언트도
"답변을 드립니다" 라고 말하지 않습니다. 개선으로 답하는 것이 유일한 답입니다.

### 같은 말이 여러 건 보이면

같은 사람이 반복해 보낸 것과 여러 사람이 같은 지적을 한 것은 전혀 다른 신호입니다.
`userId` 로 구분하세요. 다만 **탈퇴한 사람들의 피드백은 전부 `null`** 이라 그 구분이
되지 않습니다. 신고 목록의 `fromDeletedAccounts` 와 같은 한계입니다.

---

## 8. 알고 있어야 할 것

**계정이 하나이고 팀이 공유합니다.** 누가 무엇을 했는지 구분할 수 없습니다. 사람마다
계정을 나눌 일이 생기면 그때 계정 테이블을 만들어야 합니다.

**로그인 시도 횟수를 제한하지 않습니다.** 실패는 서버 로그에 남지만 막지는 않습니다.
비밀번호를 길게 쓰세요.

**관리 화면은 같은 도메인 아래 두는 것을 전제로 합니다.** 지금은 이 서버가 `/admin` 으로
직접 서빙하므로 이미 그렇습니다. 다른 곳에 배포하면 쿠키가 교차 출처가 되어 CORS 와
`SameSite` 설정이 따로 필요합니다.
