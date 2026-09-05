using System;
using System.Collections;
using System.Collections.Generic;
using Game.Client.Home;
using Game.Core.Lobby;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Client.Match
{
    public interface IMatchChatView
    {
        event Action<string> SendRequested;

        void SetMessages(IReadOnlyList<LobbyChatMessage> messages);
        void ClearInput();
    }

    /// <summary>한 줄 입력과 최근 메시지만 표시하는 인게임 채팅 View.</summary>
    public sealed class MatchChatView : MonoBehaviour, IMatchChatView
    {
        public const int VisibleMessageCount = 4;
        public const float NameFontSize = 14f;
        public const float BodyFontSize = 20f;
        public const float InputFontSize = 16f;
        public const string PlaceholderText = "채팅 입력..";
        public const float InputWidth = 320f;
        public const float ContentPadding = 16f;
        public const float OpenCooldownSeconds = 0.12f;
        public static readonly Color NameColor = new Color32(0xC1, 0xC1, 0xC1, 0xFF);
        public static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.62f);
        private const float HistoryHeight = 248f;
        private const float InputHeight = 48f;
        private const float PanelGap = 10f;
        private const float Margin = 24f;
        private const float SendIconSize = 24f;
        private const string SendOrangeResource = "UI/ic_send_orange";
        private const string SendGrayResource = "UI/ic_send_gray";

        private TMP_InputField inputField;
        private Image sendImage;
        private Sprite sendOrange;
        private Sprite sendGray;
        private Button sendButton;
        private Transform itemRoot;
        private static Sprite verticalFadeSprite;
        private Coroutine focusRoutine;
        private Coroutine clearRoutine;
        private float lastSendUnscaledTime = -1f;
        private float lastDeactivateUnscaledTime = -1f;
        private bool activated;

        public event Action<string> SendRequested;
        public static bool BlocksPlayerInput { get; private set; }
        public bool IsActivated => activated;
        public bool IsInputFocused =>
            activated || (inputField != null && inputField.isFocused);

        public static TMP_FontAsset ChatFont()
        {
            var regular = HomeUiFonts.ApplyRegular();
            if (IsPaperlogy(regular))
            {
                return regular;
            }

            return HomeUiFonts.Apply();
        }

        public static MatchChatView Create(Transform canvasParent)
        {
            var root = new GameObject("Match Chat", typeof(RectTransform));
            if (canvasParent != null)
            {
                root.transform.SetParent(canvasParent, false);
            }
            else
            {
                var canvas = root.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 20;
                root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                root.AddComponent<GraphicRaycaster>();
            }

            return root.AddComponent<MatchChatView>();
        }

        public static IReadOnlyList<LobbyChatMessage> VisibleMessages(
            IReadOnlyList<LobbyChatMessage> messages)
        {
            var list = messages ?? Array.Empty<LobbyChatMessage>();
            var first = Mathf.Max(0, list.Count - VisibleMessageCount);
            if (first == 0)
            {
                return list;
            }

            var visible = new LobbyChatMessage[list.Count - first];
            for (var index = 0; index < visible.Length; index++)
            {
                visible[index] = list[first + index];
            }

            return visible;
        }

        public static float HistoryFadeAlpha(float normalizedFromTop)
        {
            return Mathf.Clamp01(normalizedFromTop);
        }

        public static bool ShouldOpenOnEnter(
            bool isActivated,
            bool isOpening,
            bool enterPressed,
            float now,
            float lastClosedAt)
        {
            return enterPressed &&
                   !isActivated &&
                   !isOpening &&
                   now - lastClosedAt >= OpenCooldownSeconds;
        }

        private void Awake()
        {
            EnsureLayout();
            ApplyFonts();
            SetActivated(false);
        }

        private void OnEnable()
        {
            if (inputField != null)
            {
                inputField.onSubmit.AddListener(HandleSubmit);
            }

            if (sendButton != null)
            {
                sendButton.onClick.AddListener(HandleSendClicked);
            }
        }

        private void OnDisable()
        {
            if (inputField != null)
            {
                inputField.onSubmit.RemoveListener(HandleSubmit);
            }

            if (sendButton != null)
            {
                sendButton.onClick.RemoveListener(HandleSendClicked);
            }

            if (focusRoutine != null)
            {
                StopCoroutine(focusRoutine);
                focusRoutine = null;
            }

            if (clearRoutine != null)
            {
                StopCoroutine(clearRoutine);
                clearRoutine = null;
            }

            SetActivated(false);
        }

        private void Update()
        {
            RefreshSendIcon();
            if (!WasEnterPressedThisFrame())
            {
                return;
            }

            if (activated)
            {
                if (inputField != null && string.IsNullOrWhiteSpace(inputField.text))
                {
                    SetActivated(false);
                }

                return;
            }

            if (!ShouldOpenOnEnter(
                    activated,
                    focusRoutine != null,
                    true,
                    Time.unscaledTime,
                    lastDeactivateUnscaledTime))
            {
                return;
            }

            // Open on the next frame so the key that opens chat cannot submit it.
            focusRoutine = StartCoroutine(FocusInputNextFrame());
        }

        private static bool WasEnterPressedThisFrame()
        {
            var keyboard = Keyboard.current;
            return keyboard != null &&
                   (keyboard.enterKey.wasPressedThisFrame ||
                    keyboard.numpadEnterKey.wasPressedThisFrame);
        }

        public void SetMessages(IReadOnlyList<LobbyChatMessage> messages)
        {
            EnsureLayout();
            ApplyFonts();
            var visible = VisibleMessages(messages);
            for (var index = 0; index < VisibleMessageCount; index++)
            {
                var row = itemRoot.Find($"Row{index}");
                if (row == null)
                {
                    continue;
                }

                if (index >= visible.Count)
                {
                    row.gameObject.SetActive(false);
                    continue;
                }

                row.gameObject.SetActive(true);
                var message = visible[index];
                var name = row.Find("Name")?.GetComponent<TMP_Text>();
                var body = row.Find("Body")?.GetComponent<TMP_Text>();
                if (name != null)
                {
                    name.text = message.SenderName;
                    name.font = ChatFont();
                    name.fontSize = NameFontSize;
                    name.color = NameColor;
                    ApplyWrap(name);
                }

                if (body != null)
                {
                    body.text = message.Text;
                    body.font = ChatFont();
                    body.fontSize = BodyFontSize;
                    body.color = Color.white;
                    ApplyWrap(body);
                }
            }

            ApplyRowFade();
        }

        public void ClearInput()
        {
            if (inputField == null)
            {
                return;
            }

            if (!isActiveAndEnabled)
            {
                ApplyClearedInput(keepFocus: activated);
                return;
            }

            if (clearRoutine != null)
            {
                StopCoroutine(clearRoutine);
            }

            clearRoutine = StartCoroutine(ClearInputNextFrame());
        }

        private IEnumerator FocusInputNextFrame()
        {
            yield return null;
            SetActivated(true);
            focusRoutine = null;
        }

        private IEnumerator ClearInputNextFrame()
        {
            yield return null;
            ApplyClearedInput(keepFocus: activated);
            clearRoutine = null;
        }

        private void ApplyClearedInput(bool keepFocus)
        {
            inputField.text = string.Empty;
            inputField.caretPosition = 0;
            inputField.selectionAnchorPosition = 0;
            inputField.selectionFocusPosition = 0;
            inputField.ForceLabelUpdate();
            if (keepFocus)
            {
                inputField.ActivateInputField();
                inputField.Select();
                return;
            }

            inputField.DeactivateInputField();
            if (EventSystem.current?.currentSelectedGameObject == inputField.gameObject)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private void HandleSendClicked()
        {
            HandleSubmit(inputField != null ? inputField.text : string.Empty);
        }

        private void HandleSubmit(string text)
        {
            if (Time.unscaledTime - lastSendUnscaledTime < 0.08f)
            {
                return;
            }

            lastSendUnscaledTime = Time.unscaledTime;
            if (string.IsNullOrWhiteSpace(text))
            {
                SetActivated(false);
                ApplyClearedInput(keepFocus: false);
                return;
            }

            SendRequested?.Invoke(text.Trim());
        }

        private void SetActivated(bool value)
        {
            activated = value;
            BlocksPlayerInput = value;
            RefreshSendIcon();
            if (!value)
            {
                lastDeactivateUnscaledTime = Time.unscaledTime;
            }

            if (inputField == null)
            {
                return;
            }

            if (value)
            {
                EventSystem.current?.SetSelectedGameObject(null);
                inputField.Select();
                inputField.ActivateInputField();
                return;
            }

            inputField.DeactivateInputField();
            if (EventSystem.current?.currentSelectedGameObject == inputField.gameObject)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private void RefreshSendIcon()
        {
            if (sendImage == null)
            {
                return;
            }

            var icon = activated ? sendOrange : sendGray;
            if (icon != null)
            {
                sendImage.sprite = icon;
            }
        }

        private void EnsureLayout()
        {
            sendOrange ??= Resources.Load<Sprite>(SendOrangeResource);
            sendGray ??= Resources.Load<Sprite>(SendGrayResource);
            if (transform.Find("HistoryPanel") == null)
            {
                ClearLegacyLayout();
                BuildLayout();
            }

            BindRefs();
            FitPanels();
            FitTextViewport();
            EnsureHistoryFade();
            ApplyFonts();
        }

        private void ApplyFonts()
        {
            var font = ChatFont();
            var texts = GetComponentsInChildren<TMP_Text>(true);
            for (var index = 0; index < texts.Length; index++)
            {
                texts[index].font = font;
                texts[index].fontStyle = FontStyles.Normal;
                texts[index].richText = false;
            }

            if (inputField != null)
            {
                inputField.fontAsset = font;
                inputField.richText = false;
                if (inputField.textComponent != null)
                {
                    inputField.textComponent.font = font;
                    inputField.textComponent.richText = false;
                }

                if (inputField.placeholder is TMP_Text placeholder)
                {
                    placeholder.font = font;
                    placeholder.richText = false;
                }
            }
        }

        private static bool IsPaperlogy(TMP_FontAsset font)
        {
            if (font == null)
            {
                return false;
            }

            return font.name.IndexOf("Paperlogy", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   font.faceInfo.familyName.IndexOf("Paperlogy", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void BindRefs()
        {
            if (itemRoot == null)
            {
                itemRoot = transform.Find("HistoryPanel/Items");
            }

            if (inputField == null)
            {
                inputField = transform.Find("InputPanel")?.GetComponent<TMP_InputField>();
            }

            if (sendButton == null)
            {
                sendButton = transform.Find("InputPanel/Send")?.GetComponent<Button>();
            }

            if (sendImage == null)
            {
                sendImage = transform.Find("InputPanel/Send")?.GetComponent<Image>();
            }

            sendOrange ??= Resources.Load<Sprite>(SendOrangeResource);
            sendGray ??= Resources.Load<Sprite>(SendGrayResource);
        }

        private void FitPanels()
        {
            var root = transform as RectTransform;
            if (root != null)
            {
                root.sizeDelta = new Vector2(InputWidth, HistoryHeight + PanelGap + InputHeight);
            }

            var history = transform.Find("HistoryPanel") as RectTransform;
            if (history != null)
            {
                Place(
                    history,
                    new Vector2(0f, 1f),
                    new Vector2(0f, 0f),
                    new Vector2(InputWidth, HistoryHeight),
                    new Vector2(0f, 1f));
            }

            var input = transform.Find("InputPanel") as RectTransform;
            if (input == null)
            {
                return;
            }

            Place(
                input,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(InputWidth, InputHeight),
                new Vector2(0f, 0f));
        }

        private void FitTextViewport()
        {
            var viewport = transform.Find("InputPanel/TextViewport") as RectTransform;
            if (viewport == null)
            {
                return;
            }

            Stretch(viewport);
            viewport.offsetMin = new Vector2(ContentPadding, 0f);
            viewport.offsetMax = new Vector2(-(SendIconSize + ContentPadding), 0f);
            FitInputLabel(viewport.Find("Text") as RectTransform);
            FitInputLabel(viewport.Find("Placeholder") as RectTransform);
        }

        private void ApplyRowFade()
        {
            var history = transform.Find("HistoryPanel") as RectTransform;
            if (history == null || itemRoot == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            var panelRect = history.rect;
            var height = panelRect.height;
            if (height <= 1f)
            {
                return;
            }

            for (var index = 0; index < VisibleMessageCount; index++)
            {
                var row = itemRoot.Find($"Row{index}") as RectTransform;
                if (row == null)
                {
                    continue;
                }

                var group = row.GetComponent<CanvasGroup>();
                if (group == null)
                {
                    group = row.gameObject.AddComponent<CanvasGroup>();
                    group.blocksRaycasts = false;
                }

                if (!row.gameObject.activeSelf)
                {
                    continue;
                }

                var local = (Vector2)history.InverseTransformPoint(
                    row.TransformPoint(row.rect.center));
                var fromTop = (panelRect.yMax - local.y) / height;
                group.alpha = HistoryFadeAlpha(fromTop);
            }
        }

        private static void ApplyWrap(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.enableAutoSizing = false;
            var layout = text.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.minHeight = text.fontSize + 4f;
                layout.preferredHeight = -1f;
                layout.preferredWidth = -1f;
                layout.flexibleWidth = 1f;
            }
        }

        private void EnsureHistoryFade()
        {
            var history = transform.Find("HistoryPanel") as RectTransform;
            if (history == null)
            {
                return;
            }

            var mask = history.GetComponent<Mask>();
            if (mask != null)
            {
                DestroyImmediate(mask);
            }

            if (history.GetComponent<RectMask2D>() == null)
            {
                history.gameObject.AddComponent<RectMask2D>();
            }

            var panelImage = history.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.enabled = false;
            }

            var background = history.Find("Background") as RectTransform;
            if (background == null)
            {
                background = CreatePanel(history, "Background");
            }

            Stretch(background);
            var backgroundImage = background.GetComponent<Image>();
            backgroundImage.sprite = VerticalFadeSprite;
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.color = PanelColor;
            backgroundImage.raycastTarget = false;
            backgroundImage.preserveAspect = false;
            background.SetSiblingIndex(0);
            itemRoot?.SetAsLastSibling();
        }

        private static Sprite VerticalFadeSprite
        {
            get
            {
                if (verticalFadeSprite != null)
                {
                    return verticalFadeSprite;
                }

                const int width = 8;
                const int height = 128;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };

                for (var y = 0; y < height; y++)
                {
                    var alpha = 1f - (y / (height - 1f));
                    var color = new Color(1f, 1f, 1f, alpha);
                    for (var x = 0; x < width; x++)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }

                texture.Apply(false, false);
                verticalFadeSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, width, height),
                    new Vector2(0.5f, 0.5f),
                    100f,
                    0,
                    SpriteMeshType.FullRect);
                verticalFadeSprite.hideFlags = HideFlags.HideAndDontSave;
                return verticalFadeSprite;
            }
        }

        private void ClearLegacyLayout()
        {
            var rootImage = GetComponent<Image>();
            if (rootImage != null)
            {
                DestroyImmediate(rootImage);
            }

            for (var index = transform.childCount - 1; index >= 0; index--)
            {
                DestroyImmediate(transform.GetChild(index).gameObject);
            }
        }

        private void BuildLayout()
        {
            var root = (RectTransform)transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.zero;
            root.pivot = Vector2.zero;
            root.anchoredPosition = new Vector2(Margin, Margin);
            root.sizeDelta = new Vector2(InputWidth, HistoryHeight + PanelGap + InputHeight);

            var history = CreatePanel(root, "HistoryPanel");
            Place(
                history,
                new Vector2(0f, 1f),
                new Vector2(0f, 0f),
                new Vector2(InputWidth, HistoryHeight),
                new Vector2(0f, 1f));

            var items = new GameObject("Items", typeof(RectTransform), typeof(VerticalLayoutGroup));
            items.transform.SetParent(history, false);
            var itemsRect = (RectTransform)items.transform;
            Stretch(itemsRect, ContentPadding);
            var layout = items.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.LowerLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            itemRoot = items.transform;

            for (var index = 0; index < VisibleMessageCount; index++)
            {
                BuildRow(itemRoot, index);
            }

            var inputPanel = CreatePanel(root, "InputPanel");
            Place(
                inputPanel,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(InputWidth, InputHeight),
                new Vector2(0f, 0f));

            var textArea = new GameObject("TextViewport", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(inputPanel, false);
            var textAreaRect = (RectTransform)textArea.transform;
            Stretch(textAreaRect);
            textAreaRect.offsetMin = new Vector2(ContentPadding, 0f);
            textAreaRect.offsetMax = new Vector2(-(SendIconSize + ContentPadding), 0f);

            var text = CreateText(
                textAreaRect,
                "Text",
                string.Empty,
                InputFontSize,
                Color.white);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
            FitInputLabel(text.rectTransform);

            var placeholder = CreateText(
                textAreaRect,
                "Placeholder",
                PlaceholderText,
                InputFontSize,
                new Color(1f, 1f, 1f, 0.58f));
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;
            placeholder.textWrappingMode = TextWrappingModes.NoWrap;
            placeholder.overflowMode = TextOverflowModes.Truncate;
            FitInputLabel(placeholder.rectTransform);

            var send = CreateImage(inputPanel, "Send", Color.white, sendGray);
            send.preserveAspect = true;
            Place(
                send.rectTransform,
                new Vector2(1f, 0.5f),
                new Vector2(-ContentPadding, 0f),
                new Vector2(SendIconSize, SendIconSize),
                new Vector2(1f, 0.5f));
            sendButton = send.gameObject.AddComponent<Button>();
            sendButton.transition = Selectable.Transition.None;
            sendImage = send;

            inputField = inputPanel.gameObject.AddComponent<TMP_InputField>();
            inputField.targetGraphic = inputPanel.gameObject.GetComponent<Image>();
            inputField.textViewport = textAreaRect;
            inputField.textComponent = text;
            inputField.placeholder = placeholder;
            inputField.fontAsset = ChatFont();
            inputField.pointSize = InputFontSize;
            inputField.lineType = TMP_InputField.LineType.SingleLine;
            inputField.characterLimit = LobbyChatMessage.MaxTextLength;
            inputField.navigation = new Navigation { mode = Navigation.Mode.None };
            inputField.interactable = true;
            inputField.richText = false;
            text.richText = false;
            text.margin = Vector4.zero;
            placeholder.richText = false;
            placeholder.margin = Vector4.zero;
        }

        private static void BuildRow(Transform parent, int index)
        {
            var row = new GameObject(
                $"Row{index}",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            row.transform.SetParent(parent, false);
            var layout = row.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 2f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = row.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var name = CreateText(row.transform, "Name", string.Empty, NameFontSize, NameColor);
            name.alignment = TextAlignmentOptions.TopLeft;
            ApplyWrap(name);
            name.font = ChatFont();
            row.AddComponent<CanvasGroup>().blocksRaycasts = false;

            var body = CreateText(row.transform, "Body", string.Empty, BodyFontSize, Color.white);
            body.alignment = TextAlignmentOptions.TopLeft;
            ApplyWrap(body);
            body.font = ChatFont();
            row.SetActive(false);
        }

        private static RectTransform CreatePanel(Transform parent, string name)
        {
            var panel = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            panel.transform.SetParent(parent, false);
            var image = panel.GetComponent<Image>();
            image.sprite = HomeUiFonts.RoundedSprite;
            image.type = Image.Type.Sliced;
            image.color = PanelColor;
            image.raycastTarget = true;
            return panel.GetComponent<RectTransform>();
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
            image.raycastTarget = true;
            return image;
        }

        private static TMP_Text CreateText(
            Transform parent,
            string name,
            string content,
            float fontSize,
            Color color)
        {
            var gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI),
                typeof(LayoutElement));
            gameObject.transform.SetParent(parent, false);
            var text = gameObject.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.font = ChatFont();
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Normal;
            text.color = color;
            text.raycastTarget = false;
            text.richText = false;
            var layout = gameObject.GetComponent<LayoutElement>();
            layout.minHeight = fontSize + 4f;
            layout.preferredHeight = fontSize + 6f;
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

        private static void FitInputLabel(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            Stretch(rect);
            var text = rect.GetComponent<TMP_Text>();
            if (text != null)
            {
                text.margin = Vector4.zero;
                text.extraPadding = false;
            }

            var layout = rect.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.ignoreLayout = true;
            }
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
