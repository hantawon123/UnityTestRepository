using System;
using Game.Core.Lobby;
using R3;
using UnityEngine;
using VContainer.Unity;

namespace Game.Client.Lobby
{
    public sealed class PlaySettingsPresenter : IStartable, IDisposable
    {
        private readonly ILobbyHostSession hostSession;
        private readonly IPlaySettingsView view;
        private readonly ILobbyPauseMenuView pauseMenu;
        private IDisposable hostSubscription;
        private IDisposable settingsSubscription;
        private bool isOpen;
        private PlaySettingsDraft displayedSettings;

        public PlaySettingsPresenter(
            ILobbyHostSession hostSession,
            IPlaySettingsView view,
            ILobbyPauseMenuView pauseMenu)
        {
            this.hostSession = hostSession ?? throw new ArgumentNullException(nameof(hostSession));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.pauseMenu = pauseMenu ?? throw new ArgumentNullException(nameof(pauseMenu));
        }

        public void Start()
        {
            view.SetVisible(false);
            view.OpenRequested += Open;
            view.CloseRequested += Close;
            view.CopyRoomCodeRequested += CopyRoomCode;
            view.InviteRequested += Invite;
            view.CopyPasswordRequested += CopyPassword;
            view.SaveTitleRequested += SaveTitle;
            pauseMenu.PlaySettingsClicked += Open;
            hostSubscription = hostSession.IsLocalHost.Subscribe(HandleHostChanged);
            settingsSubscription = hostSession.Settings.Subscribe(HandleSettingsChanged);
        }

        public void Dispose()
        {
            view.OpenRequested -= Open;
            view.CloseRequested -= Close;
            view.CopyRoomCodeRequested -= CopyRoomCode;
            view.InviteRequested -= Invite;
            view.CopyPasswordRequested -= CopyPassword;
            view.SaveTitleRequested -= SaveTitle;
            pauseMenu.PlaySettingsClicked -= Open;
            hostSubscription?.Dispose();
            settingsSubscription?.Dispose();
        }

        private void HandleHostChanged(bool isHost)
        {
            view.SetEditable(isHost);
            if (!isHost && isOpen)
            {
                DisplaySettings(hostSession.Settings.CurrentValue);
            }
        }

        private void HandleSettingsChanged(PlaySettingsDraft draft)
        {
            if (!isOpen || hostSession.IsLocalHost.CurrentValue)
            {
                return;
            }

            DisplaySettings(draft);
        }

        private void Open()
        {
            if (isOpen) return;
            view.SetEditable(hostSession.IsLocalHost.CurrentValue);
            DisplaySettings(hostSession.Settings.CurrentValue);
            isOpen = true;
            view.SetVisible(true);
        }

        private void Close()
        {
            if (!isOpen)
            {
                return;
            }

            if (hostSession.IsLocalHost.CurrentValue)
            {
                var draft = view.ReadDraft();
                if (!RoomSettings.IsValidTitle(draft.Title)) return;
                // An untouched host view may be older than the accepted session settings.
                if (!draft.Equals(displayedSettings) &&
                    !draft.Equals(hostSession.Settings.CurrentValue))
                {
                    hostSession.RequestApplySettings(draft);
                }
            }

            isOpen = false;
            view.SetVisible(false);
        }

        private void DisplaySettings(PlaySettingsDraft draft)
        {
            displayedSettings = draft;
            view.SetDraft(draft);
        }

        private void SaveTitle()
        {
            if (!isOpen || !hostSession.IsLocalHost.CurrentValue) return;
            var draft = view.ReadDraft();
            var current = hostSession.Settings.CurrentValue;
            if (!RoomSettings.IsValidTitle(draft.Title) || draft.Title == current.Title) return;
            hostSession.RequestApplySettings(new PlaySettingsDraft(draft.Title, current.RoomCode,
                current.PasswordEnabled, current.Password, current.MaxPlayers, current.DestructionLimit,
                current.MapId, current.MatchRules));
            if (hostSession.Settings.CurrentValue.Title == draft.Title)
            {
                displayedSettings = new PlaySettingsDraft(draft.Title, displayedSettings.RoomCode,
                    displayedSettings.PasswordEnabled, displayedSettings.Password, displayedSettings.MaxPlayers,
                    displayedSettings.DestructionLimit, displayedSettings.MapId, displayedSettings.MatchRules);
                view.SetDraft(draft);
            }
        }

        private void CopyRoomCode()
        {
            var code = hostSession.Settings.CurrentValue.RoomCode;
            if (string.IsNullOrWhiteSpace(code))
            {
                return;
            }

            GUIUtility.systemCopyBuffer = code;
        }

        private void Invite()
        {
            var settings = hostSession.Settings.CurrentValue;
            if (string.IsNullOrWhiteSpace(settings.RoomCode))
            {
                return;
            }

            GUIUtility.systemCopyBuffer =
                $"방 초대\n방제목: {settings.Title}\n방코드: {settings.RoomCode}";
        }

        private void CopyPassword()
        {
            var settings = hostSession.Settings.CurrentValue;
            if (!settings.PasswordEnabled || string.IsNullOrEmpty(settings.Password))
            {
                return;
            }

            GUIUtility.systemCopyBuffer = settings.Password;
        }
    }
}
