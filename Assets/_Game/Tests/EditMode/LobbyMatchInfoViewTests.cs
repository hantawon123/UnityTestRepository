using Game.Client.Home;
using Game.Client.Lobby;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Architecture.Tests
{
    public sealed class LobbyMatchInfoViewTests
    {
        [Test]
        public void Create_PlacesTheCardInTheTopRight()
        {
            var canvas = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            try
            {
                var view = LobbyMatchInfoView.Create(canvas.transform);
                var rect = view.GetComponent<RectTransform>();

                Assert.That(view.name, Is.EqualTo(LobbyMatchInfoView.RootName));
                Assert.That(rect.anchorMin, Is.EqualTo(new Vector2(1f, 1f)));
                Assert.That(rect.anchorMax, Is.EqualTo(new Vector2(1f, 1f)));
                Assert.That(rect.pivot, Is.EqualTo(new Vector2(1f, 1f)));
                Assert.That(rect.anchoredPosition, Is.EqualTo(
                    new Vector2(-LobbyMatchInfoView.MarginRight, -LobbyMatchInfoView.MarginTop)));
                Assert.That(rect.sizeDelta.x, Is.EqualTo(LobbyMatchInfoView.Width));
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void Create_UsesSpecifiedTypeAndMapPreviewSize()
        {
            var canvas = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            try
            {
                var view = LobbyMatchInfoView.Create(canvas.transform);
                var caption = view.transform.Find("CategoryRow/CategoryCaption")
                    .GetComponent<TMP_Text>();
                var category = view.transform.Find("CategoryRow/CategoryValue")
                    .GetComponent<TMP_Text>();
                var mapName = view.transform.Find("MapRow/MapName").GetComponent<TMP_Text>();
                var preview = view.transform.Find("MapRow/MapPreview") as RectTransform;

                Assert.That(caption.text, Is.EqualTo(LobbyMatchInfoView.CategoryCaption));
                Assert.That(caption.fontSize, Is.EqualTo(LobbyMatchInfoView.FontSize));
                Assert.That(caption.font, Is.EqualTo(HomeUiFonts.ApplyRegular()));
                Assert.That(category.fontSize, Is.EqualTo(LobbyMatchInfoView.FontSize));
                Assert.That(category.font, Is.EqualTo(HomeUiFonts.Apply()));
                Assert.That(mapName.fontSize, Is.EqualTo(LobbyMatchInfoView.FontSize));
                Assert.That(mapName.font, Is.EqualTo(HomeUiFonts.ApplyRegular()));
                Assert.That(preview.sizeDelta, Is.EqualTo(LobbyMatchInfoView.MapPreviewSize));
                Assert.That(
                    LobbyMatchInfoView.MapPreviewSize,
                    Is.EqualTo(PlaySettingsStyle.Layout.MapPreviewSize * 0.5f));
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void SetInfo_ReplacesCategoryAndMapLabels()
        {
            var canvas = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            try
            {
                var view = LobbyMatchInfoView.Create(canvas.transform);
                view.SetInfo("과일", "playground");

                Assert.That(view.CategoryLabel, Is.EqualTo("과일"));
                Assert.That(view.MapLabel, Is.EqualTo("playground"));
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void LobbyHud_AttachesTheCard()
        {
            var canvas = new GameObject("LobbyHud", typeof(RectTransform), typeof(Canvas));
            try
            {
                var hud = canvas.AddComponent<LobbyHudView>();
                var info = hud.EnsureMatchInfo();

                Assert.That(info, Is.Not.Null);
                Assert.That(
                    canvas.transform.Find(LobbyMatchInfoView.RootName),
                    Is.SameAs(info.transform));
                Assert.That(info.GetComponent<Image>(), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }
    }
}
