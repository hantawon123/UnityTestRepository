package com.ssafy.d205.domain.admin.controller;

import lombok.RequiredArgsConstructor;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import com.ssafy.d205.domain.feedback.dto.FeedbackListResponse;
import com.ssafy.d205.domain.feedback.service.FeedbackReadService;

/**
 * 운영자가 피드백을 읽습니다.
 *
 * <p>경로가 /api/v1/admin 아래인 것이 곧 접근 규칙입니다. SecurityConfig 의 첫 체인이
 * 이 경로 전체에 로그인을 요구하므로 메서드마다 권한을 적을 필요가 없습니다. 반대로
 * <b>이 경로 밖에 관리 기능을 만들면 아무나 부를 수 있습니다.</b>
 *
 * <p>신고와 달리 쓰는 API 가 없습니다. 검토 상태를 두지 않았으므로 운영자가 바꿀 값이
 * 없습니다. 읽음 표시를 두지 않은 이유는 UserFeedback 주석에 있습니다.
 */
@RestController
@RequestMapping("/api/v1/admin/feedback")
@RequiredArgsConstructor
public class AdminFeedbackController {

    private final FeedbackReadService feedbackReadService;

    /**
     * 최근 피드백. 한 건씩 그대로 봅니다.
     *
     * <p>신고 목록처럼 사람 단위로 묶지 않습니다. 신고는 "이 사람이 몇 번 신고당했나"가
     * 판단 단위지만 피드백은 한 건 한 건이 읽을 내용이라, 묶으면 정작 본문이 사라집니다.
     *
     * <p>limit 을 크게 줘도 상한(200)으로 깎입니다. 400 을 주지 않는 이유는
     * FeedbackReadService 의 상수 주석에 있습니다.
     */
    @GetMapping
    public FeedbackListResponse list(@RequestParam(required = false) Integer limit) {
        return feedbackReadService.recent(limit);
    }
}
