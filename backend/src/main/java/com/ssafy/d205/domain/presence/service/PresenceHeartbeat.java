package com.ssafy.d205.domain.presence.service;

import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;

import java.util.Set;

import com.ssafy.d205.domain.notification.service.NotificationSessionRegistry;

/**
 * 알림 채널에 붙어 있는 사람들의 하트비트를 주기적으로 밉니다.
 *
 * <p><b>클라이언트의 30초 주기 PUT 을 대체하는 자리입니다</b>(S15P21D205-890). 접속자 천
 * 명이면 예전에는 30초마다 요청 천 개가 들어와 각각 트랜잭션을 열고 users 와
 * user_presence 를 조회한 뒤 UPDATE 를 냈습니다. 지금은 접속자가 몇 명이든 UPDATE 문장
 * 하나입니다.
 *
 * <p>그래도 하트비트를 아예 없애지 않은 이유는 {@link
 * com.ssafy.d205.domain.presence.entity.PresenceTimeout} 의 만료 판정을 살리기
 * 위해서입니다. 친구 목록과 초대 차단이 그 판정을 지나고, 무엇도 heartbeat_at 을 밀지
 * 않으면 <b>붙어 있는 사람이 90초 뒤 전부 오프라인으로 읽힙니다.</b>
 *
 * <p>만료 판정을 살려 둔 값은 <b>서버가 죽었다 살아났을 때</b>입니다. 그때 세션
 * 레지스트리는 비어 있으므로 죽은 서버가 남긴 ONLINE 행은 더 이상 갱신되지 않고, 90초
 * 뒤 PresenceSweeper 가 내립니다. 기동 시 일괄 정리를 하지 않는 이유는 그 방식이
 * 인스턴스를 늘리는 날 <b>다른 인스턴스의 살아 있는 사용자를 꺼버리기</b> 때문입니다.
 *
 * <p>간격은 타임아웃(90초)보다 <b>확실히 짧아야</b> 합니다. 길면 갱신 사이에 만료가 먼저
 * 와서 접속자가 주기적으로 오프라인으로 깜빡입니다. 기본값 30초는 타임아웃의 3분의
 * 1이라 한 번 놓쳐도 여유가 있습니다.
 *
 * <p>ping 과 같은 30초이지만 스케줄러를 따로 둡니다. NotificationKeepAlive 는 죽은 연결을
 * 걷어내는 일만 하고, 이 클래스는 살아남은 사람들을 DB 에 반영합니다. 둘의 순서는
 * 중요하지 않습니다 — 방금 죽은 연결의 하트비트를 한 번 더 밀어도, 그 연결은 같은 틱에
 * unbind 되면서 오프라인으로 쓰이기 때문입니다.
 */
@Component
@RequiredArgsConstructor
@Slf4j
public class PresenceHeartbeat {

    private final NotificationSessionRegistry registry;
    private final PresenceService presenceService;

    /**
     * 간격을 설정으로 뺀 이유는 테스트입니다. PresenceSweeper 와 같은 사정으로,
     * 스케줄러가 테스트 중에 돌면 하트비트를 과거로 밀어 크래시를 재현하는 테스트와
     * 경합합니다. application-test.yml 이 한 시간으로 늘려 사실상 끕니다.
     */
    @Scheduled(fixedDelayString = "${presence.heartbeat-refresh-ms:30000}")
    public void refresh() {
        Set<Integer> connected = registry.boundUserSeqs();
        if (connected.isEmpty()) {
            return;
        }
        int refreshed = presenceService.refreshHeartbeats(connected);
        log.debug("붙어 있는 {}명 중 {}개 행의 하트비트를 갱신했습니다.", connected.size(), refreshed);
    }
}
