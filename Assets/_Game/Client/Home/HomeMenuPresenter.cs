using System;
using System.Collections.Generic;
using Game.Core.Flow;
using Game.Core.Home;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace Game.Client.Home
{
    public interface IHomeApplicationHost
    {
        void Quit();

        void OpenHome();

        void OpenRoomBrowser();

        void OpenLobby();
    }

    public sealed class UnityHomeApplicationHost : IHomeApplicationHost
    {
        public const string HomeSceneName = "Home";
        public const string RoomBrowserSceneName = "Room";
        public const string LobbySceneName = "Lobby";

        public void Quit()
        {
            Application.Quit();
#if UNITY_EDITOR
            var editorApplicationType = Type.GetType("UnityEditor.EditorApplication, UnityEditor");
            editorApplicationType?.GetProperty("isPlaying")?.SetValue(null, false);
#endif
        }

        /// <summary>
        /// The two ways out of a screen, loaded asynchronously so the click that
        /// asked finishes before the scene it was on is torn down.
        /// </summary>
        /// <remarks>
        /// Both are taken while something is still live: the browser may be
        /// connecting to matchmaking when Home is asked for, and the lobby is in
        /// a room when the browser is. <c>OpenLobby</c> below is an entry rather
        /// than an exit and stays synchronous.
        /// <para>
        /// This alone does not make leaving safe. The unload still runs on the
        /// main thread as part of the load, so anything a departing scene does
        /// during <c>OnDestroy</c> that needs frames of its own will still
        /// deadlock here — see <c>NetworkRoomScreenBridge.Dispose</c>, which is
        /// where that was actually fixed.
        /// </para>
        /// </remarks>
        public void OpenHome()
        {
            LoadSceneAsync(HomeSceneName);
        }

        /// <inheritdoc cref="OpenHome"/>
        public void OpenRoomBrowser()
        {
            LoadSceneAsync(RoomBrowserSceneName);
        }

        public void OpenLobby()
        {
            var source = SceneManager.GetActiveScene().name;
            var startedAt = Time.realtimeSinceStartupAsDouble;
            Debug.Log($"[SceneTiming] Local load requested: {source} -> {LobbySceneName}.");
            SceneManager.LoadScene(LobbySceneName);
            Debug.Log(
                $"[SceneTiming] Local load completed: {source} -> {LobbySceneName}, " +
                $"elapsed={Time.realtimeSinceStartupAsDouble - startedAt:F3}s.");
        }

        private static void LoadSceneAsync(string target)
        {
            var source = SceneManager.GetActiveScene().name;
            var startedAt = Time.realtimeSinceStartupAsDouble;
            Debug.Log($"[SceneTiming] Local load requested: {source} -> {target}.");
            var previousPriority = Application.backgroundLoadingPriority;
            Application.backgroundLoadingPriority = ThreadPriority.High;
            var operation = SceneManager.LoadSceneAsync(target);
            if (operation == null)
            {
                Application.backgroundLoadingPriority = previousPriority;
                return;
            }

            operation.priority = 100;
            operation.completed += _ =>
            {
                Application.backgroundLoadingPriority = previousPriority;
                Debug.Log(
                    $"[SceneTiming] Local load completed: {source} -> {target}, " +
                    $"elapsed={Time.realtimeSinceStartupAsDouble - startedAt:F3}s.");
            };
        }
    }

    public sealed class HomeMenuPresenter : IStartable, IDisposable
    {
        private readonly PlayerProfile profile;
        private readonly HomeMenuSystem menu;
        private readonly FriendListSystem friends;
        private readonly FriendSearchSystem search;
        private readonly IHomeMenuView view;
        private readonly IHomeApplicationHost applicationHost;
        private readonly AppFlowSystem appFlow;
        private readonly INicknameAvailabilityCheck availability;
        private readonly ServerRegionSystem regions;
        private readonly FriendRequestSystem requests;
        private bool isFriendListVisible;
        private bool isRequestTabOpen;

        /// <summary>
        /// What was typed while the list tab was showing. Held here because the
        /// list is rebuilt from the friend system whenever it changes, and the
        /// filter has to survive that.
        /// </summary>
        private string listFilter = string.Empty;
        private bool isProfileSettingsVisible;
        private bool isServerSettingsVisible;

        public HomeMenuPresenter(
            PlayerProfile profile,
            HomeMenuSystem menu,
            IHomeMenuView view,
            IHomeApplicationHost applicationHost,
            AppFlowSystem appFlow,
            FriendListSystem friends,
            FriendSearchSystem search,
            INicknameAvailabilityCheck availability,
            ServerRegionSystem regions,
            FriendRequestSystem requests)
        {
            this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
            this.menu = menu ?? throw new ArgumentNullException(nameof(menu));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.applicationHost = applicationHost
                ?? throw new ArgumentNullException(nameof(applicationHost));
            this.appFlow = appFlow ?? throw new ArgumentNullException(nameof(appFlow));
            this.friends = friends ?? throw new ArgumentNullException(nameof(friends));
            this.search = search ?? throw new ArgumentNullException(nameof(search));
            this.availability = availability
                ?? throw new ArgumentNullException(nameof(availability));
            this.regions = regions ?? throw new ArgumentNullException(nameof(regions));
            this.requests = requests ?? throw new ArgumentNullException(nameof(requests));
        }

        public void Start()
        {
            view.ActionClicked += OnActionClicked;
            view.FriendListDismissed += HideFriendList;
            view.ProfileSettingsDismissed += HideProfileSettings;
            view.ServerSettingsDismissed += HideServerSettings;
            view.RegionSelected += OnRegionSelected;
            view.NicknameChangeRequested += OnNicknameChangeRequested;
            view.NicknameDuplicateCheckRequested += OnNicknameDuplicateCheckRequested;
            view.NicknameEdited += OnNicknameEdited;
            view.FriendSearchOpened += OnFriendSearchOpened;
            view.FriendSearchClosed += OnFriendSearchClosed;
            view.FriendSearchRequested += OnFriendSearchRequested;
            view.FriendRequestClicked += OnFriendRequestClicked;
            view.FriendListRefreshRequested += OnFriendListRefreshRequested;
            view.FriendRequestAccepted += OnFriendRequestAccepted;
            view.FriendRequestRejected += OnFriendRequestRejected;
            profile.Changed += BindProfile;
            friends.FriendsChanged += BindFriends;
            search.ResultsChanged += BindSearchResults;
            requests.Changed += BindRequests;
            requests.Accepted += friends.AddFriend;
            BindProfile(profile);
            BindFriends();
            BindRequests();
            HideFriendList();
            HideProfileSettings();
            view.SetServerSettingsVisible(false);
            view.SetSelectedRegion(regions.Current.Code);
        }

        public void Dispose()
        {
            view.ActionClicked -= OnActionClicked;
            view.FriendListDismissed -= HideFriendList;
            view.ProfileSettingsDismissed -= HideProfileSettings;
            view.ServerSettingsDismissed -= HideServerSettings;
            view.RegionSelected -= OnRegionSelected;
            view.NicknameChangeRequested -= OnNicknameChangeRequested;
            view.NicknameDuplicateCheckRequested -= OnNicknameDuplicateCheckRequested;
            view.NicknameEdited -= OnNicknameEdited;
            view.FriendSearchOpened -= OnFriendSearchOpened;
            view.FriendSearchClosed -= OnFriendSearchClosed;
            view.FriendSearchRequested -= OnFriendSearchRequested;
            view.FriendRequestClicked -= OnFriendRequestClicked;
            view.FriendListRefreshRequested -= OnFriendListRefreshRequested;
            view.FriendRequestAccepted -= OnFriendRequestAccepted;
            view.FriendRequestRejected -= OnFriendRequestRejected;
            profile.Changed -= BindProfile;
            friends.FriendsChanged -= BindFriends;
            search.ResultsChanged -= BindSearchResults;
            requests.Changed -= BindRequests;
            requests.Accepted -= friends.AddFriend;
        }

        private void OnActionClicked(HomeMenuAction action)
        {
            menu.Request(action);
            if (action == HomeMenuAction.Quit)
            {
                applicationHost.Quit();
                return;
            }

            if (action == HomeMenuAction.ServerSettings)
            {
                // The globe both opens and closes this one: the design gives
                // the panel no other way out. Opening it puts away whatever
                // else was up, so only one panel is ever on screen.
                var opening = !isServerSettingsVisible;
                if (opening)
                {
                    HideFriendList();
                    HideProfileSettings();
                }

                SetServerSettingsVisible(opening);
                return;
            }

            if (action == HomeMenuAction.Friends)
            {
                HideProfileSettings();
                HideServerSettings();
                ShowFriendList();
                return;
            }

            if (action == HomeMenuAction.ProfileSettings)
            {
                HideFriendList();
                HideServerSettings();
                ShowProfileSettings();
                return;
            }

            if (action == HomeMenuAction.FindRoom &&
                appFlow.TryTransitionTo(AppFlowState.RoomBrowser))
            {
                HideFriendList();
                HideProfileSettings();
                HideServerSettings();
                applicationHost.OpenRoomBrowser();
            }
        }

        private void OnNicknameChangeRequested(string nickname)
        {
            if (!isProfileSettingsVisible)
            {
                return;
            }

            if (profile.TryChangeNickname(nickname, out _))
            {
                view.SetNicknameAppliedFeedbackVisible(true);
            }
        }

        /// <summary>
        /// Passes the question on, and hands whatever comes back to the panel.
        /// </summary>
        /// <remarks>
        /// The answer may arrive later than the ask, so the panel is only told
        /// while it is still the thing on screen: a reply that lands after the
        /// player has closed it would light up an apply button nobody can see,
        /// and it would still be lit the next time they open the panel.
        /// </remarks>
        private void OnNicknameDuplicateCheckRequested(string nickname)
        {
            if (!isProfileSettingsVisible)
            {
                return;
            }

            availability.Check(nickname, outcome =>
            {
                if (isProfileSettingsVisible)
                {
                    view.SetNicknameAvailability(outcome);
                }
            });
        }

        private void OnNicknameEdited(string nickname)
        {
            if (!isProfileSettingsVisible)
            {
                return;
            }

            if (!string.Equals(nickname, profile.Nickname, StringComparison.Ordinal))
            {
                view.SetNicknameAppliedFeedbackVisible(false);
            }
        }

        private void OnFriendSearchOpened()
        {
            // The box is shared, and the view empties it on the way across, so
            // the list's filter goes with it.
            isRequestTabOpen = true;
            listFilter = string.Empty;
            search.ClearResults();
            view.SetFriendSearchVisible(true);
            BindFriends();
            BindSearchResults();
        }

        private void OnFriendSearchClosed()
        {
            isRequestTabOpen = false;
            listFilter = string.Empty;
            HideFriendSearch();
            BindFriends();
        }

        /// <summary>
        /// One box, two jobs: on the request tab it asks the directory for a
        /// player, and on the list tab it narrows the friends already shown.
        /// </summary>
        private void OnFriendSearchRequested(string query)
        {
            if (isRequestTabOpen)
            {
                search.Search(query, CollectFriendIds());
                return;
            }

            listFilter = query == null ? string.Empty : query.Trim();
            BindFriends();
        }

        /// <summary>
        /// Reads the list again. With no service behind it yet this only
        /// redraws, which is still worth having: it is the one control that
        /// picks up a friend who came online while the panel was open.
        /// </summary>
        private void OnFriendListRefreshRequested()
        {
            BindFriends();
            if (isRequestTabOpen)
            {
                BindSearchResults();
            }
        }

        private void OnFriendRequestClicked(string playerId)
        {
            search.TrySendRequest(playerId);
        }

        private void ShowFriendList()
        {
            if (isFriendListVisible)
            {
                return;
            }

            isFriendListVisible = true;
            HideFriendSearch();
            view.SetFriendListVisible(true);
        }

        private void HideFriendList()
        {
            HideFriendSearch();
            isFriendListVisible = false;
            view.SetFriendListVisible(false);
        }

        /// <summary>
        /// Records the pick. The panel has already moved its own mark, so this
        /// only puts the mark back when the code was not one we offer.
        /// </summary>
        private void OnRegionSelected(string code)
        {
            if (!regions.TrySelect(code))
            {
                view.SetSelectedRegion(regions.Current.Code);
            }
        }

        private void SetServerSettingsVisible(bool visible)
        {
            if (isServerSettingsVisible == visible)
            {
                return;
            }

            isServerSettingsVisible = visible;
            view.SetServerSettingsVisible(visible);
        }

        private void HideServerSettings()
        {
            SetServerSettingsVisible(false);
        }

        private void ShowProfileSettings()
        {
            if (isProfileSettingsVisible)
            {
                return;
            }

            isProfileSettingsVisible = true;
            view.SetNickname(profile.Nickname);
            view.SetLevel(profile.Level);
            view.SetNicknameAppliedFeedbackVisible(false);
            view.SetProfileSettingsVisible(true);
        }

        private void HideProfileSettings()
        {
            isProfileSettingsVisible = false;
            view.SetNickname(profile.Nickname);
            view.SetNicknameAppliedFeedbackVisible(false);
            view.SetProfileSettingsVisible(false);
        }

        private void HideFriendSearch()
        {
            search.ClearResults();
            view.SetFriendSearchVisible(false);
        }

        private void BindFriends()
        {
            view.SetFriends(
                Filtered(friends.OnlineFriends), Filtered(friends.OfflineFriends));
        }

        /// <summary>
        /// The friends whose nickname contains what was typed. One character is
        /// enough to start narrowing; nothing typed shows everyone.
        /// </summary>
        private IReadOnlyList<FriendSummary> Filtered(IReadOnlyList<FriendSummary> source)
        {
            if (listFilter.Length == 0)
            {
                return source;
            }

            var kept = new List<FriendSummary>();
            for (var index = 0; index < source.Count; index++)
            {
                if (source[index].Nickname.IndexOf(listFilter, StringComparison.Ordinal) >= 0)
                {
                    kept.Add(source[index]);
                }
            }

            return kept;
        }

        private void OnFriendRequestAccepted(string playerId)
        {
            requests.TryAccept(playerId);
        }

        private void OnFriendRequestRejected(string playerId)
        {
            requests.TryReject(playerId);
        }

        /// <summary>
        /// The panel draws a name and two buttons, so the arrival time that
        /// ordered the list is left behind here.
        /// </summary>
        private void BindRequests()
        {
            var incoming = requests.Incoming;
            var senders = new FriendSummary[incoming.Count];
            for (var index = 0; index < incoming.Count; index++)
            {
                senders[index] = incoming[index].Friend;
            }

            view.SetIncomingRequests(senders);
        }

        private void BindSearchResults()
        {
            view.SetFriendSearchResults(search.Results);
        }

        private string[] CollectFriendIds()
        {
            var ids = new string[friends.OnlineFriends.Count + friends.OfflineFriends.Count];
            var index = 0;
            for (var i = 0; i < friends.OnlineFriends.Count; i++)
            {
                ids[index++] = friends.OnlineFriends[i].PlayerId;
            }

            for (var i = 0; i < friends.OfflineFriends.Count; i++)
            {
                ids[index++] = friends.OfflineFriends[i].PlayerId;
            }

            return ids;
        }

        private void BindProfile(PlayerProfile source)
        {
            view.SetNickname(source.Nickname);
            view.SetLevel(source.Level);
        }
    }
}
