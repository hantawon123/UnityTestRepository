using Game.Bootstrap;
using NUnit.Framework;
using UnityEditor;

namespace Game.Architecture.Tests
{
    public sealed class EditorGameViewCursorTests
    {
        [Test]
        public void InstalledEditor_ExposesNativeGameViewCapturePermission()
        {
            // The bridge uses a non-public Editor API. Fail explicitly after an
            // incompatible Unity upgrade instead of silently losing ESC capture.
            Assert.That(EditorGameViewCursor.GameViewType, Is.Not.Null);
            Assert.That(typeof(EditorWindow).IsAssignableFrom(EditorGameViewCursor.GameViewType), Is.True);
            Assert.That(EditorGameViewCursor.AllowCursorLockAndHide, Is.Not.Null);
            Assert.That(EditorGameViewCursor.AllowCursorLockAndHide.ReturnType, Is.EqualTo(typeof(void)));
        }
    }
}
