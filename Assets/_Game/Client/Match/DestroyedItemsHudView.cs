using Game.Client.Home;
using Game.Core.Lobby;
using Game.Core.Match;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.Match
{
    public interface IDestroyedItemsHudView
    {
        void Show(int playerCount, System.Collections.Generic.IReadOnlyList<PlayerItemStatusSnapshot> statuses);
        void Hide();
    }

    /// <summary>
    /// Top-left circles for destroyed assignment items. Empty slots show "?".
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DestroyedItemsHudView : MonoBehaviour, IDestroyedItemsHudView
    {
        public const float SlotSize = 100f;
        public const int PreviewTextureSize = 256;
        public const float SlotGap = 12f;
        public const float QuestionFontSize = 30f;
        public const float LeftPadding = 36f;
        public const float TopPadding = 36f;
        public const string QuestionMark = "?";

        public static readonly Color SlotColor = new Color(0f, 0f, 0f, 0.6f);
        public static readonly Color QuestionColor = new Color(200f / 255f, 200f / 255f, 200f / 255f, 1f);

        [SerializeField]
        private GameObject panel;

        [SerializeField]
        private RectTransform slotRoot;

        private Slot[] slots = System.Array.Empty<Slot>();

        public static DestroyedItemsHudView Create(Transform parent)
        {
            var rootObject = new GameObject("DestroyedItems", typeof(RectTransform));
            rootObject.transform.SetParent(parent, false);
            Stretch((RectTransform)rootObject.transform);
            return rootObject.AddComponent<DestroyedItemsHudView>();
        }

        private void Awake()
        {
            EnsureLayout();
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            DisposeSlots();
        }

        public void Show(
            int playerCount,
            System.Collections.Generic.IReadOnlyList<PlayerItemStatusSnapshot> statuses)
        {
            EnsureLayout();
            var count = Mathf.Clamp(playerCount, 0, RoomSettings.MaxPlayerCount);
            if (count <= 0)
            {
                Hide();
                return;
            }

            EnsureSlots(count);
            statuses ??= System.Array.Empty<PlayerItemStatusSnapshot>();
            for (var index = 0; index < slots.Length; index++)
            {
                var destroyed = index < statuses.Count && statuses[index].IsDestroyed;
                var itemId = index < statuses.Count ? statuses[index].ItemId : null;
                slots[index].Set(destroyed, itemId);
            }

            if (panel != null)
            {
                panel.SetActive(true);
            }
        }

        public void Hide()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        private void EnsureLayout()
        {
            if (panel == null || slotRoot == null)
            {
                var panelRect = CreateRect(transform, "Panel");
                panelRect.anchorMin = new Vector2(0f, 1f);
                panelRect.anchorMax = new Vector2(0f, 1f);
                panelRect.pivot = new Vector2(0f, 1f);
                var layout = panelRect.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = SlotGap;
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
                panel = panelRect.gameObject;
                slotRoot = panelRect;
            }

            slotRoot.anchoredPosition = new Vector2(LeftPadding, -TopPadding);
            slotRoot.sizeDelta = new Vector2(
                (SlotSize * RoomSettings.MaxPlayerCount) + (SlotGap * (RoomSettings.MaxPlayerCount - 1)),
                SlotSize);
        }

        private void EnsureSlots(int count)
        {
            if (slots.Length == count && SlotsMatchSize())
            {
                return;
            }

            DisposeSlots();
            slots = new Slot[count];
            for (var index = 0; index < count; index++)
            {
                slots[index] = Slot.Create(slotRoot, index);
            }
        }

        private void DisposeSlots()
        {
            for (var index = 0; index < slots.Length; index++)
            {
                slots[index]?.Dispose();
            }

            slots = System.Array.Empty<Slot>();
        }

        private bool SlotsMatchSize()
        {
            if (slotRoot == null)
            {
                return false;
            }

            var layout = slotRoot.Find("Slot0")?.GetComponent<LayoutElement>();
            return layout != null &&
                   Mathf.Approximately(layout.preferredWidth, SlotSize) &&
                   Mathf.Approximately(layout.preferredHeight, SlotSize);
        }

        private static RectTransform CreateRect(Transform parent, string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private sealed class Slot
        {
            private readonly GameObject root;
            private readonly TMP_Text question;
            private readonly RawImage previewImage;
            private readonly HidingIntroItemPreview preview;

            private Slot(
                GameObject root,
                TMP_Text question,
                RawImage previewImage,
                HidingIntroItemPreview preview)
            {
                this.root = root;
                this.question = question;
                this.previewImage = previewImage;
                this.preview = preview;
            }

            public static Slot Create(Transform parent, int index)
            {
                var root = new GameObject(
                    $"Slot{index}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Mask),
                    typeof(LayoutElement));
                root.transform.SetParent(parent, false);
                var image = root.GetComponent<Image>();
                image.sprite = HomeUiFonts.CircleSprite;
                image.color = SlotColor;
                image.raycastTarget = false;
                root.GetComponent<Mask>().showMaskGraphic = true;
                var layout = root.GetComponent<LayoutElement>();
                layout.preferredWidth = SlotSize;
                layout.preferredHeight = SlotSize;
                layout.minWidth = SlotSize;
                layout.minHeight = SlotSize;

                var questionObject = new GameObject(
                    "Question",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI));
                questionObject.transform.SetParent(root.transform, false);
                Stretch((RectTransform)questionObject.transform);
                var question = questionObject.GetComponent<TextMeshProUGUI>();
                question.text = QuestionMark;
                question.font = HomeUiFonts.Apply();
                question.fontSize = QuestionFontSize;
                question.fontStyle = FontStyles.Normal;
                question.alignment = TextAlignmentOptions.Center;
                question.color = QuestionColor;
                question.raycastTarget = false;
                question.textWrappingMode = TextWrappingModes.NoWrap;

                var previewObject = new GameObject(
                    "Preview",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(RawImage));
                previewObject.transform.SetParent(root.transform, false);
                Stretch((RectTransform)previewObject.transform);
                var previewImage = previewObject.GetComponent<RawImage>();
                previewImage.color = Color.white;
                previewImage.raycastTarget = false;
                previewImage.enabled = false;
                var preview = new HidingIntroItemPreview(
                    previewImage,
                    PreviewTextureSize,
                    new Color(0f, 0f, 0f, 0f),
                    Vector3.right * (20f * (index + 1)));
                return new Slot(root, question, previewImage, preview);
            }

            public void Set(bool destroyed, string itemId)
            {
                if (destroyed && !string.IsNullOrWhiteSpace(itemId))
                {
                    preview.Show(itemId);
                    var shown = preview.HasPreview;
                    previewImage.enabled = shown;
                    question.gameObject.SetActive(!shown);
                    return;
                }

                preview.Clear();
                previewImage.enabled = false;
                question.gameObject.SetActive(true);
            }

            public void Dispose()
            {
                preview.Dispose();
                if (root != null)
                {
                    Object.Destroy(root);
                }
            }
        }
    }
}
