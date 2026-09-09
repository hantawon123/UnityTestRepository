using System;
using System.Collections.Generic;

namespace Game.Core.Home
{
    /// <summary>
    /// One invitation to a room, as the toast shows it.
    /// </summary>
    public readonly struct RoomInvite
    {
        public RoomInvite(string id, string fromPlayerId, string fromNickname, string roomCode)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Invite id is required.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(fromPlayerId))
            {
                throw new ArgumentException("Sender id is required.", nameof(fromPlayerId));
            }

            if (string.IsNullOrWhiteSpace(roomCode))
            {
                throw new ArgumentException("Room code is required.", nameof(roomCode));
            }

            Id = id;
            FromPlayerId = fromPlayerId;
            FromNickname = string.IsNullOrWhiteSpace(fromNickname) ? "누군가" : fromNickname.Trim();
            RoomCode = roomCode.Trim();
        }

        /// <summary>Names this invite to the view, which hands it back on a press.</summary>
        public string Id { get; }

        public string FromPlayerId { get; }

        /// <summary>The name on the card. Never empty, so the line always reads.</summary>
        public string FromNickname { get; }

        public string RoomCode { get; }
    }

    /// <summary>
    /// The room invitations waiting for an answer, and which of them are on screen.
    /// </summary>
    /// <remarks>
    /// Pure, so the stacking rules are tested without a canvas. The screen shows
    /// at most <see cref="VisibleLimit"/> cards; anything past that waits its
    /// turn rather than pushing an older, unanswered invite off the screen. An
    /// invite nobody saw is one nobody could answer. Cards leave only when the
    /// player answers them, because an invite is a question, not a notice.
    /// <para>
    /// Two invites from the same person to the same room are two cards. The
    /// server sent two, and folding them would hide that the friend asked
    /// twice, which is itself the message.
    /// </para>
    /// </remarks>
    public sealed class RoomInviteInbox
    {
        public const int VisibleLimit = 3;

        private readonly List<RoomInvite> pending = new List<RoomInvite>();
        private readonly List<RoomInvite> visible = new List<RoomInvite>(VisibleLimit);
        private int nextId;

        /// <summary>Raised after every change to <see cref="Visible"/>.</summary>
        public event Action Changed;

        /// <summary>
        /// The cards on screen, oldest first, never more than <see cref="VisibleLimit"/>.
        /// </summary>
        public IReadOnlyList<RoomInvite> Visible => visible;

        /// <summary>Everything waiting, on screen or not.</summary>
        public int PendingCount => pending.Count;

        public RoomInvite Receive(string fromPlayerId, string fromNickname, string roomCode)
        {
            var invite = new RoomInvite(
                (++nextId).ToString(), fromPlayerId, fromNickname, roomCode);
            pending.Add(invite);
            Refresh();
            return invite;
        }

        /// <summary>
        /// Removes the invite the player answered, whichever way, and hands it
        /// back so the caller can act on it. False when it is no longer here,
        /// which is a second press on a card already leaving.
        /// </summary>
        public bool Take(string id, out RoomInvite invite)
        {
            for (var index = 0; index < pending.Count; index++)
            {
                if (pending[index].Id == id)
                {
                    invite = pending[index];
                    pending.RemoveAt(index);
                    Refresh();
                    return true;
                }
            }

            invite = default;
            return false;
        }

        public void Clear()
        {
            if (pending.Count == 0)
            {
                return;
            }

            pending.Clear();
            Refresh();
        }

        private void Refresh()
        {
            visible.Clear();
            for (var index = 0; index < pending.Count && index < VisibleLimit; index++)
            {
                visible.Add(pending[index]);
            }

            Changed?.Invoke();
        }
    }
}
