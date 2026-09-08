package com.ssafy.d205.domain.notification.controller;

import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.scheduling.TaskScheduler;
import org.springframework.stereotype.Component;
import org.springframework.web.socket.CloseStatus;
import org.springframework.web.socket.TextMessage;
import org.springframework.web.socket.WebSocketSession;
import org.springframework.web.socket.handler.TextWebSocketHandler;
import tools.jackson.core.JacksonException;
import tools.jackson.databind.JsonNode;
import tools.jackson.databind.ObjectMapper;

import java.io.IOException;
import java.time.Instant;
import java.util.Map;
import java.util.Optional;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.ScheduledFuture;

import com.ssafy.d205.domain.notification.service.NotificationSessionRegistry;
import com.ssafy.d205.domain.user.entity.User;
import com.ssafy.d205.domain.user.repository.UserRepository;
import com.ssafy.d205.global.config.NotificationProperties;

/**
 * {@code /ws/notifications} 로 들어오는 연결의 입구.
 *
 * <p>연결이 열리면 클라이언트가 먼저 말해야 합니다.
 * <pre>{ "type": "HELLO", "userId": "..." }</pre>
 * 브라우저의 WebSocket 은 헤더를 붙일 수 없어 X-User-Id 를 쓸 수 없고, 쿼리스트링에 넣으면
 * nginx 접근 로그에 남습니다. 그래서 첫 프레임으로 받습니다. 신뢰 수준은 헤더와 같습니다.
 * 남의 id 를 아는 사람이 그 사람의 알림을 받을 수 있고, 그것은 REST 와 같은 문제입니다.
 *
 * <p>정해진 시간 안에 HELLO 가 없으면 끊습니다. 누구 것도 아닌 연결이 쌓이지 않게 하려는
 * 것이고, 정상 클라이언트는 연결 직후 바로 보내므로 걸리지 않습니다.
 *
 * <p>HELLO 가 받아들여지면 {@code { "type": "HELLO_ACK" }} 를 돌려줍니다. 클라이언트는 이것을
 * 받은 시점을 "연결됨"으로 보고 놓친 것이 없도록 목록을 전체 조회합니다. 연결이 열린 시점을
 * 쓰면 서버가 아직 나를 모르는 사이에 온 알림을 놓칩니다.
 */
@Component
@RequiredArgsConstructor
@Slf4j
public class NotificationWebSocketHandler extends TextWebSocketHandler {

    private static final String HELLO = "HELLO";
    private static final String HELLO_ACK = "{\"type\":\"HELLO_ACK\"}";

    private final NotificationSessionRegistry registry;
    private final UserRepository userRepository;
    private final ObjectMapper objectMapper;
    private final TaskScheduler scheduler;
    private final NotificationProperties properties;

    /** 아직 HELLO 를 보내지 않은 연결마다 걸어 둔 마감. HELLO 가 오면 취소합니다. */
    private final Map<String, ScheduledFuture<?>> helloDeadlines = new ConcurrentHashMap<>();

    @Override
    public void afterConnectionEstablished(WebSocketSession session) {
        ScheduledFuture<?> deadline = scheduler.schedule(
                () -> closeIfStillSilent(session),
                Instant.now().plusMillis(properties.helloTimeoutMs()));
        helloDeadlines.put(session.getId(), deadline);
    }

    @Override
    protected void handleTextMessage(WebSocketSession session, TextMessage message) throws IOException {
        JsonNode frame;
        try {
            frame = objectMapper.readTree(message.getPayload());
        } catch (JacksonException e) {
            log.debug("연결 {} 이 JSON 이 아닌 프레임을 보냈습니다. 무시합니다.", session.getId());
            return;
        }

        // 지금 클라이언트가 보내는 프레임은 HELLO 하나입니다. 모르는 type 은 조용히 버립니다.
        // 끊어버리면 새 프레임을 추가한 클라이언트가 옛 서버에 붙었을 때 계속 튕깁니다.
        if (!HELLO.equals(frame.path("type").asString())) {
            return;
        }
        if (registry.isBound(session)) {
            return;
        }

        String userId = frame.path("userId").asString();
        Optional<User> user = userId.isBlank() ? Optional.empty() : userRepository.findByPublicId(userId);
        if (user.isEmpty()) {
            close(session, CloseStatus.POLICY_VIOLATION.withReason("UNKNOWN_USER"));
            return;
        }

        cancelDeadline(session);
        WebSocketSession bound = registry.bind(user.get().getSeq(), session);
        bound.sendMessage(new TextMessage(HELLO_ACK));
    }

    @Override
    public void afterConnectionClosed(WebSocketSession session, CloseStatus status) {
        cancelDeadline(session);
        registry.unbind(session);
    }

    @Override
    public void handleTransportError(WebSocketSession session, Throwable exception) {
        log.debug("알림 연결 {} 에 전송 오류: {}", session.getId(), exception.getMessage());
        close(session, CloseStatus.SERVER_ERROR);
    }

    private void closeIfStillSilent(WebSocketSession session) {
        helloDeadlines.remove(session.getId());
        if (!registry.isBound(session)) {
            close(session, CloseStatus.POLICY_VIOLATION.withReason("HELLO_TIMEOUT"));
        }
    }

    private void cancelDeadline(WebSocketSession session) {
        ScheduledFuture<?> deadline = helloDeadlines.remove(session.getId());
        if (deadline != null) {
            deadline.cancel(false);
        }
    }

    private void close(WebSocketSession session, CloseStatus status) {
        try {
            if (session.isOpen()) {
                session.close(status);
            }
        } catch (IOException e) {
            log.debug("알림 연결 {} 을 닫지 못했습니다: {}", session.getId(), e.getMessage());
        }
    }
}
