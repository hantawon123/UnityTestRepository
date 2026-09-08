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
# deploy/README.md 대로 이 파일을 컨테이너에 복사해 사람이 한 번 실행합니다.
#
# 이 파일은 두 가지 방식으로 읽힙니다. 실행 권한이 있으면 별도 셸에서 실행되고, 없으면
# 엔트리포인트가 자기 셸 안으로 source 합니다. 어느 쪽인지는 우리가 정할 수 없습니다.
# Windows 에서 마운트하거나 복사한 파일은 늘 755 라 실행되고, Linux(Jenkins)에서는 git 의
# 644 그대로라 source 됩니다. 그래서 아래 규칙을 지킵니다.
#
#   - 본문 전체를 서브셸 ( ... ) 로 감쌉니다. set -euo pipefail 이 엔트리포인트 셸에 새어
#     나가지 않고, exit 가 엔트리포인트를 끝내지 않습니다. 실제로 set -u 가 새어 나가
#     엔트리포인트의 "MYSQL_ONETIME_PASSWORD: unbound variable" 로 컨테이너가 죽었습니다.
#   - 엔트리포인트 내부 함수(docker_process_sql)를 쓰지 않습니다. 실행 모드에서는 없습니다.
#     대신 mysql 클라이언트로 임시 서버의 소켓에 직접 붙습니다. 이 시점에 임시 서버는 포트를
#     열지 않고 소켓만 열어 두며, root 비밀번호는 이미 설정돼 있습니다.
(
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
)
