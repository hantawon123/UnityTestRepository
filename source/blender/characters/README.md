# 캐릭터 블렌더 원본

플레이어 캐릭터의 리깅·리타겟·액션 작업 파일(`.blend`)을 둡니다.

- 파일명은 영문과 언더스코어. 예: `PlayerCapsule_CuteJump.blend`
- `*.blend1` 백업은 커밋하지 않습니다.
- 액션 제작 현황은 `docs/design/character/animation/animation-checklist.md`에 적습니다.

## 게임 적용

- 메시·리그: `Assets/_Game/Content/Models/`
- 애니메이션 클립: `Assets/_Game/Content/Animations/`

## 목록

- `PlayerCapsule_CuteJump.blend` — 현재 통합 파일. 걷기·달리기·점프 액션 포함. 기본 액션 `Jump_Cute`
- `PlayerCapsule_Idle_Breathing_2s.blend` — 기본 대기. 액션 `Idle_Breathing`
- `PlayerCapsule_Fall_Flutter.blend` — 공중 유지·낙하. 액션 `Fall_Flutter`
- `PlayerCapsule_Land_Matched.blend` — 착지. 액션 `Land_Matched`
- `PlayerCapsule_Walk_Run.blend` — 걷기·달리기 작업본
- `PlayerCapsule_Walk_Wide_Clean.blend` — 보폭 넓힌 걷기. 액션 `Walk_Wide_Clean`, `Walk_Wide_Clean_Forward`
