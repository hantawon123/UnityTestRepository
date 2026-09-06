using Game.Client.Match;
using Game.Core.Match;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Architecture.Tests
{
    public sealed class DestroyedItemsHudViewTests
    {
        [Test]
        public void Show_BuildsPlayerCountSlotsWithQuestionMarks()
        {
            var canvas = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            try
            {
                var view = DestroyedItemsHudView.Create(canvas.transform);
                view.Show(6, System.Array.Empty<PlayerItemStatusSnapshot>());

                var panel = view.transform.Find("Panel");
                Assert.That(panel, Is.Not.Null);
                Assert.That(panel.gameObject.activeSelf, Is.True);
                Assert.That(DestroyedItemsHudView.SlotSize, Is.EqualTo(100f));
                Assert.That(DestroyedItemsHudView.PreviewTextureSize, Is.EqualTo(256));
                Assert.That(DestroyedItemsHudView.QuestionFontSize, Is.EqualTo(30f));

                for (var index = 0; index < 6; index++)
                {
                    var slot = panel.Find($"Slot{index}");
                    Assert.That(slot, Is.Not.Null);
                    var circle = slot.GetComponent<Image>();
                    Assert.That(circle.color, Is.EqualTo(DestroyedItemsHudView.SlotColor));
                    var question = slot.Find("Question")?.GetComponent<TMP_Text>();
                    Assert.That(question, Is.Not.Null);
                    Assert.That(question.text, Is.EqualTo(DestroyedItemsHudView.QuestionMark));
                    Assert.That(question.fontSize, Is.EqualTo(DestroyedItemsHudView.QuestionFontSize));
                    Assert.That(question.gameObject.activeSelf, Is.True);
                    Assert.That(slot.GetComponent<LayoutElement>().preferredWidth, Is.EqualTo(100f));
                    Assert.That(slot.GetComponent<LayoutElement>().preferredHeight, Is.EqualTo(100f));
                }
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void Show_KeepsUnknownSlotsWhenSomeItemsAreDestroyed()
        {
            var canvas = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            try
            {
                var view = DestroyedItemsHudView.Create(canvas.transform);
                view.Show(
                    3,
                    new[]
                    {
                        new PlayerItemStatusSnapshot("Soda_01", true),
                        new PlayerItemStatusSnapshot("Burger_01", false),
                        new PlayerItemStatusSnapshot("Pineapple_01", false),
                    });

                var intact = view.transform.Find("Panel/Slot1/Question");
                Assert.That(intact.gameObject.activeSelf, Is.True);
                Assert.That(
                    view.transform.Find("Panel/Slot2/Question").gameObject.activeSelf,
                    Is.True);
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }
    }
}
