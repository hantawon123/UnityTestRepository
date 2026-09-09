using Game.Client.Character;
using Game.Client.Home;
using Game.Client.Lobby;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Architecture.Tests
{
    public sealed class KickConfirmViewTests
    {
        [Test]
        public void Show_BuildsTheSettingsConfirmPlateWithoutASubtitle()
        {
            var canvas = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            try
            {
                var view = canvas.AddComponent<KickConfirmView>();
                view.Show(KickConfirmView.FormatTitle("게스트닉"));

                var overlay = canvas.transform.Find(KickConfirmView.RootName);
                Assert.That(overlay, Is.Not.Null);
                Assert.That(overlay.gameObject.activeSelf, Is.True);
                Assert.That(overlay.Find("Panel/Subtitle"), Is.Null);

                var panel = overlay.Find("Panel") as RectTransform;
                Assert.That(panel.sizeDelta, Is.EqualTo(CharacterClosetStyle.Modal.PanelSize));

                var title = overlay.Find("Panel/Title").GetComponent<TMP_Text>();
                Assert.That(title.text, Is.EqualTo("게스트닉 님을\n강퇴하시겠습니까?"));
                Assert.That(title.fontSize, Is.EqualTo(CharacterClosetStyle.Modal.TitleFontSize));
                Assert.That(title.font, Is.EqualTo(HomeUiFonts.Apply()));

                var cancel = overlay.Find("Panel/DeclineButton/Label").GetComponent<TMP_Text>();
                var accept = overlay.Find("Panel/AcceptButton/Label").GetComponent<TMP_Text>();
                Assert.That(cancel.text, Is.EqualTo(KickConfirmView.CancelLabel));
                Assert.That(accept.text, Is.EqualTo(KickConfirmView.ConfirmLabel));
                Assert.That(
                    overlay.Find("Panel/DeclineButton").GetComponent<Image>().color,
                    Is.EqualTo(CharacterClosetStyle.Palette.DeclineFill));
                Assert.That(
                    overlay.Find("Panel/AcceptButton").GetComponent<Image>().color,
                    Is.EqualTo(CharacterClosetStyle.Palette.AcceptFill));
                Assert.That(overlay.Find("Panel/CloseButton"), Is.Not.Null);

                view.Hide();
                Assert.That(overlay.gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }
    }
}
