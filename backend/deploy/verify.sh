#!/usr/bin/env bash
# 배포 상태 확인. EC2에서 실행합니다.
#
#   ssh d205 'bash /tmp/verify.sh'
#
# SQL을 명령줄에 인라인하지 않는 이유: PowerShell에서 ssh로 넘길 때
# 홑따옴표 안의 겹따옴표가 벗겨져 bash가 SHOW TABLES를 두 단어로 해석합니다.
# 스크립트로 두면 인용부호 문제가 사라집니다.
set -uo pipefail

ENV_FILE=/home/ubuntu/d205/.env
COMPOSE_DIR=$(docker inspect d205-app \
  --format '{{index .Config.Labels "com.docker.compose.project.working_dir"}}' 2>/dev/null)

echo "=== 컨테이너 ==="
docker ps --filter label=com.docker.compose.project=d205 \
  --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}'

echo
echo "=== 배포 주체 ==="
echo "compose 실행 위치: ${COMPOSE_DIR:-(알 수 없음)}"
docker images d205-app --format '이미지: {{.Repository}}:{{.Tag}}  {{.CreatedSince}}  {{.Size}}'

echo
echo "=== 애플리케이션 ==="
printf '내부 8080: '
curl -sS --max-time 5 http://localhost:8080/actuator/health || echo '응답 없음'
echo
printf 'HTTPS    : '
curl -sS --max-time 5 https://j15d205.p.ssafy.io/actuator/health || echo '응답 없음'
echo

echo
echo "=== 대시보드 (Metabase) ==="
printf '내부 3000: '
curl -sS --max-time 5 http://localhost:3000/api/health || echo '응답 없음 (첫 기동은 1분 넘게 걸립니다)'
echo
# 8443 은 Basic Auth 뒤라 401 이 정상입니다. 000 이면 nginx 나 방화벽, 502 면 Metabase 가 없는 것입니다.
printf 'HTTPS 8443 (401 이 정상): '
curl -sS --max-time 5 -o /dev/null -w '%{http_code}\n' https://j15d205.p.ssafy.io:8443/api/health || echo '응답 없음'
printf '분석 DB 준비 로그: '
docker logs d205-app --tail 500 2>&1 | grep -E '분석 DB' | tail -1 || echo '없음'

echo
echo "=== 상시 연결 (알림 WebSocket) ==="
# 클라이언트마다 연결 하나를 상시 붙들고 있습니다(883·890). 받을 수 있는 수는 nginx 의
# worker_connections 와 앱의 server.tomcat.max-connections(application.yml, 8192) 중
# 작은 쪽입니다. nginx 전역 설정은 저장소에 없어 여기서 읽습니다. 기본값 768 이면
# 그쪽이 먼저 막히고, 프록시라 접속자 하나가 연결 둘(클라이언트↔nginx, nginx↔앱)을 씁니다.
printf 'nginx worker_connections: '
sudo nginx -T 2>/dev/null | grep -m1 -oE 'worker_connections\s+[0-9]+' | awk '{print $2}' || echo '읽기 실패 (sudo 필요)'
printf '지금 443 에 맺힌 연결: '
ss -Htan state established '( sport = :443 )' 2>/dev/null | wc -l
printf '앱 8080 에 맺힌 연결: '
ss -Htan state established '( sport = :8080 )' 2>/dev/null | wc -l

if [ ! -r "$ENV_FILE" ]; then
    echo
    echo "(DB 확인 생략: $ENV_FILE 을 읽을 수 없습니다)"
    exit 0
fi

PW=$(grep '^MYSQL_ROOT_PASSWORD=' "$ENV_FILE" | cut -d= -f2)
DB=$(grep '^DB_NAME=' "$ENV_FILE" | cut -d= -f2)

echo
echo "=== 테이블 ($DB) ==="
docker exec d205-mysql mysql -uroot -p"$PW" -N -B -e 'SHOW TABLES;' "$DB" 2>/dev/null \
  || echo '조회 실패'

echo
echo "=== Flyway 적용 이력 ==="
docker exec d205-mysql mysql -uroot -p"$PW" -t "$DB" 2>/dev/null -e \
  'SELECT installed_rank AS n, version, description, success, installed_on
     FROM flyway_schema_history ORDER BY installed_rank;' \
  || echo '조회 실패 (마이그레이션이 아직 적용되지 않았을 수 있습니다)'

echo
echo "=== 인증서 만료 ==="
sudo certbot certificates 2>/dev/null | grep -E 'Certificate Name|Expiry' || echo '확인 실패'
