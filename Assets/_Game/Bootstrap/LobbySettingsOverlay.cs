using System;
using Game.Client.Lobby;
using Game.Client.Match;
using Game.Client.Settings;
using Game.Network.Session;
using VContainer.Unity;

namespace Game.Bootstrap
{
    /// <summary>Reuses the settings presenter without leaving the network-owned lobby scene.</summary>
    public sealed class LobbySettingsOverlay : IStartable, ITickable, IDisposable
    {
        private readonly ILobbyPauseMenuView menu;
        private readonly LobbyPauseMenuPresenter pause;
        private readonly SettingsView view;
        private readonly SettingsPresenter presenter;
        private readonly MatchChatView chat;
        private readonly NetworkRunnerService network;
        private bool opened, chatWasEnabled;

        public LobbySettingsOverlay(ILobbyPauseMenuView menu, LobbyPauseMenuPresenter pause,
            SettingsView view, SettingsPresenter presenter, MatchChatView chat, NetworkRunnerService network)
        {
            this.menu = menu; this.pause = pause; this.view = view;
            this.presenter = presenter; this.chat = chat; this.network = network;
        }

        public void Start()
        {
            menu.SettingsClicked += OpenFromMenu;
            pause.SettingsOpenRequested += OpenFromWorld;
            view.Closed += OnClosed;
        }

        private void OpenFromMenu() => Open(fromWorld: false);

        private void OpenFromWorld() => Open(fromWorld: true);

        private void Open(bool fromWorld)
        {
            if (opened || (network.HasRoomSession && !network.IsWaitingForMatch)) return;
            opened = true;
            chatWasEnabled = chat.enabled;
            chat.enabled = false;
            view.gameObject.SetActive(true);
            pause.OpenSettingsScreen(view.RequestBack, fromWorld);
        }

        private void OnClosed()
        {
            if (!opened) return;
            opened = false;
            presenter.Open(); // Cancel microphone/key capture and any unapplied draft.
            if (chat != null) chat.enabled = chatWasEnabled;
            pause.OnScreenClosed();
        }

        public void Tick()
        {
            // Another participant can start the match while this local panel is open.
            if (opened && network.HasRoomSession && !network.IsWaitingForMatch)
                view.gameObject.SetActive(false);
        }

        public void Dispose()
        {
            menu.SettingsClicked -= OpenFromMenu;
            pause.SettingsOpenRequested -= OpenFromWorld;
            view.Closed -= OnClosed;
            if (opened && chat != null) chat.enabled = chatWasEnabled;
        }
    }
}
