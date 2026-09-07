using System;
using System.Text;

namespace Game.Network.Session
{
    /// <summary>
    /// Encodes the password and nickname presented before a peer joins a session.
    /// The wire format is versioned and length-prefixed so passwords may contain
    /// any character without being split by a separator.
    /// </summary>
    internal static class SessionConnectionTokenCodec
    {
        private const byte Version = 1;
        private const int HeaderSize = 3;

        public static byte[] Encode(string password, string nickname, string accountId = null)
        {
            var passwordBytes = Encoding.UTF8.GetBytes(password ?? string.Empty);
            var nicknameBytes = Encoding.UTF8.GetBytes(nickname ?? string.Empty);

            if (passwordBytes.Length == 0 && nicknameBytes.Length == 0)
            {
                return null;
            }

            if (passwordBytes.Length > ushort.MaxValue)
            {
                return null;
            }

            var token = new byte[HeaderSize + passwordBytes.Length + nicknameBytes.Length];
            token[0] = Version;
            token[1] = (byte)(passwordBytes.Length >> 8);
            token[2] = (byte)(passwordBytes.Length & 0xFF);

            passwordBytes.CopyTo(token, HeaderSize);
            nicknameBytes.CopyTo(token, HeaderSize + passwordBytes.Length);

            if (string.IsNullOrEmpty(accountId)) return token;
            if (accountId.Length > 64 || !IsAccountId(accountId)) return token;
            var id = Encoding.ASCII.GetBytes(accountId);
            var versioned = new byte[token.Length + id.Length + 1];
            versioned[0] = 2;
            versioned[1] = (byte)id.Length;
            id.CopyTo(versioned, 2);
            Buffer.BlockCopy(token, 1, versioned, 2 + id.Length, token.Length - 1);
            return versioned;
        }

        /// <summary>
        /// Invalid peer input is decoded as empty values instead of throwing.
        /// </summary>
        public static void Decode(
            byte[] token,
            out string password,
            out string nickname)
        {
            Decode(token, out password, out nickname, out _);
        }

        private static bool IsAccountId(string id)
        {
            foreach (var c in id)
                if (!(c >= 'a' && c <= 'z') && !(c >= 'A' && c <= 'Z') &&
                    !(c >= '0' && c <= '9') && c != '-') return false;
            return true;
        }

        public static void Decode(byte[] token, out string password, out string nickname, out string accountId)
        {
            password = string.Empty;
            nickname = string.Empty;
            accountId = string.Empty;
            if (token != null && token.Length >= 2 && token[0] == 2)
            {
                var count = token[1];
                if (count == 0 || count > 64 || token.Length < count + 4) return;
                var id = Encoding.ASCII.GetString(token, 2, count);
                if (!IsAccountId(id)) return;
                accountId = id;
                var legacy = new byte[token.Length - count - 1];
                legacy[0] = Version;
                Buffer.BlockCopy(token, count + 2, legacy, 1, legacy.Length - 1);
                token = legacy;
            }

            if (token == null || token.Length < HeaderSize || token[0] != Version)
            {
                return;
            }

            var passwordLength = (token[1] << 8) | token[2];
            if (HeaderSize + passwordLength > token.Length)
            {
                return;
            }

            password = Encoding.UTF8.GetString(token, HeaderSize, passwordLength);

            var nicknameStart = HeaderSize + passwordLength;
            nickname = Encoding.UTF8.GetString(
                token,
                nicknameStart,
                token.Length - nicknameStart);
        }

        public static bool MatchesPassword(string presented, string expected) =>
            !string.IsNullOrEmpty(expected) &&
            string.Equals(presented, expected, StringComparison.Ordinal);
    }
}
