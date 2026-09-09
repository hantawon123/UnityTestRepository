using Game.Client;
using Game.Client.Home;
using Game.Client.Lobby;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Architecture.Tests
{
    public sealed class LobbyShortcutGuideViewTests
    {
        [Test]
        public void Create_PlacesTheRowOnTheBottomRight()
        {
            var canvas = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            try
            {
                var view = LobbyShortcutGuideView.Create(canvas.transform);
                var rect = view.GetComponent<RectTransform>();

                Assert.That(view.name, Is.EqualTo(LobbyShortcutGuideView.RootName));
                Assert.That(rect.anchorMin, Is.EqualTo(new Vector2(1f, 0f)));
                Assert.That(rect.anchorMax, Is.EqualTo(new Vector2(1f, 0f)));
                Assert.That(rect.pivot, Is.EqualTo(new Vector2(1f, 0f)));
                Assert.That(
                    rect.anchoredPosition,
                    Is.EqualTo(new Vector2(
                        -LobbyShortcutGuideView.MarginRight,
                        LobbyShortcutGuideView.MarginBottom)));
                Assert.That(
                    LobbyShortcutGuideView.MarginRight,
                    Is.EqualTo(KeySettingGuideView.MarginRight));
                Assert.That(LobbyShortcutGuideView.MarginBottom, Is.EqualTo(34f));
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void Create_BuildsThreeKeyChipsWithSpecifiedType()
        {
            var canvas = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            try
            {
                var view = LobbyShortcutGuideView.Create(canvas.transform);

                for (var index = 0; index < LobbyShortcutGuideView.KeyLabels.Length; index++)
                {
                    var key = view.transform.Find($"Item{index}/Key") as RectTransform;
                    var keySize = key.GetComponent<LayoutElement>();
                    var keyImage = key.GetComponent<Image>();
                    var keyLabel = key.Find("Label").GetComponent<TMP_Text>();
                    var action = view.transform.Find($"Item{index}/Action").GetComponent<TMP_Text>();

                    Assert.That(keySize.preferredWidth, Is.EqualTo(LobbyShortcutGuideView.KeyBoxSize));
                    Assert.That(keySize.preferredHeight, Is.EqualTo(LobbyShortcutGuideView.KeyBoxSize));
                    Assert.That(keyImage.color, Is.EqualTo(LobbyShortcutGuideView.KeyBoxColor));
                    Assert.That(keyImage.sprite, Is.EqualTo(HomeUiFonts.Rounded(LobbyShortcutGuideView.KeyBoxRadius)));
                    Assert.That(keyLabel.text, Is.EqualTo(LobbyShortcutGuideView.KeyLabels[index]));
                    Assert.That(keyLabel.fontSize, Is.EqualTo(LobbyShortcutGuideView.KeyFontSize));
                    Assert.That(keyLabel.font, Is.EqualTo(HomeUiFonts.ApplyRegular()));
                    Assert.That(action.text, Is.EqualTo(LobbyShortcutGuideView.Actions[index]));
                    Assert.That(action.fontSize, Is.EqualTo(LobbyShortcutGuideView.ActionFontSize));
                    Assert.That(action.font, Is.EqualTo(HomeUiFonts.ApplyMedium()));
                }
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void LobbyHud_AttachesTheShortcutGuide()
        {
            var canvas = new GameObject("LobbyHud", typeof(RectTransform), typeof(Canvas));
            try
            {
                var hud = canvas.AddComponent<LobbyHudView>();
                var guide = hud.EnsureShortcutGuide();

                Assert.That(guide, Is.Not.Null);
                Assert.That(
                    canvas.transform.Find(LobbyShortcutGuideView.RootName),
                    Is.SameAs(guide.transform));
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }
    }
}
