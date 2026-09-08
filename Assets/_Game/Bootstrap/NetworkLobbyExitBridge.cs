using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Client.Lobby;
using Game.Client.Match;
using Game.Core.Lobby;
using UnityEngine;
using VContainer.Unity;

namespace Game.Bootstrap
{
    /// <summary>
    /// Carries the lobby's leave request to the session, so leaving the screen
    /// also leaves the Photon room.
    /// </summary>
    /// <remarks>
    /// Without this the player walked back to the browser while their character
    /// stayed spawned, their seat stayed taken and everyone still in the room
    /// kept seeing them on the list.
    /// </remarks>
    public sealed class NetworkLobbyExitBridge : IStartable, IDisposable
    {
        private readonly LobbyExitPresenter exit;
        private readonly RoomUiCommands commands;
        private readonly IHighlightTransitionView cover;

        public NetworkLobbyExitBridge(
            LobbyExitPresenter exit,
            RoomUiCommands commands,
            IHighlightTransitionView cover)
        {
            this.exit = exit ?? throw new ArgumentNullException(nameof(exit));
            this.commands = commands ?? throw new ArgumentNullException(nameof(commands));
            this.cover = cover ?? throw new ArgumentNullException(nameof(cover));
        }

        public void Start()
        {
            exit.LeaveRequested += OnLeaveRequested;
        }

        public void Dispose()
        {
            exit.LeaveRequested -= OnLeaveRequested;
        }

        private void OnLeaveRequested()
        {
            Leave().Forget();
        }

        /// <remarks>
        /// Deliberately uncancellable. Fade-out finishes first so the next
        /// screen is not shown through the lobby. The token is not tied to this
        /// scene: unloading mid-fade would cancel the Photon leave and leave
        /// the room believing this player is still in it. The runner outlives
        /// the scene, so the call finishes on its own.
        /// </remarks>
        private async UniTaskVoid Leave()
        {
            try
            {
                await FadeOutCover();
                await commands.LeaveAsync(CancellationToken.None);
            }
            catch (Exception failure)
            {
                Debug.LogError($"[Rooms] Could not leave the room: {failure.Message}");
            }
        }

        private async UniTask FadeOutCover()
        {
            var from = cover.Opacity;
            if (from < 0.999f)
            {
                var elapsed = 0f;
                while (elapsed < LobbySceneFade.DurationSeconds)
                {
                    elapsed += Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
                    cover.SetOpacity(LobbySceneFade.Lerp(from, 1f, elapsed));
                    await UniTask.Yield(PlayerLoopTiming.Update);
                }
            }

            cover.SetOpacity(1f);
            var covers = UnityEngine.Object.FindObjectsByType<HighlightTransitionView>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var index = 0; index < covers.Length; index++)
            {
                covers[index].SetOpacity(1f);
            }
        }
    }
}
