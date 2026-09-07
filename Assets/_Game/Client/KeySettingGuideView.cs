using Game.Client.Home;
using Game.Client.Match;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// The on-screen key-setting guide. Lobby and playground each attach one
    /// copy; the list itself does not belong to a match phase.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KeySettingGuideView : MonoBehaviour
    {
        public const string RootName = "KeySettingGuide";
        public const float MarginRight = 48f;
        public const float ActionFontSize = 18f;
        public const string ClickKeyLabel = "클릭";
        public static readonly Vector2 PanelSize = new Vector2(280f, 320f);

        public static readonly string[] Actions =
        {
            "공격",
            "앉기",
            "엎드리기",
            "시점 변경",
            "달리기",
            "점프"
        };

        public static readonly string[] Labels =
        {
            ClickKeyLabel,
            "C",
            "Z",
            "V",
            "Shift",
            "Space"
        };

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
            }
        }

        public static KeySettingGuideView Create(Transform parent)
        {
            var root = new GameObject(RootName, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var view = root.AddComponent<KeySettingGuideView>();
            view.BuildLayout();
            view.ApplyStyle();
            return view;
        }

        public static KeySettingGuideView Ensure(Transform parent)
        {
            if (parent == null)
            {
                return null;
            }

            var existing = parent.Find(RootName) ?? parent.Find("KeyGuide");
            if (existing == null)
            {
                return Create(parent);
            }

            if (existing.name != RootName)
            {
                existing.name = RootName;
            }

            var view = existing.GetComponent<KeySettingGuideView>();
            if (view == null)
            {
                view = existing.gameObject.AddComponent<KeySettingGuideView>();
            }

            view.EnsureLayout();
            return view;
        }

        public void ApplyStyle()
        {
            PlacePanel();
            var light = HomeUiFonts.ApplyLight();
            for (var index = 0; index < Actions.Length; index++)
            {
                var row = transform.Find($"Row{index}");
                if (row == null)
                {
                    continue;
                }

                var action = row.Find("Action")?.GetComponent<TMP_Text>();
                if (action != null)
                {
                    action.font = light;
                    action.fontSize = ActionFontSize;
                    action.fontStyle = FontStyles.Normal;
                    action.color = Color.white;
                    action.alignment = TextAlignmentOptions.MidlineRight;
                }

                var chip = row.Find("Key") as RectTransform;
                var keyLabel = row.Find("Key/Label")?.GetComponent<TMP_Text>();
                if (chip != null)
                {
                    ApplyKeyChipLook(chip.GetComponent<Image>());
                    FitKeyChip(chip, keyLabel);
                }

                if (action != null && chip != null)
                {
                    PlaceAction(action.rectTransform, chip.sizeDelta.x);
                }
            }
        }

        private void Awake() => EnsureLayout();

        private void EnsureLayout()
        {
            if (transform.Find("Row0") == null)
            {
                BuildLayout();
            }

            ApplyStyle();
        }

        private void BuildLayout()
        {
            PlacePanel();
            for (var index = 0; index < Actions.Length; index++)
            {
                if (transform.Find($"Row{index}") != null)
                {
                    continue;
                }

                var row = CreateRect(transform, $"Row{index}");
                Place(
                    row,
                    new Vector2(1f, 1f),
                    new Vector2(-140f, -24f - (index * 48f)),
                    new Vector2(280f, 40f));

                var action = CreateText(
                    row,
                    "Action",
                    Actions[index],
                    ActionFontSize,
                    HomeUiFonts.ApplyLight());
                action.alignment = TextAlignmentOptions.MidlineRight;

                var chip = CreateImage(
                    row,
                    "Key",
                    HidingActiveHudView.KeyChipColor,
                    HidingActiveHudView.KeyChipSprite);
                chip.type = Image.Type.Sliced;
                var keyLabel = CreateText(
                    chip.transform,
                    "Label",
                    Labels[index],
                    HidingActiveHudView.KeyChipFontSize,
                    HomeUiFonts.ApplyLight());
                Stretch(keyLabel.rectTransform);
                FitKeyChip(chip.rectTransform, keyLabel);
                PlaceAction(action.rectTransform, chip.rectTransform.sizeDelta.x);
            }
        }

        private void PlacePanel()
        {
            Place(
                (RectTransform)transform,
                new Vector2(1f, 0.5f),
                new Vector2(-MarginRight, 0f),
                PanelSize,
                new Vector2(1f, 0.5f));
        }

        private static void ApplyKeyChipLook(Image chip)
        {
            if (chip == null)
            {
                return;
            }

            chip.color = HidingActiveHudView.KeyChipColor;
            chip.sprite = HidingActiveHudView.KeyChipSprite;
            chip.type = Image.Type.Sliced;
            chip.pixelsPerUnitMultiplier = 1f;
        }

        private static void FitKeyChip(RectTransform chip, TMP_Text label)
        {
            var usesIcon = label != null && label.text == ClickKeyLabel;
            var icon = chip.Find("Icon")?.GetComponent<Image>();
            if (usesIcon)
            {
                if (label != null)
                {
                    label.gameObject.SetActive(false);
                }

                icon = EnsureClickIcon(chip);
                if (icon != null)
                {
                    icon.gameObject.SetActive(true);
                    Place(
                        icon.rectTransform,
                        new Vector2(0.5f, 0.5f),
                        Vector2.zero,
                        new Vector2(HidingActiveHudView.KeyIconSize, HidingActiveHudView.KeyIconSize));
                }

                Place(
                    chip,
                    new Vector2(1f, 0.5f),
                    Vector2.zero,
                    new Vector2(HidingActiveHudView.KeyChipWidth, HidingActiveHudView.KeyChipHeight),
                    new Vector2(1f, 0.5f));
                return;
            }

            if (icon != null)
            {
                icon.gameObject.SetActive(false);
            }

            var width = HidingActiveHudView.KeyChipWidth;
            if (label != null)
            {
                label.gameObject.SetActive(true);
                label.font = HomeUiFonts.ApplyLight();
                label.fontSize = HidingActiveHudView.KeyChipFontSize;
                label.fontStyle = FontStyles.Normal;
                label.color = Color.white;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Overflow;
                label.ForceMeshUpdate();
                width = HidingActiveHudView.MeasureKeyChipWidth(label.text, label.preferredWidth);
            }

            Place(
                chip,
                new Vector2(1f, 0.5f),
                Vector2.zero,
                new Vector2(width, HidingActiveHudView.KeyChipHeight),
                new Vector2(1f, 0.5f));
        }

        private static Image EnsureClickIcon(RectTransform chip)
        {
            if (chip == null)
            {
                return null;
            }

            var existing = chip.Find("Icon")?.GetComponent<Image>();
            if (existing != null)
            {
                if (existing.sprite == null)
                {
                    existing.sprite = Resources.Load<Sprite>("UI/ic_left_click");
                }

                return existing;
            }

            var sprite = Resources.Load<Sprite>("UI/ic_left_click");
            var icon = CreateImage(chip, "Icon", Color.white, sprite);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            return icon;
        }

        private static void PlaceAction(RectTransform action, float chipWidth)
        {
            Place(
                action,
                new Vector2(1f, 0.5f),
                new Vector2(-(chipWidth + 8f), 0f),
                new Vector2(160f, HidingActiveHudView.KeyChipHeight),
                new Vector2(1f, 0.5f));
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
            return image;
        }

        private static TMP_Text CreateText(
            Transform parent,
            string name,
            string content,
            float fontSize,
            TMP_FontAsset font = null)
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
            text.richText = true;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.font = font != null ? font : HomeUiFonts.Apply();
            text.fontStyle = FontStyles.Normal;
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

        private static void Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }
    }
}
