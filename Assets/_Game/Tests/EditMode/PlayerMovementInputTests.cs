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
                if (Cursor.lockState != CursorLockMode.Locked)
                {
                    Assert.Ignore("This Editor session cannot capture the cursor (for example, -nographics).");
                }
                if (UnityEngine.EventSystems.EventSystem.current != null)
                {
                    Assert.Ignore("This test requires a scene without an EventSystem.");
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
