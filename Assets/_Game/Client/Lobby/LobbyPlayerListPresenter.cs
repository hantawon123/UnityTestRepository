using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core.Home;
using Game.Core.Lobby;
using Game.Core.Ports;
using R3;
using VContainer.Unity;

namespace Game.Client.Lobby
{
    public sealed class LobbyPlayerListPresenter : IStartable, IDisposable
    {
        private readonly ILobbyParticipantList participantList;
        private readonly ILobbyHostSession hostSession;
        private readonly FriendListSystem friends;
        private readonly IInviteGateway invites;
        private readonly IReportGateway reports;
        private readonly ILobbyPlayerListView view;
        private readonly ILobbyPlayerCountView countView;
        private readonly ILobbyConfirmView confirmView;

        private readonly CancellationTokenSource lifetime = new();
        private IDisposable refreshSubscription;
        private string pendingPlayerId;
        private PendingConfirm pending;

        public LobbyPlayerListPresenter(
            ILobbyParticipantList participantList,
            ILobbyHostSession hostSession,
            FriendListSystem friends,
            IInviteGateway invites,
            IReportGateway reports,
            ILobbyPlayerListView view,
            ILobbyPlayerCountView countView,
            ILobbyConfirmView confirmView)
        {
            this.participantList = participantList
                ?? throw new ArgumentNullException(nameof(participantList));
            this.hostSession = hostSession ?? throw new ArgumentNullException(nameof(hostSession));
            this.friends = friends ?? throw new ArgumentNullException(nameof(friends));
            this.invites = invites ?? throw new ArgumentNullException(nameof(invites));
            this.reports = reports ?? throw new ArgumentNullException(nameof(reports));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.countView = countView ?? throw new ArgumentNullException(nameof(countView));
            this.confirmView = confirmView
                ?? throw new ArgumentNullException(nameof(confirmView));
        }

        public void Start()
        {
            confirmView.Hide();

            view.KickClicked += OnKickClicked;
            view.InviteClicked += OnInviteClicked;
            view.ReportClicked += OnReportClicked;
            confirmView.Confirmed += ConfirmPending;
            confirmView.Cancelled += CancelPending;
            friends.FriendsChanged += BindFriends;

            refreshSubscription = Observable.CombineLatest(
                    participantList.Participants,
                    hostSession.IsLocalHost,
                    hostSession.Settings,
                    (participants, isLocalHost, settings) =>
                        (Participants: participants, IsLocalHost: isLocalHost, Settings: settings))
                .Subscribe(state =>
                {
                    var people = state.Participants ?? Array.Empty<LobbyParticipant>();
                    view.SetParticipants(
                        people,
                        state.IsLocalHost,
                        hostSession.LocalPlayerId);
                    countView.SetCount(people.Count, state.Settings.MaxPlayers);
                    BindFriends();
                });
        }

        public void Dispose()
        {
            view.KickClicked -= OnKickClicked;
            view.InviteClicked -= OnInviteClicked;
            view.ReportClicked -= OnReportClicked;
            lifetime.Cancel();
            lifetime.Dispose();
            confirmView.Confirmed -= ConfirmPending;
            confirmView.Cancelled -= CancelPending;
            friends.FriendsChanged -= BindFriends;
            refreshSubscription?.Dispose();
        }

        private void BindFriends()
        {
            var people = participantList.Participants.CurrentValue
                ?? Array.Empty<LobbyParticipant>();
            var inRoom = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < people.Count; index++)
            {
                inRoom.Add(people[index].Id);
            }

            var online = friends.OnlineFriends;
            var offline = friends.OfflineFriends;
            var combined = new List<FriendSummary>(online.Count + offline.Count);
            AppendInvitable(combined, online, inRoom);
            AppendInvitable(combined, offline, inRoom);
            view.SetFriends(combined);
        }

        private static void AppendInvitable(
            List<FriendSummary> destination,
            IReadOnlyList<FriendSummary> source,
            HashSet<string> inRoom)
        {
            for (var index = 0; index < source.Count; index++)
            {
                var friend = source[index];
                if (inRoom.Contains(friend.PlayerId))
                {
                    continue;
                }

                destination.Add(friend);
            }
        }

        private void OnInviteClicked(string playerId, string _)
        {
            var roomCode = hostSession.Settings.CurrentValue.RoomCode;
            if (string.IsNullOrWhiteSpace(playerId) || string.IsNullOrWhiteSpace(roomCode))
            {
                return;
            }

            invites.SendAsync(playerId, roomCode, lifetime.Token).Forget();
        }

        private void OnKickClicked(string playerId, string displayName)
        {
            if (!hostSession.IsLocalHost.CurrentValue)
            {
                return;
            }

            pending = PendingConfirm.Kick;
            pendingPlayerId = playerId;
            confirmView.Show(KickConfirmView.FormatTitle(displayName), KickConfirmView.ConfirmLabel);
        }

        private void OnReportClicked(string playerId, string displayName)
        {
            if (string.IsNullOrWhiteSpace(playerId)
                || string.Equals(playerId, hostSession.LocalPlayerId, StringComparison.Ordinal))
            {
                return;
            }

            pending = PendingConfirm.Report;
            pendingPlayerId = playerId;
            confirmView.Show(
                LobbyPlayerListView.FormatReportTitle(displayName),
                LobbyPlayerListView.ReportConfirmLabel,
                chooseReason: true);
        }

        private void ConfirmPending()
        {
            if (string.IsNullOrWhiteSpace(pendingPlayerId))
            {
                CancelPending();
                return;
            }

            if (pending == PendingConfirm.Kick)
            {
                hostSession.RequestKick(pendingPlayerId);
            }
            else if (pending == PendingConfirm.Report)
            {
                reports.ReportAsync(
                    pendingPlayerId,
                    confirmView.SelectedReason,
                    null,
                    lifetime.Token).Forget();
            }

            CancelPending();
        }

        private void CancelPending()
        {
            pending = PendingConfirm.None;
            pendingPlayerId = null;
            confirmView.Hide();
        }

        private enum PendingConfirm
        {
            None,
            Kick,
            Report
        }
    }
}
