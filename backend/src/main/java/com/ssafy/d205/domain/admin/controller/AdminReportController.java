package com.ssafy.d205.domain.admin.controller;

import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.security.core.Authentication;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PatchMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import com.ssafy.d205.domain.report.dto.ReportDetail;
import com.ssafy.d205.domain.report.dto.ReportedUserListResponse;
import com.ssafy.d205.domain.report.dto.ReviewReportRequest;
import com.ssafy.d205.domain.report.entity.ReportStatus;
import com.ssafy.d205.domain.report.service.ReportReviewService;

/**
 * 운영자가 신고를 보고 마무리합니다.
 *
 * <p>경로가 /api/v1/admin 아래인 것이 곧 접근 규칙입니다. SecurityConfig 의 첫 체인이
 * 이 경로 전체에 로그인을 요구하므로, 여기 메서드마다 권한을 적을 필요가 없습니다.
 * 반대로 <b>이 경로 밖에 관리 기능을 만들면 아무나 부를 수 있습니다.</b>
 *
 * <p>신고를 접수하는 것은 /api/v1/reports 이고 인증이 없습니다. 게임 클라이언트가
 * 부르는 자리라 성격이 반대입니다.
 */
@RestController
@RequestMapping("/api/v1/admin/reports")
@RequiredArgsConstructor
public class AdminReportController {

    private final ReportReviewService reportReviewService;

    /**
     * 신고당한 사람 목록. 기본은 아직 보지 않은 것입니다.
     *
     * <p>신고 한 건씩이 아니라 <b>사람 단위로 묶어서</b> 돌려줍니다. 한 건씩 나열하면
     * 같은 사람에 대한 다섯 건이 흩어져 나와, 그 사람이 문제인지 신고한 사람이
     * 문제인지 구분되지 않습니다.
     *
     * <p>status 로 이미 검토한 것도 볼 수 있습니다. 지난 판단을 확인할 방법이 없으면
     * "그때 왜 기각했지"를 DB 를 직접 열어야 알 수 있습니다.
     */
    @GetMapping
    public ReportedUserListResponse list(
            @RequestParam(defaultValue = "PENDING") ReportStatus status) {
        return reportReviewService.list(status);
    }

    /**
     * 한 사람에 대한 신고를 하나씩. 목록의 사유 분포로 부족할 때 봅니다.
     *
     * <p>status 를 주면 그 상태만 봅니다. 미검토 목록에서 펼쳤는데 예전에 처리한 것까지
     * 섞여 나오면 지금 무엇을 판단해야 하는지가 흐려집니다.
     *
     * <p>비워 두면 전부 봅니다. "이 사람 그동안 어땠나"를 볼 때 씁니다.
     */
    @GetMapping("/{userId}")
    public ReportDetail.ListResponse detail(@PathVariable String userId,
                                            @RequestParam(required = false) ReportStatus status) {
        return reportReviewService.detail(userId, status);
    }

    /**
     * 그 사람의 미검토 신고를 한 번에 마무리합니다.
     *
     * <p>경로가 신고 번호가 아니라 사용자입니다. 운영자는 신고 한 건이 아니라 사람을
     * 보고 판단하므로, 다섯 건 쌓인 사람을 다섯 번 누르게 할 이유가 없습니다.
     *
     * <p>이미 검토한 신고는 그대로 둡니다. 검토 뒤에 새로 들어온 것만 미검토로 남아
     * 다음 차례에 다시 올라옵니다.
     *
     * <p>처리할 것이 없어도 200 입니다. 두 사람이 같은 화면을 보다가 둘 다 눌렀을 때
     * 뒤에 누른 쪽에게 오류를 주면 무엇이 잘못됐는지 알 수 없는데, 원하는 결과는 이미
     * 이루어져 있습니다. 응답의 reviewed 가 0 이면 그런 경우입니다.
     */
    @PatchMapping("/{userId}")
    public ReviewResult review(@PathVariable String userId,
                               @Valid @RequestBody ReviewReportRequest request,
                               Authentication authentication) {
        int reviewed = reportReviewService.review(
                userId, request.status(), authentication.getName());

        return new ReviewResult(reviewed);
    }

    /** @param reviewed 이번 요청이 마무리한 건수. 0 이면 이미 처리돼 있었다는 뜻입니다. */
    public record ReviewResult(int reviewed) {
    }
}
