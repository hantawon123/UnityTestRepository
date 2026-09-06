using System;
using Game.Core.Ports;

namespace Game.Core.Home
{
    /// <summary>
    /// Which region the player is set to play on, and remembering it.
    /// </summary>
    /// <remarks>
    /// Reads the saved choice once, at construction, because that is when the
    /// answer is needed: the picker opens showing it and the network layer asks
    /// for it before the first connection.
    /// </remarks>
    public sealed class ServerRegionSystem
    {
        private readonly IServerRegionStore store;

        public ServerRegionSystem(IServerRegionStore store)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));

            // A saved code the catalogue no longer lists falls back to the
            // default rather than being kept. Regions get renamed and switched
            // off, and a stale one would leave the picker showing nothing
            // selected while the game connected somewhere unnamed.
            Current = store.TryLoad(out var code)
                && ServerRegionCatalog.TryFind(code, out var saved)
                    ? saved
                    : ServerRegionCatalog.Default;
        }

        public ServerRegion Current { get; private set; }

        public event Action<ServerRegion> Changed;

        /// <summary>
        /// Moves to another region and writes it down. False when the code is
        /// not one of ours; true also when it was already the current one, so a
        /// caller cannot read a repeated press as a failure.
        /// </summary>
        public bool TrySelect(string code)
        {
            if (!ServerRegionCatalog.TryFind(code, out var region))
            {
                return false;
            }

            if (string.Equals(region.Code, Current.Code, StringComparison.Ordinal))
            {
                return true;
            }

            Current = region;
            store.Save(region.Code);
            Changed?.Invoke(region);
            return true;
        }
    }
}
