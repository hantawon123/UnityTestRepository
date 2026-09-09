using System;
using System.Collections.Generic;
using Game.Core.Home;
using Game.Core.Lobby;
using R3;
using VContainer.Unity;

namespace Game.Client.Lobby
{
    public sealed class LobbyPlayerListPresenter : IStartable, IDisposable
    {
        private readonly ILobbyParticipantList participantList;
        private readonly ILobbyHostSession hostSession;
        private readonly FriendListSystem friends;
        private readonly ILobbyPlayerListView view;
        private readonly ILobbyPlayerCountView countView;
        private readonly ILobbyConfirmView kickConfirmView;
        private readonly ILobbyConfirmView transferConfirmView;

        private IDisposable refreshSubscription;
        private string pendingPlayerId;
        private bool pendingIsKick;

        public LobbyPlayerListPresenter(
            ILobbyParticipantList participantList,
            ILobbyHostSession hostSession,
            FriendListSystem friends,
            ILobbyPlayerListView view,
            ILobbyPlayerCountView countView,
            IKickConfirmView kickConfirmView,
            IHostTransferConfirmView transferConfirmView)
        {
            this.participantList = participantList
                ?? throw new ArgumentNullException(nameof(participantList));
            this.hostSession = hostSession ?? throw new ArgumentNullException(nameof(hostSession));
            this.friends = friends ?? throw new ArgumentNullException(nameof(friends));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.countView = countView ?? throw new ArgumentNullException(nameof(countView));
            this.kickConfirmView = kickConfirmView
                ?? throw new ArgumentNullException(nameof(kickConfirmView));
            this.transferConfirmView = transferConfirmView
                ?? throw new ArgumentNullException(nameof(transferConfirmView));
        }

        public void Start()
        {
            kickConfirmView.Hide();
            transferConfirmView.Hide();

            view.KickClicked += OnKickClicked;
            view.TransferClicked += OnTransferClicked;
            kickConfirmView.Confirmed += ConfirmPending;
            kickConfirmView.Cancelled += CancelPending;
            transferConfirmView.Confirmed += ConfirmPending;
            transferConfirmView.Cancelled += CancelPending;
            friends.FriendsChanged += BindFriends;
            BindFriends();

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
                });
        }

        public void Dispose()
        {
            view.KickClicked -= OnKickClicked;
            view.TransferClicked -= OnTransferClicked;
            kickConfirmView.Confirmed -= ConfirmPending;
            kickConfirmView.Cancelled -= CancelPending;
            transferConfirmView.Confirmed -= ConfirmPending;
            transferConfirmView.Cancelled -= CancelPending;
            friends.FriendsChanged -= BindFriends;
            refreshSubscription?.Dispose();
        }

        private void BindFriends()
        {
            var online = friends.OnlineFriends;
            var offline = friends.OfflineFriends;
            var combined = new List<FriendSummary>(online.Count + offline.Count);
            combined.AddRange(online);
            combined.AddRange(offline);
            view.SetFriends(combined);
        }

        private void OnKickClicked(string playerId, string displayName)
        {
            if (!hostSession.IsLocalHost.CurrentValue)
            {
                return;
            }

            pendingIsKick = true;
            pendingPlayerId = playerId;
            transferConfirmView.Hide();
            kickConfirmView.Show($"{displayName}님을 강퇴하시겠습니까?");
        }

        private void OnTransferClicked(string playerId, string displayName)
        {
            if (!hostSession.IsLocalHost.CurrentValue)
            {
                return;
            }

            pendingIsKick = false;
            pendingPlayerId = playerId;
            kickConfirmView.Hide();
            transferConfirmView.Show($"{displayName}님에게 방장을 위임하시겠습니까?");
        }

        private void ConfirmPending()
        {
            if (string.IsNullOrWhiteSpace(pendingPlayerId))
            {
                CancelPending();
                return;
            }

            if (pendingIsKick)
            {
                hostSession.RequestKick(pendingPlayerId);
            }
            else
            {
                hostSession.RequestHostTransfer(pendingPlayerId);
            }

            CancelPending();
        }

        private void CancelPending()
        {
            pendingPlayerId = null;
            kickConfirmView.Hide();
            transferConfirmView.Hide();
        }
    }
}
