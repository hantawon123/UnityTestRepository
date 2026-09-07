#!/bin/bash
# 앱 계정에 분석 스키마(d205_analytics) 권한을 줍니다.
#
# MySQL 이미지는 MYSQL_USER 에게 MYSQL_DATABASE 하나의 권한만 줍니다. 분석 로그는 같은
# 인스턴스의 다른 스키마에 두기로 했으므로(에픽 S15P21D205-780) 그 권한을 따로 줘야 합니다.
# 스키마 자체는 여기서 만들지 않습니다. MySQL 은 없는 스키마에도 GRANT 를 허용하고, 앱이
# 첫 접속에서 createDatabaseIfNotExist 로 만든 뒤 Flyway 가 테이블을 만듭니다. 그래서
# 이 파일은 권한 한 줄이 전부입니다.
#
# 이 파일은 docker-entrypoint-initdb.d 에 놓여 MySQL 이 *데이터 디렉터리가 비어 있을 때 한 번*
# 실행합니다. 이미 초기화된 볼륨에서는 돌지 않습니다. 운영 서버가 그 경우라서, 운영에는
# 이 스크립트가 아니라 deploy/README.md 의 GRANT 명령을 사람이 한 번 실행합니다.
#
# 엔트리포인트의 내부 함수(docker_process_sql)를 쓰지 않습니다. 그 함수는 파일이 source 될
# 때만 있는데, Windows 에서 마운트하거나 Testcontainers 가 복사한 파일은 늘 실행 권한이
# 붙어 별도 셸에서 돌아갑니다. 실제로 그렇게 "command not found" 로 초기화가 통째로 실패해
# 컨테이너가 뜨지 못했습니다. 대신 mysql 클라이언트로 임시 서버의 소켓에 직접 붙습니다.
# 이 시점에 임시 서버는 포트를 열지 않고 소켓만 열어 두며, root 비밀번호는 이미 설정돼 있습니다.
set -euo pipefail

if [ -z "${MYSQL_USER:-}" ]; then
    echo "[analytics-grant] MYSQL_USER 가 없어 권한을 주지 않습니다." >&2
    exit 0
fi

mysql --protocol=socket -uroot -p"${MYSQL_ROOT_PASSWORD}" <<EOSQL
    GRANT ALL PRIVILEGES ON \`d205_analytics\`.* TO '${MYSQL_USER}'@'%';
    FLUSH PRIVILEGES;
EOSQL

echo "[analytics-grant] ${MYSQL_USER} 에게 d205_analytics 권한을 주었습니다."
