using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Game.Bootstrap;
using Game.Client.Home;
using Game.Core.Backend;
using Game.Core.Home;
using Game.Core.Players;
using Game.Core.Ports;
using NUnit.Framework;
using R3;

namespace Game.Architecture.Tests
{
    /// <summary>
    /// What the friend panel re-reads when the server pushes a change, checked
    /// against a gateway that counts every read.
    /// </summary>
    /// <remarks>
    /// The bridge is the production class. The counts below are taken after the
    /// bridge's own first load, so each test states only what a push added.
    /// </remarks>
    public sealed class HomeFriendBridgeNotificationTests
    {
        [Test]
        public async Task AReceivedRequest_ReReadsBothRequestListsAndNotFriends()
        {
            using var wiring = await Wiring.StartAsync();

            wiring.Push(ServerNotificationKind.FriendRequestReceived);
            await wiring.Settle();

            Assert.That(wiring.Gateway.IncomingReads, Is.EqualTo(1));
            Assert.That(wiring.Gateway.OutgoingReads, Is.EqualTo(1));
            Assert.That(wiring.Gateway.FriendReads, Is.EqualTo(0));
        }

        [Test]
        public async Task ARemovedRequest_ReReadsBothRequestLists()
        {
            using var wiring = await Wiring.StartAsync();

            // Declined or withdrawn — the server does not say which, and either
            // way both directions may have changed.
            wiring.Push(ServerNotificationKind.FriendRequestRemoved);
            await wiring.Settle();

            Assert.That(wiring.Gateway.IncomingReads, Is.EqualTo(1));
            Assert.That(wiring.Gateway.OutgoingReads, Is.EqualTo(1));
            Assert.That(wiring.Gateway.FriendReads, Is.EqualTo(0));
        }

        [Test]
        public async Task AnAcceptedRequest_ReReadsFriendsAndRequests()
        {
            using var wiring = await Wiring.StartAsync();

            // A new friend, and one fewer sent request.
            wiring.Push(ServerNotificationKind.FriendRequestAccepted);
            await wiring.Settle();

            Assert.That(wiring.Gateway.FriendReads, Is.EqualTo(1));
            Assert.That(wiring.Gateway.IncomingReads, Is.EqualTo(1));
            Assert.That(wiring.Gateway.OutgoingReads, Is.EqualTo(1));
        }

        [Test]
        public async Task ARemovedFriend_ReReadsFriendsOnly()
        {
            using var wiring = await Wiring.StartAsync();

            wiring.Push(ServerNotificationKind.FriendRemoved);
            await wiring.Settle();

            Assert.That(wiring.Gateway.FriendReads, Is.EqualTo(1));
            Assert.That(wiring.Gateway.IncomingReads, Is.EqualTo(0));
        }

        [Test]
        public async Task ARoomInvite_ReadsNothingHere()
        {
            using var wiring = await Wiring.StartAsync();

            // The toast owns invites. This panel shows none.
            wiring.Push(ServerNotificationKind.RoomInviteReceived, roomCode: "7K2M9P");
            await wiring.Settle();

            Assert.That(wiring.Gateway.FriendReads, Is.EqualTo(0));
            Assert.That(wiring.Gateway.IncomingReads, Is.EqualTo(0));
        }

        [Test]
        public async Task TheLinkComingBack_ReReadsTheRequests()
        {
            using var wiring = await Wiring.StartAsync();

            // Whatever was pushed while the link was down is gone. The requests
            // are re-read here because this panel is the only thing holding them;
            // the friend list is caught up project-wide by NotificationLink.
            wiring.Link.State.Value = NotificationLinkState.Connected;
            await wiring.Settle();

            Assert.That(wiring.Gateway.IncomingReads, Is.EqualTo(1));
            Assert.That(wiring.Gateway.FriendReads, Is.EqualTo(0));
        }

        [Test]
        public async Task PushesWhileARead_IsInFlight_QueueOneMoreReadNotSeveral()
        {
            using var wiring = await Wiring.StartAsync();
            wiring.Gateway.HoldIncoming = true;

            wiring.Push(ServerNotificationKind.FriendRequestReceived);
            await wiring.Settle();
            wiring.Push(ServerNotificationKind.FriendRequestReceived);
            wiring.Push(ServerNotificationKind.FriendRequestRemoved);
            await wiring.Settle();

            // One read is stuck on the wire. The two pushes behind it did not
            // start reads of their own.
            Assert.That(wiring.Gateway.IncomingReads, Is.EqualTo(1));

            wiring.Gateway.ReleaseIncoming();
            await wiring.Settle();

            // Exactly one follow-up. Not dropped — the held read was issued before
            // those pushes and may have missed them — and not one per push.
            Assert.That(wiring.Gateway.IncomingReads, Is.EqualTo(2));
        }

