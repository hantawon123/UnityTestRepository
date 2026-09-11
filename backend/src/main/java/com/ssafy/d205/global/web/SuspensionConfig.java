package com.ssafy.d205.global.web;

import lombok.RequiredArgsConstructor;
import org.springframework.context.annotation.Configuration;
import org.springframework.web.servlet.config.annotation.InterceptorRegistry;
import org.springframework.web.servlet.config.annotation.WebMvcConfigurer;

/**
 * {@link SuspensionInterceptor} 를 어디에 걸지 정합니다.
 *
 * <p>기본이 "전부"이고 빼는 쪽을 적습니다. 반대로 두면(걸 경로를 하나씩 나열) 새 API 가
 * 생길 때마다 여기 적어야 하고, 적지 않은 것이 조용히 열린 채로 남습니다. 정지 검사는
 * 빠뜨리면 기능이 안 되는 쪽이 아니라 <b>막았다고 생각한 것이 안 막히는</b> 쪽이라
 * 실수를 알아채기 어렵습니다.
 */
@Configuration
@RequiredArgsConstructor
public class SuspensionConfig implements WebMvcConfigurer {

    private final SuspensionInterceptor suspensionInterceptor;

    @Override
    public void addInterceptors(InterceptorRegistry registry) {
        registry.addInterceptor(suspensionInterceptor)
                .addPathPatterns("/**")

                // 운영자 화면은 X-User-Id 를 쓰지 않고 세션으로 인증합니다. 정지된 사람의
                // 계정을 해제하는 것도 이 경로라, 검사를 걸면 정지를 푸는 길이 막힙니다.
                .excludePathPatterns("/api/v1/admin/**", "/admin", "/admin/**")

                // 헬스 체크는 헤더가 없어 어차피 통과하지만, 조회 한 번을 아끼려고 뺍니다.
                // 자동화가 짧은 주기로 부르는 자리입니다.
                .excludePathPatterns("/actuator/**")

                // Photon 이 부르는 인증 경로입니다. userId 를 헤더가 아니라 쿼리로 받으므로
                // 지금 구현으로는 어차피 통과하지만, 검사 기준이 바뀌면 이 엔드포인트가
                // 자기 자신을 막는 모양이 됩니다 - 정지 여부를 묻는 곳이 정지 때문에
                // 막히면 막으려던 사람이 오히려 들어옵니다(Photon 은 오류를 고장으로 읽고
                // 통과시킵니다).
                .excludePathPatterns("/api/v1/photon/**");
    }
}
