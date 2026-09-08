using System;
using System.Collections.Generic;

namespace Game.Core.Maps
{
    /// <summary>
    /// Maps currently available to room creation and match startup.
    /// Add a map id here when another playable map is ready.
    /// </summary>
    public static class MapCatalog
    {
        public const string PlaygroundId = "playground";

        private static readonly string[] MapIdValues =
        {
            PlaygroundId
        };

        private static readonly Random RandomPicker = new();

        public static IReadOnlyList<string> MapIds { get; } =
            Array.AsReadOnly(MapIdValues);

        public static string DefaultMapId => MapIds[0];

        public static bool Contains(string mapId)
        {
            if (string.IsNullOrWhiteSpace(mapId))
            {
                return false;
            }

            var candidate = mapId.Trim();

            foreach (var availableMapId in MapIds)
            {
                if (string.Equals(availableMapId, candidate, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Picks one of the playable maps. Used when the lobby map choice is random.
        /// </summary>
        public static string PickRandom()
        {
            if (MapIdValues.Length == 0)
            {
                return DefaultMapId;
            }

            if (MapIdValues.Length == 1)
            {
                return MapIdValues[0];
            }

            lock (RandomPicker)
            {
                return MapIdValues[RandomPicker.Next(MapIdValues.Length)];
            }
        }
    }
}
