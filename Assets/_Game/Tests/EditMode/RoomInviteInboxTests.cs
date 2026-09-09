using System.Linq;
using Game.Core.Home;
using NUnit.Framework;

namespace Game.Architecture.Tests
{
    /// <summary>
    /// How invitations queue up on the home screen: three at a time, oldest
    /// first, gone only when answered.
    /// </summary>
    public sealed class RoomInviteInboxTests
    {
        [Test]
        public void AnInvite_IsShownAsItArrives()
        {
            var inbox = new RoomInviteInbox();

            var invite = inbox.Receive("p1", "친구", "ROOM01");

            Assert.That(inbox.Visible.Select(v => v.Id), Is.EqualTo(new[] { invite.Id }));
            Assert.That(invite.FromNickname, Is.EqualTo("친구"));
            Assert.That(invite.RoomCode, Is.EqualTo("ROOM01"));
        }

        [Test]
        public void AFourthInvite_WaitsRatherThanPushingAnUnansweredOneOff()
        {
            var inbox = new RoomInviteInbox();
            var first = inbox.Receive("p1", "하나", "R1");
            inbox.Receive("p2", "둘", "R2");
            inbox.Receive("p3", "셋", "R3");

            inbox.Receive("p4", "넷", "R4");

            Assert.That(inbox.Visible.Count, Is.EqualTo(RoomInviteInbox.VisibleLimit));
            Assert.That(inbox.Visible[0].Id, Is.EqualTo(first.Id), "the oldest is still first");
            Assert.That(
                inbox.Visible.Select(v => v.FromNickname), Is.EqualTo(new[] { "하나", "둘", "셋" }));
            Assert.That(inbox.PendingCount, Is.EqualTo(4));
        }

        [Test]
        public void AnsweringOne_LetsTheWaitingOneIn()
        {
            var inbox = new RoomInviteInbox();
            inbox.Receive("p1", "하나", "R1");
            var second = inbox.Receive("p2", "둘", "R2");
            inbox.Receive("p3", "셋", "R3");
            inbox.Receive("p4", "넷", "R4");

            Assert.That(inbox.Take(second.Id, out var taken), Is.True);

            Assert.That(taken.RoomCode, Is.EqualTo("R2"));
            Assert.That(
                inbox.Visible.Select(v => v.FromNickname), Is.EqualTo(new[] { "하나", "셋", "넷" }));
        }

        [Test]
        public void TakingTwice_SaysTheSecondPressFoundNothing()
        {
            var inbox = new RoomInviteInbox();
            var invite = inbox.Receive("p1", "친구", "R1");

            Assert.That(inbox.Take(invite.Id, out _), Is.True);
            Assert.That(inbox.Take(invite.Id, out _), Is.False);
            Assert.That(inbox.Visible, Is.Empty);
        }

        [Test]
        public void TheSameFriendTwice_IsTwoCards()
        {
            var inbox = new RoomInviteInbox();

            inbox.Receive("p1", "친구", "R1");
            inbox.Receive("p1", "친구", "R1");

            Assert.That(inbox.Visible.Count, Is.EqualTo(2));
            Assert.That(inbox.Visible[0].Id, Is.Not.EqualTo(inbox.Visible[1].Id));
        }

        [Test]
        public void ANamelessSender_StillReadsAsSomeone()
        {
            var inbox = new RoomInviteInbox();

            var invite = inbox.Receive("p1", "", "R1");

            Assert.That(invite.FromNickname, Is.Not.Empty);
        }

        [Test]
        public void Changed_FiresOnArrivalAndOnAnswer_NotOnAnEmptyClear()
        {
            var inbox = new RoomInviteInbox();
            var fired = 0;
            inbox.Changed += () => fired++;

            var invite = inbox.Receive("p1", "친구", "R1");
            inbox.Take(invite.Id, out _);
            inbox.Clear();

            Assert.That(fired, Is.EqualTo(2));
        }
    }
}
