using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core.Home;
using VContainer.Unity;

namespace Game.Bootstrap
{
    /// <summary>
    /// Reloads the shared friend store when the lobby opens, so the 2-key
    /// roster shows the same people Home already knows.
    /// </summary>
    public sealed class LobbyFriendRefresh : IStartable, IDisposable
    {
        private readonly FriendUiCommands friends;
        private readonly CancellationTokenSource lifetime = new();

        public LobbyFriendRefresh(FriendUiCommands friends)
        {
            this.friends = friends ?? throw new ArgumentNullException(nameof(friends));
        }

        public void Start()
        {
            friends.RefreshFriendsAsync(lifetime.Token).Forget();
        }

        public void Dispose()
        {
            lifetime.Cancel();
            lifetime.Dispose();
        }
    }
}
