package com.ssafy.d205.domain.feedback.repository;

/**
 * 피드백 한 건. 운영자 목록의 한 줄입니다.
 *
 * <p>신고 목록처럼 사람 단위로 묶지 않습니다. 신고는 "이 사람이 몇 번 신고당했나"가
 * 판단의 단위지만, 피드백은 한 건 한 건이 읽을 내용입니다. 묶으면 정작 읽어야 하는
 * 본문이 사라집니다.
 */
public interface FeedbackRow {

    /**
     * 쓴 사람의 공개 식별자. <b>탈퇴했으면 null 입니다.</b>
     *
     * <p>users_seq 가 아니라 public_id 를 내보냅니다. 운영자 화면도 결국 브라우저이고,
     * users_seq 는 가입자 수가 드러나는 값이라 내보내지 않는다는 규칙이 여기서도
     * 같습니다(V1 주석).
     */
    String getAuthorUserId();

    /** 쓴 사람의 닉네임. 탈퇴했으면 null 입니다. */
    String getAuthorNickname();

    /** 본문. */
    String getMessage();

    /** 어느 빌드에서 왔나. 클라이언트가 안 보냈으면 null 입니다. */
    String getBuildVer();

    /** 실행 환경. 클라이언트가 안 보냈으면 null 입니다. */
    String getPlatform();

    /** 보낸 시각. yyyyMMddHHmmss, UTC. 목록 정렬의 기준입니다. */
    String getCreatedAt();
}
