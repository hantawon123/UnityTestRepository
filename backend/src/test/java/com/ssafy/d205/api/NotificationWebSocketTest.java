package com.ssafy.d205.api;

import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.MediaType;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.web.socket.CloseStatus;
import org.springframework.web.socket.TextMessage;
import org.springframework.web.socket.WebSocketSession;
import org.springframework.web.socket.client.standard.StandardWebSocketClient;
import org.springframework.web.socket.handler.TextWebSocketHandler;
import tools.jackson.databind.JsonNode;
import tools.jackson.databind.ObjectMapper;

import java.io.IOException;
import java.time.Duration;
import java.util.ArrayList;
import java.util.List;
import java.util.UUID;
import java.util.concurrent.BlockingQueue;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.LinkedBlockingQueue;
import java.util.concurrent.TimeUnit;

import static org.assertj.core.api.Assertions.assertThat;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.delete;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import com.ssafy.d205.domain.notification.service.NotificationSessionRegistry;
import com.ssafy.d205.support.IntegrationTest;

/**
 * 실시간 알림 채널을 실제 소켓으로 시험합니다.
 *
 * <p>다른 API 테스트와 달리 서버를 진짜 포트에 띄웁니다. MockMvc 는 서블릿 호출을 흉내낼 뿐
 * WebSocket 핸드셰이크와 프레임은 흉내내지 못합니다. 친구 요청·초대 같은 <b>일으키는 쪽</b>은
 * 여전히 MockMvc 로 부르고, <b>받는 쪽</b>만 소켓으로 붙습니다. 두 경로가 같은 컨텍스트라
 * 커밋 뒤 발송이 실제 소켓으로 나갑니다.
 */
