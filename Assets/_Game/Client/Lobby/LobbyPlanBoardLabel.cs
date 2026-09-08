using UnityEngine;

namespace Game.Client.Lobby
{
    /// <summary>
    /// 작전 계획판 위에 떠 있는 "ROOM SETTING ▼" 안내 라벨.
    /// 항상 플레이어 카메라를 향해 돌아서고(수평 회전만), 화살표는 위아래로 살짝 흔들린다.
    /// </summary>
    /// <remarks>
    /// 라벨의 켜고 끄기는 <see cref="LobbyPlanBoardInteractable"/>이 바인딩 여부에 따라
    /// 이 오브젝트의 활성 상태로 다룬다. 이 컴포넌트는 보이는 동안의 움직임만 맡는다.
    /// 카메라는 매 프레임 <see cref="Camera.main"/>으로 찾는다: 로비 카메라는 씬이 만들고
    /// 클라이언트에서는 나중에 다시 만들어질 수 있어 한 번 잡아 두면 놓친다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class LobbyPlanBoardLabel : MonoBehaviour
    {
        [Tooltip("위아래로 흔들 화살표. 없으면 라벨 전체가 회전만 한다.")]
        [SerializeField]
        private Transform arrow;

        [Tooltip("화살표 흔들림 폭(m).")]
        [SerializeField]
        [Min(0f)]
        private float bobAmplitude = 0.04f;

        [Tooltip("화살표 흔들림 속도(초당 왕복 횟수).")]
        [SerializeField]
        [Min(0f)]
        private float bobFrequency = 1.2f;

        private Vector3 arrowRestPosition;
        private bool arrowRestCaptured;

        private void OnEnable()
        {
            CaptureArrowRest();
        }

        private void LateUpdate()
        {
            FaceCamera();
            BobArrow();
        }

        private void FaceCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            // 글자가 기울지 않도록 수평 방향만 본다. TMP 텍스트의 앞면은 -Z라
            // 카메라에서 라벨로 향하는 방향을 +Z로 두면 글자가 카메라를 본다.
            var toLabel = transform.position - camera.transform.position;
            toLabel.y = 0f;
            if (toLabel.sqrMagnitude < 0.0001f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(toLabel, Vector3.up);
        }

        private void BobArrow()
        {
            if (arrow == null)
            {
                return;
            }

            CaptureArrowRest();
            var offset = Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
            arrow.localPosition = arrowRestPosition + new Vector3(0f, offset, 0f);
        }

        private void CaptureArrowRest()
        {
            if (arrowRestCaptured || arrow == null)
            {
                return;
            }

            arrowRestPosition = arrow.localPosition;
            arrowRestCaptured = true;
        }
    }
}
