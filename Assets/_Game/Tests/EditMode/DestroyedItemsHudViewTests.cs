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
                    var fill = slot.Find(DestroyedItemsHudView.FillName)?.GetComponent<Image>();
                    Assert.That(fill, Is.Not.Null);
                    Assert.That(fill.color, Is.EqualTo(DestroyedItemsHudView.SlotColor));
                    var question = slot.Find($"{DestroyedItemsHudView.FillName}/Question")
                        ?.GetComponent<TMP_Text>();
                    Assert.That(question, Is.Not.Null);
                    Assert.That(question.text, Is.EqualTo(DestroyedItemsHudView.QuestionMark));
                    Assert.That(question.fontSize, Is.EqualTo(DestroyedItemsHudView.QuestionFontSize));
                    Assert.That(question.gameObject.activeSelf, Is.True);
                    Assert.That(slot.GetComponent<LayoutElement>().preferredWidth, Is.EqualTo(100f));
                    Assert.That(slot.GetComponent<LayoutElement>().preferredHeight, Is.EqualTo(100f));
                    Assert.That(
                        slot.Find(DestroyedItemsHudView.OwnBorderName).gameObject.activeSelf,
                        Is.False);
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

                var intact = view.transform.Find(
                    $"Panel/Slot1/{DestroyedItemsHudView.FillName}/Question");
                Assert.That(intact.gameObject.activeSelf, Is.True);
                Assert.That(
                    view.transform.Find(
                        $"Panel/Slot2/{DestroyedItemsHudView.FillName}/Question")
                        .gameObject.activeSelf,
                    Is.True);
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void Show_MarksLocalSlotWithOrangeBorder()
        {
            var canvas = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            try
            {
                var view = DestroyedItemsHudView.Create(canvas.transform);
                view.Show(
                    3,
                    new[]
                    {
                        new PlayerItemStatusSnapshot("Soda_01", false),
                        new PlayerItemStatusSnapshot("Burger_01", true),
                        new PlayerItemStatusSnapshot("Pineapple_01", false),
                    },
                    "Soda_01",
                    new[] { "Burger_01" });

                var ownBorder = view.transform.Find(
                    $"Panel/Slot0/{DestroyedItemsHudView.OwnBorderName}")
                    ?.GetComponent<Image>();
                Assert.That(ownBorder, Is.Not.Null);
                Assert.That(ownBorder.gameObject.activeSelf, Is.True);
                Assert.That(ownBorder.color, Is.EqualTo(DestroyedItemsHudView.OwnBorderColor));
                var ownFill = view.transform.Find(
                    $"Panel/Slot0/{DestroyedItemsHudView.FillName}")
                    ?.GetComponent<Image>();
                var otherFill = view.transform.Find(
                    $"Panel/Slot1/{DestroyedItemsHudView.FillName}")
                    ?.GetComponent<Image>();
                Assert.That(ownFill.color, Is.EqualTo(DestroyedItemsHudView.SlotColor));
                Assert.That(otherFill.color, Is.EqualTo(DestroyedItemsHudView.SlotColor));
                Assert.That(((RectTransform)ownFill.transform).offsetMin, Is.EqualTo(Vector2.zero));
                Assert.That(
                    view.transform.Find($"Panel/Slot1/{DestroyedItemsHudView.OwnBorderName}")
                        .gameObject.activeSelf,
                    Is.False);
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void Show_KeepsDefaultPreviewMaterialWhenLocalItemIsDestroyed()
        {
            var canvas = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            try
            {
                var view = DestroyedItemsHudView.Create(canvas.transform);
                view.Show(
                    2,
                    new[]
                    {
                        new PlayerItemStatusSnapshot("Soda_01", true),
                        new PlayerItemStatusSnapshot("Burger_01", true),
                    },
                    "Soda_01",
                    new[] { "Burger_01", "Soda_01" });

                var ownPreview = view.transform.Find(
                    $"Panel/Slot0/{DestroyedItemsHudView.FillName}/Preview")
                    ?.GetComponent<RawImage>();
                var otherPreview = view.transform.Find(
                    $"Panel/Slot1/{DestroyedItemsHudView.FillName}/Preview")
                    ?.GetComponent<RawImage>();
                Assert.That(ownPreview, Is.Not.Null);
                Assert.That(otherPreview, Is.Not.Null);
                Assert.That(ownPreview.material, Is.Null);
                Assert.That(otherPreview.material, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void NetworkHud_KeepsDestroyedItemsVisibleWhenPlayerStatusIsHidden()
        {
            var canvas = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            try
            {
                var hud = canvas.AddComponent<NetworkMatchHudView>();
                hud.SetPhase(MatchPhase.Hiding, string.Empty);
                hud.SetDestroyedItems(
                    2,
                    new[]
                    {
                        new PlayerItemStatusSnapshot("Soda_01", false),
                        new PlayerItemStatusSnapshot("Burger_01", false),
                    },
                    "Soda_01",
                    System.Array.Empty<string>());
                hud.SetPlayerStatusVisible(false);

                var panel = hud.transform.Find("DestroyedItems/Panel");
                Assert.That(panel, Is.Not.Null);
                Assert.That(panel.gameObject.activeSelf, Is.True);
                Assert.That(
                    panel.Find($"Slot0/{DestroyedItemsHudView.OwnBorderName}")
                        .gameObject.activeSelf,
                    Is.True);
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }

        [TestCase(MatchPhase.Highlight)]
        [TestCase(MatchPhase.Result)]
        public void NetworkHud_HidesDestroyedItemsOnHighlightAndResult(MatchPhase phase)
        {
            var canvas = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            try
            {
                var hud = canvas.AddComponent<NetworkMatchHudView>();
                hud.SetPhase(MatchPhase.Searching, string.Empty);
                hud.SetDestroyedItems(
                    2,
                    new[]
                    {
                        new PlayerItemStatusSnapshot("Soda_01", false),
                        new PlayerItemStatusSnapshot("Burger_01", true),
                    },
                    "Soda_01",
                    new[] { "Burger_01" });

                var panel = hud.transform.Find("DestroyedItems/Panel");
                Assert.That(panel.gameObject.activeSelf, Is.True);

                hud.SetPhase(phase, string.Empty);
                Assert.That(panel.gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }
    }
}
