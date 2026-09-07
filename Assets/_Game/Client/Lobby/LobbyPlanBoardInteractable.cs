using System;
using Game.Client.Interactions;
using UnityEngine;

namespace Game.Client.Lobby
{
    /// <summary>
    /// 로비 벽의 작전 계획판. 조준하고 F키를 누르면 방 설정 화면이 열린다.
    /// 방장은 편집, 나머지는 읽기 전용으로 보는 것은 설정 화면 쪽 규칙이며,
    /// 이 컴포넌트는 안내 문구만 방장 여부에 따라 바꾼다.
    /// </summary>
    /// <remarks>
    /// Scene component with no dependencies of its own: the presenter that
    /// knows the host session and how to open the screen binds in at start.
    /// Until then the board is inert, so a scene without the lobby scope
    /// shows no prompt for it.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class LobbyPlanBoardInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField]
        private string hostPrompt = "방 설정 열기";

        [SerializeField]
        private string guestPrompt = "작전 계획 보기";

        private Func<bool> isLocalHost;
        private Action onInteract;

        public bool IsBound => onInteract != null;

        public string InteractionPrompt =>
            isLocalHost != null && isLocalHost() ? hostPrompt : guestPrompt;

        public void Bind(Func<bool> localHostQuery, Action interact)
        {
            isLocalHost = localHostQuery ?? throw new ArgumentNullException(nameof(localHostQuery));
            onInteract = interact ?? throw new ArgumentNullException(nameof(interact));
        }

        public void Unbind()
        {
            isLocalHost = null;
            onInteract = null;
        }

        /// <remarks>
        /// A carried item takes the F key for dropping, and a board that
        /// answered the same press would open a screen over a dropped box.
        /// </remarks>
        public bool CanInteract(PlayerInteractor interactor)
        {
            return IsBound && interactor != null && interactor.CarriedItem == null;
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            onInteract.Invoke();
        }
    }
}