        [Test]
        public async Task AfterDispose_PushesReadNothing()
        {
            var wiring = await Wiring.StartAsync();
            wiring.Dispose();

            wiring.Push(ServerNotificationKind.FriendRequestReceived);
            wiring.Link.State.Value = NotificationLinkState.Connected;
            await wiring.Settle();

            Assert.That(wiring.Gateway.IncomingReads, Is.EqualTo(0));
            Assert.That(wiring.Gateway.FriendReads, Is.EqualTo(0));
        }

        private sealed class Wiring : IDisposable
        {
            private readonly HomeFriendBridge bridge;

            private Wiring(BackendSignIn signIn)
            {
                var commands = new FriendUiCommands(Gateway, new FriendListSystem(), new FriendSearchSystem());
                bridge = new HomeFriendBridge(new SilentView(), commands, signIn, Link);
                bridge.Start();
            }

            public CountingGateway Gateway { get; } = new CountingGateway();

            public FakeNotificationStream Link { get; } = new FakeNotificationStream();

            public static async UniTask<Wiring> StartAsync()
            {
                var signIn = new BackendSignIn(new SignedInAccounts(), new PlayerProfile("나"));
                await signIn.StartAsync(CancellationToken.None);

                var wiring = new Wiring(signIn);

                // Let the bridge's own first load finish, then count from zero so
                // each test speaks only of what its push added.
                await wiring.Settle();
                wiring.Gateway.ResetCounts();
                return wiring;
            }

            public void Push(ServerNotificationKind kind, string roomCode = null)
            {
                Link.Pushed.OnNext(new ServerNotification(
                    kind, "other", "상대", roomCode, new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc)));
            }

            /// <remarks>
            /// The fakes answer without ever yielding, so a few turns carry the
            /// bridge's fire-and-forget through to the end.
            /// </remarks>
            public async UniTask Settle()
            {
                await UniTask.Yield();
                await UniTask.Yield();
                await UniTask.Yield();
            }

            public void Dispose() => bridge.Dispose();
        }

        /// <summary>The server's push channel, driven by hand.</summary>
        private sealed class FakeNotificationStream : INotificationStream
        {
            public readonly Subject<ServerNotification> Pushed = new Subject<ServerNotification>();

            public readonly ReactiveProperty<NotificationLinkState> State =
                new ReactiveProperty<NotificationLinkState>(NotificationLinkState.Disconnected);

            Observable<ServerNotification> INotificationStream.Notifications => Pushed;

            ReadOnlyReactiveProperty<NotificationLinkState> INotificationStream.State => State;
        }

        /// <summary>Counts every read, and can hold the incoming-request read open.</summary>
        private sealed class CountingGateway : IFriendGateway
        {
            private UniTaskCompletionSource<BackendResult<IReadOnlyList<FriendRequestSummary>>> held;

            public int FriendReads { get; private set; }

            public int IncomingReads { get; private set; }

            public int OutgoingReads { get; private set; }

            /// <summary>While true, the next incoming read does not answer until released.</summary>
            public bool HoldIncoming { get; set; }

            public void ResetCounts()
            {
                FriendReads = 0;
                IncomingReads = 0;
                OutgoingReads = 0;
            }

            public void ReleaseIncoming()
            {
                HoldIncoming = false;
                held?.TrySetResult(NoRequests());
                held = null;
            }

            public UniTask<BackendResult<IReadOnlyList<FriendSummary>>> ListFriendsAsync(
                CancellationToken cancellation)
            {
                FriendReads++;
                return UniTask.FromResult(
                    BackendResult<IReadOnlyList<FriendSummary>>.Success(Array.Empty<FriendSummary>()));
            }

            public UniTask<BackendResult<IReadOnlyList<FriendRequestSummary>>> ListIncomingRequestsAsync(
                CancellationToken cancellation)
            {
                IncomingReads++;
                if (!HoldIncoming)
                {
                    return UniTask.FromResult(NoRequests());
                }

                held = new UniTaskCompletionSource<BackendResult<IReadOnlyList<FriendRequestSummary>>>();
                return held.Task;
            }

            public UniTask<BackendResult<IReadOnlyList<FriendRequestSummary>>> ListOutgoingRequestsAsync(
                CancellationToken cancellation)
            {
                OutgoingReads++;
                return UniTask.FromResult(NoRequests());
            }

            public UniTask<BackendResult<IReadOnlyList<FriendSummary>>> SearchAsync(
                string nickname, CancellationToken cancellation) =>
                UniTask.FromResult(
                    BackendResult<IReadOnlyList<FriendSummary>>.Success(Array.Empty<FriendSummary>()));

