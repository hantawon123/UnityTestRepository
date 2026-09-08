using System;
using System.Collections.Generic;
using Game.Client.Lobby;
using Game.Core.Lobby;
using NUnit.Framework;
using R3;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// The play settings screen can be reached from the Esc menu or from the
    /// plan board in the room. Where it goes back to on close depends on where
    /// it came from.
    /// </summary>
    public sealed class LobbyPauseMenuPresenterTests
    {
        [Test]
        public void OpenFromWorld_RequestsSettingsOpen_WithoutShowingTheMenu()
        {
            using var fixture = new Fixture();
            fixture.Presenter.Start();
            fixture.Menu.VisibleCalls.Clear();

            fixture.Presenter.OpenPlaySettingsFromWorld();

            Assert.That(fixture.Settings.OpenRequests, Is.EqualTo(1));
            Assert.That(fixture.Menu.VisibleCalls, Has.No.Member(true));
        }

        [Test]
        public void OpenFromWorld_ThenClose_ReturnsToRoomNotMenu()
        {
            using var fixture = new Fixture();
            fixture.Presenter.Start();
            fixture.Presenter.OpenPlaySettingsFromWorld();
            fixture.Menu.VisibleCalls.Clear();

            fixture.Settings.RequestClose();

            Assert.That(fixture.Menu.VisibleCalls, Has.No.Member(true));
            Assert.That(fixture.Menu.IsOpen, Is.False);
        }

        [Test]
        public void OpenFromMenu_ThenClose_ReturnsToMenu()
        {
            using var fixture = new Fixture();
            fixture.Presenter.Start();
            fixture.Menu.ClickPlaySettings();
            fixture.Menu.VisibleCalls.Clear();

            fixture.Settings.RequestClose();

            Assert.That(fixture.Menu.VisibleCalls, Has.Member(true));
        }

        [Test]
        public void OpenFromWorld_WhileMenuIsUp_DoesNothing()
        {
            using var fixture = new Fixture();
            fixture.Presenter.Start();
            fixture.Menu.SetVisible(true); // as if Esc had opened it

            fixture.Presenter.OpenPlaySettingsFromWorld();

            Assert.That(fixture.Settings.OpenRequests, Is.Zero);
        }

        [Test]
        public void OpenFromWorld_WhileSettingsAlreadyOpen_DoesNotOpenTwice()
        {
            using var fixture = new Fixture();
            fixture.Presenter.Start();
            fixture.Presenter.OpenPlaySettingsFromWorld();

            fixture.Presenter.OpenPlaySettingsFromWorld();

            Assert.That(fixture.Settings.OpenRequests, Is.EqualTo(1));
        }

        [Test]
        public void OpenFromWorld_AfterAMenuRoundTrip_StillReturnsToRoom()
        {
            using var fixture = new Fixture();
            fixture.Presenter.Start();
            // Menu -> settings -> back to menu, then leave the menu via Resume.
            fixture.Menu.ClickPlaySettings();
            fixture.Settings.RequestClose();
            fixture.Menu.ClickResume();

            fixture.Presenter.OpenPlaySettingsFromWorld();
            fixture.Menu.VisibleCalls.Clear();
            fixture.Settings.RequestClose();

            Assert.That(fixture.Menu.VisibleCalls, Has.No.Member(true));
        }

        private sealed class Fixture : IDisposable
        {
            public readonly PauseView Menu = new();
            public readonly SettingsView Settings = new();
            public readonly HostSession Session = new();
            public readonly LobbyPauseMenuPresenter Presenter;

            public Fixture()
            {
                Presenter = new LobbyPauseMenuPresenter(
                    Menu, Settings, Session, new LobbyExitPresenter());
            }

            public void Dispose()
            {
                Presenter.Dispose();
                Session.Dispose();
            }
        }

        private sealed class PauseView : ILobbyPauseMenuView
        {
            public readonly List<bool> VisibleCalls = new();
            public event Action StartClicked { add { } remove { } }
            public event Action LeaveClicked { add { } remove { } }
            public event Action ResumeClicked;
            public event Action SettingsClicked { add { } remove { } }
            public event Action PlaySettingsClicked;
            public bool IsOpen { get; private set; }
            public void SetVisible(bool visible) { IsOpen = visible; VisibleCalls.Add(visible); }
            public void SetStartVisible(bool visible) { }
            public void SetPlaySettingsVisible(bool visible) { }
            public void ClickPlaySettings() => PlaySettingsClicked?.Invoke();
            public void ClickResume() => ResumeClicked?.Invoke();
        }

        private sealed class SettingsView : IPlaySettingsView
        {
            public int OpenRequests;
            public event Action OpenRequested;
            public event Action CloseRequested;
            public event Action CopyRoomCodeRequested { add { } remove { } }
            public event Action InviteRequested { add { } remove { } }
            public event Action CopyPasswordRequested { add { } remove { } }
            public event Action SaveTitleRequested { add { } remove { } }
            public event Action StartRequested { add { } remove { } }
            public void SetVisible(bool visible) { }
            public void SetEditable(bool editable) { }
            public void SetDraft(PlaySettingsDraft draft) { }
            public PlaySettingsDraft ReadDraft() =>
                new("방", "CODE", false, null, 6, 3, "playground");
            public void RequestClose() => CloseRequested?.Invoke();
            public void RequestOpen() { OpenRequests++; OpenRequested?.Invoke(); }
        }

        private sealed class HostSession : ILobbyHostSession, IDisposable
        {
            private readonly ReactiveProperty<bool> host = new(true);
            private readonly ReactiveProperty<PlaySettingsDraft> settings =
                new(new PlaySettingsDraft("방", "CODE", false, null, 6, 3, "playground"));

            public string LocalPlayerId => "me";
            public ReadOnlyReactiveProperty<bool> IsLocalHost => host;
            public ReadOnlyReactiveProperty<PlaySettingsDraft> Settings => settings;
            public event Action StartRequested { add { } remove { } }
            public event Action<string> KickRequested { add { } remove { } }
            public event Action<string> HostTransferRequested { add { } remove { } }
            public event Action<PlaySettingsDraft> SettingsApplyRequested { add { } remove { } }
            public void SetLocalHost(bool value) => host.Value = value;
            public void ReplaceSettings(PlaySettingsDraft value) => settings.Value = value;
            public void RequestStart() { }
            public void RequestKick(string id) { }
            public void RequestHostTransfer(string id) { }
            public void RequestApplySettings(PlaySettingsDraft value) => settings.Value = value;
            public void Dispose() { host.Dispose(); settings.Dispose(); }
        }
    }
}
