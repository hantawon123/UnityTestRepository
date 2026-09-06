using System;
using System.Collections.Generic;

namespace Game.Core.Home
{
    /// <summary>
    /// What came back when a nickname was offered up for checking.
    /// </summary>
    public enum NicknameCheckOutcome
    {
        Available,
        Taken,

        /// <summary>
        /// The name breaks the rule. Reachable only when the two sides of
        /// <see cref="NicknamePolicy"/> have drifted apart, since the screen
        /// refuses such a name before asking.
        /// </summary>
        Rejected,

        /// <summary>
        /// Nobody answered. Distinct from <see cref="Taken"/> on purpose: a
        /// player told the name is taken will pick another one, and a player
        /// told nothing answered will try again.
        /// </summary>
        Unreachable
    }

    /// <summary>
    /// Asks whoever owns the list of names whether one is free.
    /// </summary>
    /// <remarks>
    /// A port rather than a call, because the answer comes from the server and
    /// this project has no HTTP layer yet. Everything above it — the panel, the
    /// presenter and their tests — is finished against this interface, and the
    /// real implementation drops in behind it when the endpoint lands.
    /// <para>
    /// Callback rather than a task: the systems in this namespace are plain C#
    /// with events, and an async signature here would put a scheduler into a
    /// layer that has none.
    /// </para>
    /// </remarks>
    public interface INicknameAvailabilityCheck
    {
        void Check(string nickname, Action<NicknameCheckOutcome> onAnswered);
    }

    /// <summary>
    /// Answers from a list held in memory, and answers immediately.
    /// </summary>
    /// <remarks>
    /// Stands in until the server endpoint exists, and stays afterwards as the
    /// thing tests check against. Comparison is case sensitive because the
    /// nickname rule says upper and lower case are different names.
    /// </remarks>
    public sealed class InMemoryNicknameAvailabilityCheck : INicknameAvailabilityCheck
    {
        private readonly HashSet<string> taken = new HashSet<string>(StringComparer.Ordinal);

        public InMemoryNicknameAvailabilityCheck(IEnumerable<string> takenNicknames = null)
        {
            if (takenNicknames == null)
            {
                return;
            }

            foreach (var nickname in takenNicknames)
            {
                if (!string.IsNullOrWhiteSpace(nickname))
                {
                    taken.Add(nickname.Trim());
                }
            }
        }

        public void Check(string nickname, Action<NicknameCheckOutcome> onAnswered)
        {
            if (onAnswered == null)
            {
                throw new ArgumentNullException(nameof(onAnswered));
            }

            if (!NicknamePolicy.IsValid(nickname))
            {
                onAnswered(NicknameCheckOutcome.Rejected);
                return;
            }

            onAnswered(taken.Contains(nickname.Trim())
                ? NicknameCheckOutcome.Taken
                : NicknameCheckOutcome.Available);
        }
    }
}