            public UniTask<BackendResult<FriendRequestOutcome>> SendRequestAsync(
                string playerId, CancellationToken cancellation) =>
                UniTask.FromResult(BackendResult<FriendRequestOutcome>.Success(FriendRequestOutcome.Sent));

            public UniTask<BackendResult> AcceptRequestAsync(string playerId, CancellationToken cancellation) => Ok();

            public UniTask<BackendResult> DeclineRequestAsync(string playerId, CancellationToken cancellation) => Ok();

            public UniTask<BackendResult> RemoveFriendAsync(string playerId, CancellationToken cancellation) => Ok();

            private static UniTask<BackendResult> Ok() => UniTask.FromResult(BackendResult.Success());

            private static BackendResult<IReadOnlyList<FriendRequestSummary>> NoRequests() =>
                BackendResult<IReadOnlyList<FriendRequestSummary>>.Success(Array.Empty<FriendRequestSummary>());
        }

        /// <summary>An account gateway that signs in and does nothing else.</summary>
        private sealed class SignedInAccounts : IAccountGateway
        {
            public UniTask<BackendResult<AccountSnapshot>> SignInAsync(CancellationToken cancellation) => Account();

            public UniTask<BackendResult<AccountSnapshot>> RefreshAsync(CancellationToken cancellation) => Account();

            public UniTask<BackendResult<AccountSnapshot>> RenameAsync(
                string nickname, CancellationToken cancellation) => Account();

            public UniTask<BackendResult<AccountSnapshot>> SetSearchableAsync(
                bool searchable, CancellationToken cancellation) => Account();

            public UniTask<BackendResult<AccountSnapshot>> SetAppearanceAsync(
                AvatarAppearance appearance, CancellationToken cancellation) => Account();

            public UniTask<BackendResult<AccountSnapshot>> ClearAppearanceAsync(
                CancellationToken cancellation) => Account();

            public UniTask<BackendResult> DeleteAccountAsync(CancellationToken cancellation) =>
                UniTask.FromResult(BackendResult.Success());

            private static UniTask<BackendResult<AccountSnapshot>> Account() =>
                UniTask.FromResult(
                    BackendResult<AccountSnapshot>.Success(new AccountSnapshot("me", "나", true, true)));
        }

        /// <summary>A home screen that shows nothing and raises nothing.</summary>
        private sealed class SilentView : IHomeMenuView
        {
            public event Action<HomeMenuAction> ActionClicked { add { } remove { } }
            public event Action FriendListDismissed { add { } remove { } }
            public event Action ProfileSettingsDismissed { add { } remove { } }
            public event Action<string> NicknameChangeRequested { add { } remove { } }
            public event Action<string> NicknameEdited { add { } remove { } }
            public event Action<bool> NicknameSearchAllowedChanged { add { } remove { } }
            public event Action FriendSearchOpened { add { } remove { } }
            public event Action FriendSearchClosed { add { } remove { } }
            public event Action<string> FriendSearchRequested { add { } remove { } }
            public event Action<string> FriendRequestClicked { add { } remove { } }
            public event Action<string> FriendRequestAccepted { add { } remove { } }
            public event Action<string> FriendRequestDeclined { add { } remove { } }
            public event Action ServerSettingsDismissed { add { } remove { } }
            public event Action<string> RegionSelected { add { } remove { } }
            public event Action<string, bool, int> RoomCreationRequested { add { } remove { } }
            public event Action CreateRoomDismissed { add { } remove { } }
            public event Action<string> FriendRequestCancelled { add { } remove { } }
            public event Action FriendListRefreshRequested { add { } remove { } }
            public event Action<string> FriendRemoved { add { } remove { } }

            public void SetNickname(string nickname) { }
            public void SetProfileSettingsVisible(bool visible) { }
            public void SetNicknameAppliedFeedbackVisible(bool visible) { }
            public void SetNicknameError(string message) { }
            public void SetNicknameSearchAllowed(bool allowed) { }
            public void SetNicknameSearchAllowedError(string message) { }
            public void ShowConnectionError(string message) { }
            public void SetFriendListVisible(bool visible) { }
            public void SetFriends(IReadOnlyList<FriendSummary> onlineFriends, IReadOnlyList<FriendSummary> offlineFriends) { }
            public void SetFriendSearchVisible(bool visible) { }
            public void SetFriendSearchResults(IReadOnlyList<FriendSearchHit> results) { }
            public void SetIncomingRequests(IReadOnlyList<FriendRequestSummary> requests) { }
            public void SetOutgoingRequests(IReadOnlyList<FriendRequestSummary> requests) { }
            public void SetFriendActionError(string message) { }
            public void SetServerSettingsVisible(bool visible) { }
            public void SetSelectedRegion(string code) { }
            public void SetCreateRoomVisible(bool visible) { }
            public void SetNicknameSettled(bool settled) { }
        }
    }
}
