using System;
using Game.Core.Home;
using Game.Core.Lobby;
using Game.Core.Rooms;
using Game.Core.Settings;
using Game.Client.Match;
using NUnit.Framework;
using UnityEngine;

namespace Game.Architecture.Tests
{
    public sealed class InterfacePresentationTests
    {
        [Test]
        public void TwoPeers_RespectPublicationAndViewerScope_UsingAccountIds()
        {
            using var room = new RoomBrowserSystem();
            room.SetLocalPlayer("P1");
            room.SetParticipants(new[] { new RoomParticipant("P1", 0, true, "same", "user1"), new RoomParticipant("P2", 1, false, "same", "user2") });
            var store = new InMemoryInterfaceSettingsStore();
            var settings = new InterfaceSettingsSystem(store);
            var friends = new FriendListSystem();
            using var policy = new InterfacePresentation(settings, friends, room);
            Assert.That(policy.Name("P2", "same"), Is.Empty, "unpublished names fail closed");
            policy.SetPermission("P2", true);
            Assert.That(policy.Name("P2", "same"), Is.EqualTo("same"));
            settings.Apply(settings.Current.With(InterfaceOption.PlayerNames, InterfaceCatalog.FriendsOnly)
                .With(InterfaceOption.ChatScope, InterfaceCatalog.On));
            Assert.That(policy.Name("P2", "same"), Is.Empty);
            Assert.That(policy.ShowsChat("P2"), Is.False);
            Assert.That(policy.ShowsChat("P1"), Is.True);
            friends.ReplaceFriends(new[] { new FriendSummary("user2", "different nickname", FriendPresence.Offline) });
            Assert.That(policy.Name("P2", "same"), Is.EqualTo("same"));
            Assert.That(policy.ShowsChat("P2"), Is.True);
            policy.SetPermission("P2", false);
            Assert.That(policy.Name("P2", "same"), Is.Empty, "owner privacy overrides viewer friendship");
            friends.ReplaceFriends(Array.Empty<FriendSummary>());
            Assert.That(policy.ShowsChat("P2"), Is.False);
            var restored = new InterfaceSettingsSystem(store);
            Assert.That(restored.Current, Is.EqualTo(settings.Current));
            policy.ClearPermissions();
            Assert.That(policy.Name("P2", "same"), Is.Empty);
        }

        [Test]
        public void TwoIndependentViews_NeverRenderHiddenNickname()
        {
            using var roomA = new RoomBrowserSystem();
            using var roomB = new RoomBrowserSystem();
            roomA.SetLocalPlayer("A"); roomB.SetLocalPlayer("B");
            var peers = new[] { new RoomParticipant("A", 0, true, "SecretA", "accountA"), new RoomParticipant("B", 1, false, "SecretB", "accountB") };
            roomA.SetParticipants(peers); roomB.SetParticipants(peers);
            var settingsA = new InterfaceSettingsSystem(new InMemoryInterfaceSettingsStore());
            var settingsB = new InterfaceSettingsSystem(new InMemoryInterfaceSettingsStore());
            using var policyA = new InterfacePresentation(settingsA, new FriendListSystem(), roomA);
            using var policyB = new InterfacePresentation(settingsB, new FriendListSystem(), roomB);
            policyA.SetPermission("B", true); policyB.SetPermission("A", false);
            var a = MatchChatView.Create(null); var b = MatchChatView.Create(null);
            try
            {
                a.BindPresentation(policyA); b.BindPresentation(policyB);
                var messageA = new[] { new LobbyChatMessage("A", "SecretA", "hello") };
                b.SetMessages(messageA);
                Assert.That(Array.Exists(b.GetComponentsInChildren<TMPro.TMP_Text>(true), t => t.text.Contains("SecretA")), Is.False);
                a.SetMessages(new[] { new LobbyChatMessage("B", "SecretB", "hello") });
                Assert.That(Array.Exists(a.GetComponentsInChildren<TMPro.TMP_Text>(true), t => t.text.Contains("SecretB")), Is.True);
                settingsA.Apply(settingsA.Current.With(InterfaceOption.PlayerNames, InterfaceCatalog.Off));
                a.SetMessages(new[] { new LobbyChatMessage("B", "SecretB", "hello") });
                Assert.That(Array.Exists(a.GetComponentsInChildren<TMPro.TMP_Text>(true), t => t.text.Contains("SecretB")), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(a.gameObject); UnityEngine.Object.DestroyImmediate(b.gameObject); }
        }
    }
}
