# WebGL 자동 빌드와 배포

GitLab develop → EC2 Jenkins → Unity Docker → `https://j15d205.p.ssafy.io/play/`.
PC 에이전트를 사용하지 않는다. 기존 `backend/Jenkinsfile` 작업은 독립적으로 유지한다.
새 접속에는 **최신 성공 빌드**를 제공한다. 커밋 반영에는 SCM 확인(최대 약 5분)과 빌드 시간이 필요하다.

## Jenkins

- 작업: `d205-unity-webgl`, Pipeline script from SCM, `Tools/webgl/Jenkinsfile`.
- 저장소: 기존 GitLab 저장소와 `gitlab-deploy-token` 읽기 자격 증명 사용.
- 운영 Branch Specifier: `*/develop`. MR 병합 전 검증에는 `*/feature/server/webgl-delivery` 사용.
- feature 빌드는 검증·산출물 보관까지만 실행한다. `origin/develop`만 Publish 단계를 실행한다.
- 실행 동시성 1, 제한 120분. NuGet 복원 1 CPU/1GB, Unity 빌드 2 CPU/8GB.
- Git LFS로 모델·텍스처를 복원하고, NuGetForUnity CLI 4.5.0으로 R3 등을 먼저 복원한다.
- Unity 이미지: `unityci/editor:ubuntu-6000.3.22f1-webgl-3`, 검증한 digest 고정.
- Unity `Library`는 Jenkins workspace에 남아 다음 빌드에서 재사용된다. Unity 버전 변경 시 캐시 재생성이 필요하다.
- 빌드 로그는 실패 시에도 보관한다. 성공 산출물은 최근 3회, 빌드 기록은 최근 15회 보관한다.
- 실패한 빌드는 Publish까지 진행하지 않는다. 시작할 때 이전 산출물 폴더를 비워 오래된 파일의 재배포를 막는다.

## 서버 사전 구성

Jenkins 실행 계정에 Docker 실행 권한과 `git-lfs`, `python3`가 필요하다.
라이선스 초기 활성화에는 공식 Unity CLI, `gnome-keyring`, `libsecret-1-0`, `unzip`을 사용했다.
인증은 Unity 공식 OAuth 화면에서 계정 소유자가 진행하고, Personal 약관 동의 후 무료 활성화를 실행한다.

다음 파일은 서버에서만 관리하고 Git이나 빌드 아티팩트에 포함하지 않는다.

| 경로 | 용도 |
| --- | --- |
| `/var/lib/jenkins/.config/unity-webgl/PhotonAppSettings.asset` | 승인된 게임 Photon 연결 설정, Jenkins 전용 읽기 권한 |
| `/var/lib/jenkins/.config/unity-webgl/keyring-password` | 서버에서 생성한 키링 암호, 소유자만 읽기 |
| `/var/lib/jenkins/.local/share/keyrings/` | Unity OAuth 인증의 암호화 저장소 |
| `/var/lib/jenkins/.config/unity3d/Unity/licenses/` | 이 EC2에서 정식 활성화한 Unity 라이선스 |

Docker는 **동일 EC2의 실제 `/etc/machine-id`**와 활성화된 Unity 설정 디렉터리를 사용한다.
다른 PC의 식별자나 라이선스를 복사하거나 XML을 수정하지 않는다. 호스트 교체 시 새 호스트에서 다시 활성화한다.
계정 재인증이 필요하면 키링을 연 세션에서 `unity auth login`을 실행한다.
무료 Personal 활성화의 `--accept-eula`는 계정 소유자의 약관 동의 후에만 사용한다.

## HTTPS 경로 설치

`nginx.conf`를 `/etc/nginx/snippets/d205-webgl.conf`로 설치한다.
기존 HTTPS server 블록에는 `include /etc/nginx/snippets/d205-webgl*.conf;`를 추가한다.
추가 전 원본을 백업하고 `nginx -t` 성공 후 reload한다. API와 `/jenkins/` 프록시는 유지한다.
`/var/www/d205-webgl`은 Jenkins가 쓰고 nginx가 읽을 수 있어야 한다(디렉터리 755, 공개 파일 644).
인증 파일은 이 공개 디렉터리에 두지 않는다.

