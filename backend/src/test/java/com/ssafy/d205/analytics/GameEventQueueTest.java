package com.ssafy.d205.analytics;

import tools.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.MediaType;
import org.springframework.test.context.TestPropertySource;
import org.springframework.test.web.servlet.MockMvc;

import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import static com.ssafy.d205.analytics.GameEventFixtures.event;
import static com.ssafy.d205.analytics.GameEventFixtures.fromIp;
import static com.ssafy.d205.analytics.GameEventFixtures.newSessionId;

import com.ssafy.d205.domain.analytics.config.AnalyticsDatabase;
import com.ssafy.d205.domain.analytics.service.GameEventBuffer;
import com.ssafy.d205.domain.analytics.service.GameEventFlusher;
import com.ssafy.d205.support.IntegrationTest;

/**
 * 상한 큐가 약속대로 동작하는지 봅니다. 별도 서비스로 나누지 않은 전제가 이 테스트입니다.
 *
 * <p>큐 용량을 3으로, flush 주기를 한 시간으로 둔 별도 컨텍스트입니다. 10만 개를 실제로 채우는
 * 테스트는 느리고, 플러셔가 도는 중에는 "가득 찬 큐"를 재현할 수 없습니다. flush 는 직접 부릅니다.
 */
@TestPropertySource(properties = {
        "analytics.queue-capacity=3",
        "analytics.flush-interval-ms=3600000"
})
class GameEventQueueTest extends IntegrationTest {

    @Autowired
    MockMvc mvc;

    @Autowired
    ObjectMapper objectMapper;

    @Autowired
    GameEventBuffer buffer;

    @Autowired
    GameEventFlusher flusher;

    @Autowired
    AnalyticsDatabase analytics;

    @Test
    @DisplayName("큐가 가득 차도 API 는 블로킹 없이 202 를 주고, 넘친 만큼만 버린다")
    void fullQueueDropsWithoutBlocking() throws Exception {
        String session = newSessionId();
        long droppedBefore = buffer.droppedCount();

        // 용량 3에 5건. 앞의 3건은 들어가고 2건은 버려져야 합니다.
        List<?> five = List.of(event(session, 0), event(session, 1), event(session, 2),
                event(session, 3), event(session, 4));

        long started = System.currentTimeMillis();
        mvc.perform(post("/api/v1/events").with(fromIp("10.1.0.1"))
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(five)))
                .andExpect(status().isAccepted());
        long elapsed = System.currentTimeMillis() - started;

        assertThat(elapsed).as("가득 찬 큐에서 요청이 기다리면 안 됩니다").isLessThan(2_000);
        assertThat(buffer.droppedCount() - droppedBefore).isEqualTo(2);
        assertThat(buffer.size()).isEqualTo(3);

        // 직접 비우면 들어간 3건만 저장됩니다.
        int flushed = flusher.flushOnce();
        assertThat(flushed).isEqualTo(3);
        assertThat(flusher.failedCount()).isZero();

        Integer stored = analytics.jdbcTemplate().queryForObject(
                "SELECT COUNT(*) FROM game_event WHERE client_session_id = ?", Integer.class, session);
        assertThat(stored).isEqualTo(3);
        assertThat(buffer.isEmpty()).isTrue();
    }

    @Test
    @DisplayName("비운 뒤에는 다시 받는다")
    void acceptsAgainAfterFlush() throws Exception {
        flusher.flushOnce();
        String session = newSessionId();
        long droppedBefore = buffer.droppedCount();

        mvc.perform(post("/api/v1/events").with(fromIp("10.1.0.2"))
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(List.of(event(session, 0)))))
                .andExpect(status().isAccepted());

        assertThat(buffer.droppedCount()).isEqualTo(droppedBefore);
        assertThat(flusher.flushOnce()).isEqualTo(1);
    }
}
