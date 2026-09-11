using Game.Client.Players;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public sealed class PlayerNameplateViewTests
    {
        [Test]
        public void RefreshPlacement_SitsJustAboveMeshTop()
        {
            var player = new GameObject("Player");
            try
            {
                var visual = new GameObject("Visual");
                visual.transform.SetParent(player.transform, false);

                var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                body.transform.SetParent(visual.transform, false);
                body.transform.localPosition = new Vector3(0f, 0.5f, 0f);

                var view = PlayerNameplateView.Attach(player.transform);
                view.RefreshPlacement();

                Assert.That(
                    view.transform.position.y,
                    Is.EqualTo(1f + PlayerNameplateView.HeadClearance).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void RefreshPlacement_FollowsVisualScale()
        {
            var player = new GameObject("Player");
            try
            {
                var visual = new GameObject("Visual");
                visual.transform.SetParent(player.transform, false);
                visual.transform.localScale = Vector3.one * 2f;

                var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                body.transform.SetParent(visual.transform, false);
                body.transform.localPosition = new Vector3(0f, 0.5f, 0f);

                var view = PlayerNameplateView.Attach(player.transform);
                view.RefreshPlacement();

                Assert.That(
                    view.transform.position.y,
                    Is.EqualTo(2f + PlayerNameplateView.HeadClearance).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void RefreshPlacement_UsesCharacterControllerWhenMeshIsMissing()
        {
            var player = new GameObject("Player");
            try
            {
                var controller = player.AddComponent<CharacterController>();
                controller.height = 1.2f;
                controller.center = new Vector3(0f, 0.6f, 0f);

                var view = PlayerNameplateView.Attach(player.transform);
                view.RefreshPlacement();

                Assert.That(
                    view.transform.position.y,
                    Is.EqualTo(controller.bounds.max.y + PlayerNameplateView.HeadClearance)
                        .Within(0.05f));
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }
    }
}
