package com.ssafy.d205.domain.report.service;

import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.HashMap;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

import com.ssafy.d205.domain.report.dto.ReportDetail;
import com.ssafy.d205.domain.report.dto.ReportedUserListResponse;
import com.ssafy.d205.domain.report.dto.ReportedUserSummary;
import com.ssafy.d205.domain.report.entity.ReportStatus;
import com.ssafy.d205.domain.report.entity.UserReport;
import com.ssafy.d205.domain.report.repository.ReasonCountRow;
import com.ssafy.d205.domain.report.repository.UserReportRepository;
import com.ssafy.d205.domain.user.entity.User;
import com.ssafy.d205.domain.user.repository.UserRepository;
import com.ssafy.d205.global.common.TimeProvider;
import com.ssafy.d205.global.exception.TargetUserNotFoundException;

/**
 * 운영자가 신고를 읽고 마무리합니다.
 *
 * <p>신고를 접수하는 {@link ReportService} 와 나눈 이유는 부르는 사람이 다르기
 * 때문입니다. 접수는 게임 클라이언트가 인증 없이 부르고, 이쪽은 로그인한 운영자만
 * 부릅니다. 한 서비스에 두면 "이 메서드는 누가 부를 수 있는가"를 매번 따져야 합니다.
 */
@Service
@RequiredArgsConstructor
public class ReportReviewService {

    private final UserReportRepository userReportRepository;
    private final UserRepository userRepository;
    private final TimeProvider timeProvider;

    /**
     * 신고당한 사람들을 묶어서 돌려줍니다.
     *
     * <p>사유별 건수를 따로 조회해 붙입니다. 한 쿼리로 합치면 사람마다 사유 수만큼
     * 행이 늘어나 건수와 인원수가 부풀려집니다.
     */
    @Transactional(readOnly = true)
    public ReportedUserListResponse list(ReportStatus status) {
        Map<String, Map<String, Integer>> reasons = reasonsByUser(status);

        return new ReportedUserListResponse(
                userReportRepository.summarizeByStatus(status.name()).stream()
                        .map(row -> new ReportedUserSummary(
                                row.getUserId(),
                                row.getNickname(),
                                row.getReportCount(),
                                row.getReporterCount(),
                                row.getFromDeletedAccounts(),
                                reasons.getOrDefault(row.getUserId(), Map.of()),
                                row.getLastReportedAt()))
                        .toList());
    }

    /** 한 사람에 대한 신고를 하나씩. 최근 순입니다. */
    @Transactional(readOnly = true)
    public ReportDetail.ListResponse detail(String userId) {
        // 신고가 하나도 없는 사람과 없는 계정을 구분합니다. 전자는 빈 목록이고
        // 후자는 404 입니다. 구분하지 않으면 오타로 부른 것과 정상 조회가 같아 보입니다.
        target(userId);

        return new ReportDetail.ListResponse(
                userReportRepository.findByReportedUserId(userId).stream()
                        .map(row -> new ReportDetail(
                                row.getReason(), row.getMemo(),
                                row.getCreatedAt(), row.getStatus()))
                        .toList());
    }

    /**
     * 한 사람의 미검토 신고를 한 번에 마무리합니다.
     *
     * <p>신고 한 건이 아니라 사람 단위로 처리하는 이유는 운영자가 사람을 보고 판단하기
     * 때문입니다. 다섯 건 쌓인 사람을 다섯 번 누르게 할 이유가 없습니다.
     *
     * <p><b>이미 검토한 것은 건드리지 않습니다.</b> 어제 기각한 것을 오늘 조치함으로
     * 덮으면 그때 무슨 판단을 했는지가 사라집니다. 검토 뒤에 새로 들어온 신고만
     * 미검토로 남아 다음 차례에 다시 올라옵니다.
     *
     * <p>처리할 것이 없어도 성공입니다. 두 사람이 같은 화면을 보고 있다가 둘 다 눌렀을
     * 때, 뒤에 누른 쪽에게 오류를 주면 무엇이 잘못됐는지 알 수 없습니다. 원하는 결과는
     * 이미 이루어져 있습니다.
     *
     * @return 이번에 마무리한 건수
     */
    @Transactional
    public int review(String userId, ReportStatus decision, String reviewer) {
        User target = target(userId);

        List<UserReport> pending = userReportRepository.findPendingAbout(target.getSeq());
        String now = timeProvider.now();

        for (UserReport report : pending) {
            report.review(decision, reviewer, now);
        }

        return pending.size();
    }

    /**
     * 사용자별 사유 분포를 미리 모아 둡니다.
     *
     * <p>목록에 스무 명이 있을 때 사람마다 따로 물으면 쿼리가 스물한 번 나갑니다.
     */
    private Map<String, Map<String, Integer>> reasonsByUser(ReportStatus status) {
        Map<String, Map<String, Integer>> byUser = new HashMap<>();

        for (ReasonCountRow row : userReportRepository.countReasonsByStatus(status.name())) {
            byUser.computeIfAbsent(row.getUserId(), key -> new LinkedHashMap<>())
                    .put(row.getReason(), row.getCount());
        }

        return byUser;
    }

    private User target(String userId) {
        return userRepository.findByPublicId(userId)
                .orElseThrow(() -> new TargetUserNotFoundException(userId));
    }
}
