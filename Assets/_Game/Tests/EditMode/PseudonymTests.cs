using System.Collections.Generic;
using Game.Core.Settings;
using NUnit.Framework;

namespace Game.Architecture.Tests
{
    public sealed class PseudonymTests
    {
        [Test]
        public void ASeed_AlwaysGivesTheSameName()
        {
            Assert.That(Pseudonym.From(12345), Is.EqualTo(Pseudonym.From(12345)));
        }

        /// <summary>
        /// A name that could pass for a real one would leave the room unable to
        /// tell, and somebody would address a person by a name not theirs.
        /// </summary>
        [Test]
        public void EveryName_SaysThatItIsOne()
        {
            for (var seed = -1000; seed < 1000; seed++)
            {
                Assert.That(Pseudonym.IsOne(Pseudonym.From(seed)), Is.True, $"seed {seed}");
            }
        }

        /// <summary>
        /// A hash is as likely to be negative as not, and a name that threw for
        /// half of them would be a crash in the one place privacy was asked for.
        /// </summary>
        [Test]
        public void ANegativeSeed_IsAName_LikeAnyOther()
        {
            Assert.That(Pseudonym.From(int.MinValue), Is.Not.Empty);
            Assert.That(Pseudonym.From(-1), Is.Not.Empty);
        }

        [Test]
        public void TheNames_SpreadAcrossTheWholeList()
        {
            var seen = new HashSet<string>();
            for (var seed = 0; seed < 2000; seed++)
            {
                seen.Add(Pseudonym.From(seed));
            }

            Assert.That(
                seen.Count,
                Is.GreaterThan(Pseudonym.NounCount * 50),
                "A generator that keeps landing on the same few names is one people notice.");
        }

        [Test]
        public void ARealNickname_IsNotMistakenForOne()
        {
            Assert.That(Pseudonym.IsOne("길드마스터"), Is.False);
            Assert.That(Pseudonym.IsOne(""), Is.False);
            Assert.That(Pseudonym.IsOne(null), Is.False);
        }
    }
}
