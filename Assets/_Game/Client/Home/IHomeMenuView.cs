using System;
using System.Collections.Generic;
using Game.Core.Home;

namespace Game.Client.Home
{
    public interface IHomeMenuView
    {
        event Action<HomeMenuAction> ActionClicked;

        event Action FriendListDismissed;

        event Action ProfileSettingsDismissed;

        event Action ServerSettingsDismissed;

        /// <summary>
        /// The code of the region the player picked.
        /// </summary>
        event Action<string> RegionSelected;

        /// <summary>
        /// The player filled the room form in and pressed create.
        /// </summary>
        event Action<string, bool, int> RoomCreationRequested;

        event Action CreateRoomDismissed;

        event Action<string> NicknameChangeRequested;

        event Action<string> NicknameEdited;

        event Action FriendSearchOpened;

        event Action FriendSearchClosed;

        event Action<string> FriendSearchRequested;

        event Action<string> FriendRequestClicked;

        /// <summary>
        /// The player asked for the list to be read again.
        /// </summary>
        event Action FriendListRefreshRequested;

        /// <summary>
        /// An incoming request was answered, by the id of who sent it.
        /// </summary>
        event Action<string> FriendRequestAccepted;

        event Action<string> FriendRequestRejected;

        void SetNickname(string nickname);

        void SetLevel(int level);

        void SetProfileSettingsVisible(bool visible);

        void SetNicknameAppliedFeedbackVisible(bool visible);

        /// <summary>
        /// How the attempt to take a name went.
        /// </summary>
        void SetNicknameAvailability(NicknameCheckOutcome outcome);

        void SetServerSettingsVisible(bool visible);

        /// <summary>
        /// Marks which region is in use. A code the picker does not list leaves
        /// every row unmarked.
        /// </summary>
        void SetSelectedRegion(string code);

        void SetCreateRoomVisible(bool visible);

        void SetFriendListVisible(bool visible);

        void SetFriends(
            IReadOnlyList<FriendSummary> onlineFriends,
            IReadOnlyList<FriendSummary> offlineFriends);

        void SetFriendSearchVisible(bool visible);

        void SetFriendSearchResults(IReadOnlyList<FriendSearchHit> results);

        /// <summary>
        /// The requests waiting to be answered, newest list wins.
        /// </summary>
        void SetIncomingRequests(IReadOnlyList<FriendSummary> requests);
    }
}
