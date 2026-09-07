#!/bin/bash
# 분석용 MySQL 계정 둘을 만듭니다. 비밀번호는 환경변수로 받고, 없는 계정은 건너뜁니다.
#
#   d205_reader  Metabase 가 데이터를 읽는 계정. d205_analytics 와 게임 DB 전체에 SELECT 만.
#                Metabase 는 사람이 임의 SQL 을 치는 창구라, 그 창구가 게임 데이터를 지울 수
#                있으면 안 됩니다. INSERT/UPDATE/DELETE/DDL 을 주지 않습니다(S15P21D205-810).
#   metabase     Metabase 가 자기 설정(대시보드, 사용자)을 저장하는 계정. metabase 스키마에만
#                전권. 내장 H2 를 쓰지 않는 이유는 컨테이너가 날아가면 대시보드도 함께 사라지기
#                때문입니다(S15P21D205-805).
#
# 01-analytics-grant.sh 와 같은 규칙으로 씁니다. 서브셸로 감싸서 source 되든 실행되든 엔트리포인트를
# 건드리지 않고, 내부 함수 대신 mysql 클라이언트로 소켓에 붙습니다. 그 이유는 01 의 주석에 있습니다.
#
# 운영에서는 initdb 가 돌지 않으므로 deploy/README.md 대로 컨테이너에 복사해 실행합니다. 그때
# 비밀번호는 `docker exec -e` 로 넘깁니다. compose.prod.yml 의 mysql 환경변수에 넣지 않는 이유는,
# 환경변수를 바꾸면 compose 가 MySQL 컨테이너를 다시 만들어 배포 중 DB 가 잠깐 끊기기 때문입니다.
#
# 두 번 실행해도 안전합니다. CREATE ... IF NOT EXISTS 와 GRANT 만 있고, 비밀번호는 ALTER 로 맞춥니다.
(
    set -euo pipefail

    sql() {
        mysql --protocol=socket -uroot -p"${MYSQL_ROOT_PASSWORD}"
    }

    if [ -n "${ANALYTICS_READER_PASSWORD:-}" ]; then
        # 게임 DB 이름은 MYSQL_DATABASE 에서 옵니다. 운영은 d205, Testcontainers 는 test 입니다.
        sql <<EOSQL
    CREATE USER IF NOT EXISTS 'd205_reader'@'%' IDENTIFIED BY '${ANALYTICS_READER_PASSWORD}';
    ALTER USER 'd205_reader'@'%' IDENTIFIED BY '${ANALYTICS_READER_PASSWORD}';
    GRANT SELECT ON \`d205_analytics\`.* TO 'd205_reader'@'%';
    GRANT SELECT ON \`${MYSQL_DATABASE}\`.* TO 'd205_reader'@'%';
    FLUSH PRIVILEGES;
EOSQL
        echo "[analytics-accounts] d205_reader 에게 d205_analytics 와 ${MYSQL_DATABASE} 의 SELECT 권한을 주었습니다."
    else
        echo "[analytics-accounts] ANALYTICS_READER_PASSWORD 가 없어 d205_reader 를 만들지 않습니다." >&2
    fi

    if [ -n "${METABASE_DB_PASSWORD:-}" ]; then
        sql <<EOSQL
    CREATE DATABASE IF NOT EXISTS \`metabase\` CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
    CREATE USER IF NOT EXISTS 'metabase'@'%' IDENTIFIED BY '${METABASE_DB_PASSWORD}';
    ALTER USER 'metabase'@'%' IDENTIFIED BY '${METABASE_DB_PASSWORD}';
    GRANT ALL PRIVILEGES ON \`metabase\`.* TO 'metabase'@'%';
    FLUSH PRIVILEGES;
EOSQL
        echo "[analytics-accounts] metabase 스키마와 계정을 준비했습니다."
    else
        echo "[analytics-accounts] METABASE_DB_PASSWORD 가 없어 metabase 계정을 만들지 않습니다." >&2
    fi
)
