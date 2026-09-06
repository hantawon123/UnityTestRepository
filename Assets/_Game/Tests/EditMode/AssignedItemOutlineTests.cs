using Game.Client.Interactions;
using NUnit.Framework;
using UnityEngine;

namespace Game.Architecture.Tests
{
    public sealed class AssignedItemOutlineTests
    {
        [Test]
        public void Carryable_ShowsOutlineOnlyWhileLocallyAssigned()
        {
            var itemObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            itemObject.AddComponent<Rigidbody>();

            try
            {
                var item = itemObject.AddComponent<CarryableItem>();
                item.SetAssignedHighlight(true);

                var outline = item.GetComponent<AssignedItemOutline>();
                Assert.That(outline, Is.Not.Null);
                Assert.That(outline.IsVisible, Is.True);

                item.SetAssignedHighlight(false);
                Assert.That(outline.IsVisible, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(itemObject);
            }
        }

        [Test]
        public void Carryable_ShowsOrangeFocusOutlineWhileAimed()
        {
            var itemObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            itemObject.AddComponent<Rigidbody>();

            try
            {
                var item = itemObject.AddComponent<CarryableItem>();
                item.SetAimed(true, 1f);

                var outline = item.GetComponent<InteractableFocusOutline>();
                Assert.That(outline, Is.Not.Null);
                Assert.That(outline.IsVisible, Is.True);
                Assert.That(outline.Color, Is.EqualTo(new Color(1f, 154f / 255f, 106f / 255f, 1f)));
                Assert.That(outline.PixelWidth, Is.EqualTo(2f));

                item.SetAimed(false, 1f);
                Assert.That(outline.IsVisible, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(itemObject);
            }
        }

        [Test]
        public void Carryable_HidesAssignedOutlineWhileAimed()
        {
            var itemObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            itemObject.AddComponent<Rigidbody>();

            try
            {
                var item = itemObject.AddComponent<CarryableItem>();
                item.SetAssignedHighlight(true);
                item.SetAimed(true, 1f);

                Assert.That(item.GetComponent<AssignedItemOutline>().IsVisible, Is.False);
                Assert.That(item.GetComponent<InteractableFocusOutline>().IsVisible, Is.True);

                item.SetAimed(false, 1f);
                Assert.That(item.GetComponent<AssignedItemOutline>().IsVisible, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(itemObject);
            }
        }
    }
}
