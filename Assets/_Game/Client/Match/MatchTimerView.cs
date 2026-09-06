using Game.Client.Home;
using TMPro;
using UnityEngine;

namespace Game.Client.Match
{
    public interface IMatchTimerView
    {
        void SetRemainingSeconds(double remainingSeconds);
        void SetHintVisible(bool visible);
    }

    /// <summary>
    /// Top-of-screen searching clock. Matches the hiding timer until the last
    /// thirty seconds, then grows to black 64 type and shows an orange prompt.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchTimerView : MonoBehaviour, IMatchTimerView
    {
        public const float TimerFontSize = 64f;
        public const float HintFontSize = 36f;
        public const float WarningSeconds = 30f;
        public const float TimerHeight = 80f;
        public const float HintHeight = 48f;
        public const string HintText = "서둘러 자신의 물건을 확보하세요 !";
        public static readonly Color TimerColor = Color.black;
        public static readonly Color WarningColor = HidingActiveHudView.WarningColor;

        [SerializeField]
        private TMP_Text timerText;

        [SerializeField]
        private TMP_Text hintText;

        private int lastTotalSeconds = -1;
        private bool warningActive;
        private bool hintAllowed = true;

        public static bool IsWarning(double remainingSeconds)
        {
            return Mathf.Max(0, Mathf.CeilToInt((float)remainingSeconds)) <= WarningSeconds;
        }

        private void Awake()
        {
            EnsureLayout();
        }

        public void SetRemainingSeconds(double remainingSeconds)
        {
            EnsureLayout();
            if (timerText == null)
            {
                return;
            }

            var totalSeconds = Mathf.Max(0, Mathf.CeilToInt((float)remainingSeconds));
            if (totalSeconds != lastTotalSeconds)
            {
                timerText.text = HidingTurnStartView.FormatTimer(remainingSeconds);
                lastTotalSeconds = totalSeconds;
            }

            ApplyUrgency(remainingSeconds);
        }

        public void SetHintVisible(bool visible)
        {
            hintAllowed = visible;
            EnsureLayout();
            ApplyHintVisibility();
        }

        private void EnsureLayout()
        {
            if (timerText == null)
            {
                timerText = transform.Find("Timer")?.GetComponent<TMP_Text>() ??
                            GetComponent<TMP_Text>();
            }

            if (hintText == null)
            {
                hintText = transform.Find("Hint")?.GetComponent<TMP_Text>();
            }

            if (hintText == null)
            {
                hintText = CreateText(transform, "Hint", HintText, HintFontSize);
            }

            ApplyTimerStyle();
            ApplyHintStyle();
            ApplyPlacement();
        }

        private void ApplyTimerStyle()
        {
            if (timerText == null)
            {
                return;
            }

            timerText.font = HomeUiFonts.Apply();
            timerText.fontStyle = FontStyles.Normal;
            timerText.alignment = TextAlignmentOptions.Center;
            timerText.enableWordWrapping = false;
            timerText.overflowMode = TextOverflowModes.Overflow;
            timerText.raycastTarget = false;
        }

        private void ApplyHintStyle()
        {
            if (hintText == null)
            {
                return;
            }

            hintText.font = HomeUiFonts.Apply();
            hintText.fontSize = HintFontSize;
            hintText.fontStyle = FontStyles.Normal;
            hintText.alignment = TextAlignmentOptions.Center;
            hintText.color = WarningColor;
            hintText.enableWordWrapping = false;
            hintText.overflowMode = TextOverflowModes.Overflow;
            hintText.raycastTarget = false;
            hintText.text = HintText;
            ApplyHintVisibility();
        }

        private void ApplyPlacement()
        {
            if (transform is not RectTransform viewRect)
            {
                return;
            }

            var timerOnSelf = timerText != null && timerText.transform == transform;
            if (timerOnSelf)
            {
                Place(
                    viewRect,
                    new Vector2(0.5f, 1f),
                    new Vector2(0f, -HidingActiveHudView.TopPadding),
                    new Vector2(420f, TimerHeight),
                    new Vector2(0.5f, 1f));
            }
            else
            {
                Place(
                    viewRect,
                    new Vector2(0.5f, 1f),
                    new Vector2(0f, -HidingActiveHudView.TopPadding),
                    new Vector2(1200f, TimerHeight + HintHeight),
                    new Vector2(0.5f, 1f));
                if (timerText != null)
                {
                    Place(
                        timerText.rectTransform,
                        new Vector2(0.5f, 1f),
                        Vector2.zero,
                        new Vector2(420f, TimerHeight),
                        new Vector2(0.5f, 1f));
                }
            }

            if (hintText != null)
            {
                Place(
                    hintText.rectTransform,
                    new Vector2(0.5f, 1f),
                    new Vector2(0f, -TimerHeight),
                    new Vector2(1200f, HintHeight),
                    new Vector2(0.5f, 1f));
            }
        }

        private void ApplyUrgency(double remainingSeconds)
        {
            warningActive = IsWarning(remainingSeconds);
            if (timerText != null)
            {
                timerText.fontSize = warningActive
                    ? TimerFontSize
                    : HidingActiveHudView.TimerFontSize;
                timerText.color = warningActive ? TimerColor : Color.white;
            }

            ApplyHintVisibility();
        }

        private void ApplyHintVisibility()
        {
            if (hintText != null)
            {
                hintText.gameObject.SetActive(hintAllowed && warningActive);
            }
        }

        private static TMP_Text CreateText(
            Transform parent,
            string name,
            string content,
            float fontSize)
        {
            var gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            gameObject.transform.SetParent(parent, false);

            var text = gameObject.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = WarningColor;
            text.raycastTarget = false;
            text.font = HomeUiFonts.Apply();
            text.fontStyle = FontStyles.Normal;
            return text;
        }

        private static void Place(
            RectTransform rect,
            Vector2 anchor,
            Vector2 anchoredPosition,
            Vector2 size,
            Vector2 pivot)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }
    }
}
