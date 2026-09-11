package com.ssafy.d205.domain.photon;

import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;

import javax.crypto.Mac;
import javax.crypto.spec.SecretKeySpec;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.util.Base64;
import java.util.HexFormat;

/**
 * Photon 커스텀 인증에 쓰는 토큰을 만들고 검증합니다.
 *
 * <p><b>왜 필요한가.</b> Photon 은 클라이언트가 보낸 인증 파라미터를 그대로 우리에게
 * 전달할 뿐입니다. userId 만 받으면 정지된 사람이 남의 값이나 아무 값이나 넣어 통과합니다.
 * 그래서 "이 클라이언트가 정말 그 계정인가"를 증명할 값이 함께 와야 합니다.
 *
 * <p><b>HMAC 입니다. 저장하지 않습니다.</b> 토큰은 {@code HMAC-SHA256(비밀, publicId)} 이고
 * 같은 입력에 늘 같은 값이 나옵니다. 발급 기록을 남기지 않으므로 테이블도, 만료를 쓸어내는
 * 일도 없습니다. 서버 비밀을 모르면 남의 토큰을 만들 수 없다는 것이 이 방식이 주는 전부이고,
 * 여기서 필요한 것도 그것뿐입니다.
 *
 * <p><b>만료가 없습니다.</b> 한 번 받은 토큰은 계속 씁니다. 세션 토큰이 아니라 "이 id 는
 * 서버가 이 클라이언트에게 준 것"이라는 증명이기 때문입니다. 유출되면 그 계정으로 게임에
 * 접속할 수 있지만, 그건 publicId 와 기기 식별자가 유출된 것과 같은 수준의 일입니다.
 * 무르려면 비밀을 바꾸면 되고 그러면 모두의 토큰이 한 번에 무효가 됩니다.
 *
 * <p><b>비밀이 없으면 토큰을 만들지 않습니다.</b> 설정을 잊은 서버가 빈 비밀로
 * 예측 가능한 토큰을 뿌리는 것보다, 토큰이 없어 인증이 통과(fail-open)하는 편이 낫습니다.
 * 그 상태는 로그로 알립니다.
 */
@Component
@Slf4j
public class PhotonAuthTokens {

    private static final String ALGORITHM = "HmacSHA256";

    private final byte[] secret;

    public PhotonAuthTokens(@Value("${photon.auth.secret:}") String secret) {
        this.secret = secret == null || secret.isBlank()
                ? null
                : secret.getBytes(StandardCharsets.UTF_8);

        if (this.secret == null) {
            log.warn("photon.auth.secret 이 비어 있습니다. Photon 인증 토큰을 발급하지 않고, "
                    + "인증 요청은 전부 통과시킵니다. 운영에서는 반드시 설정하세요.");
        }
    }

    /** 비밀이 설정돼 있는가. 없으면 이 기능 전체가 꺼진 것과 같습니다. */
    public boolean isEnabled() {
        return secret != null;
    }

    /**
     * 그 계정의 토큰. 비밀이 없으면 null 이고, 응답에서 필드가 빠집니다.
     */
    public String issue(String publicId) {
        if (secret == null || publicId == null) {
            return null;
        }
        return Base64.getUrlEncoder().withoutPadding().encodeToString(sign(publicId));
    }

    /**
     * 토큰이 그 계정의 것인가.
     *
     * <p>{@link MessageDigest#isEqual} 로 비교합니다. 바이트마다 일찍 끝나는 비교는 맞는
     * 앞부분이 길수록 오래 걸려, 그 시간 차이로 토큰을 한 글자씩 맞혀 나갈 수 있습니다.
     */
    public boolean matches(String publicId, String token) {
        if (secret == null || publicId == null || token == null || token.isBlank()) {
            return false;
        }

        byte[] presented;
        try {
            presented = Base64.getUrlDecoder().decode(token);
        } catch (IllegalArgumentException e) {
            // 토큰 자리에 아무 문자열이나 들어온 경우입니다. 예외를 밖으로 내보내면
            // 500 이 나가고, Photon 에게 그것은 "인증 서버 고장"이라 fail-open 으로
            // 통과시킵니다. 여기서 조용히 false 로 끝내는 편이 뜻에 맞습니다.
            return false;
        }

        return MessageDigest.isEqual(presented, sign(publicId));
    }

    private byte[] sign(String publicId) {
        try {
            Mac mac = Mac.getInstance(ALGORITHM);
            mac.init(new SecretKeySpec(secret, ALGORITHM));
            return mac.doFinal(publicId.getBytes(StandardCharsets.UTF_8));
        } catch (Exception e) {
            // HmacSHA256 은 모든 JDK 에 있고 키도 우리가 만든 것이라 여기 올 일이 없습니다.
            // 온다면 설정이 아니라 런타임이 잘못된 것이므로 감추지 않습니다.
            throw new IllegalStateException("Photon 인증 토큰을 만들지 못했습니다.", e);
        }
    }

    /** 로그에 비밀을 남기지 않으면서 어느 비밀이 걸려 있는지 구분하려고 씁니다. */
    public String fingerprint() {
        if (secret == null) {
            return "(없음)";
        }
        return HexFormat.of().formatHex(sign("fingerprint"), 0, 4);
    }
}
