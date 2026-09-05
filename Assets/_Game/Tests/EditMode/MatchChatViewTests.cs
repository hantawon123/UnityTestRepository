using Game.Client.Match;
using Game.Core.Lobby;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Architecture.Tests
{
    public sealed class MatchChatViewTests
    {
        [Test]
        public void SetMessages_ShowsLastFour_NewestAtBottom()
        {
            var canvas = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            try
            {
                var view = MatchChatView.Create(canvas.transform);
                view.SetMessages(new[]
                {
                    new LobbyChatMessage("a", "싸피생1", "하나"),
                    new LobbyChatMessage("b", "싸피생2", "둘"),
                    new LobbyChatMessage("c", "싸피생3", "셋"),
                    new LobbyChatMessage("d", "싸피생4", "넷"),
                    new LobbyChatMessage("e", "금오산냥냥이", "안녕하십니까 여러분")
                });

                Assert.That(
                    view.transform.Find("HistoryPanel/Items/Row0").gameObject.activeSelf,
                    Is.True);
                Assert.That(
                    view.transform.Find("HistoryPanel/Items/Row0/Name").GetComponent<TMP_Text>().text,
                    Is.EqualTo("싸피생2"));
                Assert.That(
                    view.transform.Find("HistoryPanel/Items/Row3/Name").GetComponent<TMP_Text>().text,
                    Is.EqualTo("금오산냥냥이"));
                Assert.That(
                    view.transform.Find("HistoryPanel/Items/Row3/Body").GetComponent<TMP_Text>().text,
                    Is.EqualTo("안녕하십니까 여러분"));
                Assert.That(view.transform.Find("HistoryPanel/Items").childCount, Is.EqualTo(4));
                var history = view.transform.Find("HistoryPanel");
                Assert.That(history.GetComponent<Mask>(), Is.Null);
                Assert.That(history.Find("Background"), Is.Not.Null);
                var body = view.transform.Find("HistoryPanel/Items/Row3/Body").GetComponent<TMP_Text>();
                Assert.That(body.textWrappingMode, Is.EqualTo(TextWrappingModes.Normal));
                Assert.That(body.overflowMode, Is.Not.EqualTo(TextOverflowModes.Ellipsis));
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void SetMessages_AppliesNameAndBodyStyle()
        {
            var canvas = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            try
            {
                var view = MatchChatView.Create(canvas.transform);
                view.SetMessages(new[]
                {
                    new LobbyChatMessage("a", "금오산냥냥이", "안녕하십니까 여러분")
                });

                var name = view.transform.Find("HistoryPanel/Items/Row0/Name").GetComponent<TMP_Text>();
                var body = view.transform.Find("HistoryPanel/Items/Row0/Body").GetComponent<TMP_Text>();
                Assert.That(name.fontSize, Is.EqualTo(MatchChatView.NameFontSize));
                Assert.That(name.color, Is.EqualTo(MatchChatView.NameColor));
                Assert.That(body.fontSize, Is.EqualTo(MatchChatView.BodyFontSize));
                Assert.That(body.color, Is.EqualTo(Color.white));
                Assert.That(name.font, Is.Not.Null);
                Assert.That(name.font.name, Does.Contain("Paperlogy").IgnoreCase);
                Assert.That(body.font.name, Does.Contain("Paperlogy").IgnoreCase);
                var input = view.transform.Find("InputPanel").GetComponent<TMP_InputField>();
                Assert.That(input.fontAsset.name, Does.Contain("Paperlogy").IgnoreCase);
                Assert.That(input.textComponent.overflowMode, Is.EqualTo(TextOverflowModes.Overflow));
                Assert.That(input.textComponent.textWrappingMode, Is.EqualTo(TextWrappingModes.NoWrap));
                Assert.That(
                    view.transform.Find("InputPanel/Placeholder").GetComponent<TMP_Text>().text,
                    Is.EqualTo(MatchChatView.PlaceholderText));
                Assert.That(view.transform.Find("InputPanel/Send"), Is.Not.Null);
                Assert.That(view.GetComponent<Canvas>(), Is.Not.Null);
                Assert.That(view.GetComponent<Canvas>().overrideSorting, Is.True);
                Assert.That(
                    view.transform.Find("InputPanel").GetComponent<Canvas>(),
                    Is.Not.Null);
                Assert.That(
                    (view.transform.Find("InputPanel") as RectTransform).sizeDelta.x,
                    Is.EqualTo(MatchChatView.InputWidth));
                Assert.That(
                    (view.transform.Find("HistoryPanel") as RectTransform).sizeDelta.x,
                    Is.EqualTo(MatchChatView.InputWidth));
                Assert.That(
                    (view.transform.Find("InputPanel/TextViewport") as RectTransform).offsetMin.x,
                    Is.EqualTo(MatchChatView.ContentPadding));
                Assert.That(MatchChatView.HistoryFadeAlpha(0f), Is.EqualTo(0f));
                Assert.That(MatchChatView.HistoryFadeAlpha(1f), Is.EqualTo(1f));
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void SearchingMode_HidesHistoryAndInputUntilActivated()
        {
            var canvas = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            try
            {
                var view = MatchChatView.Create(canvas.transform);
                view.SetMessages(new[]
                {
                    new LobbyChatMessage("a", "싸피생1", "하나")
                });
                view.SetMode(MatchChatHudMode.Searching);

                Assert.That(view.Mode, Is.EqualTo(MatchChatHudMode.Searching));
                Assert.That(view.gameObject.activeSelf, Is.True);
                Assert.That(
                    view.transform.Find("HistoryPanel").gameObject.activeSelf,
                    Is.False);
                Assert.That(
                    view.transform.Find("InputPanel").gameObject.activeSelf,
                    Is.False);
                Assert.That(MatchChatView.ShowsHistory(MatchChatHudMode.Searching), Is.False);
                Assert.That(MatchChatView.ShowsInput(MatchChatHudMode.Searching, false), Is.False);
                Assert.That(MatchChatView.ShowsInput(MatchChatHudMode.Searching, true), Is.True);
                Assert.That(MatchChatView.ShowsHistory(MatchChatHudMode.Full), Is.True);
                Assert.That(MatchChatView.ShowsInput(MatchChatHudMode.Full, false), Is.False);
                Assert.That(MatchChatView.ShowsInput(MatchChatHudMode.Full, true), Is.True);

                view.SetMode(MatchChatHudMode.Full);
                Assert.That(
                    view.transform.Find("HistoryPanel").gameObject.activeSelf,
                    Is.True);
                Assert.That(
                    view.transform.Find("InputPanel").gameObject.activeSelf,
                    Is.False);
                view.ClearInput();
                Assert.That(view.IsActivated, Is.False);
                Assert.That(
                    view.transform.Find("InputPanel").gameObject.activeSelf,
                    Is.False);
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void ShouldOpenOnEnter_IgnoresTheEnterThatClosedChat()
        {
            Assert.That(
                MatchChatView.ShouldOpenOnEnter(false, false, true, 1f, 0.95f),
                Is.False);
            Assert.That(
                MatchChatView.ShouldOpenOnEnter(
                    false,
                    false,
                    true,
                    1f,
                    1f - MatchChatView.OpenCooldownSeconds),
                Is.True);
            Assert.That(
                MatchChatView.ShouldOpenOnEnter(true, false, true, 10f, 0f),
                Is.False);
        }

        [Test]
        public void ChatFont_PrefersBakedStaticRegularWhenPresent()
        {
            var font = MatchChatView.ChatFont();
            Assert.That(font, Is.Not.Null);
            Assert.That(font.name, Does.Contain("Paperlogy").IgnoreCase);
            var baked = Resources.Load<TMP_FontAsset>("Fonts/Paperlogy-4Regular SDF");
            if (baked != null)
            {
                Assert.That(font, Is.SameAs(baked));
                Assert.That(font.atlasPopulationMode, Is.EqualTo(AtlasPopulationMode.Static));
            }
        }

        [Test]
        public void VisibleMessages_KeepsOldestOfWindowFirst()
        {
            var messages = new[]
            {
                new LobbyChatMessage("1", "A", "1"),
                new LobbyChatMessage("2", "B", "2"),
                new LobbyChatMessage("3", "C", "3"),
                new LobbyChatMessage("4", "D", "4"),
                new LobbyChatMessage("5", "E", "5")
            };

            var visible = MatchChatView.VisibleMessages(messages);
            Assert.That(visible.Count, Is.EqualTo(4));
            Assert.That(visible[0].Text, Is.EqualTo("2"));
            Assert.That(visible[3].Text, Is.EqualTo("5"));
        }
    }
}
