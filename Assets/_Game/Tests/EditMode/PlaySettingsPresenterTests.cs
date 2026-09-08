using System;
using Game.Client.Lobby;
using Game.Core.Lobby;
using NUnit.Framework;
using R3;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Tests.EditMode
{
    public sealed class PlaySettingsPresenterTests
    {
        [Test]
        public void Host_SaveTitlePreservesUnsavedCapacity_AndAllowsRevertingTitle()
        {
            using var session = new HostSession();
            session.SetLocalHost(true);
            var view = new SettingsView();
            var menu = new PauseView();
            using var presenter = new PlaySettingsPresenter(session, view, menu);
            presenter.Start();
            menu.OpenSettings();
            view.Draft = new PlaySettingsDraft("새 방", "CODE", false, null, 4, 3, "playground");
            view.SaveTitle();
            Assert.That(session.Settings.CurrentValue.Title, Is.EqualTo("새 방"));
            Assert.That(session.Settings.CurrentValue.MaxPlayers, Is.EqualTo(6));
            Assert.That(view.Draft.MaxPlayers, Is.EqualTo(4));
            view.Draft = Draft(6);
            view.RequestClose();
            Assert.That(session.Settings.CurrentValue.Title, Is.EqualTo("방"));
            Assert.That(session.ApplyCount, Is.EqualTo(2));
        }

        [TestCase(false, "새 방")]
        [TestCase(true, "")]
        [TestCase(true, "123456789012345678901")]
        public void SaveTitle_WithoutAuthorityOrValidTitle_DoesNotApply(bool isHost, string title)
        {
            using var session = new HostSession();
            session.SetLocalHost(isHost);
            var view = new SettingsView();
            var menu = new PauseView();
            using var presenter = new PlaySettingsPresenter(session, view, menu);
            presenter.Start();
            menu.OpenSettings();
            view.Draft = new PlaySettingsDraft(title, "CODE", false, null, 6, 3, "playground");
            view.SaveTitle();
            Assert.That(session.ApplyCount, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Host_UnchangedOrRevertedDraft_DoesNotOverwriteNewSessionSettings(bool revertEdit)
        {
            using var session = new HostSession();
            session.SetLocalHost(true);
            var view = new SettingsView();
            var menu = new PauseView();
            using var presenter = new PlaySettingsPresenter(session, view, menu);
            presenter.Start();
            menu.OpenSettings();
            if (revertEdit) { view.Draft = Draft(3); view.Draft = Draft(6); }
            session.ReplaceSettings(Draft(5));
            view.RequestClose();
            Assert.That(session.ApplyCount, Is.Zero);
            Assert.That(session.Settings.CurrentValue.MaxPlayers, Is.EqualTo(5));
            menu.OpenSettings();
            Assert.That(view.Draft.MaxPlayers, Is.EqualTo(5));
        }

        [Test]
        public void Host_DraftAlreadyAccepted_DoesNotApplyAgain()
        {
            using var session = new HostSession();
            session.SetLocalHost(true);
            var view = new SettingsView();
            var menu = new PauseView();
            using var presenter = new PlaySettingsPresenter(session, view, menu);
            presenter.Start();
            menu.OpenSettings();
            view.Draft = Draft(4);
            session.ReplaceSettings(Draft(4));
            view.RequestClose();
            Assert.That(session.ApplyCount, Is.Zero);
        }

        [Test]
        public void Host_StartRequested_ClosesThenRequestsStart()
        {
            using var session = new HostSession();
            session.SetLocalHost(true);
            var view = new SettingsView();
            var menu = new PauseView();
            using var presenter = new PlaySettingsPresenter(session, view, menu);
            presenter.Start();
            menu.OpenSettings();
            view.Draft = Draft(4);
            view.RequestStart();
            Assert.That(view.Visible, Is.False);
            Assert.That(session.ApplyCount, Is.EqualTo(1));
            Assert.That(session.StartCount, Is.EqualTo(1));
        }

        [Test]
        public void Guest_StartRequested_ClosesWithoutStarting()
        {
            using var session = new HostSession();
            session.SetLocalHost(false);
            var view = new SettingsView();
            var menu = new PauseView();
            using var presenter = new PlaySettingsPresenter(session, view, menu);
            presenter.Start();
            menu.OpenSettings();
            view.RequestStart();
            Assert.That(view.Visible, Is.False);
            Assert.That(session.StartCount, Is.Zero);
        }

        [Test]
        public void Host_RepeatedOpenPreservesEdit_AndRepeatedCloseAppliesOnce()
        {
            using var session = new HostSession();
            session.SetLocalHost(true);
            var view = new SettingsView();
            var menu = new PauseView();
            using var presenter = new PlaySettingsPresenter(session, view, menu);
            presenter.Start();
            menu.OpenSettings();
            view.Draft = Draft(3);
            menu.OpenSettings();
            Assert.That(view.Draft.MaxPlayers, Is.EqualTo(3));
            view.RequestClose();
            view.RequestClose();
            Assert.That(session.ApplyCount, Is.EqualTo(1));
            Assert.That(session.Settings.CurrentValue.MaxPlayers, Is.EqualTo(3));
        }

        [Test]
        public void Host_RuleOnlyChange_IsApplied()
        {
            using var session = new HostSession();
            session.SetLocalHost(true);
            var view = new SettingsView();
            var menu = new PauseView();
            using var presenter = new PlaySettingsPresenter(session, view, menu);
            presenter.Start();
            menu.OpenSettings();
            Assert.That(MatchRuleSettings.TryCreate(60, 10, 1.5f, 5, "food", out var rules, out _), Is.True);
            view.Draft = new PlaySettingsDraft("방", "CODE", false, null, 6, 3, "playground", rules);
            view.RequestClose();
            Assert.That(session.ApplyCount, Is.EqualTo(1));
            Assert.That(session.Settings.CurrentValue.MatchRules, Is.EqualTo(rules));
        }

        [Test]
        public void Guest_SeesLiveSettingsWithoutApplyingOnClose()
        {
            using var session = new HostSession();
            var view = new SettingsView();
            var menu = new PauseView();
            using var presenter = new PlaySettingsPresenter(session, view, menu);
            presenter.Start();
            menu.OpenSettings();
            Assert.That(view.Visible, Is.True);
            Assert.That(view.Editable, Is.False);
            session.ReplaceSettings(Draft(4));
            Assert.That(view.Draft.MaxPlayers, Is.EqualTo(4));
            view.RequestClose();
            Assert.That(view.Visible, Is.False);
            Assert.That(session.ApplyCount, Is.Zero);
        }

        [Test]
        public void Host_KeepsUnsavedEditsUntilClose_AndLosingAuthorityDiscardsThem()
        {
            using var session = new HostSession();
            session.SetLocalHost(true);
            var view = new SettingsView();
            var menu = new PauseView();
            using var presenter = new PlaySettingsPresenter(session, view, menu);
            presenter.Start();
            menu.OpenSettings();
            Assert.That(view.Editable, Is.True);
            view.Draft = Draft(3);
            session.ReplaceSettings(Draft(5));
            Assert.That(view.Draft.MaxPlayers, Is.EqualTo(3));
            view.RequestClose();
            Assert.That(session.ApplyCount, Is.EqualTo(1));
            Assert.That(session.Settings.CurrentValue.MaxPlayers, Is.EqualTo(3));
            menu.OpenSettings();
            view.Draft = Draft(6);
            session.SetLocalHost(false);
            Assert.That(view.Editable, Is.False);
            Assert.That(view.Draft.MaxPlayers, Is.EqualTo(3));
            view.RequestClose();
            Assert.That(session.ApplyCount, Is.EqualTo(1));
        }

        [Test]
        public void RealView_ReadOnlyBlocksChanges_AndPreservesUnexposedRules()
        {
            var root = new GameObject("Settings view test");
            var panel = new GameObject("PlaySettingsPanel", typeof(RectTransform));
            panel.transform.SetParent(root.transform, false);
            root.SetActive(false);
            try
            {
                var view = root.AddComponent<PlaySettingsView>();
                var serialized = new SerializedObject(view);
                serialized.FindProperty("panel").objectReferenceValue = panel;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                root.SetActive(true);
                typeof(PlaySettingsView).GetMethod("OnEnable",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(view, null);
                var plusField = typeof(PlaySettingsView).GetField(
                    "maxPlayersPlusButton",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var button = (Button)plusField.GetValue(view);
                MatchRuleSettings.TryCreate(60, 10, 1.5f, 5, "food", out var rules, out _);
                view.SetDraft(new PlaySettingsDraft("방", "CODE", false, null, 4, 3, "playground", rules));
                view.SetEditable(false);
                Assert.That(button.interactable, Is.False);
                button.onClick.Invoke();
                Assert.That(view.ReadDraft().MaxPlayers, Is.EqualTo(4));
                view.SetEditable(true);
                Assert.That(button.interactable, Is.True);
                button.onClick.Invoke();
                Assert.That(view.ReadDraft().MaxPlayers, Is.EqualTo(5));
                Assert.That(view.ReadDraft().MatchRules, Is.EqualTo(rules));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void RealView_RuleEditingEnforcesBoundsAndAuthority_AndKeepsCategory()
        {
            var root = new GameObject("Rule editing test");
            var panel = new GameObject("PlaySettingsPanel", typeof(RectTransform));
            panel.transform.SetParent(root.transform, false);
            root.SetActive(false);
            try
            {
                var view = root.AddComponent<PlaySettingsView>();
                var serialized = new SerializedObject(view);
                serialized.FindProperty("panel").objectReferenceValue = panel;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                root.SetActive(true);
                typeof(PlaySettingsView).GetMethod("OnEnable",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(view, null);
                MatchRuleSettings.TryCreate(10, 1, 0.5f, 1, "fruit", out var rules, out _);
                view.SetDraft(new PlaySettingsDraft("방", "CODE", false, null, 6, 3, "playground", rules));
                var change = typeof(PlaySettingsView).GetMethod("ChangeRule",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                void Step(int field, int direction) => change.Invoke(view, new object[] { field, direction });
                view.SetEditable(false);
                for (var field = 0; field < 4; field++) Step(field, 1);
                Assert.That(view.ReadDraft().MatchRules, Is.EqualTo(rules));
                view.SetEditable(true);
                for (var field = 0; field < 4; field++) Step(field, -1);
                Assert.That(view.ReadDraft().MatchRules, Is.EqualTo(rules));
                foreach (var speed in new[] { 1f, 1.5f, 2f, 3f })
                {
                    Step(2, 1);
                    Assert.That(view.ReadDraft().MatchRules.SprintMultiplier, Is.EqualTo(speed));
                }
                for (var n = 0; n < 125; n++)
                    for (var field = 0; field < 4; field++) Step(field, 1);
                var actual = view.ReadDraft().MatchRules;
                Assert.That(actual.HidingDurationSeconds, Is.EqualTo(120));
                Assert.That(actual.SearchingDurationMinutes, Is.EqualTo(15));
                Assert.That(actual.SprintMultiplier, Is.EqualTo(3));
                Assert.That(actual.StunHitCount, Is.EqualTo(10));
                Assert.That(actual.CategoryId, Is.EqualTo("fruit"));
                view.SetDraft(new PlaySettingsDraft("방", "CODE", false, null, 6, 3, "playground", rules));
                Assert.That(view.ReadDraft().MatchRules, Is.EqualTo(rules));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        private static PlaySettingsDraft Draft(int capacity) =>
            new("방", "CODE", false, null, capacity, 3, "playground");

        private sealed class HostSession : ILobbyHostSession, IDisposable
        {
            private readonly ReactiveProperty<bool> host = new(false);
            private readonly ReactiveProperty<PlaySettingsDraft> settings = new(Draft(6));
            public string LocalPlayerId => "local";
            public ReadOnlyReactiveProperty<bool> IsLocalHost => host;
            public ReadOnlyReactiveProperty<PlaySettingsDraft> Settings => settings;
            public int ApplyCount;
            public int StartCount;
            public event Action StartRequested { add { } remove { } }
            public event Action<string> KickRequested { add { } remove { } }
            public event Action<string> HostTransferRequested { add { } remove { } }
            public event Action<PlaySettingsDraft> SettingsApplyRequested { add { } remove { } }
            public void SetLocalHost(bool value) => host.Value = value;
            public void ReplaceSettings(PlaySettingsDraft value) => settings.Value = value;
            public void RequestStart()
            {
                if (!host.CurrentValue)
                {
                    return;
                }

                StartCount++;
            }
            public void RequestKick(string id) { }
            public void RequestHostTransfer(string id) { }
            public void RequestApplySettings(PlaySettingsDraft value) { ApplyCount++; settings.Value = value; }
            public void Dispose() { host.Dispose(); settings.Dispose(); }
        }

        private sealed class SettingsView : IPlaySettingsView
        {
            public bool Visible;
            public bool Editable;
            public PlaySettingsDraft Draft;
            public event Action OpenRequested;
            public event Action CloseRequested;
            public void RequestOpen() => OpenRequested?.Invoke();
            public event Action CopyRoomCodeRequested { add { } remove { } }
            public event Action InviteRequested { add { } remove { } }
            public event Action CopyPasswordRequested { add { } remove { } }
            public event Action SaveTitleRequested;
            public event Action StartRequested;
            public void SaveTitle() => SaveTitleRequested?.Invoke();
            public void RequestStart() => StartRequested?.Invoke();
            public void SetVisible(bool value) => Visible = value;
            public void SetEditable(bool value) => Editable = value;
            public void SetDraft(PlaySettingsDraft value) => Draft = value;
            public PlaySettingsDraft ReadDraft() => Draft;
            public void RequestClose() => CloseRequested?.Invoke();
        }

        private sealed class PauseView : ILobbyPauseMenuView
        {
            public event Action StartClicked { add { } remove { } }
            public event Action LeaveClicked { add { } remove { } }
            public event Action ResumeClicked { add { } remove { } }
            public event Action SettingsClicked { add { } remove { } }
            public event Action PlaySettingsClicked;
            public bool IsOpen => true;
            public void SetVisible(bool value) { }
            public void SetStartVisible(bool value) { }
            public void SetPlaySettingsVisible(bool value) { }
            public void OpenSettings() => PlaySettingsClicked?.Invoke();
        }
    }
}
