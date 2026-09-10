using System;
using Game.Client.Cameras;
using Game.Client.Interactions;
using Game.Client.Lobby;
using Game.Client.Match;
using Game.Client.Players;
using Game.Client.Settings;
using Game.Core.Match;
using Game.Core.Ports;
using Game.Network.Match;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using VContainer.Unity;

namespace Game.Bootstrap
{
    /// <summary>
    /// In-match Esc opens the same environment-settings overlay the lobby
    /// uses, and Esc again closes it. 게임 나가기 leaves the room.
    /// </summary>
    public sealed class MatchSettingsOverlay : IStartable, ITickable, ILateTickable, IDisposable
    {
        public const int OverlaySortingOrder = 250;

        private readonly SettingsView view;
        private readonly SettingsPresenter presenter;
        private readonly MatchChatView chat;
        private readonly IKeyCapture keys;
        private readonly LobbyExitPresenter exit;
        private readonly INetworkMatchEvents events;
        private bool opened;
        private bool chatWasEnabled;
        private MatchPhase currentPhase;
        private PlayerCameraController cameraRig;
        private PlayerMovement lockedMovement;

        public MatchSettingsOverlay(
            SettingsView view,
            SettingsPresenter presenter,
            MatchChatView chat,
            IKeyCapture keys,
            LobbyExitPresenter exit,
            INetworkMatchEvents events)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            this.chat = chat ?? throw new ArgumentNullException(nameof(chat));
            this.keys = keys ?? throw new ArgumentNullException(nameof(keys));
            this.exit = exit ?? throw new ArgumentNullException(nameof(exit));
            this.events = events ?? throw new ArgumentNullException(nameof(events));
        }

        public static bool BlocksEscapeDuringPresentation(MatchPhase phase)
        {
            return phase == MatchPhase.Highlight || phase == MatchPhase.Result;
        }

        public static bool ShouldHandleEscape(
            bool textFocused,
            bool capturing,
            bool modalBlocking,
            bool consumedEscape,
            bool presentationBlocks = false)
        {
            return !textFocused &&
                   !capturing &&
                   !modalBlocking &&
                   !consumedEscape &&
                   !presentationBlocks;
        }

        public void Start()
        {
            presenter.LeaveGameConfirmed += OnLeaveGame;
            view.Closed += OnClosed;
            events.MatchStateReceived += OnMatchStateReceived;
            BindCameraEsc(false);
        }

        public void Tick()
        {
            if (opened && lockedMovement == null)
            {
                SetCursorCaptured(false);
                LockMovement();
                SetObjectPromptsVisible(false);
            }
        }

        public void LateTick()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            if (!Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                return;
            }

            if (!ShouldHandleEscape(
                    PlayerMovement.IsTextInputFocused() || chat.ConsumedEscapeThisFrame,
                    keys.IsCapturing,
                    view.BlocksEscape,
                    view.ConsumedEscapeThisFrame,
                    BlocksEscapeDuringPresentation(currentPhase)))
            {
                return;
            }

            if (opened)
            {
                Hide();
                return;
            }

            Open();
        }

        public void Dispose()
        {
            presenter.LeaveGameConfirmed -= OnLeaveGame;
            view.Closed -= OnClosed;
            events.MatchStateReceived -= OnMatchStateReceived;
            if (opened && chat != null)
            {
                chat.enabled = chatWasEnabled;
            }

            ReleaseMovement();
            BindCameraEsc(true);
        }

        private void OnMatchStateReceived(MatchStateSnapshot received)
        {
            currentPhase = received.Phase;
            if (BlocksEscapeDuringPresentation(currentPhase) && opened)
            {
                CloseForPresentation();
            }
        }

        private void CloseForPresentation()
        {
            if (!opened)
            {
                return;
            }

            opened = false;
            presenter.Open();
            if (chat != null)
            {
                chat.enabled = chatWasEnabled;
            }

            Hide();
            ReleaseMovement();
            SetCursorCaptured(false);
            ClearUiSelection();
            SetObjectPromptsVisible(true);
        }

        private void Open()
        {
            if (opened)
            {
                return;
            }

            opened = true;
            chatWasEnabled = chat.enabled;
            chat.enabled = false;
            view.gameObject.SetActive(true);
            RaiseOverlayCanvas();
            presenter.Open();
            SetCursorCaptured(false);
            LockMovement();
            ClearUiSelection();
            SetObjectPromptsVisible(false);
        }

        private void Hide()
        {
            if (view != null)
            {
                view.gameObject.SetActive(false);
            }
        }

        private void OnLeaveGame()
        {
            if (!opened)
            {
                return;
            }

            opened = false;
            presenter.Open();
            if (chat != null)
            {
                chat.enabled = chatWasEnabled;
            }

            Hide();
            ReleaseMovement();
            SetCursorCaptured(false);
            SetObjectPromptsVisible(true);
            exit.RequestLeave();
        }

        private void OnClosed()
        {
            if (!opened)
            {
                return;
            }

            opened = false;
            presenter.Open();
            if (chat != null)
            {
                chat.enabled = chatWasEnabled;
            }

            SetCursorCaptured(true);
            ReleaseMovement();
            ClearUiSelection();
            SetObjectPromptsVisible(true);
        }

        private void RaiseOverlayCanvas()
        {
            var canvas = view.GetComponentInChildren<Canvas>(true);
            if (canvas != null)
            {
                canvas.sortingOrder = OverlaySortingOrder;
            }
        }

        private void SetCursorCaptured(bool captured)
        {
            var rig = ResolveCameraRig();
            if (rig == null)
            {
                return;
            }

            rig.SetEscapeReleasesCursor(false);
            rig.SetCursorCaptureEnabled(captured);
        }

        private void BindCameraEsc(bool releasesCursor)
        {
            ResolveCameraRig()?.SetEscapeReleasesCursor(releasesCursor);
        }

        private void LockMovement()
        {
            var movement = ResolveCameraRig()?.FollowMovement;
            if (movement == null)
            {
                return;
            }

            movement.IsMovementLocked = true;
            lockedMovement = movement;
        }

        private void ReleaseMovement()
        {
            if (lockedMovement == null)
            {
                return;
            }

            lockedMovement.IsMovementLocked = false;
            lockedMovement = null;
        }

        private PlayerCameraController ResolveCameraRig()
        {
            if (cameraRig == null)
            {
                cameraRig = UnityEngine.Object.FindFirstObjectByType<PlayerCameraController>(
                    FindObjectsInactive.Include);
            }

            return cameraRig;
        }

        private static void ClearUiSelection()
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private static void SetObjectPromptsVisible(bool visible)
        {
            var interactors = UnityEngine.Object.FindObjectsByType<PlayerInteractor>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var index = 0; index < interactors.Length; index++)
            {
                interactors[index].SetInteractionPromptVisible(visible);
            }
        }
    }
}
