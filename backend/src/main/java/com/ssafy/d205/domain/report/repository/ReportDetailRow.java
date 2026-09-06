package com.ssafy.d205.domain.report.repository;

/**
 * 신고 한 건. 상세 화면의 한 줄입니다.
 *
 * <p>신고자가 누구인지는 담지 않습니다. 운영자가 판단할 때 필요한 것은 무엇이 몇 번
 * 일어났는가이고, 신고자를 드러내면 보복의 여지가 생깁니다.
 */
public interface ReportDetailRow {

    String getReason();

    /** 신고자가 적은 한 줄. 없을 수 있습니다. */
    String getMemo();

    /** yyyyMMddHHmmss, UTC. */
    String getCreatedAt();

    String getStatus();
}
