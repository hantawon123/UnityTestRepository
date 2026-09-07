package com.ssafy.d205.analytics;

import tools.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.MediaType;
import org.springframework.test.web.servlet.MockMvc;

import java.time.Instant;
import java.util.List;
import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThat;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.delete;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import static com.ssafy.d205.analytics.GameEventFixtures.fromIp;
import static com.ssafy.d205.analytics.GameEventFixtures.newSessionId;

import com.ssafy.d205.domain.analytics.config.AnalyticsDatabase;
import com.ssafy.d205.domain.analytics.dto.GameEventRequest;
import com.ssafy.d205.domain.user.dto.IssueAccountRequest;
import com.ssafy.d205.support.IntegrationTest;

/**
 * 탈퇴하면 플레이 로그에서 사람이 지워진다. 행은 남는다.
 *
 * <p>docs/analytics-events.md 8절의 약속을 고정합니다. 게임 DB 는 CASCADE 가 지우지만 분석 스키마는
 * FK 가 없어 코드가 지워야 하고, 그 코드가 빠지면 아무 테스트도 실패하지 않은 채 약속만 깨집니다.
 */
class GameEventEraseOnDeleteTest extends IntegrationTest {

    @Autowired
    MockMvc mvc;

    @Autowired
    ObjectMapper objectMapper;

    @Autowired
    AnalyticsDatabase analytics;

    @Test
    @DisplayName("탈퇴하면 그 사람의 user_public_id 가 NULL 이 되고 행 수는 그대로다")
    void deletingAccountErasesIdentifierButKeepsRows() throws Exception {
        String deviceId = UUID.randomUUID().toString();
        String userId = issue(deviceId);
        String session = newSessionId();

        // 이 사람이 주체인 이벤트 둘과, 같은 세션의 다른 사람 이벤트 하나.
        String bystander = UUID.randomUUID().toString();
        mvc.perform(post("/api/v1/events").with(fromIp("10.3.0.1"))
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(List.of(
                                eventOf(session, 0, userId),
                                eventOf(session, 1, userId),
                                eventOf(session, 2, bystander)))))
                .andExpect(status().isAccepted());
        awaitRows(session, 3);

        mvc.perform(delete("/api/v1/accounts/me")
                        .header("X-User-Id", userId)
                        .header("X-Device-Id", deviceId))
                .andExpect(status().isNoContent());

        // AFTER_COMMIT 리스너는 응답 전에 같은 스레드에서 끝납니다. 그래도 비동기로 바뀌는 날을
        // 위해 잠깐 기다려 줍니다.
        awaitErased(userId);

        assertThat(count("SELECT COUNT(*) FROM game_event WHERE client_session_id = ?", session))
                .as("행은 남아야 합니다. 지우면 같은 경기의 다른 사람 집계가 뒤틀립니다.")
                .isEqualTo(3);
        assertThat(count("SELECT COUNT(*) FROM game_event WHERE user_public_id = ?", userId))
                .as("탈퇴한 사람의 식별자는 남으면 안 됩니다.")
                .isZero();
        assertThat(count("SELECT COUNT(*) FROM game_event WHERE client_session_id = ? AND user_public_id IS NULL", session))
                .isEqualTo(2);
        assertThat(count("SELECT COUNT(*) FROM game_event WHERE user_public_id = ?", bystander))
                .as("다른 사람은 건드리지 않습니다.")
                .isEqualTo(1);
    }

    private GameEventRequest eventOf(String session, long seq, String userPublicId) {
        return new GameEventRequest(Instant.now().toEpochMilli(), session, seq, null, null, null,
                userPublicId, "client_quit", null, null, null, null, null, false, (short) 1, null);
    }

    private String issue(String deviceId) throws Exception {
        String body = mvc.perform(post("/api/v1/accounts")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(new IssueAccountRequest(deviceId))))
                .andExpect(status().isCreated())
                .andReturn().getResponse().getContentAsString();
        return objectMapper.readTree(body).get("userId").asText();
    }

    private int count(String sql, Object... args) {
        Integer n = analytics.jdbcTemplate().queryForObject(sql, Integer.class, args);
        return n == null ? 0 : n;
    }

    private void awaitRows(String session, int expected) throws InterruptedException {
        long deadline = System.currentTimeMillis() + 5_000;
        while (count("SELECT COUNT(*) FROM game_event WHERE client_session_id = ?", session) < expected
                && System.currentTimeMillis() < deadline) {
            Thread.sleep(100);
        }
    }

    private void awaitErased(String userId) throws InterruptedException {
        long deadline = System.currentTimeMillis() + 3_000;
        while (count("SELECT COUNT(*) FROM game_event WHERE user_public_id = ?", userId) > 0
                && System.currentTimeMillis() < deadline) {
            Thread.sleep(100);
        }
    }
}
