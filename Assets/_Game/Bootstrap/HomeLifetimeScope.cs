using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Client.Common;
using Game.Client.Home;
using Game.Client.Lobby;
using Game.Core.Flow;
using Game.Core.Home;
using Game.Core.Lobby;
using Game.Core.Maps;
using Game.Core.Rooms;
using Game.Network.Session;
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
            builder.RegisterEntryPoint<CreateRoomWarmup>();
            builder.RegisterEntryPoint<RegionSwitcher>();
            builder.RegisterComponent(homeMenuView).As<IHomeMenuView>();

            builder.RegisterEntryPoint<HomeMenuPresenter>();
            builder.RegisterEntryPoint<HomeExitNotice>().WithParameter(homeMenuView);

            // Carries this panel's requests to the backend and its answers
            // back. The rows it shows used to be invented here.
            builder.RegisterEntryPoint<HomeFriendBridge>();

            // Sends a rename on to the account and puts the old name back when
            // the server refuses it.
            builder.RegisterEntryPoint<HomeProfileBridge>();
        }

        private sealed class HomeExitNotice : IStartable, System.IDisposable
        {
            private readonly HomeMenuView view;
            private readonly RoomBrowserSystem room;
            private LobbyConfirmView notice;

            public HomeExitNotice(HomeMenuView view, RoomBrowserSystem room)
            {
                this.view = view;
                this.room = room;
            }

            public void Start()
            {
                var reason = room.LastExit.CurrentValue;
                if (!reason.HasValue) return;
                if (reason == RoomExitReason.Left) { room.AcknowledgeExit(); return; }
                notice = LobbyConfirmView.Create(view.transform, showCancel: false);
                notice.Confirmed += Acknowledge;
                notice.Show(reason == RoomExitReason.HostClosed
                    ? "호스트의 연결이 끊어졌습니다" : "서버와의 연결이 끊어졌습니다");
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            private void Acknowledge() { room.AcknowledgeExit(); notice.Hide(); }
            public void Dispose()
            {
                if (notice != null) { notice.Confirmed -= Acknowledge; Object.Destroy(notice.gameObject); }
            }
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
        /// Starts loading the Lobby scene while the create-room form is open,
        /// so the wait after pressing 만들기 is the room being made rather than
        /// a scene being read off disk.
        /// </summary>
        /// <remarks>
        /// The room browser used to do this, from a create form it no longer
        /// has. Nothing forces the preload to be used: closing the form leaves a
        /// finished load sitting as a cache, and leaving Home releases it with
        /// the session.
        /// </remarks>
        private sealed class CreateRoomWarmup : IStartable, System.IDisposable
        {
            private readonly IHomeMenuView view;
            private readonly NetworkRunnerService network;

            public CreateRoomWarmup(IHomeMenuView view, NetworkRunnerService network)
            {
                this.view = view;
                this.network = network;
            }

            public void Start()
            {
                view.ActionClicked += OnActionClicked;
            }

            public void Dispose()
            {
                view.ActionClicked -= OnActionClicked;
            }

            private void OnActionClicked(HomeMenuAction action)
            {
                if (action == HomeMenuAction.CreateRoom)
                {
                    network.PrepareLobbyScene();
                }
            }
        }

        /// <summary>
        /// Reconnects the lobby when the player picks a different region.
        /// </summary>
        /// <remarks>
        /// The design gives the picker no apply button, so the choice has to
        /// take effect as it is made. Photon settles on a region when it
        /// connects, so the standing lobby connection is dropped and opened
        /// again: without that the panel would show one region while the room
        /// list still came from another.
        /// </remarks>
        private sealed class RegionSwitcher : IStartable, System.IDisposable
        {
            private readonly ServerRegionSystem regions;
            private readonly NetworkRunnerService network;
            private readonly RoomUiCommands rooms;

            public RegionSwitcher(
                ServerRegionSystem regions,
                NetworkRunnerService network,
                RoomUiCommands rooms)
            {
                this.regions = regions;
                this.network = network;
                this.rooms = rooms;
            }

            public void Start()
            {
                regions.Changed += OnRegionChanged;
            }

            public void Dispose()
            {
                regions.Changed -= OnRegionChanged;
            }

            private void OnRegionChanged(ServerRegion region)
            {
                Debug.Log($"[Region] Switching to {region.Code}.");
                network.DropMatchmakingConnection();
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
            private readonly NetworkRunnerService network;
            private readonly IHomeMenuView view;
            private readonly AppFlowSystem appFlow;
            private readonly UnityHomeApplicationHost fallback = new();

            public NetworkHomeApplicationHost(
                RoomUiCommands rooms,
                FrontendSceneCoordinator scenes,
                NetworkRunnerService network,
                IHomeMenuView view,
                AppFlowSystem appFlow)
            {
                this.rooms = rooms;
                this.scenes = scenes;
                this.network = network;
                this.view = view;
                this.appFlow = appFlow;
            }

            public void Quit() => fallback.Quit();

            public void OpenHome() => scenes.OpenHome();

            public void OpenCharacterCloset() => scenes.OpenCharacterCloset();

            /// <summary>
            /// Opens the room browser, and starts filling its list on the way.
            /// </summary>
            /// <remarks>
            /// The list is asked for here rather than by the browser scene, so
            /// the Photon handshake runs beside the scene load instead of after
            /// it. A refusal is said on this screen because it is the one still
            /// on it when the answer comes back.
            /// </remarks>
            public void OpenRoomBrowser()
            {
                OpenRoomBrowserAsync().Forget(exception => Debug.LogException(exception));
            }

            private async UniTask OpenRoomBrowserAsync()
            {
                scenes.OpenRoomBrowser();

                try
                {
                    await rooms.RefreshAsync(CancellationToken.None);
                }
                catch (System.OperationCanceledException)
                {
                    // Home was left while the list was being read. The browser
                    // asks again for itself.
                }
                catch (System.Exception failure)
                {
                    Debug.LogException(failure);
                    view.ShowConnectionError(
                        RoomEntryMessages.Describe(
                            RoomEntryFailure.ConnectionFailed, RoomEntrySource.RoomList));
                }
            }

            /// <summary>
            /// Opens the room, then the lobby it made.
            /// </summary>
            /// <remarks>
            /// PRIVATE is a room that stays out of the list and is reached by
            /// its code. Visibility and password protection are separate settings.
            /// </remarks>
            public void CreateRoom(string title, bool isPublic, int maxPlayers)
            {
                var request = new RoomCreateRequest(
                    title,
                    isLocked: false,
                    password: null,
                    maxPlayers: maxPlayers,
                    mapId: MapCatalog.DefaultMapId,
                    isPrivate: !isPublic);

                CreateThenOpenLobbyAsync(request)
                    .Forget(exception => Debug.LogException(exception));
            }

            private async UniTask CreateThenOpenLobbyAsync(RoomCreateRequest request)
            {
                var result = await rooms.CreateAsync(request, CancellationToken.None);
                if (!result.Ok)
                {
                    Debug.LogWarning($"[Home] Room creation failed: {result.Failure}.");

                    // The form stays open behind the notice, with what was typed
                    // still in it: the player is one press from trying again,
                    // and none of these failures are about what they typed.
                    view.ShowConnectionError(
                        RoomEntryMessages.Describe(
                            result.Failure, RoomEntrySource.RoomCreate));
                    return;
                }

                // Moved now rather than when the form was sent. There is a room
                // to be in as of this line, and the flow rules have no way back
                // from Lobby to Home for a room that never opened.
                if (appFlow.CurrentState != AppFlowState.Lobby &&
                    !appFlow.TryTransitionTo(AppFlowState.Lobby))
                {
                    Debug.LogError($"[Home] Opened a room from {appFlow.CurrentState}.");
                }

                OpenLobby();
            }

            /// <summary>
            /// Through Fusion rather than by loading the scene: the runner is
            /// already in the room, and swapping the Unity scene out from under
            /// it would leave the session behind.
            /// </summary>
            public void OpenLobby()
            {
                if (!network.EnterLobbyScene())
                {
                    Debug.LogError(
                        "[Session] Cannot enter Lobby without a running room session.");
                }
            }
        }
    }
}
