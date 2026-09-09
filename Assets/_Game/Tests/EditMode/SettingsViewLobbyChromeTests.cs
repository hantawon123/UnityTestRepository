using Game.Client.Settings;
using NUnit.Framework;
using UnityEngine;

namespace Game.Architecture.Tests
{
    public sealed class SettingsViewLobbyChromeTests
    {
        [Test]
        public void ConfigureAsLobbyOverlay_HidesFeedback()
        {
            var root = new GameObject("Lobby Settings");
            try
            {
                root.SetActive(false);
                var view = root.AddComponent<SettingsView>();
                view.ConfigureAsLobbyOverlay();
                root.SetActive(true);

                Assert.That(Find(root, "FeedbackRow"), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void HomeLayout_KeepsFeedback()
        {
            var root = new GameObject("Home Settings");
            try
            {
                root.AddComponent<SettingsView>();

                Assert.That(Find(root, "FeedbackRow"), Is.Not.Null);
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
