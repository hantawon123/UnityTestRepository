using System;
using System.Collections.Generic;
using Game.Core.Home;
using Game.Core.Lobby;

namespace Game.Core.Settings
{
    // Presentation policy only: identity and gameplay data remain unchanged.
    public sealed class InterfacePresentation : IDisposable
    {
        private readonly InterfaceSettingsSystem settings;
        private readonly FriendListSystem friends;
        private readonly RoomBrowserSystem room;
        private readonly Dictionary<string, bool> permissions = new();
        public event Action Changed;
        public InterfacePresentation(InterfaceSettingsSystem settings, FriendListSystem friends, RoomBrowserSystem room)
        {
            this.settings = settings; this.friends = friends; this.room = room;
            settings.Changed += OnSettingsChanged;
            friends.FriendsChanged += Refresh;
        }
        public bool IsFriend(string playerId)
        {
            foreach (var player in room.Participants.CurrentValue)
            {
                if (player.PlayerId != playerId || string.IsNullOrEmpty(player.UserId)) continue;
                foreach (var friend in friends.OnlineFriends) if (friend.PlayerId == player.UserId) return true;
                foreach (var friend in friends.OfflineFriends) if (friend.PlayerId == player.UserId) return true;
            }
            return false;
        }
        public bool ShowsChat(string playerId) => playerId == room.LocalPlayerId.CurrentValue ||
            !settings.Current.IsOn(InterfaceOption.ChatScope) || IsFriend(playerId);
        public string Name(string playerId, string nickname)
        {
            if (playerId == room.LocalPlayerId.CurrentValue)
                return settings.Current.Get(InterfaceOption.OwnNickname) == InterfaceCatalog.Off ? string.Empty : nickname;
            var scope = settings.Current.Get(InterfaceOption.PlayerNames);
            return permissions.TryGetValue(playerId ?? string.Empty, out var allowed) && allowed &&
                scope != InterfaceCatalog.Off && (scope != InterfaceCatalog.FriendsOnly || IsFriend(playerId))
                ? nickname : string.Empty;
        }
        public void SetPermission(string playerId, bool allowed)
        {
            if (string.IsNullOrEmpty(playerId)) return;
            if (permissions.TryGetValue(playerId, out var previous) && previous == allowed) return;
            permissions[playerId] = allowed;
            Refresh();
        }
        public void ClearPermissions() => ClearPermissions(notify: true);

        public void ClearPermissions(bool notify)
        {
            permissions.Clear();
            if (notify)
            {
                Refresh();
            }
        }
        private void OnSettingsChanged(InterfaceSettings value) => Refresh();
        private void Refresh() => Changed?.Invoke();
        public void Dispose()
        {
            settings.Changed -= OnSettingsChanged;
            friends.FriendsChanged -= Refresh;
        }
    }
}
