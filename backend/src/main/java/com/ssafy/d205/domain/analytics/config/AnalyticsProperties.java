package com.ssafy.d205.domain.analytics.config;

import org.springframework.boot.context.properties.ConfigurationProperties;

/**
 * 플레이 로그 수집 설정. application.yml 의 {@code analytics.*} 입니다.
 *
 * <p>값 하나하나가 테스트에서 작게 바꿀 수 있어야 하는 것들입니다. 큐 용량 10만을 실제로
 * 채우는 테스트는 느리고, 1초 flush 를 기다리는 테스트는 전체 스위트를 늘립니다.
 *
 * @param datasource        분석 DB 접속. 게임 DB 와 같은 인스턴스, 다른 스키마
 * @param queueCapacity     상한 큐 용량. 넘치면 버립니다. 절대 블로킹하지 않습니다
 * @param flushIntervalMs   큐를 비우는 주기
 * @param flushBatchSize    한 번에 insert 하는 최대 행 수
 * @param batchMaxSize      한 요청에 허용하는 이벤트 수
 * @param rateLimitPerMinute IP 하나가 분당 보낼 수 있는 요청 수
 * @param occurredAtPastDays occurred_at 이 이보다 과거면 거부. 스풀이 묵을 수 있는 최대 기간
 * @param occurredAtFutureMinutes occurred_at 이 이보다 미래면 거부. 시계 오차 허용치
 */
@ConfigurationProperties(prefix = "analytics")
public record AnalyticsProperties(
        Datasource datasource,
        int queueCapacity,
        long flushIntervalMs,
        int flushBatchSize,
        int batchMaxSize,
        int rateLimitPerMinute,
        int occurredAtPastDays,
        int occurredAtFutureMinutes
) {
    /**
     * @param url MySQL Connector/J URL. {@code createDatabaseIfNotExist=true} 가 들어 있어야
     *            합니다. 스키마가 없는 상태에서 스키마를 가리키는 URL 로 접속하면 Unknown
     *            database 로 실패하는데, 이 옵션이 첫 접속에서 스키마를 만들어 줍니다. 그 뒤
     *            Flyway 가 테이블을 만듭니다
     * @param username 게임 DB 와 같은 계정. deploy/mysql/init 의 GRANT 가 이 계정에 분석
     *                 스키마 권한을 줍니다
     */
    public record Datasource(String url, String username, String password) {
    }
}
