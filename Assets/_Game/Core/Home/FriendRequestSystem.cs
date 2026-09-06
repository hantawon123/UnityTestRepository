using System;
using System.Collections.Generic;

namespace Game.Core.Home
{
    /// <summary>
    /// Someone asking to be a friend, and when they asked.
    /// </summary>
    public readonly struct FriendRequest
    {
        public FriendRequest(FriendSummary friend, DateTimeOffset requestedAt)
        {
            Friend = friend;
            RequestedAt = requestedAt;
        }

        public FriendSummary Friend { get; }

        public DateTimeOffset RequestedAt { get; }
    }

    /// <summary>
    /// The friend requests waiting to be answered.
    /// </summary>
    /// <remarks>
    /// Only the ones that arrived. Requests the player sent are tracked by
    /// <see cref="FriendSearchSystem"/>, beside the search they were sent from,
    /// because that is the only place they are shown.
    /// </remarks>
    public sealed class FriendRequestSystem
    {
        private List<FriendRequest> incoming = new List<FriendRequest>();

        public IReadOnlyList<FriendRequest> Incoming => incoming;

        /// <summary>
        /// Raised with the request that was accepted, so whoever owns the
        /// friend list can take them in.
        /// </summary>
        public event Action<FriendSummary> Accepted;

        public event Action Changed;

        public void ReplaceIncoming(IEnumerable<FriendRequest> requests)
        {
            if (requests == null)
            {
                throw new ArgumentNullException(nameof(requests));
            }

            // One row per person, however many times they asked. A request
            // resent while the panel was open would otherwise arrive as a
            // second row with the same name and its own pair of buttons, and
            // answering one of them would leave the other behind.
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var next = new List<FriendRequest>();
            foreach (var request in requests)
            {
                if (seen.Add(request.Friend.PlayerId))
                {
                    next.Add(request);
                }
            }

            next.Sort(CompareByArrival);
            incoming = next;
            Changed?.Invoke();
        }

        public bool TryAccept(string playerId)
        {
            if (!TryTake(playerId, out var accepted))
            {
                return false;
            }

            // Announced before Changed so the list the player is about to see
            // already has them in it.
            Accepted?.Invoke(accepted);
            Changed?.Invoke();
            return true;
        }

        public bool TryReject(string playerId)
        {
            if (!TryTake(playerId, out _))
            {
                return false;
            }

            Changed?.Invoke();
            return true;
        }

        private bool TryTake(string playerId, out FriendSummary taken)
        {
            taken = default;
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return false;
            }

            var wanted = playerId.Trim();
            for (var index = 0; index < incoming.Count; index++)
            {
                if (!string.Equals(
                        incoming[index].Friend.PlayerId, wanted, StringComparison.Ordinal))
                {
                    continue;
                }

                taken = incoming[index].Friend;
                incoming.RemoveAt(index);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Newest first. Two that arrived at the same moment fall back to the
        /// order the rest of the panel uses.
        /// </summary>
        /// <remarks>
        /// A tie is not a corner case here: a server that stamps requests by
        /// the second, or a first load that gives them all the same time, hands
        /// over plenty of them. Without the fallback those rows would shuffle
        /// between refreshes.
        /// </remarks>
        private static int CompareByArrival(FriendRequest left, FriendRequest right)
        {
            var byArrival = right.RequestedAt.CompareTo(left.RequestedAt);
            return byArrival != 0
                ? byArrival
                : FriendNameComparer.Instance.Compare(
                    left.Friend.Nickname, right.Friend.Nickname);
        }
    }
}
