using System;
using System.Collections.Generic;

namespace Game.Core.Settings
{
    /// <summary>
    /// The rows of the 인터페이스 tab, in the order they are drawn.
    /// </summary>
    public enum InterfaceOption
    {
        UiScale,
        FontScale,
        InGameUi,
        FpsCounter,
        PingCounter,
        PlayerNames,
        OwnNickname,
        BeginnerGuide,
        ChatScope
    }

    /// <summary>
    /// What the 인터페이스 tab holds: one chosen code per row.
    /// </summary>
    /// <inheritdoc cref="GraphicsSettings"/>
    public readonly struct InterfaceSettings : IEquatable<InterfaceSettings>
    {
        internal static readonly int RowCount = Enum.GetValues(typeof(InterfaceOption)).Length;

        private readonly OptionValues values;

        internal InterfaceSettings(OptionValues values)
        {
            this.values = values;
        }

        public static InterfaceSettings Empty => default;

        internal OptionValues Values => values;

        public string Get(InterfaceOption option) => values.Get((int)option);

        public InterfaceSettings With(InterfaceOption option, string code)
        {
            if ((int)option < 0 || (int)option >= RowCount)
            {
                throw new ArgumentOutOfRangeException(nameof(option));
            }

            return new InterfaceSettings(values.With((int)option, code));
        }

        /// <summary>
        /// Whether a row that is simply on or off is on. For the rows with a
        /// third answer — who a name is shown to — ask <see cref="Get"/> and
        /// compare against <see cref="InterfaceCatalog.FriendsOnly"/>.
        /// </summary>
        public bool IsOn(InterfaceOption option) =>
            string.Equals(Get(option), InterfaceCatalog.On, StringComparison.Ordinal);

        public bool Equals(InterfaceSettings other) => values == other.values;

        public override bool Equals(object obj) => obj is InterfaceSettings other && Equals(other);

        public override int GetHashCode() => values.GetHashCode();

        public static bool operator ==(InterfaceSettings left, InterfaceSettings right) => left.Equals(right);

        public static bool operator !=(InterfaceSettings left, InterfaceSettings right) => !left.Equals(right);

        public override string ToString()
        {
            var parts = new string[RowCount];
            for (var index = 0; index < RowCount; index++)
            {
                var option = (InterfaceOption)index;
                parts[index] = $"{option}={Get(option)}";
            }

            return "Interface(" + string.Join(", ", parts) + ")";
        }
    }

    /// <summary>
    /// What every row of the 인터페이스 tab offers, and what it starts on.
    /// </summary>
    public sealed class InterfaceCatalog
    {
        public const string On = "on";
        public const string Off = "off";

        /// <summary>Shown to friends and to nobody else.</summary>
        public const string FriendsOnly = "friends";

        public const string Large = "large";
        public const string Medium = "medium";
        public const string Small = "small";

        private static readonly OptionChoices Toggle = new OptionChoices(
            new OptionChoice(On, "켜기"),
            new OptionChoice(Off, "끄기"));

        private static OptionChoices Scale => new OptionChoices(
            Medium,
            new OptionChoice(Large, "크게"),
            new OptionChoice(Medium, "중간"),
            new OptionChoice(Small, "작게"));

        /// <summary>
        /// Who a name is shown to. The three are on, friends only, and off —
        /// what "off" actually draws is the HUD's decision and is not settled;
        /// see the notes on <see cref="Shipped"/>.
        /// </summary>
        private static OptionChoices Visibility => new OptionChoices(
            new OptionChoice(On, "켜기"),
            new OptionChoice(FriendsOnly, "친구만"),
            new OptionChoice(Off, "끄기"));

        private readonly OptionCatalog rows;

        public InterfaceCatalog(IReadOnlyDictionary<InterfaceOption, OptionChoices> choices)
        {
            if (choices == null)
            {
                throw new ArgumentNullException(nameof(choices));
            }

            var byIndex = new Dictionary<int, OptionChoices>();
            foreach (var pair in choices)
            {
                byIndex[(int)pair.Key] = pair.Value;
            }

            rows = new OptionCatalog(InterfaceSettings.RowCount, byIndex);
        }

        /// <summary>
        /// What the game ships with, from the design's own lists.
        /// </summary>
        /// <remarks>
        /// Two rows are listed but not finished. 플레이어 이름 표시 and
        /// 내 닉네임 표시 both offer 끄기, and whether that hides a name
        /// outright or replaces it with something anonymous has not been
        /// decided. The choice is stored either way; what gets drawn is the
        /// HUD's, and until it is settled 끄기 means only "not shown".
        /// <para>
        /// 채팅 메시지 범위 제한 reads the other way round from the rest: it is
        /// a restriction, so 끄기 is everybody and 켜기 is friends only. The
        /// labels say so rather than leaving the player to work it out, and
        /// unrestricted is the default.
        /// </para>
        /// </remarks>
        public static InterfaceCatalog Shipped { get; } = new InterfaceCatalog(
            new Dictionary<InterfaceOption, OptionChoices>
            {
                [InterfaceOption.UiScale] = Scale,
                [InterfaceOption.FontScale] = Scale,
                [InterfaceOption.InGameUi] = Toggle,
                [InterfaceOption.FpsCounter] = Toggle,
                [InterfaceOption.PingCounter] = Toggle,
                [InterfaceOption.PlayerNames] = Visibility,
                [InterfaceOption.OwnNickname] = Visibility,
                [InterfaceOption.BeginnerGuide] = Toggle,
                [InterfaceOption.ChatScope] = new OptionChoices(
                    new OptionChoice(Off, "끄기(모두)"),
                    new OptionChoice(On, "켜기(친구만)"))
            });

        public OptionChoices For(InterfaceOption option) => rows.For((int)option);

        /// <inheritdoc cref="OptionCatalog.Defaults"/>
        public InterfaceSettings Defaults => new InterfaceSettings(rows.Defaults);

        /// <inheritdoc cref="OptionCatalog.Normalise"/>
        public InterfaceSettings Normalise(InterfaceSettings settings) =>
            new InterfaceSettings(rows.Normalise(settings.Values));

        /// <inheritdoc cref="OptionCatalog.Label"/>
        public string Label(InterfaceOption option, string code) => rows.Label((int)option, code);
    }
}
