using Game.Client.Players;
using NUnit.Framework;
using UnityEngine;

namespace Game.Architecture.Tests
{
    public sealed class PlayerMovementInputTests
    {
        [Test]
        public void ShouldIgnoreAttackInput_WhenCursorIsUnlocked()
        {
            var previous = Cursor.lockState;
            try
            {
                Cursor.lockState = CursorLockMode.None;
                Assert.That(PlayerMovement.ShouldIgnoreAttackInput(), Is.True);
            }
            finally
            {
                Cursor.lockState = previous;
            }
        }

        [Test]
        public void ShouldIgnoreAttackInput_WhenCursorIsLockedAndNoEventSystem()
        {
            var previous = Cursor.lockState;
            try
            {
                Cursor.lockState = CursorLockMode.Locked;
                if (UnityEngine.EventSystems.EventSystem.current != null)
                {
                    Assert.Pass("EventSystem is present; UI hover cannot be asserted here.");
                }

                Assert.That(PlayerMovement.ShouldIgnoreAttackInput(), Is.False);
            }
            finally
            {
                Cursor.lockState = previous;
            }
        }
    }
}
