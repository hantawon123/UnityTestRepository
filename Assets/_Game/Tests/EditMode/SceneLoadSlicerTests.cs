using Game.Client.Common;
using NUnit.Framework;

namespace Game.Architecture.Tests
{
    public sealed class SceneLoadSlicerTests
    {
        [Test]
        public void IsReadyToActivate_WaitsUntilTheUnityGate()
        {
            Assert.That(SceneLoadSlicer.IsReadyToActivate(0.89f), Is.False);
            Assert.That(SceneLoadSlicer.IsReadyToActivate(SceneLoadSlicer.ActivationGate), Is.True);
            Assert.That(SceneLoadSlicer.IsReadyToActivate(1f), Is.True);
        }
    }
}
