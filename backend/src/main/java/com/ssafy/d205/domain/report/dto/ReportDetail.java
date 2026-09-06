package com.ssafy.d205.domain.report.dto;

import java.util.List;

/**
 * @param reason    ReportReason 의 이름
 * @param memo      신고자가 적은 한 줄. 없으면 null
 * @param createdAt yyyyMMddHHmmss, UTC
 * @param status    ReportStatus 의 이름
 */
public record ReportDetail(
        String reason,
        String memo,
        String createdAt,
        String status
) {
    /** @param reports 그 사람에 대한 신고. 최근 순입니다. */
    public record ListResponse(List<ReportDetail> reports) {
    }
}
