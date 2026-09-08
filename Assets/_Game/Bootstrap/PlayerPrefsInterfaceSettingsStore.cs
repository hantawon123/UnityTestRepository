using System;
using Game.Core.Ports;
using Game.Core.Settings;
using UnityEngine;

namespace Game.Bootstrap
{
    /// <summary>
    /// Keeps the 인터페이스 settings in Unity's player preferences, beside the
    /// other tabs'.
    /// </summary>
    /// <inheritdoc cref="PlayerPrefsGraphicsSettingsStore"/>
    public sealed class PlayerPrefsInterfaceSettingsStore : IInterfaceSettingsStore
    {
        private const string Prefix = "game.settings.interface.";

        private static readonly InterfaceOption[] Options =
            (InterfaceOption[])Enum.GetValues(typeof(InterfaceOption));

        public bool TryLoad(out InterfaceSettings settings)
        {
            settings = InterfaceSettings.Empty;
            var found = false;

            foreach (var option in Options)
            {
                var key = Prefix + option;
                if (!PlayerPrefs.HasKey(key))
                {
                    continue;
                }

                var code = PlayerPrefs.GetString(key);
                if (string.IsNullOrWhiteSpace(code))
                {
                    continue;
                }

                settings = settings.With(option, code.Trim());
                found = true;
            }

            return found;
        }

        public void Save(InterfaceSettings settings)
        {
            foreach (var option in Options)
            {
                var code = settings.Get(option);
                if (string.IsNullOrWhiteSpace(code))
                {
                    Debug.LogWarning($"[Settings] Not saving a blank {option}.");
                    continue;
                }

                PlayerPrefs.SetString(Prefix + option, code.Trim());
            }

            PlayerPrefs.Save();
        }
    }
}
