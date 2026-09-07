package com.ssafy.d205.analytics;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.jdbc.core.JdbcTemplate;

import static org.assertj.core.api.Assertions.assertThat;

import com.ssafy.d205.domain.analytics.config.AnalyticsDatabase;
import com.ssafy.d205.support.IntegrationTest;

/**
 * 분석 스키마가 게임 스키마와 <b>따로</b> 만들어지는지 봅니다.
 *
 * <p>여기서 잡으려는 실수는 둘입니다. GRANT 스크립트가 빠져 분석 DB 가 준비되지 않는 것, 그리고
 * 두 Flyway 가 서로의 마이그레이션을 집어 가 game_event 가 게임 스키마에 생기는 것입니다.
 */
class AnalyticsSchemaTest extends IntegrationTest {

    @Autowired
    AnalyticsDatabase analytics;

    /** 게임 DB 쪽 템플릿. 기본 DataSource 에 붙습니다. */
    @Autowired
    JdbcTemplate gameDb;

    @Test
    @DisplayName("분석 DB 가 기동 시점에 준비된다")
    void analyticsDatabaseIsReady() {
        assertThat(analytics.isReady())
                .as("GRANT 스크립트가 컨테이너에 들어갔는지, URL 에 createDatabaseIfNotExist 가 있는지 보세요.")
                .isTrue();
    }

    @Test
    @DisplayName("game_event 는 분석 스키마에만 있고 게임 스키마에는 없다")
    void gameEventLivesOnlyInAnalyticsSchema() {
        Integer inAnalytics = analytics.jdbcTemplate().queryForObject("""
                SELECT COUNT(*) FROM information_schema.tables
                 WHERE table_schema = ? AND table_name = 'game_event'
                """, Integer.class, ANALYTICS_SCHEMA);
        Integer inGame = gameDb.queryForObject("""
                SELECT COUNT(*) FROM information_schema.tables
                 WHERE table_schema = DATABASE() AND table_name = 'game_event'
                """, Integer.class);

        assertThat(inAnalytics).isOne();
        assertThat(inGame)
                .as("게임 쪽 Flyway 가 db/migration-analytics 를 집어 갔습니다. locations 를 확인하세요.")
                .isZero();
    }

    @Test
    @DisplayName("게임 스키마의 Flyway 이력에 분석 마이그레이션이 섞이지 않는다")
    void flywayHistoriesAreSeparate() {
        Integer gameHistoryHasAnalytics = gameDb.queryForObject("""
                SELECT COUNT(*) FROM flyway_schema_history WHERE script LIKE '%game_event%'
                """, Integer.class);
        Integer analyticsHistory = analytics.jdbcTemplate().queryForObject("""
                SELECT COUNT(*) FROM flyway_schema_history WHERE script LIKE '%game_event%'
                """, Integer.class);

        assertThat(gameHistoryHasAnalytics).isZero();
        assertThat(analyticsHistory).isOne();
    }
}
