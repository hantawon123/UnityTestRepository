using Game.Client.Settings;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Architecture.Tests
{
    public sealed class SettingsViewLobbyChromeTests
    {
        [Test]
        public void ConfigureAsLobbyOverlay_HidesFeedbackAndShowsLeaveText()
        {
            var root = new GameObject("Lobby Settings");
            try
            {
                root.SetActive(false);
                var view = root.AddComponent<SettingsView>();
                view.ConfigureAsLobbyOverlay();
                root.SetActive(true);

                Assert.That(Find(root, "FeedbackRow"), Is.Null);
                Assert.That(Find(root, "BackButton"), Is.Null);
                Assert.That(Find(root, "LeaveGameButton"), Is.Null);
                var leave = Find(root, "LeaveGameLabel");
                Assert.That(leave, Is.Not.Null);
                var rect = leave.GetComponent<RectTransform>();
                Assert.That(rect.anchoredPosition, Is.EqualTo(SettingsStyle.Back.Position));
                var label = leave.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
                Assert.That(label.text, Is.EqualTo(SettingsStyle.Buttons.LeaveLabel));
                var close = Find(root, "CloseButton");
                Assert.That(close, Is.Not.Null);
                Assert.That(close.GetComponent<Image>().sprite, Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void HomeLayout_KeepsFeedbackAndOmitsLeaveButton()
        {
            var root = new GameObject("Home Settings");
            try
            {
                root.AddComponent<SettingsView>();

                Assert.That(Find(root, "FeedbackRow"), Is.Not.Null);
                Assert.That(Find(root, "LeaveGameButton"), Is.Null);
                Assert.That(Find(root, "LeaveGameLabel"), Is.Null);
                Assert.That(Find(root, "BackButton"), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static Transform Find(GameObject root, string name)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (transform.name == name)
                {
                    return transform;
                }
            }

            return null;
        }
    }
}
