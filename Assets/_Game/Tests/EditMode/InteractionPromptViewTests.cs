using Game.Bootstrap;
using Game.Client.Home;
using Game.Client.Interactions;
using NUnit.Framework;
using UnityEngine;

namespace Game.Architecture.Tests
{
    public sealed class InteractionPromptViewTests
    {
        [Test]
        public void Carryable_UsesPickupPrompt()
        {
            var itemObject = new GameObject("Item", typeof(Rigidbody));
            try
            {
                var item = itemObject.AddComponent<CarryableItem>();
                Assert.That(item.InteractionPrompt, Is.EqualTo("물건 잡기"));
            }
            finally
            {
                Object.DestroyImmediate(itemObject);
            }
        }

        [Test]
        public void Shredder_UsesDestroyPrompt()
        {
            var shredderObject = new GameObject("Shredder", typeof(BoxCollider));
            try
            {
                var shredder = shredderObject.AddComponent<ShredderInteractable>();
                Assert.That(shredder.InteractionPrompt, Is.EqualTo("파괴하기"));
            }
            finally
            {
                Object.DestroyImmediate(shredderObject);
            }
        }

        [Test]
        public void Prompt_UsesOpaqueKeyBoxAndSemibold18Label()
        {
            InteractionPromptView view = null;
            try
            {
                view = InteractionPromptView.Create();
                var follow = new GameObject("Follow");
                view.Show("F", "물건 잡기", follow.transform);

                Assert.That(view.IsVisible, Is.True);
                Assert.That(view.KeyBox.color, Is.EqualTo(InteractionPromptView.KeyBoxColor));
                Assert.That(view.KeyBox.color.a, Is.EqualTo(0.27f));
                Assert.That(view.KeyLabel.gameObject.activeSelf, Is.True);
                Assert.That(view.KeyIcon.gameObject.activeSelf, Is.False);
                Assert.That(view.KeyLabel.text, Is.EqualTo("F"));
                Assert.That(view.ActionLabel.text, Is.EqualTo("물건 잡기"));
                Assert.That(view.ActionLabel.fontSize, Is.EqualTo(InteractionPromptView.LabelFontSize));
                Assert.That(view.ActionLabel.fontSize, Is.EqualTo(18f));
                Assert.That(view.ActionLabel.font, Is.EqualTo(HomeUiFonts.Apply()));
                Assert.That(view.ActionLabel.font.name, Does.Contain("Paperlogy").IgnoreCase);
                Assert.That(view.ActionLabel.font.name, Does.Contain("SemiBold").IgnoreCase);

                view.Show("F", "파괴하기", follow.transform);
                Assert.That(view.ActionLabel.text, Is.EqualTo("파괴하기"));

                var clickIcon = InteractionPromptView.LoadLeftClickIcon();
                view.Show(string.Empty, ItemPlacementController.PlaceActionLabel, follow.transform, clickIcon);
                Assert.That(view.KeyLabel.gameObject.activeSelf, Is.False);
                Assert.That(view.KeyIcon.gameObject.activeSelf, Is.True);
                Assert.That(view.KeyIcon.sprite, Is.EqualTo(clickIcon));
                Assert.That(view.KeyIcon.sprite.name, Does.Contain("left_click").IgnoreCase);
                Assert.That(view.ActionLabel.text, Is.EqualTo("배치"));
                Assert.That(view.ActionLabel.fontSize, Is.EqualTo(18f));
                Assert.That(view.KeyBox.color.a, Is.EqualTo(0.27f));

                Object.DestroyImmediate(follow);
            }
            finally
            {
                if (view != null)
                {
                    Object.DestroyImmediate(view.gameObject);
                }
            }
        }
    }
}
