"""플레이 로그 기본 대시보드를 Metabase 에 만듭니다 (S15P21D205-812).

`docs/analytics-dashboards.md` 의 다섯 절에서 SQL 을 읽어 Metabase 질문 5개와 그것을 묶은
대시보드 하나를 만듭니다. SQL 을 스크립트에 복사해 두지 않고 문서에서 읽는 이유는, 두 곳에
같은 쿼리가 있으면 반드시 한쪽만 고쳐지기 때문입니다. 문서가 원본이고 스크립트는 시각화
종류와 대시보드 배치만 압니다.

표준 라이브러리만 씁니다. 운영 서버에 설치할 것이 없어야 합니다.

    scp backend/docs/analytics-dashboards.md d205:/tmp/analytics-dashboards.md
    scp backend/deploy/metabase/provision_dashboards.py d205:/tmp/provision_dashboards.py
    ssh -t d205 'MB_USER=<Metabase 관리자 이메일> python3 /tmp/provision_dashboards.py --doc /tmp/analytics-dashboards.md'

서버에서 `127.0.0.1:3000` 으로 붙습니다. nginx 의 8443(Basic Auth)과 인증서를 지나지 않아도
되고 Metabase 관리자 계정만 있으면 됩니다. 비밀번호는 `MB_PASS` 로 주거나, 없으면 물어봅니다.

여러 번 돌려도 안전합니다. 컬렉션·질문·대시보드를 이름으로 찾아 있으면 내용을 갱신하고 없으면
만듭니다. 사람이 Metabase 화면에서 고친 시각화 설정은 스크립트가 가진 값으로 되돌아갑니다.

데이터가 없어도 화면은 만들어집니다. 클라이언트 발행(797·799)이 붙기 전에는 결과가 빈 것이
정상이고, 그 상태로 화면을 먼저 두는 것이 이 작업의 목적입니다.
"""
import argparse
import getpass
import json
import os
import re
import sys
import urllib.error
import urllib.request

DASHBOARD_NAME = "플레이 로그 기본 대시보드"
DASHBOARD_DESCRIPTION = (
    "플레이테스트가 답해야 하는 질문 다섯 개. 쿼리와 읽는 법은 backend/docs/analytics-dashboards.md 에 있습니다."
)

# 문서의 절 번호 -> 시각화. 컬럼 이름은 문서의 SQL 이 붙인 별칭과 같아야 합니다.
# 대시보드 배치(row, col, size_x, size_y)의 가로 눈금은 24 칸입니다.
VIEWS = {
    "1": {
        "display": "bar",
        "settings": {"graph.dimensions": ["숨는 시간(초)"], "graph.metrics": ["못 숨긴 인원 %"]},
        "layout": {"row": 6, "col": 0, "size_x": 12, "size_y": 7},
    },
    "2": {
        "display": "bar",
        "settings": {"graph.dimensions": ["찾기 시작 후(초)"], "graph.metrics": ["발견 건수"]},
        "layout": {"row": 6, "col": 12, "size_x": 12, "size_y": 7},
    },
    "3": {
        "display": "table",
        "settings": {},
        "layout": {"row": 13, "col": 0, "size_x": 24, "size_y": 10},
    },
    "4": {
        "display": "bar",
        "settings": {"graph.dimensions": ["어디서", "이유"], "graph.metrics": ["건수"]},
        "layout": {"row": 23, "col": 0, "size_x": 24, "size_y": 7},
    },
    # 값이 아니라 끊김을 보는 화면이라 맨 위에 둡니다.
    "5": {
        "display": "line",
        "settings": {"graph.dimensions": ["시각(UTC)"], "graph.metrics": ["이벤트", "그 외"]},
        "layout": {"row": 0, "col": 0, "size_x": 24, "size_y": 6},
    },
}


class Metabase:
    def __init__(self, base, session):
        self.base = base.rstrip("/")
        self.session = session

    def call(self, method, path, body=None):
        data = None
        headers = {"X-Metabase-Session": self.session}
        if body is not None:
            data = json.dumps(body, ensure_ascii=False).encode("utf-8")
            headers["Content-Type"] = "application/json"
        req = urllib.request.Request(self.base + path, data=data, headers=headers, method=method)
        try:
            with urllib.request.urlopen(req, timeout=60) as res:
                raw = res.read()
        except urllib.error.HTTPError as e:
            detail = e.read().decode("utf-8", "replace")[:2000]
            raise SystemExit(f"{method} {path} 가 {e.code} 로 실패했습니다.\n{detail}")
        except urllib.error.URLError as e:
            raise SystemExit(f"{self.base} 에 붙지 못했습니다: {e.reason}")
        return json.loads(raw) if raw else None


