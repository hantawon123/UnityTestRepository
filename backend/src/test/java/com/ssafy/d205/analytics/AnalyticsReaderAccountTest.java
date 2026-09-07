package com.ssafy.d205.analytics;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

import java.sql.Connection;
import java.sql.DriverManager;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.sql.Statement;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

import com.ssafy.d205.support.IntegrationTest;

/**
 * Metabase 가 쓰는 읽기 전용 계정의 권한을 봅니다(S15P21D205-810).
 *
 * <p>Metabase 는 사람이 임의 SQL 을 치는 창구입니다. 그 창구로 게임 데이터를 지울 수 있으면 안
 * 됩니다. 스크립트 한 줄이 잘못돼 ALL 이 들어가도 어떤 테스트도 실패하지 않은 채 지나가므로
 * 여기서 실제 계정으로 붙어 확인합니다.
 *
 * <p>계정은 deploy/mysql/init/02-analytics-accounts.sh 가 만듭니다. 운영과 같은 파일입니다.
 */
class AnalyticsReaderAccountTest extends IntegrationTest {

    @Test
    @DisplayName("읽기 계정은 분석 스키마와 게임 스키마를 읽을 수 있다")
    void readerCanSelectBothSchemas() throws SQLException {
        try (Connection c = readerConnection(); Statement s = c.createStatement()) {
            try (ResultSet rs = s.executeQuery("SELECT COUNT(*) FROM d205_analytics.game_event")) {
                assertThat(rs.next()).isTrue();
            }
            try (ResultSet rs = s.executeQuery("SELECT COUNT(*) FROM " + MYSQL.getDatabaseName() + ".users")) {
                assertThat(rs.next()).isTrue();
            }
        }
    }

    @Test
    @DisplayName("읽기 계정은 분석 스키마에 쓸 수 없다")
    void readerCannotWriteAnalytics() throws SQLException {
        try (Connection c = readerConnection(); Statement s = c.createStatement()) {
            assertThatThrownBy(() -> s.executeUpdate(
                    "DELETE FROM d205_analytics.game_event WHERE 1 = 1"))
                    .isInstanceOf(SQLException.class)
                    .hasMessageContaining("denied");
        }
    }

    @Test
    @DisplayName("읽기 계정은 게임 스키마에 쓸 수 없고 테이블도 못 지운다")
    void readerCannotWriteGame() throws SQLException {
        try (Connection c = readerConnection(); Statement s = c.createStatement()) {
            assertThatThrownBy(() -> s.executeUpdate(
                    "UPDATE " + MYSQL.getDatabaseName() + ".users SET nickname = 'x' WHERE 1 = 1"))
                    .isInstanceOf(SQLException.class)
                    .hasMessageContaining("denied");
            assertThatThrownBy(() -> s.executeUpdate(
                    "DROP TABLE " + MYSQL.getDatabaseName() + ".users"))
                    .isInstanceOf(SQLException.class)
                    .hasMessageContaining("denied");
        }
    }

    @Test
    @DisplayName("Metabase 계정은 자기 스키마만 갖는다")
    void metabaseAccountIsConfinedToItsSchema() throws SQLException {
        String url = "jdbc:mysql://" + MYSQL.getHost() + ":" + MYSQL.getFirstMappedPort() + "/metabase";
        try (Connection c = DriverManager.getConnection(url, "metabase", "metabase-test");
             Statement s = c.createStatement()) {
            s.executeUpdate("CREATE TABLE IF NOT EXISTS probe (id INT)");
            s.executeUpdate("DROP TABLE probe");
            assertThatThrownBy(() -> s.executeQuery("SELECT COUNT(*) FROM d205_analytics.game_event"))
                    .isInstanceOf(SQLException.class)
                    .hasMessageContaining("denied");
        }
    }

    private static Connection readerConnection() throws SQLException {
        String url = "jdbc:mysql://" + MYSQL.getHost() + ":" + MYSQL.getFirstMappedPort() + "/" + ANALYTICS_SCHEMA;
        return DriverManager.getConnection(url, "d205_reader", READER_PASSWORD);
    }
}
