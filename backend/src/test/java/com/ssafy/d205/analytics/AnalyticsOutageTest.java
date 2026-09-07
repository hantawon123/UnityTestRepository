package com.ssafy.d205.analytics;

import tools.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.MediaType;
import org.springframework.test.context.TestPropertySource;
import org.springframework.test.web.servlet.MockMvc;

import java.util.List;
import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThat;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import static com.ssafy.d205.analytics.GameEventFixtures.event;
import static com.ssafy.d205.analytics.GameEventFixtures.fromIp;
import static com.ssafy.d205.analytics.GameEventFixtures.newSessionId;

import com.ssafy.d205.domain.analytics.config.AnalyticsDatabase;
import com.ssafy.d205.domain.analytics.service.GameEventBuffer;
import com.ssafy.d205.domain.analytics.service.GameEventFlusher;
import com.ssafy.d205.domain.user.dto.IssueAccountRequest;
import com.ssafy.d205.support.IntegrationTest;

/**
 * 분석 DB 가 없어도 게임은 산다.
 *
 * <p>분석 URL 을 아무것도 듣지 않는 포트로 바꾼 컨텍스트입니다. 기동이 막히지 않고, 게임 API 가
 * 정상이고, 수집 API 는 여전히 202 를 주며(큐에는 들어가고 쓰이지만 않음), 플러셔가 큐를 건드리지
 * 않는지를 봅니다. 이 넷 중 하나라도 깨지면 "별도 서비스로 나누지 않아도 된다"는 전제가 무너집니다.
 *
 * <p>connectTimeout 을 짧게 준 것은 테스트 시간 때문입니다. 없으면 Flyway 가 커넥션을 얻으려고
 * Hikari 타임아웃(2초)까지 기다립니다.
 */
@TestPropertySource(properties = {
        "analytics.datasource.url=jdbc:mysql://127.0.0.1:1/d205_analytics?connectTimeout=300&socketTimeout=300",
        "analytics.flush-interval-ms=3600000"
})
class AnalyticsOutageTest extends IntegrationTest {

    @Autowired
    MockMvc mvc;

    @Autowired
    ObjectMapper objectMapper;

    @Autowired
    AnalyticsDatabase analytics;

    @Autowired
    GameEventBuffer buffer;

    @Autowired
    GameEventFlusher flusher;

    @Test
    @DisplayName("분석 DB 에 붙지 못해도 앱은 뜨고 준비 안 됨으로 표시된다")
    void startsWithoutAnalyticsDatabase() {
        assertThat(analytics.isReady()).isFalse();
    }

    @Test
    @DisplayName("게임 API 는 정상이다")
    void gameApiIsUnaffected() throws Exception {
        String deviceId = UUID.randomUUID().toString();

        mvc.perform(post("/api/v1/accounts")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(new IssueAccountRequest(deviceId))))
                .andExpect(status().isCreated());

        mvc.perform(get("/api/v1/accounts/me").header("X-User-Id", UUID.randomUUID().toString()))
                .andExpect(status().isNotFound())
                .andExpect(jsonPath("$.code").value("ACCOUNT_NOT_FOUND"));
    }

    @Test
    @DisplayName("수집 API 는 202 를 주고, 플러셔는 준비될 때까지 큐를 건드리지 않는다")
    void ingestStillAcceptsAndFlusherWaits() throws Exception {
        String session = newSessionId();

        mvc.perform(post("/api/v1/events").with(fromIp("10.2.0.1"))
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(List.of(event(session, 0)))))
                .andExpect(status().isAccepted());

        int before = buffer.size();
        assertThat(before).isGreaterThanOrEqualTo(1);

        // 준비가 안 됐으니 꺼내지 않습니다. 꺼냈다면 insert 가 실패해 버려졌을 것입니다.
        assertThat(flusher.flushOnce()).isZero();
        assertThat(buffer.size()).isEqualTo(before);
        assertThat(flusher.failedCount()).isZero();
    }
}
