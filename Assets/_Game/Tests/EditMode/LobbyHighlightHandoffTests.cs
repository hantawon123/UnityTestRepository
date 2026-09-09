using System.Reflection;
using Game.Bootstrap;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class LobbyHighlightHandoffTests
    {
        [Test]
        public void ShowingLobby_HidesOutgoingGeometryAndCollisions_AndPreservesOriginalStates()
        {
            var root = new GameObject("Inactive lobby test");
            root.SetActive(false);
            var building = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var hidden = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                var scope = root.AddComponent<LobbyLifetimeScope>();
                var renderer = building.GetComponent<Renderer>();
                var collider = building.GetComponent<Collider>();
                var hiddenRenderer = hidden.GetComponent<Renderer>();
                var disabledCollider = hidden.GetComponent<Collider>();
                hiddenRenderer.forceRenderingOff = true;
                disabledCollider.enabled = false;
                Set(scope, "outgoingRenderers", new[] { renderer, hiddenRenderer });
                Set(scope, "outgoingRendererStates", new[] { false, true });
                Set(scope, "outgoingColliders", new[] { collider, disabledCollider });
                Set(scope, "outgoingColliderStates", new[] { true, false });

                Show(scope, true);
                Assert.IsTrue(renderer.forceRenderingOff);
                Assert.IsFalse(collider.enabled);
                Show(scope, false);
                Assert.IsFalse(renderer.forceRenderingOff);
                Assert.IsTrue(collider.enabled);
                Assert.IsTrue(hiddenRenderer.forceRenderingOff);
                Assert.IsFalse(disabledCollider.enabled);

                Object.DestroyImmediate(building);
                Assert.DoesNotThrow(() => Show(scope, true), "Outgoing scene may unload before phase reset.");
            }
            finally
            {
                Object.DestroyImmediate(building);
                Object.DestroyImmediate(hidden);
                Object.DestroyImmediate(root);
            }
        }

        private static void Set(LobbyLifetimeScope scope, string name, object value) =>
            typeof(LobbyLifetimeScope).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(scope, value);

        private static void Show(LobbyLifetimeScope scope, bool visible) =>
            typeof(LobbyLifetimeScope).GetMethod("SetStagingVisible", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(scope, new object[] { visible });
    }
}
