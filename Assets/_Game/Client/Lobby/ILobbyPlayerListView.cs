using System;
using System.Collections.Generic;
using Game.Core.Home;
using Game.Core.Lobby;

namespace Game.Client.Lobby
{
    public interface ILobbyPlayerListView
    {
        event Action<string, string> KickClicked;
        event Action<string, string> InviteClicked;

        void SetParticipants(
            IReadOnlyList<LobbyParticipant> participants,
            bool localIsHost,
            string localPlayerId);

        void SetFriends(IReadOnlyList<FriendSummary> friends);
    }
}