def login(base, user, password):
    url = base.rstrip("/") + "/api/session"
    body = json.dumps({"username": user, "password": password}).encode("utf-8")
    req = urllib.request.Request(url, data=body, headers={"Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(req, timeout=30) as res:
            return json.loads(res.read())["id"]
    except urllib.error.HTTPError as e:
        detail = e.read().decode("utf-8", "replace")[:2000]
        if e.code in (400, 401):
            raise SystemExit(f"로그인이 {e.code} 로 거절됐습니다. MB_USER/MB_PASS 를 확인하세요.\n{detail}")
        raise SystemExit(f"로그인이 {e.code} 로 실패했습니다.\n{detail}")
    except urllib.error.URLError as e:
        raise SystemExit(
            f"{base} 에 붙지 못했습니다: {e.reason}\n서버에서 돌리는 경우 주소는 http://127.0.0.1:3000 입니다."
        )


def parse_doc(path):
    """문서에서 `## 1.` ~ `## 5.` 절의 제목·설명·SQL 을 뽑습니다."""
    with open(path, encoding="utf-8") as f:
        text = f.read()
    sections = {}
    # 절의 시작(## 1. 제목)부터 다음 ## 앞까지.
    # re.S 를 켜므로 제목은 `.` 대신 `[^\n]` 로 잡습니다. 아니면 첫 절이 문서 끝까지 삼킵니다.
    for match in re.finditer(r"^## (([1-5])\.[^\n]*)\n(.*?)(?=^## |\Z)", text, re.M | re.S):
        title, number, rest = match.group(1).strip(), match.group(2), match.group(3)
        sql = re.search(r"```sql\n(.*?)```", rest, re.S)
        if not sql:
            raise SystemExit(f"문서의 '{title}' 절에 sql 코드 블록이 없습니다: {path}")
        # 제목과 첫 코드 블록 사이의 산문을 질문 설명으로 씁니다.
        prose = re.sub(r"[*`]", "", rest[: sql.start()].strip())
        prose = re.sub(r"\s*\n\s*", " ", prose).strip()
        sections[number] = {"title": title, "description": prose, "sql": sql.group(1).strip()}
    missing = sorted(set(VIEWS) - set(sections))
    if missing:
        raise SystemExit(f"문서에서 {', '.join(missing)} 번 절을 찾지 못했습니다: {path}")
    check_aliases(sections)
    return sections


def check_aliases(sections):
    """시각화가 가리키는 컬럼 별칭이 SQL 에 실제로 있는지 봅니다.

    문서에서 별칭 하나를 고치면 Metabase 는 그 축을 조용히 잃습니다. 차트가 비는 것이 아니라
    아무 컬럼이나 골라 그리기도 해서 눈으로는 잘 안 보입니다. 그래서 만들기 전에 막습니다.
    """
    for number, view in VIEWS.items():
        sql = sections[number]["sql"]
        for key in ("graph.dimensions", "graph.metrics"):
            for alias in view["settings"].get(key, []):
                if f"`{alias}`" not in sql:
                    raise SystemExit(
                        f"{number} 번 절의 SQL 에 별칭 `{alias}` 가 없습니다.\n"
                        "문서에서 컬럼 이름을 바꿨다면 이 스크립트의 VIEWS 도 같이 고치세요."
                    )


def find_database(mb, name):
    body = mb.call("GET", "/api/database")
    databases = body["data"] if isinstance(body, dict) else body
    for db in databases:
        if db["name"] == name:
            return db["id"]
    names = ", ".join(sorted(db["name"] for db in databases)) or "(없음)"
    raise SystemExit(
        f"'{name}' 데이터베이스가 Metabase 에 없습니다. 지금 있는 것: {names}\n"
        "deploy/README.md 의 'Metabase 첫 설정' 대로 d205_analytics 를 '플레이 로그' 라는 이름으로 추가하세요."
    )


def existing_items(mb, model):
    """루트 컬렉션에 이미 있는 질문·대시보드를 이름으로 찾습니다.

    문서가 말하는 "우리의 분석" 은 우리가 만든 컬렉션이 아니라 Metabase 의 루트 컬렉션입니다.
    한국어 화면에서 루트 컬렉션 이름이 그렇게 나옵니다. 그래서 컬렉션을 새로 만들지 않고
    `collection_id` 를 비워 둡니다. API 에서 루트를 가리키는 id 는 정수가 아니라 "root" 라,
    목록을 볼 때만 그 문자열을 씁니다.
    """
    body = mb.call("GET", f"/api/collection/root/items?models={model}")
    items = body["data"] if isinstance(body, dict) else body
    return {item["name"]: item["id"] for item in items}


def card_payload(section, view, database_id):
    return {
        "name": section["title"],
        "description": section["description"][:1000] or None,
        "collection_id": None,
        "display": view["display"],
        "visualization_settings": view["settings"],
        "dataset_query": {
            "database": database_id,
            "type": "native",
            "native": {"query": section["sql"], "template-tags": {}},
        },
    }


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    parser = argparse.ArgumentParser(description="플레이 로그 기본 대시보드를 Metabase 에 만듭니다.")
    parser.add_argument("--url", default=os.environ.get("MB_URL", "http://127.0.0.1:3000"))
    parser.add_argument("--doc", default=os.path.join(here, "..", "..", "docs", "analytics-dashboards.md"))
    parser.add_argument("--db-name", default=os.environ.get("MB_DB_NAME", "플레이 로그"))
    parser.add_argument("--dry-run", action="store_true", help="문서만 읽고 무엇을 만들지 출력합니다.")
    args = parser.parse_args()

    sections = parse_doc(args.doc)
    if args.dry_run:
        for number in sorted(sections):
            section = sections[number]
            print(f"[{number}] {section['title']}  ({VIEWS[number]['display']}, SQL {len(section['sql'])}자)")
        print(f"대시보드 '{DASHBOARD_NAME}' 에 {len(sections)}개를 배치합니다.")
        return

    user = os.environ.get("MB_USER") or input("Metabase 관리자 이메일: ").strip()
    password = os.environ.get("MB_PASS") or getpass.getpass("비밀번호: ")
    mb = Metabase(args.url, login(args.url, user, password))

    database_id = find_database(mb, args.db_name)
    print(f"데이터베이스 '{args.db_name}' (id {database_id}) 에 질문을 만듭니다.")

    cards = existing_items(mb, "card")
    card_ids = {}
    for number in sorted(sections):
        payload = card_payload(sections[number], VIEWS[number], database_id)
        name = payload["name"]
        if name in cards:
            mb.call("PUT", f"/api/card/{cards[name]}", payload)
            card_ids[number] = cards[name]
            print(f"질문 갱신: {name}")
        else:
            card_ids[number] = mb.call("POST", "/api/card", payload)["id"]
            print(f"질문 생성: {name}")

    dashboards = existing_items(mb, "dashboard")
    if DASHBOARD_NAME in dashboards:
        dashboard_id = dashboards[DASHBOARD_NAME]
        print(f"대시보드 재사용 (id {dashboard_id})")
    else:
        dashboard_id = mb.call(
            "POST",
            "/api/dashboard",
            {"name": DASHBOARD_NAME, "description": DASHBOARD_DESCRIPTION, "collection_id": None},
        )["id"]
        print(f"대시보드 생성 (id {dashboard_id})")

    # 카드 배치는 전체를 다시 씁니다. 음수 id 가 "새로 놓는 카드" 입니다.
    dashcards = [
        {
            "id": -index,
            "card_id": card_ids[number],
            "parameter_mappings": [],
            "visualization_settings": {},
            **VIEWS[number]["layout"],
        }
        for index, number in enumerate(sorted(VIEWS), start=1)
    ]
    mb.call("PUT", f"/api/dashboard/{dashboard_id}", {"dashcards": dashcards})
    print(f"카드 {len(dashcards)}개를 배치했습니다.")
    # 스크립트는 루프백으로 붙지만 사람이 여는 주소는 nginx 의 8443 입니다.
    print(f"대시보드 id {dashboard_id}. 운영이면 https://j15d205.p.ssafy.io:8443/dashboard/{dashboard_id} 입니다.")


if __name__ == "__main__":
    sys.exit(main())
