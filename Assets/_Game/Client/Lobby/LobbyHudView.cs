using Game.Client.Home;
using UnityEngine;

namespace Game.Client.Lobby
{
    /// <summary>
    /// The lobby's always-on screen furniture.
    /// </summary>
    /// <remarks>
    /// Nothing here is pressed. The cursor is captured for looking around for
    /// most of the visit, and a captured cursor reports from the centre of the
    /// screen, so a button pinned to a corner cannot be reached at all. Every
    /// button the lobby had up here — start, leave, settings, play settings,
    /// the key guide — is an entry in the Esc menu now. See
    /// <see cref="LobbyPauseMenuView"/>.
    /// <para>
    /// What is left is the things a player reads rather than clicks, the
    /// shared key guide, and the chat field, which the keyboard reaches on
    /// its own.
    /// </para>
    /// </remarks>
    public sealed class LobbyHudView : MonoBehaviour
    {
        [SerializeField]
        private RectTransform playerListRoot;

        [SerializeField]
        private RectTransform chatRoot;

        [SerializeField]
        private RectTransform voiceButton;
        private UnityEngine.UI.Text countdown;

        public void SetStartCountdown(double remaining)
        {
            if (remaining <= 0d)
            {
                if (countdown != null) countdown.gameObject.SetActive(false);
                return;
            }
            if (countdown == null)
            {
                var root = new GameObject("Start countdown", typeof(RectTransform), typeof(UnityEngine.UI.Text));
                root.transform.SetParent(transform, false);
                var rect = (RectTransform)root.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.75f);
                rect.sizeDelta = new Vector2(700, 90);
                countdown = root.GetComponent<UnityEngine.UI.Text>();
                countdown.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                countdown.fontSize = 44;
                countdown.alignment = TextAnchor.MiddleCenter;
                countdown.raycastTarget = false;
                HomeUiFonts.ApplyLegacy(root.transform);
            }
            countdown.gameObject.SetActive(true);
            countdown.text = $"게임 시작까지 {System.Math.Ceiling(remaining)}초";
        }

        private void OnEnable()
        {
            var canvas = GetComponentInParent<Canvas>();
            HomeUiFonts.ApplyLegacy(canvas != null ? canvas.transform : transform);
            KeySettingGuideView.Ensure(transform)?.SetVisible(true);
        }
    }
}
