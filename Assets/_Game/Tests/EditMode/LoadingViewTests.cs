using Game.Client.Common;
using Game.Client.Home;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Architecture.Tests
{
    public sealed class LoadingViewTests
    {
        [Test]
        public void FitGraphicSize_KeepsWidthWhenTheLabelFits()
        {
            var size = LoadingView.FitGraphicSize(1920f, 1080f, 1920f / 861f);
            Assert.That(size.x, Is.EqualTo(1920f).Within(0.01f));
            Assert.That(size.y, Is.EqualTo(861f).Within(0.01f));
        }

        [Test]
        public void FitGraphicSize_ShrinksSoTheLabelStaysOnScreen()
        {
            var size = LoadingView.FitGraphicSize(1920f, 800f, 1920f / 861f);
            Assert.That(size.y, Is.EqualTo(800f - LoadingView.LabelGap - LoadingView.LabelSize.y).Within(0.01f));
            Assert.That(size.x, Is.LessThan(1920f));
        }

        [Test]
        public void Show_FitsBgLoadingToWidthAndWritesLoadingUnderIt()
        {
            var canvas = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            try
            {
                var view = LoadingView.Create(canvas.transform);
                view.Show();

                Assert.That(view.gameObject.activeSelf, Is.True);
                Assert.That(view.GetComponent<Canvas>().sortingOrder, Is.EqualTo(LoadingView.SortingOrder));

                var background = view.transform.Find("Background") as RectTransform;
                Assert.That(background, Is.Not.Null);
                Assert.That(background.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(background.anchorMax, Is.EqualTo(Vector2.one));
                Assert.That(background.GetComponent<Image>().raycastTarget, Is.True);

                var graphic = view.transform.Find("Content/Graphic") as RectTransform;
                Assert.That(graphic, Is.Not.Null);
                Assert.That(graphic.GetComponent<AspectRatioFitter>(), Is.Null);
                Assert.That(view.transform.Find("Content/Spotlight"), Is.Null);
                Assert.That(view.transform.Find("Content/Graphic/Label"), Is.Null);

                var expected = LoadingView.FitGraphicSize(
                    HomeStyle.ReferenceResolution.x,
                    HomeStyle.ReferenceResolution.y,
                    LoadingView.AspectOf(graphic.GetComponent<Image>().sprite));
                Assert.That(graphic.sizeDelta, Is.EqualTo(expected));

                var label = view.transform.Find("Content/Label")?.GetComponent<TMPro.TMP_Text>();
                Assert.That(label, Is.Not.Null);
                Assert.That(label.text, Is.EqualTo("Loading..."));
                Assert.That(label.fontSize, Is.EqualTo(26f));
                Assert.That(label.color, Is.EqualTo(Color.white));
                Assert.That(label.font, Is.EqualTo(HomeUiFonts.ApplyMedium()));
                Assert.That(label.rectTransform.pivot, Is.EqualTo(new Vector2(0.5f, 1f)));
                Assert.That(
                    label.rectTransform.anchoredPosition.y,
                    Is.EqualTo((-expected.y + LoadingView.LabelGap + LoadingView.LabelSize.y) * 0.5f - LoadingView.LabelGap)
                        .Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void Hide_TurnsTheCoverOff()
        {
            var canvas = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            try
            {
                var view = LoadingView.Create(canvas.transform);
                view.Show();
                view.Hide();

                Assert.That(view.transform.Find("Background").gameObject.activeSelf, Is.False);
                Assert.That(view.transform.Find("Content").gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }
    }
}
