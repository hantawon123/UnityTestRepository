using Game.Client.Match;
using NUnit.Framework;
using UnityEngine;

namespace Game.Architecture.Tests
{
    public sealed class HighlightHudViewTests
    {
        [Test]
        public void Show_PlacesTitleSubtitleAndBars()
        {
            var canvas = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            try
            {
                var view = HighlightHudView.Create(canvas.transform);
                view.Show("FIRST BLOOD : 민수", new[] { 0.5f, 0f, 0f });

                var title = view.transform.Find("Header/Title")?.GetComponent<TMPro.TMP_Text>();
                Assert.That(title, Is.Not.Null);
                Assert.That(title.text, Is.EqualTo(HighlightHudView.TitleText));
                Assert.That(title.fontSize, Is.EqualTo(HighlightHudView.TitleFontSize));
                Assert.That(title.color, Is.EqualTo(Color.white));
                Assert.That(
                    view.transform.Find("Header").GetComponent<RectTransform>().anchoredPosition.y,
                    Is.EqualTo(-HighlightHudView.TopPadding));

                var subtitle = view.transform.Find("Header/Subtitle")?.GetComponent<TMPro.TMP_Text>();
                Assert.That(subtitle, Is.Not.Null);
                Assert.That(subtitle.text, Is.EqualTo("FIRST BLOOD : 민수"));
                Assert.That(subtitle.fontSize, Is.EqualTo(HighlightHudView.SubtitleFontSize));
                Assert.That(subtitle.color, Is.EqualTo(Color.white));

                var firstFill = view.transform.Find("Header/Bars/Bar0/Fill") as RectTransform;
                Assert.That(firstFill, Is.Not.Null);
                Assert.That(firstFill.anchorMax.x, Is.EqualTo(0.5f).Within(0.001f));
                Assert.That(
                    firstFill.GetComponent<UnityEngine.UI.Image>().color,
                    Is.EqualTo(HighlightHudView.BarFillColor));
                Assert.That(
                    view.transform.Find("Header/Bars/Bar1/Fill").gameObject.activeSelf,
                    Is.False);
                Assert.That(view.transform.Find("Header/Bars/Bar0").gameObject.activeSelf, Is.True);
                Assert.That(view.transform.Find("Header/Bars/Bar1").gameObject.activeSelf, Is.True);
                Assert.That(view.transform.Find("Header/Bars/Bar2").gameObject.activeSelf, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void WriteFills_FillsCompletedCurrentAndUpcomingBars()
        {
            Assert.That(
                HighlightHudView.FillsFor(new[] { 10d, 8d, 12d }, 0, 5d),
                Is.EqualTo(new[] { 0.5f, 0f, 0f }).Within(0.001f));
            Assert.That(
                HighlightHudView.FillsFor(new[] { 10d, 8d, 12d }, 1, 4d),
                Is.EqualTo(new[] { 1f, 0.5f, 0f }).Within(0.001f));
            Assert.That(
                HighlightHudView.FillsFor(new[] { 10d, 8d }, 1, 8d),
                Is.EqualTo(new[] { 1f, 1f }).Within(0.001f));
            Assert.That(
                HighlightHudView.FillsFor(new[] { 10d }, 0, 5d),
                Is.EqualTo(new[] { 0.5f }).Within(0.001f));
            Assert.That(HighlightHudView.VisibleBarCount(1), Is.EqualTo(1));
            Assert.That(HighlightHudView.VisibleBarCount(2), Is.EqualTo(2));
            Assert.That(HighlightHudView.VisibleBarCount(3), Is.EqualTo(3));
            Assert.That(HighlightHudView.VisibleBarCount(0), Is.EqualTo(0));
            Assert.That(
                HighlightHudView.FillsFor(new[] { 10d, 8d, 12d }, -1, 3d),
                Is.EqualTo(new[] { 0f, 0f, 0f }));
        }

        [Test]
        public void Show_HidesBarsBeyondTheClipCount()
        {
            var canvas = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            try
            {
                var view = HighlightHudView.Create(canvas.transform);
                view.Show("FIRST BLOOD : 민수", new[] { 0.5f });
                Assert.That(view.transform.Find("Header/Bars/Bar0").gameObject.activeSelf, Is.True);
                Assert.That(view.transform.Find("Header/Bars/Bar1").gameObject.activeSelf, Is.False);
                Assert.That(view.transform.Find("Header/Bars/Bar2").gameObject.activeSelf, Is.False);
                Assert.That(
                    (view.transform.Find("Header/Bars") as RectTransform).sizeDelta.x,
                    Is.EqualTo(HighlightHudView.BarWidth));

                view.Show("HOT ITEM : 민수", new[] { 1f, 0.25f });
                Assert.That(view.transform.Find("Header/Bars/Bar0").gameObject.activeSelf, Is.True);
                Assert.That(view.transform.Find("Header/Bars/Bar1").gameObject.activeSelf, Is.True);
                Assert.That(view.transform.Find("Header/Bars/Bar2").gameObject.activeSelf, Is.False);
                Assert.That(
                    (view.transform.Find("Header/Bars") as RectTransform).sizeDelta.x,
                    Is.EqualTo((HighlightHudView.BarWidth * 2f) + HighlightHudView.BarGap));
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void Hide_HidesHeader()
        {
            var canvas = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            try
            {
                var view = HighlightHudView.Create(canvas.transform);
                view.Show("FIRST BLOOD : 민수", new[] { 1f, 0.2f, 0f });
                view.Hide();
                Assert.That(view.transform.Find("Header").gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }
    }
}
