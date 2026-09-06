using System;

namespace Game.Core.Home
{
    public enum PlayerProfileError
    {
        None,
        NicknameRequired,

        /// <summary>
        /// There was a name, but not one the rule allows.
        /// </summary>
        NicknameNotAllowed,

        InvalidLevel
    }

    public sealed class PlayerProfile
    {
        public PlayerProfile(string nickname, int level)
        {
            if (string.IsNullOrWhiteSpace(nickname))
            {
                throw new ArgumentException("Nickname is required.", nameof(nickname));
            }

            if (level < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            Nickname = nickname.Trim();
            Level = level;
        }

        public string Nickname { get; private set; }
        public int Level { get; private set; }

        /// <summary>
        /// Whether this player has settled on a name. False means the server's
        /// temporary one is still in use and the one change is still available.
        /// </summary>
        /// <remarks>
        /// The server owns this — its account response carries
        /// <c>nicknameSet</c> — so this is the client's copy of that answer, not
        /// its own count. Until the account API is wired it only lasts as long
        /// as the process.
        /// </remarks>
        public bool NicknameSet { get; private set; }

        /// <summary>
        /// Takes the server's word for whether the name has been settled.
        /// </summary>
        public void MarkNicknameSet(bool settled)
        {
            if (NicknameSet == settled)
            {
                return;
            }

            NicknameSet = settled;
            Changed?.Invoke(this);
        }

        public event Action<PlayerProfile> Changed;

        public bool TryChangeNickname(
            string nickname,
            out PlayerProfileError error)
        {
            if (string.IsNullOrWhiteSpace(nickname))
            {
                error = PlayerProfileError.NicknameRequired;
                return false;
            }

            // The panel refuses a name the rule forbids long before it gets
            // here, and the server refuses it again afterwards. This is the
            // rule holding in the middle, for the callers that are not the
            // panel — the debug overlay is one.
            var trimmed = nickname.Trim();
            if (!NicknamePolicy.IsValid(trimmed))
            {
                error = PlayerProfileError.NicknameNotAllowed;
                return false;
            }

            Nickname = trimmed;

            // The one change is spent by a change that worked. A name refused
            // because someone else has it must leave the chance intact.
            NicknameSet = true;
            error = PlayerProfileError.None;
            Changed?.Invoke(this);
            return true;
        }

        public bool TryUpdateLevel(int level, out PlayerProfileError error)
        {
            if (level < 1)
            {
                error = PlayerProfileError.InvalidLevel;
                return false;
            }

            Level = level;
            error = PlayerProfileError.None;
            Changed?.Invoke(this);
            return true;
        }
    }
}
