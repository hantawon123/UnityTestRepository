using Game.Client.Home;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.Match
{
    public interface IMatchVitalsHudView
    {
        void Show(float stamina, float maxStamina, int hits, int maxHits, bool exhausted = false);
        void Hide();
        void SetValues(float stamina, float maxStamina, int hits, int maxHits, bool exhausted = false);
    }

    /// <summary>
    /// Bottom-center stamina and hit bars. Stamina fill follows the local motor each frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchVitalsHudView : MonoBehaviour, IMatchVitalsHudView
    {
        public const float DefaultStamina = 100f;
        public const float LowStaminaThreshold = 20f;
        public const float ShakeAmplitude = 2.5f;
        public const float ShakeCyclesPerSecond = 18f;
        public const int DefaultHits = 3;
        public const float ValueFontSize = 22f;
        public const float IconSize = 22f;
        public const float IconPadding = 12f;
        public const float BarIconGap = 8f;
        public const float BarValueGap = 8f;
        public const float ValueWidth = 72f;
        public const float ValuePadding = 8f;
        public const float RowInset = 16f;
        public const float BarHeight = 14f;
        public const float BarSlant = 10f;
        public const float SegmentGap = 10f;
        public const float PanelWidth = 380f;
        public const float PanelHeight = 88f;
        public const float BottomPadding = MatchChatView.Margin;

        public static float BarStart => IconPadding + IconSize + BarIconGap;
        public static float BarRightInset => ValuePadding + ValueWidth + BarValueGap;
        public const string FlashIconResource = "UI/ic_flash";
        public const string HeartIconResource = "UI/ic_heart";

        public static readonly Color PanelColor = new Color(11f / 255f, 16f / 255f, 24f / 255f, 0.7f);
        public static readonly Color StaminaColor = new Color(245f / 255f, 243f / 255f, 241f / 255f, 1f);
        public static readonly Color StaminaDisabledColor = new Color(136f / 255f, 136f / 255f, 136f / 255f, 1f);
        public static readonly Color StaminaLowColor = new Color(1f, 51f / 255f, 51f / 255f, 1f);
        public static readonly Color HealthStartColor = new Color(1f, 154f / 255f, 106f / 255f, 1f);
        public static readonly Color HealthEndColor = new Color(1f, 112f / 255f, 50f / 255f, 1f);

        [SerializeField]
        private GameObject panel;

        [SerializeField]
        private Image staminaIcon;

        [SerializeField]
        private TMP_Text staminaText;

        [SerializeField]
        private TMP_Text healthText;

        [SerializeField]
        private RectTransform staminaFill;

        [SerializeField]
        private RectTransform[] healthSegments;

        [SerializeField]
        [Tooltip("Shows the bars in the editor Game view without entering Play.")]
        private bool previewOnAwake;

        private bool shown;
        private bool shakeStaminaNumber;
        private float shakeElapsed;
        private Vector2 staminaTextRest = new Vector2(-ValuePadding, 0f);

        public static string FormatValue(int current, int max)
        {
            return $"{Mathf.Max(0, current)}/{Mathf.Max(0, max)}";
        }

        public static string FormatStamina(float current)
        {
            return Mathf.RoundToInt(Mathf.Max(0f, current)).ToString();
        }

        public static bool IsLowStamina(float current)
        {
            return current <= LowStaminaThreshold;
        }

        public static Color StaminaColorFor(bool exhausted)
        {
            return StaminaColorFor(DefaultStamina, exhausted);
        }

        public static Color StaminaColorFor(float current, bool exhausted)
        {
            if (exhausted)
            {
                return StaminaDisabledColor;
            }

            return IsLowStamina(current) ? StaminaLowColor : StaminaColor;
        }

        public static Color StaminaAccentFor(float current, bool exhausted)
        {
            if (exhausted)
            {
                return StaminaDisabledColor;
            }

            return IsLowStamina(current) ? StaminaLowColor : Color.white;
        }

        public static Vector2 ShakeOffset(float elapsedSeconds)
        {
            var radians = elapsedSeconds * ShakeCyclesPerSecond * Mathf.PI * 2f;
            return new Vector2(
                Mathf.Sin(radians) * ShakeAmplitude,
                Mathf.Cos(radians * 1.3f) * (ShakeAmplitude * 0.7f));
        }

        public static float FillAmount(float current, float max)
        {
            return max <= 0f ? 0f : Mathf.Clamp01(current / max);
        }

        public static Color HealthColorAt(int index, int count)
        {
            if (count <= 1)
            {
                return HealthStartColor;
            }

            return Color.Lerp(HealthStartColor, HealthEndColor, index / (float)(count - 1));
        }

        public static MatchVitalsHudView Create(Transform parent)
        {
            var rootObject = new GameObject("MatchVitals", typeof(RectTransform));
            rootObject.transform.SetParent(parent, false);
            Stretch((RectTransform)rootObject.transform);
            return rootObject.AddComponent<MatchVitalsHudView>();
        }

        private void Awake()
        {
            EnsureLayout();
            if (previewOnAwake && !shown)
            {
                Show(DefaultStamina, DefaultStamina, DefaultHits, DefaultHits);
                return;
            }

            if (!shown)
            {
                Hide();
            }
        }

        public void Show(float stamina, float maxStamina, int hits, int maxHits, bool exhausted = false)
        {
            shown = true;
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            EnsureLayout();
            SetValues(stamina, maxStamina, hits, maxHits, exhausted);
            if (panel != null)
            {
                panel.SetActive(true);
            }
        }

        public void Hide()
        {
            shown = false;
            StopStaminaShake();
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        public void SetValues(float stamina, float maxStamina, int hits, int maxHits, bool exhausted = false)
        {
            EnsureLayout();
            var staminaColor = StaminaColorFor(stamina, exhausted);
            var staminaAccent = StaminaAccentFor(stamina, exhausted);
            if (staminaText != null)
            {
                staminaText.text = FormatStamina(stamina);
                staminaText.color = staminaAccent;
            }

            if (staminaIcon != null)
            {
                staminaIcon.color = staminaAccent;
            }

            if (healthText != null)
            {
                healthText.text = FormatValue(hits, maxHits);
            }

            if (staminaFill != null)
            {
                var fill = staminaFill.GetComponent<Image>();
                if (fill != null)
                {
                    fill.type = Image.Type.Filled;
                    fill.fillMethod = Image.FillMethod.Horizontal;
                    fill.fillAmount = FillAmount(stamina, maxStamina);
                    fill.color = staminaColor;
                }
            }

            if (shown && IsLowStamina(stamina))
            {
                shakeStaminaNumber = true;
            }
            else
            {
                StopStaminaShake();
            }

            if (healthSegments == null)
            {
                return;
            }

            for (var index = 0; index < healthSegments.Length; index++)
            {
                var segment = healthSegments[index];
                if (segment != null)
                {
                    segment.gameObject.SetActive(index < hits);
                }
            }
        }

        private void Update()
        {
            if (staminaText == null)
            {
                return;
            }

            if (!shown || !shakeStaminaNumber)
            {
                ResetStaminaTextPosition();
                return;
            }

            shakeElapsed += Time.unscaledDeltaTime;
            staminaText.rectTransform.anchoredPosition = staminaTextRest + ShakeOffset(shakeElapsed);
        }

        private void StopStaminaShake()
        {
            shakeStaminaNumber = false;
            shakeElapsed = 0f;
            ResetStaminaTextPosition();
        }

        private void ResetStaminaTextPosition()
        {
            if (staminaText != null)
            {
                staminaText.rectTransform.anchoredPosition = staminaTextRest;
            }
        }

        private void EnsureLayout()
        {
            var rect = transform as RectTransform;
            if (rect != null)
            {
                Stretch(rect);
            }

            if (!HasCurrentLayout())
            {
                DestroyChild("Panel");
                panel = null;
                staminaIcon = null;
                staminaText = null;
                healthText = null;
                staminaFill = null;
                healthSegments = null;
                BuildLayout();
            }

            if (panel == null)
            {
                panel = transform.Find("Panel")?.gameObject;
            }

            if (staminaIcon == null)
            {
                staminaIcon = transform.Find("Panel/Stamina/Icon")?.GetComponent<Image>();
            }

            if (staminaText == null)
            {
                staminaText = transform.Find("Panel/Stamina/Value")?.GetComponent<TMP_Text>();
                if (staminaText != null)
                {
                    staminaTextRest = staminaText.rectTransform.anchoredPosition;
                }
            }

            if (healthText == null)
            {
                healthText = transform.Find("Panel/Health/Value")?.GetComponent<TMP_Text>();
            }

            if (staminaFill == null)
            {
                staminaFill = transform.Find("Panel/Stamina/Bar") as RectTransform;
            }

            if (healthSegments == null || healthSegments.Length == 0)
            {
                healthSegments = new RectTransform[DefaultHits];
                for (var index = 0; index < DefaultHits; index++)
                {
                    healthSegments[index] =
                        transform.Find($"Panel/Health/BarTrack/Segment{index}") as RectTransform;
                }
            }

            ApplyBarMetrics();
        }

        private bool HasCurrentLayout()
        {
            var panelRect = transform.Find("Panel") as RectTransform;
            return panelRect != null &&
                   Mathf.Approximately(panelRect.anchorMin.x, 0.5f) &&
                   Mathf.Approximately(panelRect.anchorMax.x, 0.5f) &&
                   Mathf.Approximately(panelRect.sizeDelta.x, PanelWidth) &&
                   transform.Find("Panel/Health/BarTrack") != null &&
                   transform.Find("Panel/Stamina/Bar")?.GetComponent<ParallelogramShear>() != null;
        }

        private void DestroyChild(string childName)
        {
            var child = transform.Find(childName);
            if (child == null)
            {
                return;
            }

            DestroyImmediate(child.gameObject);
        }

        private void ApplyBarMetrics()
        {
            if (panel != null && panel.transform is RectTransform panelRect)
            {
                Place(
                    panelRect,
                    new Vector2(0.5f, 0f),
                    new Vector2(0f, BottomPadding),
                    new Vector2(PanelWidth, PanelHeight),
                    new Vector2(0.5f, 0f));
            }

            var staminaRow = transform.Find("Panel/Stamina") as RectTransform;
            if (staminaRow != null)
            {
                StretchRow(staminaRow, 16f);
            }

            var healthRow = transform.Find("Panel/Health") as RectTransform;
            if (healthRow != null)
            {
                StretchRow(healthRow, -16f);
            }

            if (staminaFill != null)
            {
                StretchBetween(staminaFill, BarStart, BarRightInset, BarHeight);
            }

            var track = transform.Find("Panel/Health/BarTrack") as RectTransform;
            if (track != null)
            {
                StretchBetween(track, BarStart, BarRightInset, BarHeight);
            }
        }

        private void BuildLayout()
        {
            var panelRect = CreateImage(
                transform,
                "Panel",
                PanelColor,
                HomeUiFonts.RoundedSprite).rectTransform;
            panelRect.GetComponent<Image>().type = Image.Type.Sliced;
            Place(
                panelRect,
                new Vector2(0.5f, 0f),
                new Vector2(0f, BottomPadding),
                new Vector2(PanelWidth, PanelHeight),
                new Vector2(0.5f, 0f));
            panel = panelRect.gameObject;

            var stamina = CreateRow(panelRect, "Stamina", FlashIconResource, Color.white);
            StretchRow(stamina, 16f);
            staminaIcon = stamina.Find("Icon")?.GetComponent<Image>();
            staminaFill = CreateFillBar(stamina, "Bar", StaminaColor);
            staminaText = CreateValue(stamina, FormatStamina(DefaultStamina));
            staminaTextRest = staminaText.rectTransform.anchoredPosition;

            var health = CreateRow(panelRect, "Health", HeartIconResource, Color.white);
            StretchRow(health, -16f);
            healthSegments = CreateHealthSegments(health);
            healthText = CreateValue(health, FormatValue(DefaultHits, DefaultHits));
        }

        private static RectTransform CreateRow(
            Transform parent,
            string name,
            string iconResource,
            Color iconColor)
        {
            var row = CreateRect(parent, name);
            var icon = CreateImage(row, "Icon", iconColor, Resources.Load<Sprite>(iconResource));
            icon.preserveAspect = true;
            Place(
                icon.rectTransform,
                new Vector2(0f, 0.5f),
                new Vector2(IconPadding, 0f),
                new Vector2(IconSize, IconSize),
                new Vector2(0f, 0.5f));
            return row;
        }

        private static RectTransform CreateFillBar(Transform parent, string name, Color color)
        {
            var bar = CreateImage(parent, name, color, HomeUiFonts.WhiteSprite);
            bar.preserveAspect = false;
            bar.type = Image.Type.Filled;
            bar.fillMethod = Image.FillMethod.Horizontal;
            bar.fillAmount = 1f;
            bar.gameObject.AddComponent<ParallelogramShear>();
            StretchBetween(bar.rectTransform, BarStart, BarRightInset, BarHeight);
            return bar.rectTransform;
        }

        private static RectTransform[] CreateHealthSegments(Transform parent)
        {
            var track = CreateRect(parent, "BarTrack");
            StretchBetween(track, BarStart, BarRightInset, BarHeight);
            var layout = track.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = SegmentGap;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.padding = new RectOffset(0, 0, 0, 0);

            var segments = new RectTransform[DefaultHits];
            for (var index = 0; index < DefaultHits; index++)
            {
                var bar = CreateImage(
                    track,
                    $"Segment{index}",
                    HealthColorAt(index, DefaultHits),
                    HomeUiFonts.WhiteSprite);
                bar.preserveAspect = false;
                bar.gameObject.AddComponent<ParallelogramShear>();
                var element = bar.gameObject.AddComponent<LayoutElement>();
                element.flexibleWidth = 1f;
                element.minWidth = 0f;
                segments[index] = bar.rectTransform;
            }

            return segments;
        }

        private static TMP_Text CreateValue(Transform parent, string content)
        {
            var text = CreateText(parent, "Value", content, ValueFontSize);
            text.fontStyle = FontStyles.Italic;
            text.alignment = TextAlignmentOptions.MidlineRight;
            Place(
                text.rectTransform,
                new Vector2(1f, 0.5f),
                new Vector2(-ValuePadding, 0f),
                new Vector2(ValueWidth, 28f),
                new Vector2(1f, 0.5f));
            return text;
        }

        private static RectTransform CreateRect(Transform parent, string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<RectTransform>();
        }

        private static Image CreateImage(Transform parent, string name, Color color, Sprite sprite)
        {
            var gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            gameObject.transform.SetParent(parent, false);
            var image = gameObject.GetComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.raycastTarget = false;
            image.preserveAspect = sprite != null;
            return image;
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
            text.color = Color.white;
            text.raycastTarget = false;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.font = HomeUiFonts.Apply();
            text.fontStyle = FontStyles.Italic;
            return text;
        }

        private static void Place(
            RectTransform rect,
            Vector2 anchor,
            Vector2 anchoredPosition,
            Vector2 size,
            Vector2? pivot = null)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void StretchRow(RectTransform rect, float y)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(RowInset, y - 14f);
            rect.offsetMax = new Vector2(-RowInset, y + 14f);
        }

        private static void StretchBetween(
            RectTransform rect,
            float left,
            float right,
            float height)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, -height * 0.5f);
            rect.offsetMax = new Vector2(-right, height * 0.5f);
        }
    }
}
