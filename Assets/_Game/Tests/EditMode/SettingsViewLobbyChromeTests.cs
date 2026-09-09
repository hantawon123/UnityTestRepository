using Game.Client.Settings;
using NUnit.Framework;
using UnityEngine;

namespace Game.Architecture.Tests
{
    public sealed class SettingsViewLobbyChromeTests
    {
        [Test]
        public void ConfigureAsLobbyOverlay_HidesFeedbackAndAddsLeaveButton()
        {
            var root = new GameObject("Lobby Settings");
            try
            {
                root.SetActive(false);
                var view = root.AddComponent<SettingsView>();
                view.ConfigureAsLobbyOverlay();
                root.SetActive(true);

                Assert.That(Find(root, "FeedbackRow"), Is.Null);
                var leave = Find(root, "LeaveGameButton");
                Assert.That(leave, Is.Not.Null);
                var rect = leave.GetComponent<RectTransform>();
                Assert.That(rect.sizeDelta, Is.EqualTo(SettingsStyle.Buttons.Size));
                Assert.That(leave.GetComponent<UiLinearGradient>(), Is.Not.Null);

                var apply = Find(root, "ApplyButton").GetComponent<RectTransform>();
                Assert.That(rect.sizeDelta, Is.EqualTo(apply.sizeDelta));
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
