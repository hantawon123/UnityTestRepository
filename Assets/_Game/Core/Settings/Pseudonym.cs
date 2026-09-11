namespace Game.Core.Settings
{
    /// <summary>
    /// The made-up name somebody in 스트리머 모드 is known by.
    /// </summary>
    /// <remarks>
    /// Deliberately obvious. A pseudonym that read like an ordinary nickname
    /// would leave the rest of the room unable to tell a made-up name from a
    /// real one, and somebody would end up addressing a person by a name that
    /// is not theirs without knowing it.
    /// <para>
    /// Made from a seed rather than at random, so the same seed is the same
    /// name. What varies the name between visits is the seed, which
    /// <c>NetworkInterfaceSettings</c> makes afresh each time a player joins a
    /// room; the name then holds for as long as they stay, so the room can
    /// still talk to them.
    /// </para>
    /// <para>
    /// The owner works theirs out and sends it, rather than everybody deriving
    /// it from an account id. Two peers deriving it separately would have to
    /// agree on the seed as well, and the room already has a way to carry one
    /// short string from a player to everybody else.
    /// </para>
    /// </remarks>
    public static class Pseudonym
    {
        /// <summary>
        /// What every pseudonym starts with, so that a name which is not a
        /// person's own says so before anything else.
        /// </summary>
        public const string Prefix = "익명";

        private static readonly string[] Nouns =
        {
            "마법사", "기사", "궁수", "도적",
            "사냥꾼", "상인", "여행자", "나그네",
            "방랑자", "수집가", "탐험가", "연금술사",
            "점술가", "대장장이", "음유시인", "파수꾼"
        };

        /// <summary>How many names there are, before the number.</summary>
        public static int NounCount => Nouns.Length;

        /// <summary>
        /// The pseudonym for a seed — <c>익명마법사47</c> and the like.
        /// </summary>
        /// <remarks>
        /// Two digits, so the name stays short enough to sit over a character
        /// without covering it, and negative seeds are folded rather than
        /// rejected: a hash is as likely to be one as not.
        /// </remarks>
        public static string From(int seed)
        {
            var spread = seed & int.MaxValue;
            return Prefix + Nouns[spread % Nouns.Length] + (spread / Nouns.Length % 100).ToString("00");
        }

        /// <summary>
        /// Whether a name is one of ours. For the screens that would rather
        /// not offer to add somebody as a friend under a name that is not
        /// theirs.
        /// </summary>
        public static bool IsOne(string name) =>
            !string.IsNullOrEmpty(name) && name.StartsWith(Prefix, System.StringComparison.Ordinal);
    }
}
