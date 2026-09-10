using System;
using Game.Client.Cameras;
using Game.Client.Lobby;
using Game.Client.Match;
using Game.Client.Players;
using Game.Client.Settings;
using Game.Network.Session;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer.Unity;

namespace Game.Bootstrap
{
    public sealed class MatchSettingsOverlay : IStartable, ITickable, IDisposable
    {
        private readonly SettingsView view;
        private readonly SettingsPresenter presenter;
        private readonly LobbyExitPresenter exit;
        private readonly MatchChatView chat;
        private readonly NetworkRunnerService network;
        private PlayerCameraController camera;
        private bool chatWasEnabled;
        public bool IsOpen { get; private set; }

        public MatchSettingsOverlay(SettingsView view, SettingsPresenter presenter,
            LobbyExitPresenter exit, MatchChatView chat, NetworkRunnerService network)
        {
            this.view = view;
            this.presenter = presenter;
            this.exit = exit;
            this.chat = chat;
            this.network = network;
        }

        public void Start()
        {
            presenter.LeaveGameConfirmed += Leave;
            view.Closed += OnClosed;
        }

        public void Tick()
        {
            if (network.IsResultSceneLoaded || network.IsHighlightInProgress || network.IsWaitingForMatch)
            {
                if (IsOpen) view.gameObject.SetActive(false);
                return;
            }

            if (IsOpen || !network.IsRuntimeReady || PlayerMovement.IsTextInputFocused() ||
                Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            IsOpen = true;
            chatWasEnabled = chat.enabled;
            chat.enabled = false;
            camera = UnityEngine.Object.FindFirstObjectByType<PlayerCameraController>();
            if (camera != null) camera.SetCursorCaptureEnabled(false);
            view.gameObject.SetActive(true);
        }

        private void OnClosed()
        {
            if (!IsOpen) return;
            IsOpen = false;
            presenter.Open();
            if (chat != null) chat.enabled = chatWasEnabled;
            if (camera != null && !network.IsResultSceneLoaded && !network.IsHighlightInProgress)
                camera.SetCursorCaptureEnabled(true);
        }

        private void Leave()
        {
            if (!IsOpen) return;
            view.gameObject.SetActive(false);
            exit.RequestLeave();
        }

        public void Dispose()
        {
            presenter.LeaveGameConfirmed -= Leave;
            view.Closed -= OnClosed;
            if (IsOpen && chat != null) chat.enabled = chatWasEnabled;
        }
    }
}
