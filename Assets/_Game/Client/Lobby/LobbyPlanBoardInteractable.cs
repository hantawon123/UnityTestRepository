using System;
using Game.Client.Interactions;
using UnityEngine;

namespace Game.Client.Lobby
{
    /// <summary>
    /// 로비 서벽의 작업대(공구판 + 책상)를 하나로 묶은 작전 계획판.
    /// 조준하고 F키를 누르면 방 설정 화면이 열린다.
    /// 방장은 편집, 나머지는 읽기 전용으로 보는 것은 설정 화면 쪽 규칙이며,
    /// 이 컴포넌트는 안내 문구만 방장 여부에 따라 바꾼다.
    /// </summary>
    /// <remarks>
    /// Scene component with no dependencies of its own: the presenter that
    /// knows the host session and how to open the screen binds in at start.
    /// Until then the board is inert, so a scene without the lobby scope
    /// shows no prompt for it.
    /// <para>
    /// 상호작용이 가능한 동안(바인딩된 동안)에는 <see cref="outline"/>으로
    /// 작업대 전체에 실루엣을 늘 켜 둔다. 집을 수 있는 소품은 조준했을 때만
    /// 실루엣이 켜지지만, 이 판은 방에 하나뿐인 설정 입구라 처음 들어온
    /// 플레이어도 무엇이 눌리는지 바로 알아야 한다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class LobbyPlanBoardInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField]
        private string hostPrompt = "방 설정 열기";

        [SerializeField]
        private string guestPrompt = "작전 계획 보기";

        [Tooltip("바인딩된 동안 켜 두는 작업대 실루엣. 없으면 실루엣 없이 동작한다.")]
        [SerializeField]
        private InteractableFocusOutline outline;

        private Func<bool> isLocalHost;
        private Action onInteract;

        public bool IsBound => onInteract != null;

        public string InteractionPrompt =>
            isLocalHost != null && isLocalHost() ? hostPrompt : guestPrompt;

        /// <summary>실루엣이 현재 켜져 있는지. 실루엣 컴포넌트가 없으면 false.</summary>
        public bool IsOutlineVisible => outline != null && outline.IsVisible;

        public void Bind(Func<bool> localHostQuery, Action interact)
        {
            isLocalHost = localHostQuery ?? throw new ArgumentNullException(nameof(localHostQuery));
            onInteract = interact ?? throw new ArgumentNullException(nameof(interact));
            SetOutlineVisible(true);
        }

        public void Unbind()
        {
            isLocalHost = null;
            onInteract = null;
            SetOutlineVisible(false);
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

        private void OnDisable()
        {
            SetOutlineVisible(false);
        }

        private void SetOutlineVisible(bool visible)
        {
            if (outline == null)
            {
                return;
            }

            outline.SetVisible(visible);
        }
    }
}
