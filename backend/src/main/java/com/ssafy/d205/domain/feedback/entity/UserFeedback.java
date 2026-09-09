package com.ssafy.d205.domain.feedback.entity;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import lombok.AccessLevel;
import lombok.Getter;
import lombok.NoArgsConstructor;

/**
 * 플레이어가 설정 화면에서 보낸 피드백 한 건.
 *
 * <p><b>이 행은 아무 동작도 일으키지 않습니다.</b> 신고와 같습니다. 어느 조회 경로도
 * 이 테이블을 읽지 않고 운영자가 나중에 읽습니다.
 *
 * <p>신고와 다른 점은 <b>바뀌는 값이 하나도 없다</b>는 것입니다. 신고에는 검토 상태가
 * 있어서 운영자가 마무리 표시를 하지만, 피드백에는 판단할 것이 없습니다. 읽었다는
 * 표시를 두면 "읽음"이 "처리했음"처럼 읽히고, 실제로는 아무것도 하지 않은 채 목록만
 * 깨끗해집니다. 그래서 이 엔티티에는 상태를 바꾸는 메서드가 없습니다.
 *
 * <p>고치는 API 도, 지우는 API 도 없습니다. 보낸 사람이 취소할 수 없다는 뜻인데, 취소를
 * 허용하면 "썼다가 지운 피드백"을 남길지 정해야 하고 그 질문에 답할 근거가 없습니다.
 */
@Entity
@Table(name = "user_feedback")
@Getter
@NoArgsConstructor(access = AccessLevel.PROTECTED)
public class UserFeedback {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "user_feedback_seq")
    private Integer seq;

    /**
     * 쓴 사람. <b>탈퇴하면 NULL 이 됩니다.</b>
     *
     * <p>내용을 지우지 않고 작성자만 비우는 이유는 V14 주석에 있습니다. 떠난 사람이
     * 남긴 지적은 그 사람이 떠난 뒤에도 사실입니다.
     */
    @Column(name = "author_seq")
    private Integer authorSeq;

    /** 본문. 500자까지입니다. 클라이언트 화면 상한과 같은 값입니다. */
    @Column(name = "message", nullable = false, length = 500)
    private String message;

    /** 보낸 클라이언트의 빌드. 클라이언트가 안 보내면 null 입니다. */
    @Column(name = "build_ver", length = 32)
    private String buildVer;

    /** 실행 환경. 클라이언트가 안 보내면 null 입니다. */
    @Column(name = "platform", length = 16)
    private String platform;

    /** 보낸 시각. yyyyMMddHHmmss, UTC. */
    @Column(name = "created_at", nullable = false, length = 14)
    private String createdAt;

    private UserFeedback(Integer authorSeq, String message, String buildVer, String platform, String now) {
        this.authorSeq = authorSeq;
        this.message = message;
        this.buildVer = buildVer;
        this.platform = platform;
        this.createdAt = now;
    }

    public static UserFeedback of(Integer authorSeq, String message,
                                  String buildVer, String platform, String now) {
        return new UserFeedback(authorSeq, message, buildVer, platform, now);
    }
}