@SpringBootTest(webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
class NotificationWebSocketTest extends IntegrationTest {

    private static final String USER_ID_HEADER = "X-User-Id";
    private static final String ROOM = "7K2M9P";
    private static final Duration WAIT = Duration.ofSeconds(5);

    // Boot 4 에서 @LocalServerPort 가 든 모듈이 webmvc-test 스타터에 딸려오지 않아 속성으로 읽습니다.
    @Value("${local.server.port}")
    int port;

    @Autowired
    MockMvc mvc;

    @Autowired
    ObjectMapper objectMapper;

    @Autowired
    NotificationSessionRegistry registry;

    private final List<Client> clients = new ArrayList<>();

    @AfterEach
    void closeClients() {
        for (Client client : clients) {
            client.closeQuietly();
        }
        clients.clear();
    }

    @Test
    @DisplayName("HELLO 를 보내면 HELLO_ACK 가 온다")
    void helloIsAcknowledged() throws Exception {
        String me = createUser();

        Client client = connect();
        client.hello(me);

        // 클라이언트는 이 프레임을 받은 시점을 "연결됨"으로 봅니다. 연결이 열린 시점을 쓰면
        // 서버가 아직 나를 모르는 사이의 알림을 놓칩니다.
        assertThat(client.next().path("type").asText()).isEqualTo("HELLO_ACK");
    }

    @Test
    @DisplayName("친구 요청을 받으면 상대에게 즉시 프레임이 간다")
    void friendRequestIsPushedToTheTarget() throws Exception {
        String host = createUser();
        String guest = createUser();
        Client guestClient = connectAs(guest);

        sendRequest(host, guest);

        JsonNode frame = guestClient.next();
        assertThat(frame.path("type").asText()).isEqualTo("FRIEND_REQUEST_RECEIVED");
        assertThat(frame.path("from").path("userId").asText()).isEqualTo(host);
        assertThat(frame.path("from").path("nickname").asText()).isNotBlank();
        assertThat(frame.path("sentAt").asText()).matches("\\d{14}");
        assertThat(frame.path("roomCode").isNull()).isTrue();
    }

    @Test
    @DisplayName("요청을 수락하면 요청자에게 프레임이 간다")
    void acceptingIsPushedToTheRequester() throws Exception {
        String host = createUser();
        String guest = createUser();
        Client hostClient = connectAs(host);
        sendRequest(host, guest);

        accept(guest, host);

        JsonNode frame = hostClient.next();
        assertThat(frame.path("type").asText()).isEqualTo("FRIEND_REQUEST_ACCEPTED");
        assertThat(frame.path("from").path("userId").asText()).isEqualTo(guest);
    }

    @Test
    @DisplayName("서로 요청을 보내 자동 수락되면 먼저 보낸 쪽이 수락 프레임을 받는다")
    void mutualRequestsNotifyTheFirstRequester() throws Exception {
        String host = createUser();
        String guest = createUser();
        Client hostClient = connectAs(host);
        sendRequest(host, guest);

        // 상대도 나에게 요청하면 새 요청이 아니라 내 요청의 수락입니다. 그래서 201 이 아니라
        // 200 이고, 요청자인 나에게 "받았다"가 아니라 "수락됐다"가 와야 합니다.
        mvc.perform(post("/api/v1/friend-requests")
                        .header(USER_ID_HEADER, guest)
                        .contentType(MediaType.APPLICATION_JSON)
                        .content("{\"userId\":\"" + host + "\"}"))
                .andExpect(status().isOk());

        JsonNode frame = hostClient.next();
        assertThat(frame.path("type").asText()).isEqualTo("FRIEND_REQUEST_ACCEPTED");
        assertThat(frame.path("from").path("userId").asText()).isEqualTo(guest);
    }

    @Test
    @DisplayName("요청이 거절되면 요청자에게 지워졌다는 프레임이 간다")
    void decliningIsPushedAsRemoved() throws Exception {
        String host = createUser();
        String guest = createUser();
        Client hostClient = connectAs(host);
        sendRequest(host, guest);

        mvc.perform(delete("/api/v1/friend-requests/{userId}", host).header(USER_ID_HEADER, guest))
                .andExpect(status().isNoContent());

        JsonNode frame = hostClient.next();
        assertThat(frame.path("type").asText()).isEqualTo("FRIEND_REQUEST_REMOVED");
        assertThat(frame.path("from").path("userId").asText()).isEqualTo(guest);
    }

    @Test
    @DisplayName("방으로 부르면 상대에게 방 코드가 실린 프레임이 간다")
    void inviteIsPushedWithTheRoomCode() throws Exception {
        String host = createUser();
        String guest = createUser();
        befriend(host, guest);
        Client guestClient = connectAs(guest);

        invite(host, guest, ROOM);

        JsonNode frame = guestClient.next();
        assertThat(frame.path("type").asText()).isEqualTo("ROOM_INVITE_RECEIVED");
        assertThat(frame.path("from").path("userId").asText()).isEqualTo(host);
        assertThat(frame.path("roomCode").asText()).isEqualTo(ROOM);
    }

    @Test
    @DisplayName("같은 방으로 다시 부르면 프레임이 한 번 더 간다")
    void invitingAgainPushesAgain() throws Exception {
        String host = createUser();
        String guest = createUser();
        befriend(host, guest);
        Client guestClient = connectAs(guest);
        invite(host, guest, ROOM);
        guestClient.next();

        // 갱신은 저장 행을 늘리지 않지만 알림은 다시 갑니다. 상대 화면에서 토스트가 이미
        // 사라졌을 수 있고, 다시 부른 것은 다시 봐 달라는 뜻입니다.
        invite(host, guest, ROOM);

        assertThat(guestClient.next().path("type").asText()).isEqualTo("ROOM_INVITE_RECEIVED");
    }

    @Test
    @DisplayName("친구를 끊으면 상대에게 끊겼다는 프레임이 간다")
    void unfriendingPushesFriendRemoved() throws Exception {
        String host = createUser();
        String guest = createUser();
        befriend(host, guest);
        invite(host, guest, ROOM);
        Client guestClient = connectAs(guest);

        mvc.perform(delete("/api/v1/friends/{userId}", guest).header(USER_ID_HEADER, host))
                .andExpect(status().isNoContent());

        JsonNode frame = guestClient.next();
        assertThat(frame.path("type").asText()).isEqualTo("FRIEND_REMOVED");
        assertThat(frame.path("from").path("userId").asText()).isEqualTo(host);
    }

    @Test
    @DisplayName("한 사람이 여러 연결을 열어 두면 전부 받는다")
    void everyConnectionOfTheSameUserReceives() throws Exception {
        String host = createUser();
        String guest = createUser();
        Client first = connectAs(guest);
        Client second = connectAs(guest);

        sendRequest(host, guest);

        assertThat(first.next().path("type").asText()).isEqualTo("FRIEND_REQUEST_RECEIVED");
        assertThat(second.next().path("type").asText()).isEqualTo("FRIEND_REQUEST_RECEIVED");
    }

    @Test
    @DisplayName("요청이 실패하면 아무것도 가지 않는다")
    void nothingIsPushedWhenTheRequestFails() throws Exception {
        String host = createUser();
        String guest = createUser();
        befriend(host, guest);
        Client guestClient = connectAs(guest);

        // 이미 친구라 409 입니다. 저장이 없었으니 알림도 없어야 합니다.
        mvc.perform(post("/api/v1/friend-requests")
                        .header(USER_ID_HEADER, host)
                        .contentType(MediaType.APPLICATION_JSON)
                        .content("{\"userId\":\"" + guest + "\"}"))
                .andExpect(status().isConflict());

        assertThat(guestClient.nothingWithin(Duration.ofSeconds(1))).isTrue();
    }

    @Test
    @DisplayName("HELLO 없이 가만히 있는 연결은 서버가 끊는다")
    void aSilentConnectionIsClosed() throws Exception {
        Client client = connect();

        // 테스트 프로필의 마감은 1초입니다. 누구 것도 아닌 연결이 쌓이지 않게 합니다.
        assertThat(client.closed.await(WAIT.toMillis(), TimeUnit.MILLISECONDS)).isTrue();
        assertThat(client.closeStatus.getCode()).isEqualTo(CloseStatus.POLICY_VIOLATION.getCode());
    }

    @Test
    @DisplayName("모르는 사용자로 HELLO 하면 끊는다")
    void anUnknownUserIsClosed() throws Exception {
        Client client = connect();

        client.hello(UUID.randomUUID().toString());

        assertThat(client.closed.await(WAIT.toMillis(), TimeUnit.MILLISECONDS)).isTrue();
        assertThat(client.closeStatus.getCode()).isEqualTo(CloseStatus.POLICY_VIOLATION.getCode());
    }

    @Test
    @DisplayName("연결을 닫으면 레지스트리에서 빠진다")
    void closingRemovesTheBinding() throws Exception {
        String me = createUser();
        int before = registry.connectionCount();
        Client client = connectAs(me);
        assertThat(registry.connectionCount()).isEqualTo(before + 1);

        client.session.close();

        // 닫힘은 서버 쪽 스레드에서 처리되므로 잠깐 기다립니다.
        long deadline = System.currentTimeMillis() + WAIT.toMillis();
        while (registry.connectionCount() != before && System.currentTimeMillis() < deadline) {
            Thread.sleep(20);
        }
        assertThat(registry.connectionCount()).isEqualTo(before);
    }

    /** 테스트용 클라이언트. 받은 프레임을 큐에 쌓고, 닫히면 래치를 내립니다. */
    private final class Client extends TextWebSocketHandler {

        final BlockingQueue<JsonNode> inbox = new LinkedBlockingQueue<>();
        final CountDownLatch closed = new CountDownLatch(1);
        volatile CloseStatus closeStatus;
        WebSocketSession session;

        @Override
        protected void handleTextMessage(WebSocketSession session, TextMessage message) {
            inbox.add(objectMapper.readTree(message.getPayload()));
        }

        @Override
        public void afterConnectionClosed(WebSocketSession session, CloseStatus status) {
            closeStatus = status;
            closed.countDown();
        }

        void hello(String userId) throws IOException {
            session.sendMessage(new TextMessage("{\"type\":\"HELLO\",\"userId\":\"" + userId + "\"}"));
        }

        JsonNode next() throws InterruptedException {
            JsonNode frame = inbox.poll(WAIT.toMillis(), TimeUnit.MILLISECONDS);
            assertThat(frame).as(WAIT.toSeconds() + "초 안에 프레임이 오지 않았습니다.").isNotNull();
            return frame;
        }

        boolean nothingWithin(Duration duration) throws InterruptedException {
            return inbox.poll(duration.toMillis(), TimeUnit.MILLISECONDS) == null;
        }

        void closeQuietly() {
            try {
                if (session != null && session.isOpen()) {
                    session.close();
                }
            } catch (IOException ignored) {
                // 테스트 뒷정리라 실패해도 할 일이 없습니다.
            }
        }
    }

    private Client connect() throws Exception {
        Client client = new Client();
        client.session = new StandardWebSocketClient()
                .execute(client, "ws://localhost:" + port + "/ws/notifications")
                .get(WAIT.toMillis(), TimeUnit.MILLISECONDS);
        clients.add(client);
        return client;
    }

    private Client connectAs(String userId) throws Exception {
        Client client = connect();
        client.hello(userId);
        assertThat(client.next().path("type").asText()).isEqualTo("HELLO_ACK");
        return client;
    }

    private String createUser() throws Exception {
        String body = mvc.perform(post("/api/v1/accounts")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content("{\"deviceId\":\"" + UUID.randomUUID() + "\"}"))
                .andExpect(status().isCreated())
                .andReturn().getResponse().getContentAsString();
        return objectMapper.readTree(body).get("userId").asText();
    }

    private void sendRequest(String from, String to) throws Exception {
        mvc.perform(post("/api/v1/friend-requests")
                        .header(USER_ID_HEADER, from)
                        .contentType(MediaType.APPLICATION_JSON)
                        .content("{\"userId\":\"" + to + "\"}"))
                .andExpect(status().isCreated());
    }

    private void accept(String me, String requester) throws Exception {
        mvc.perform(post("/api/v1/friend-requests/{userId}/accept", requester).header(USER_ID_HEADER, me))
                .andExpect(status().isNoContent());
    }

    private void befriend(String a, String b) throws Exception {
        sendRequest(a, b);
        accept(b, a);
    }

    private void invite(String from, String to, String roomCode) throws Exception {
        mvc.perform(post("/api/v1/invites")
                        .header(USER_ID_HEADER, from)
                        .contentType(MediaType.APPLICATION_JSON)
                        .content("{\"userId\":\"" + to + "\",\"roomCode\":\"" + roomCode + "\"}"))
                .andExpect(status().isCreated());
    }
}
