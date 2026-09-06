using System;
using System.Collections.Generic;

namespace Game.Core.Home
{
    /// <summary>
    /// The order friends are listed in: Hangul first, then Latin, then digits.
    /// </summary>
    /// <remarks>
    /// Ordinal comparison alone would put digits first and Hangul last, because
    /// that is the order their code points happen to fall in. The design asks
    /// for the reverse, so each character is ranked by which alphabet it
    /// belongs to before its own value is looked at.
    /// </remarks>
    public sealed class FriendNameComparer : IComparer<string>
    {
        public static readonly FriendNameComparer Instance = new FriendNameComparer();

        public int Compare(string left, string right)
        {
            if (left == null)
            {
                return right == null ? 0 : 1;
            }

            if (right == null)
            {
                return -1;
            }

            var shared = Math.Min(left.Length, right.Length);
            for (var index = 0; index < shared; index++)
            {
                var byAlphabet = Rank(left[index]).CompareTo(Rank(right[index]));
                if (byAlphabet != 0)
                {
                    return byAlphabet;
                }

                var byCharacter = left[index].CompareTo(right[index]);
                if (byCharacter != 0)
                {
                    return byCharacter;
                }
            }

            // One is a prefix of the other, so the shorter comes first.
            return left.Length.CompareTo(right.Length);
        }

        private static int Rank(char character)
        {
            if (character >= '가' && character <= '힣')
            {
                return 0;
            }

            if ((character >= 'a' && character <= 'z') || (character >= 'A' && character <= 'Z'))
            {
                return 1;
            }

            if (character >= '0' && character <= '9')
            {
                return 2;
            }

            return 3;
        }
    }
}
