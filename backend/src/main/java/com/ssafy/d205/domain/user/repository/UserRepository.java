package com.ssafy.d205.domain.user.repository;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

import java.util.List;
import java.util.Optional;

import com.ssafy.d205.domain.user.entity.User;

public interface UserRepository extends JpaRepository<User, Integer> {

    Optional<User> findByPublicId(String publicId);

    /**
     * nickname 컬럼 콜레이션이 utf8mb4_0900_as_cs라(V4) 대소문자를 구분합니다.
     * Player가 있어도 player는 없다고 답하고, uk_users_nickname도 같게 동작합니다.
     */
    boolean existsByNickname(String nickname);

    /**
     * 닉네임이 정확히 일치하는 사용자를 찾습니다.
     *
     * <p><b>앞글자로는 찾히지 않습니다.</b> 예전에는 접두사로 찾을 수 있었지만, 몇 글자만
     * 쳐도 남이 걸려 나오는 것을 막기 위해 정확히 일치로 바꿨습니다. 검색을 끄는
     * 스위치(searchable)와 같은 방향입니다.
     *
     * <p><b>대소문자를 구분합니다.</b> nickname 컬럼이 as_cs 콜레이션이라 player 를
     * 검색하면 Player 는 나오지 않습니다. 둘은 서로 다른 닉네임이고 동시에 존재할 수
     * 있으므로, 구분하지 않으면 한 번의 검색이 서로 다른 두 사람을 함께 내놓습니다.
     *
     * <p><b>결과는 많아야 한 건입니다.</b> uk_users_nickname 이 유니크이기 때문입니다.
     * 그래서 limit 과 정렬이 결과에 영향을 주지 않습니다. 파라미터는 계약을 깨지 않으려고
     * 남겨두었을 뿐입니다.
     *
     * <p>인덱스는 uk_users_nickname 을 그대로 씁니다. 유니크 인덱스라 접두사 스캔보다
     * 낫습니다. nickname_lower 는 이제 이 쿼리가 쓰지 않지만, 친구 목록 정렬이 아직
     * 쓰고 있으므로 남겨 둡니다.
     *
     * <p>JPQL 이 아니라 네이티브 쿼리인 이유는 searchable 과 users_seq 를 함께 걸러야
     * 하는데 이 조합을 파생 쿼리 이름으로 표현하면 읽기 어려워지기 때문입니다.
     *
     * <p>거르는 것은 나 자신과 <b>검색을 꺼 둔 사람</b>입니다. 화면에서 가리는 것이
     * 아니라 여기서 빠지므로, 응답 자체에 그 사람이 담기지 않습니다.
     */
    @Query(value = """
            SELECT u.public_id AS userId,
                   u.nickname  AS nickname
              FROM users u
             WHERE u.nickname = :nickname
               AND u.users_seq <> :meSeq
               AND u.searchable = TRUE
            """, nativeQuery = true)
    List<UserSummaryRow> findByExactNickname(@Param("nickname") String nickname,
                                             @Param("meSeq") Integer meSeq);
}
