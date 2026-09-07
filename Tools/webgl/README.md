# WebGL 자동 배포

GitLab develop → EC2 Jenkins → Unity Docker 빌드 → Nginx `/play/`를 목표로 한다. Windows PC 에이전트는 사용하지 않는다. 기존 `backend/Jenkinsfile` 작업은 유지한다.

## 진행 중인 검증

- Unity 6000.3.22f1 WebGL 모듈 및 Photon WebGL 컴파일 심볼 구성.
- 로컬 실행: Unity를 `-batchmode -quit -buildTarget WebGL -executeMethod Game.Editor.WebBuild.Build`로 실행. `WEBGL_OUTPUT`으로 출력 폴더 지정(기본 `Builds/WebGL`). Git에서 제외된 PhotonAppSettings.asset 복원 필요.
- EC2의 `unityci/editor:ubuntu-6000.3.22f1-webgl-3` 이미지 제공 확인. 빌드 중 CPU/메모리 제한을 적용하여 기존 API/DB와 자원을 나눠 사용해야 한다.
- EC2 Unity 라이선스 활성화는 미완료. 로컬 Personal의 UnityEntitlementLicense.xml을 서버에 복사하지 않는다. 서버에서 유효한 활성화가 확인되기 전 자동 빌드 완료로 표시하지 않는다.
- 브라우저 Host는 탭 백그라운드 전환 시 전체 경기 정지 가능성이 있어 별도 검증 필요. Windows Host 기본 동작은 유지한다.

Jira: 875 호환성 검증 → 876 자동 빌드 → 877 HTTPS 배포 → 878 버전/롤백. 전체 하나의 MR로 묶는다.

## 라이선스

Personal도 자격 조건을 충족하면 웹/상용 게임 배포가 가능하다. 최근 12개월 재정 기준 및 외주/조직 적용 범위는 Unity 약관을 확인한다. 배포 가능 여부와 무인 빌드 환경의 활성화는 별개의 문제다.

- https://unity.com/products/unity-personal
- https://unity.com/pages/license-compliance
- https://game.ci/docs/github/activation/
- https://docs.unity.com/hub/manage-license
