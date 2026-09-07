using Game.Client;
using Game.Client.Lobby;
using Game.Client.Match;
using Game.Core.Match;
using NUnit.Framework;
using UnityEngine;

namespace Game.Architecture.Tests
{
    public sealed class KeySettingGuideViewTests
    {
        [Test]
        public void Create_PlacesSharedRowsOnTheRightEdge()
        {
            var canvas = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            try
            {
                var guide = KeySettingGuideView.Create(canvas.transform).GetComponent<RectTransform>();

                Assert.That(guide.name, Is.EqualTo(KeySettingGuideView.RootName));
                Assert.That(guide.anchorMin.x, Is.EqualTo(1f));
                Assert.That(guide.pivot, Is.EqualTo(new Vector2(1f, 0.5f)));
                Assert.That(guide.anchoredPosition, Is.EqualTo(new Vector2(-KeySettingGuideView.MarginRight, 0f)));
                Assert.That(guide.sizeDelta, Is.EqualTo(KeySettingGuideView.PanelSize));
                Assert.That(
                    guide.Find("Row0/Action").GetComponent<TMPro.TMP_Text>().text,
                    Is.EqualTo(KeySettingGuideView.Actions[0]));
                Assert.That(
                    guide.Find("Row5/Key/Label").GetComponent<TMPro.TMP_Text>().text,
                    Is.EqualTo(KeySettingGuideView.Labels[5]));
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void LobbyAndPlayground_AttachTheSameGuide()
        {
            var lobby = new GameObject("LobbyHud", typeof(RectTransform), typeof(Canvas));
            var playground = new GameObject("PlaygroundHud", typeof(RectTransform), typeof(Canvas));
            try
            {
                lobby.AddComponent<LobbyHudView>();
                lobby.SetActive(false);
                lobby.SetActive(true);
                var playgroundHud = playground.AddComponent<NetworkMatchHudView>();
                playgroundHud.SetPhase(MatchPhase.Hiding, "숨기는사람");

                var lobbyGuide = lobby.transform.Find(KeySettingGuideView.RootName);
                var playgroundGuide = playground.transform.Find(KeySettingGuideView.RootName);
                Assert.That(lobbyGuide, Is.Not.Null);
                Assert.That(playgroundGuide, Is.Not.Null);
                Assert.That(lobbyGuide.GetComponent<KeySettingGuideView>(), Is.Not.Null);
                Assert.That(playgroundGuide.GetComponent<KeySettingGuideView>(), Is.Not.Null);
                Assert.That(
                    ((RectTransform)lobbyGuide).anchoredPosition,
                    Is.EqualTo(((RectTransform)playgroundGuide).anchoredPosition));
                Assert.That(lobbyGuide.gameObject.activeSelf, Is.True);
                Assert.That(playgroundGuide.gameObject.activeSelf, Is.True);

                playgroundHud.SetPhase(MatchPhase.Searching, string.Empty);
                Assert.That(playgroundGuide.gameObject.activeSelf, Is.True);
                playgroundHud.SetPhase(MatchPhase.Highlight, string.Empty);
                Assert.That(playgroundGuide.gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(lobby);
                Object.DestroyImmediate(playground);
            }
        }
    }
}
