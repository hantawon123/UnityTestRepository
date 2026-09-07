package com.ssafy.d205.domain.analytics.service;

import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;
import org.springframework.transaction.event.TransactionPhase;
import org.springframework.transaction.event.TransactionalEventListener;

import com.ssafy.d205.domain.analytics.config.AnalyticsDatabase;
import com.ssafy.d205.domain.user.event.AccountDeletedEvent;

/**
 * 탈퇴한 사람을 플레이 로그에서 지웁니다. 행은 남기고 사람만 지웁니다.
 *
 * <p>게임 DB 는 탈퇴 시 CASCADE 로 흔적을 지우고, 그것을 "탈퇴는 흔적을 남기지 않는다"는 약속으로
 * 적어 두었습니다. game_event 는 FK 가 없어 그냥 두면 user_public_id 가 남으므로 같은 약속을 여기서
 * 지킵니다(docs/analytics-events.md 8절).
 *
 * <p>행을 지우지 않는 이유: 경기 집계는 "누군가 여기 숨겼다"만 알면 되고 그게 누구였는지는 필요
 * 없습니다. 행을 지우면 같은 경기의 다른 다섯 명 집계가 뒤틀리고, 사람만 지우면 아무것도 뒤틀리지
 * 않습니다. params 안의 owner_id 같은 값은 건드리지 않습니다. 최상위 컬럼이 NULL 이 되면
 * match_start.players 연결이 끊겨 이미 이어지지 않습니다.
 *
 * <p><b>best-effort 입니다.</b> AFTER_COMMIT 이라 탈퇴는 이미 끝났고, 여기서 실패해도 되돌릴 것이
 * 없습니다. 분석 DB 가 죽어 있으면 WARN 만 남기고, 그 로그를 본 사람이 같은 UPDATE 를 나중에
 * 실행합니다. 탈퇴를 막는 것보다 그 편이 약속에 가깝습니다.
 *
 * <p>user_public_id 선두 인덱스가 없어 풀 스캔입니다. 탈퇴는 드물어 지금은 두지 않습니다.
 */
@Component
@Slf4j
@RequiredArgsConstructor
public class GameEventEraser {

    private static final String ERASE = "UPDATE game_event SET user_public_id = NULL WHERE user_public_id = ?";

    private final AnalyticsDatabase database;

    @TransactionalEventListener(phase = TransactionPhase.AFTER_COMMIT)
    public void onAccountDeleted(AccountDeletedEvent event) {
        erase(event.publicId());
    }

    /**
     * @return 지운 행 수. 분석 DB 가 준비되지 않았거나 실패하면 -1
     */
    public int erase(String publicId) {
        if (!database.isReady()) {
            log.warn("분석 DB 가 준비되지 않아 탈퇴한 계정을 플레이 로그에서 지우지 못했습니다. "
                    + "나중에 직접 실행하세요: UPDATE game_event SET user_public_id = NULL WHERE user_public_id = '{}'",
                    publicId);
            return -1;
        }
        try {
            int erased = database.jdbcTemplate().update(ERASE, publicId);
            if (erased > 0) {
                log.info("탈퇴한 계정의 플레이 로그 {}건에서 식별자를 지웠습니다.", erased);
            }
            return erased;
        } catch (Exception e) {
            log.warn("탈퇴한 계정을 플레이 로그에서 지우지 못했습니다. 나중에 직접 실행하세요: "
                    + "UPDATE game_event SET user_public_id = NULL WHERE user_public_id = '{}'. 원인: {}",
                    publicId, e.getMessage());
            return -1;
        }
    }
}
