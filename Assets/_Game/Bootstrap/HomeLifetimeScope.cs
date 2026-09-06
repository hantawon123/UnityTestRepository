using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Client.Home;
using Game.Core.Home;
using Game.Core.Lobby;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Bootstrap
{
    public sealed class HomeLifetimeScope : LifetimeScope
    {
        [SerializeField]
        private HomeMenuView homeMenuView;

        protected override void Configure(IContainerBuilder builder)
        {
            if (homeMenuView == null)
            {
                Debug.LogError("HomeMenuView must be assigned on HomeLifetimeScope.", this);
                return;
            }

            // No PlayerProfile here on purpose. Registering one would shadow the
            // application-wide profile for this scene only, so renaming yourself
            // on this screen would change a copy that nothing else can see: not
            // the saved profile, and not the name the network sends. Resolution
            // falls through to the project scope instead.
            builder.Register<NetworkHomeApplicationHost>(Lifetime.Scoped)
                .As<IHomeApplicationHost>();
            builder.RegisterEntryPoint<RoomBrowserWarmup>();
            builder.RegisterComponent(homeMenuView).As<IHomeMenuView>();

            // Placeholder until the server grows an endpoint for this, in the
            // same spirit as the preview friends below: the panel and its
            // presenter are finished, and only this registration changes when
            // the real check arrives.
            builder.RegisterInstance(
                    new InMemoryNicknameAvailabilityCheck(new[] { "금오산냥냥이", "관리자" }))
                .As<INicknameAvailabilityCheck>();
            builder.RegisterEntryPoint<HomeMenuPresenter>();

            // Placeholder rows until a Steam adapter calls FriendListSystem.ReplaceFriends.
            builder.RegisterBuildCallback(container =>
            {
                var friendList = container.Resolve<FriendListSystem>();
                var friendSearch = container.Resolve<FriendSearchSystem>();
                if (friendList.OnlineFriends.Count > 0 || friendList.OfflineFriends.Count > 0)
                {
                    return;
                }

                // Deliberately more than the panel is tall, and deliberately
                // out of order: this is what the scrolling, the three-tier
                // grouping and the Hangul-Latin-digit sort are looked at with
                // until a Steam adapter fills the list for real.
                var previewFriends = new[]
                {
                    new FriendSummary("preview-1", "999구구구", FriendPresence.Online),
                    new FriendSummary("preview-2", "zebra", FriendPresence.InGame),
                    new FriendSummary("preview-3", "가나다", FriendPresence.Online),
                    new FriendSummary("preview-4", "나비야", FriendPresence.InGame),
                    new FriendSummary("preview-5", "apple", FriendPresence.Online),
                    new FriendSummary("preview-6", "12345", FriendPresence.Online),
                    new FriendSummary("preview-7", "다람쥐", FriendPresence.Online),
                    new FriendSummary("preview-8", "스팀만켠친구", FriendPresence.SteamOnline),
                    new FriendSummary("preview-9", "steamer", FriendPresence.SteamOnline),
                    new FriendSummary("preview-10", "77스팀", FriendPresence.SteamOnline),
                    new FriendSummary("preview-11", "잠수친구", FriendPresence.Offline),
                    new FriendSummary("preview-12", "banana", FriendPresence.Offline),
                    new FriendSummary("preview-13", "404낫파운드", FriendPresence.Offline),
                    new FriendSummary("preview-14", "이건바로열두글자이지렁롱", FriendPresence.Offline),

                    // The widest a nickname can be: twelve of the broadest
                    // letter in the face. If a row survives this it survives
                    // anything the rule allows.
                    new FriendSummary("preview-15", "MMMMMMMMMMMM", FriendPresence.Offline)
                };
                friendList.ReplaceFriends(previewFriends);
                friendSearch.ReplaceDirectory(new[]
                {
                    previewFriends[0],
                    previewFriends[1],
                    previewFriends[2],
                    new FriendSummary("preview-search-1", "금오산냥펀치", FriendPresence.Online),
                    new FriendSummary("preview-search-2", "금오산냥옹2", FriendPresence.Offline),
                    new FriendSummary("preview-search-3", "플레이어A", FriendPresence.Online)
                });
            });
        }

        /// <summary>
        /// Pays the Photon lobby handshake while the player is still on Home,
        /// so opening the room browser can reuse an established connection.
        /// </summary>
        private sealed class RoomBrowserWarmup : IStartable
        {
            private readonly RoomUiCommands rooms;

            public RoomBrowserWarmup(RoomUiCommands rooms)
            {
                this.rooms = rooms;
            }

            public void Start()
            {
                rooms.RefreshAsync(CancellationToken.None)
                    .Forget(exception => Debug.LogException(exception));
            }
        }

        /// <summary>
        /// Starts matchmaking beside the Room scene load instead of waiting for
        /// that scene to finish before opening the Photon lobby.
        /// </summary>
        private sealed class NetworkHomeApplicationHost : IHomeApplicationHost
        {
            private readonly RoomUiCommands rooms;
            private readonly FrontendSceneCoordinator scenes;
            private readonly UnityHomeApplicationHost fallback = new();

            public NetworkHomeApplicationHost(
                RoomUiCommands rooms,
                FrontendSceneCoordinator scenes)
            {
                this.rooms = rooms;
                this.scenes = scenes;
            }

            public void Quit() => fallback.Quit();

            public void OpenHome() => scenes.OpenHome();

            public void OpenRoomBrowser()
            {
                rooms.RefreshAsync(CancellationToken.None)
                    .Forget(exception => Debug.LogException(exception));
                scenes.OpenRoomBrowser();
            }

            public void OpenLobby() => fallback.OpenLobby();
        }
    }
}
