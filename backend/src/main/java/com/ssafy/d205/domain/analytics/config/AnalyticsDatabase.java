package com.ssafy.d205.domain.analytics.config;

import com.zaxxer.hikari.HikariConfig;
import com.zaxxer.hikari.HikariDataSource;
import jakarta.annotation.PreDestroy;
import lombok.extern.slf4j.Slf4j;
import org.flywaydb.core.Flyway;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.stereotype.Component;

import java.time.Duration;
import java.util.concurrent.atomic.AtomicBoolean;

/**
 * 분석 DB 접속. 게임 DB 와 <b>완전히 따로</b> 갑니다.
 *
 * <p>{@code DataSource} 빈으로 노출하지 않고 이 객체 안에 감춥니다. 이유가 있습니다.
 * Spring Boot 는 컨텍스트에 DataSource 빈이 하나라도 있으면 기본 DataSource 자동 설정을
 * 건너뜁니다. 분석용을 빈으로 두면 게임 DB 접속이 사라지고, 그걸 살리려면 게임 쪽도 손으로
 * 만들어야 하는데 그러면 테스트의 &#64;ServiceConnection 이 붙일 곳이 없어집니다. 감추면 게임
 * 쪽은 지금까지와 똑같고, 분석 쪽은 이 클래스 하나만 봅니다.
 *
 * <p>같은 이유로 Flyway 도 여기서 직접 돌립니다. 자동 설정은 기본 DataSource 하나만 다룹니다.
 *
 * <p><b>기동을 막지 않습니다.</b> 게임 DB 마이그레이션이 실패하면 앱이 뜨지 않지만, 분석 DB 는
 * 실패해도 뜹니다. "이벤트 수집이 막혀도 게임 API 는 산다"가 이 설계의 첫 번째 약속이고,
 * 배포 파이프라인의 기동 검증이 로그 시스템 문제로 게임 배포를 막아서는 안 됩니다. 대신
 * ERROR 로그를 남기고 {@link #isReady()} 를 false 로 두며, 플러셔가 30초마다 다시 시도합니다.
 * 그동안 들어온 이벤트는 큐가 차면 버려지고 드롭 카운터에 잡힙니다.
 *
 * <p>풀이 작고(3개) 커넥션 타임아웃이 짧은(2초) 것도 같은 이유입니다. 분석 DB 가 느려질 때
 * 수집 스레드가 오래 매달려 있으면 안 됩니다.
 */
@Component
@Slf4j
public class AnalyticsDatabase {

    /** 마이그레이션 재시도 간격. 플러셔가 이 간격으로 {@link #tryMigrate()} 를 다시 부릅니다. */
    public static final Duration RETRY_INTERVAL = Duration.ofSeconds(30);

    private static final int POOL_SIZE = 3;
    private static final Duration CONNECTION_TIMEOUT = Duration.ofSeconds(2);

    private final HikariDataSource dataSource;
    private final JdbcTemplate jdbcTemplate;
    private final AtomicBoolean ready = new AtomicBoolean(false);

    public AnalyticsDatabase(AnalyticsProperties properties) {
        AnalyticsProperties.Datasource ds = properties.datasource();

        HikariConfig config = new HikariConfig();
        config.setPoolName("analytics");
        config.setJdbcUrl(ds.url());
        config.setUsername(ds.username());
        config.setPassword(ds.password());
        config.setMaximumPoolSize(POOL_SIZE);
        config.setConnectionTimeout(CONNECTION_TIMEOUT.toMillis());
        // 기동 시점에 붙지 못해도 풀은 만들어져야 합니다. 기본값(true 에 해당하는 동작)은
        // 첫 커넥션을 생성자에서 열어 보고 실패하면 예외를 던져 앱이 뜨지 못합니다.
        config.setInitializationFailTimeout(-1);

        this.dataSource = new HikariDataSource(config);
        this.jdbcTemplate = new JdbcTemplate(dataSource);

        tryMigrate();
    }

    /**
     * 스키마와 테이블을 맞춥니다. 성공하면 true.
     *
     * <p>실패는 예외로 올리지 않고 로그로 남깁니다. 기동 시점에도, 플러셔의 재시도에서도
     * 같은 메서드를 씁니다. 이미 준비됐으면 아무것도 하지 않습니다.
     */
    public boolean tryMigrate() {
        if (ready.get()) {
            return true;
        }
        try {
            Flyway.configure()
                    .dataSource(dataSource)
                    .locations("classpath:db/migration-analytics")
                    .load()
                    .migrate();
            ready.set(true);
            log.info("분석 DB 준비 완료: {}", dataSource.getJdbcUrl());
            return true;
        } catch (Exception e) {
            // 스택은 넣지 않습니다. 30초마다 재시도하므로 스택이 반복해 쌓이면 진짜 원인이
            // 묻힙니다. 메시지에 "Unknown database" 나 "Access denied" 가 그대로 보입니다.
            log.error("분석 DB 를 준비하지 못했습니다. 게임 API 는 정상이고 이벤트는 버려집니다. "
                    + "{}초 뒤 다시 시도합니다. 원인: {}", RETRY_INTERVAL.toSeconds(), e.getMessage());
            return false;
        }
    }

    /** 마이그레이션이 끝나 insert 할 수 있는 상태인지. */
    public boolean isReady() {
        return ready.get();
    }

    /** 분석 DB 에 대고 도는 템플릿. {@link #isReady()} 가 true 일 때만 쓰세요. */
    public JdbcTemplate jdbcTemplate() {
        return jdbcTemplate;
    }

    @PreDestroy
    void close() {
        dataSource.close();
    }
}