WebGL은 gzip으로 빌드하며 nginx가 `.wasm.gz`, `.js.gz`, `.data.gz`에 올바른 MIME과
`Content-Encoding: gzip`을 설정한다. 동일 HTTPS origin의 기존 API를 사용하므로 배포용 CORS 완화가 필요 없다.

## 버전과 롤백

- 빌드마다 전체 Git SHA를 `Application.version`과 `version.txt`에 넣는다.
- `/play/releases/<sha>/`는 덮어쓰지 않는다. 최신 포인터 `current.json`만 원자적으로 교체한다.
- 고정 주소와 포인터는 캐시를 재검증/차단하고 버전별 파일은 immutable 캐시를 사용한다.
- 실행 중인 게임은 자신의 버전 URL에 남는다. 새 버전 알림을 선택하고 확인한 경우에만 게임을 나간다.
  브라우저 전체 화면에서는 HTML 알림이 가려질 수 있으며, 전체 화면을 나오면 확인할 수 있다.
- WebGL Photon AppVersion은 `web-<sha>`다. 서로 다른 웹 빌드와 기존 네이티브 빌드의 방은 분리된다.
  Windows/전용 서버와 웹의 교차 플레이를 도입하려면 양쪽의 호환 버전 정책을 함께 맞춰야 한다.
- 배포 잠금과 Jenkins 빌드 번호로 오래된 작업의 덮어쓰기를 차단한다. 같은 작업의 빌드 번호를 초기화하지 않는다.

```sh
# 서버에서 성공한 빌드 배포
python3 Tools/webgl/publish.py /var/www/d205-webgl FULL_COMMIT_SHA \
  --source Builds/WebGL --sequence JENKINS_BUILD_NUMBER

# 이미 배포했던 버전으로 신규 접속만 롤백
python3 Tools/webgl/publish.py /var/www/d205-webgl PREVIOUS_FULL_COMMIT_SHA

# Linux에서 배포 순서, 잘못된 입력, 실패 보존, 롤백 검증
python3 Tools/webgl/test_publish.py
```

이전 release는 자동 삭제하지 않는다. 플레이 중인 버전과 롤백 후보를 확인한 뒤 별도로 정리한다.
롤백 시 이미 열린 경기는 유지하고 신규 접속만 이전 버전으로 보낸다.

## 검증 범위와 제한

- 로컬 Unity 6000.3.22f1 WebGL 빌드 성공, 브라우저 홈 표시·비공개 방 생성·코드 입장 확인.
- 동일 PC의 브라우저에서 4명 연결 및 호스트 시작 카운트다운 확인. 6인 전체 경기·음성 검증은 미완료.
- EC2 계정 OAuth 및 무료 Personal 활성화 성공. 동일 EC2 Docker에서 Unity 빈 프로젝트 생성·정상 종료 확인.
- Linux 배포/롤백 자동 검사와 nginx 문법 검사 통과. 실제 Jenkins 프로젝트 빌드 및 HTTPS 검증은 진행 중.
- 브라우저 Host/Client는 Photon 권장 토폴로지가 아니다. Host 탭이 백그라운드로 가면 경기 지연/연결 끊김이 발생할 수 있다.
- 여러 내장 브라우저를 동시에 제어하는 검증 중 Chromium PointerLock UnknownError가 관측됐다. 6인 안정성 검증 성공으로 간주하지 않는다.
- WebRTC 음성 DSP는 WebGL에서 지원되지 않는다는 Photon 경고가 있다. 실제 마이크 송수신은 별도 검증 대상이다.

참고: [Photon 토폴로지](https://doc.photonengine.com/fusion/v2/fusion-choose),
[Unity Web 배포](https://docs.unity3d.com/6000.3/Documentation/Manual/webgl-deploying.html),
[Unity Personal 자격 조건](https://unity.com/products/unity-personal),
[Unity 라이선스](https://unity.com/pages/license-compliance).

Jira: S15P21D205-875~878, Story 874 / Epic 413. 전체 하나의 MR로 제출한다.
