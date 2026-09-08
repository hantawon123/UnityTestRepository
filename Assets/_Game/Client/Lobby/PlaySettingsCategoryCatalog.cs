using System;
using System.Collections.Generic;

namespace Game.Client.Lobby
{
    public readonly struct PlaySettingsCategoryOption
    {
        public PlaySettingsCategoryOption(string id, string label)
        {
            Id = id?.Trim() ?? string.Empty;
            Label = label ?? string.Empty;
        }

        /// <summary>Empty id selects a random assignment category at runtime.</summary>
        public string Id { get; }

        public string Label { get; }

        public bool IsRandom => string.IsNullOrEmpty(Id);
    }

    /// <summary>
    /// Category choices shown in play settings. Add entries when new assignment
    /// categories are ready. Ids should match <see cref="Game.Core.Items.ItemCatalog"/>
    /// categories once gameplay supports them.
    /// </summary>
    public static class PlaySettingsCategoryCatalog
    {
        private static readonly PlaySettingsCategoryOption[] Options =
        {
            new(string.Empty, "랜덤"),
        };

        public static IReadOnlyList<PlaySettingsCategoryOption> All { get; } = Options;

        public static int DefaultIndex => 0;

        public static PlaySettingsCategoryOption Default => Options[DefaultIndex];

        public static PlaySettingsCategoryOption GetOption(int index)
        {
            if (Options.Length == 0)
            {
                return default;
            }

            return Options[Math.Clamp(index, 0, Options.Length - 1)];
        }

        public static int IndexOf(string categoryId)
        {
            var normalized = categoryId?.Trim() ?? string.Empty;
            for (var i = 0; i < Options.Length; i++)
            {
                if (string.Equals(Options[i].Id, normalized, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        public static bool Contains(string categoryId) => IndexOf(categoryId) >= 0;
    }
}
