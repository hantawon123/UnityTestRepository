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
    public enum MatchChatHudMode
    {
        Hidden,
        Full,
        Searching
    }

    public interface IMatchChatView
    {
        event Action<string> SendRequested;

        void SetMessages(IReadOnlyList<LobbyChatMessage> messages);
        void ClearInput();
        void Deactivate();
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
        public const float Margin = 24f;
        private const float SendIconSize = 24f;
        private const string SendOrangeResource = "UI/ic_send_orange";
        private const string SendGrayResource = "UI/ic_send_gray";

        private TMP_InputField inputField;
        private Image sendImage;
        private Sprite sendOrange;
        private Sprite sendGray;
        private Button sendButton;
        private Transform itemRoot;
        private RectTransform historyRect;
        private readonly RectTransform[] rows = new RectTransform[VisibleMessageCount];
        private readonly TMP_Text[] nameTexts = new TMP_Text[VisibleMessageCount];
        private readonly TMP_Text[] bodyTexts = new TMP_Text[VisibleMessageCount];
        private readonly CanvasGroup[] rowFades = new CanvasGroup[VisibleMessageCount];
        private static Sprite verticalFadeSprite;
        private TMP_FontAsset cachedFont;
        private Sprite lastSendIcon;
        private Coroutine focusRoutine;
        private Coroutine clearRoutine;
        private float lastSendUnscaledTime = -1f;
        private float lastDeactivateUnscaledTime = -1f;
        private bool activated;
        private MatchChatHudMode mode = MatchChatHudMode.Full;
        private bool layoutReady;
        private bool fontPrewarmed;
        private Coroutine prewarmRoutine;

        public event Action<string> SendRequested;
        public static bool BlocksPlayerInput { get; private set; }
        public bool IsActivated => activated;
        public MatchChatHudMode Mode => mode;
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

        private TMP_FontAsset ResolveFont() => cachedFont ??= ChatFont();

        public static MatchChatView Create(Transform canvasParent)
        {
            var root = new GameObject(
                "Match Chat",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(GraphicRaycaster));
            if (canvasParent != null)
            {
                root.transform.SetParent(canvasParent, false);
            }
            else
            {
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 20;
                root.AddComponent<CanvasScaler>().uiScaleMode =
                    CanvasScaler.ScaleMode.ScaleWithScreenSize;
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

        public static bool ShowsHistory(MatchChatHudMode hudMode)
        {
            return hudMode == MatchChatHudMode.Full;
        }

        public static bool ShowsInput(MatchChatHudMode hudMode, bool isActivated)
        {
            return hudMode != MatchChatHudMode.Hidden && isActivated;
        }

        public void SetMode(MatchChatHudMode value)
        {
            EnsureLayout();
            if (mode == value)
            {
                if (value == MatchChatHudMode.Hidden)
                {
                    if (gameObject.activeSelf)
                    {
                        gameObject.SetActive(false);
                    }

                    return;
                }

                if (!gameObject.activeSelf)
                {
                    gameObject.SetActive(true);
                    ApplyPresentation();
                }

                return;
            }

            mode = value;
            if (value == MatchChatHudMode.Hidden)
            {
                SetActivated(false);
                if (gameObject.activeSelf)
                {
                    gameObject.SetActive(false);
                }

                return;
            }

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            SetActivated(false);
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

            if (!fontPrewarmed && prewarmRoutine == null)
            {
                prewarmRoutine = StartCoroutine(PrewarmChatFont());
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

            if (prewarmRoutine != null)
            {
                StopCoroutine(prewarmRoutine);
                prewarmRoutine = null;
            }

            SetActivated(false);
        }

        private void Update()
        {
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

        private IEnumerator PrewarmChatFont()
        {
            var font = ResolveFont();
            if (font != null && font.atlasPopulationMode == AtlasPopulationMode.Static)
            {
                fontPrewarmed = true;
                prewarmRoutine = null;
                yield break;
            }

            var glyphs = LoadKoreanGlyphs();
            if (font == null || glyphs == null || string.IsNullOrEmpty(glyphs.text))
            {
                fontPrewarmed = true;
                prewarmRoutine = null;
                yield break;
            }

            var set = glyphs.text.Replace("\r", string.Empty).Replace("\n", string.Empty);
            const int chunk = 160;
            for (var index = 0; index < set.Length; index += chunk)
            {
                font.TryAddCharacters(set.Substring(index, Mathf.Min(chunk, set.Length - index)));
                yield return null;
            }

            fontPrewarmed = true;
            prewarmRoutine = null;
        }

        private static TextAsset LoadKoreanGlyphs()
        {
            var glyphs = Resources.Load<TextAsset>("Fonts/KoreanGlyphs");
#if UNITY_EDITOR
            if (glyphs == null)
            {
                glyphs = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(
                    "Assets/_Game/Editor/FontAtlasCharacterSet.txt");
            }
#endif
            return glyphs;
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
            var list = messages ?? Array.Empty<LobbyChatMessage>();
            var first = Mathf.Max(0, list.Count - VisibleMessageCount);
            var visibleCount = list.Count - first;
            var font = ResolveFont();
            for (var index = 0; index < VisibleMessageCount; index++)
            {
                var row = rows[index];
                if (row == null)
                {
                    continue;
                }

                if (index >= visibleCount)
                {
                    if (row.gameObject.activeSelf)
                    {
                        row.gameObject.SetActive(false);
                    }

                    continue;
                }

                if (!row.gameObject.activeSelf)
                {
                    row.gameObject.SetActive(true);
                }

                var message = list[first + index];
                ApplyLine(nameTexts[index], message.SenderName, font, NameFontSize, NameColor);
                ApplyLine(bodyTexts[index], message.Text, font, BodyFontSize, Color.white);
            }

            ApplyRowFade();
        }

        public void ClearInput()
        {
            if (inputField == null)
            {
                return;
            }

            Deactivate();
            if (!isActiveAndEnabled)
            {
                ApplyClearedInput(keepFocus: false);
                return;
            }

            if (clearRoutine != null)
            {
                StopCoroutine(clearRoutine);
            }

            clearRoutine = StartCoroutine(ClearInputNextFrame());
        }

        public void Deactivate()
        {
            SetActivated(false);
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
            ApplyClearedInput(keepFocus: false);
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
            Deactivate();
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

            ApplyPresentation();
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

        private void ApplyPresentation()
        {
            if (!layoutReady)
            {
                return;
            }

            var history = historyRect != null
                ? historyRect.gameObject
                : transform.Find("HistoryPanel")?.gameObject;
            var input = transform.Find("InputPanel")?.gameObject;
            if (history != null && history.activeSelf != ShowsHistory(mode))
            {
                history.SetActive(ShowsHistory(mode));
            }

            if (input != null && input.activeSelf != ShowsInput(mode, activated))
            {
                input.SetActive(ShowsInput(mode, activated));
            }

            if (transform is not RectTransform root)
            {
                return;
            }

            var showHistory = ShowsHistory(mode);
            var showInput = ShowsInput(mode, activated);
            var height = 0f;
            if (showHistory)
            {
                height += HistoryHeight;
            }

            if (showHistory && showInput)
            {
                height += PanelGap;
            }

            if (showInput)
            {
                height += InputHeight;
            }

            root.sizeDelta = new Vector2(InputWidth, height);
        }

        private void RefreshSendIcon()
        {
            if (sendImage == null)
            {
                return;
            }

            var icon = activated ? sendOrange : sendGray;
            if (icon == null || lastSendIcon == icon)
            {
                return;
            }

            lastSendIcon = icon;
            sendImage.sprite = icon;
        }

        private void EnsureLayout()
        {
            if (layoutReady && itemRoot != null && inputField != null)
            {
                ApplyInputOverflow();
                return;
            }

            sendOrange ??= Resources.Load<Sprite>(SendOrangeResource);
            sendGray ??= Resources.Load<Sprite>(SendGrayResource);
            if (transform.Find("HistoryPanel") == null)
            {
                ClearLegacyLayout();
                BuildLayout();
            }

            BindRefs();
            BindRows();
            FitPanels();
            FitTextViewport();
            IsolateCanvases();
            EnsureHistoryFade();
            ApplyFonts();
            layoutReady = true;
            ApplyInputOverflow();
            ApplyPresentation();
        }

        private void IsolateCanvases()
        {
            var rootCanvas = GetComponent<Canvas>();
            if (rootCanvas == null)
            {
                rootCanvas = gameObject.AddComponent<Canvas>();
            }

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            rootCanvas.overrideSorting = true;
            if (rootCanvas.sortingOrder < 25)
            {
                rootCanvas.sortingOrder = 25;
            }

            var input = transform.Find("InputPanel");
            if (input == null)
            {
                return;
            }

            var inputCanvas = input.GetComponent<Canvas>();
            if (inputCanvas == null)
            {
                inputCanvas = input.gameObject.AddComponent<Canvas>();
            }

            if (input.GetComponent<GraphicRaycaster>() == null)
            {
                input.gameObject.AddComponent<GraphicRaycaster>();
            }

            inputCanvas.overrideSorting = true;
            inputCanvas.sortingOrder = rootCanvas.sortingOrder + 1;
        }

        private static void ApplyLine(
            TMP_Text text,
            string value,
            TMP_FontAsset font,
            float fontSize,
            Color color)
        {
            if (text == null)
            {
                return;
            }

            if (text.text != value)
            {
                text.text = value;
            }

            if (text.font != font)
            {
                text.font = font;
            }

            if (!Mathf.Approximately(text.fontSize, fontSize))
            {
                text.fontSize = fontSize;
            }

            if (text.color != color)
            {
                text.color = color;
            }

            ApplyWrap(text);
        }

        private void ApplyFonts()
        {
            var font = ResolveFont();
            var texts = GetComponentsInChildren<TMP_Text>(true);
            for (var index = 0; index < texts.Length; index++)
            {
                var text = texts[index];
                if (text.font != font)
                {
                    text.font = font;
                }

                text.fontStyle = FontStyles.Normal;
                text.richText = false;
            }

            if (inputField != null)
            {
                inputField.fontAsset = font;
                inputField.richText = false;
                if (inputField.textComponent != null)
                {
                    if (inputField.textComponent.font != font)
                    {
                        inputField.textComponent.font = font;
                    }

                    inputField.textComponent.richText = false;
                }

                if (inputField.placeholder is TMP_Text placeholder)
                {
                    if (placeholder.font != font)
                    {
                        placeholder.font = font;
                    }

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
            if (historyRect == null)
            {
                historyRect = transform.Find("HistoryPanel") as RectTransform;
            }

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

        private void BindRows()
        {
            if (itemRoot == null)
            {
                return;
            }

            for (var index = 0; index < VisibleMessageCount; index++)
            {
                var row = itemRoot.Find($"Row{index}") as RectTransform;
                rows[index] = row;
                nameTexts[index] = row != null ? row.Find("Name")?.GetComponent<TMP_Text>() : null;
                bodyTexts[index] = row != null ? row.Find("Body")?.GetComponent<TMP_Text>() : null;
                if (row == null)
                {
                    rowFades[index] = null;
                    continue;
                }

                var group = row.GetComponent<CanvasGroup>();
                if (group == null)
                {
                    group = row.gameObject.AddComponent<CanvasGroup>();
                    group.blocksRaycasts = false;
                }

                rowFades[index] = group;
            }
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
            ApplyInputOverflow();
        }

        private void ApplyInputOverflow()
        {
            var text = inputField != null
                ? inputField.textComponent
                : transform.Find("InputPanel/TextViewport/Text")?.GetComponent<TMP_Text>();
            if (text == null)
            {
                return;
            }

            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            if (inputField != null)
            {
                inputField.lineType = TMP_InputField.LineType.SingleLine;
            }
        }

        private void ApplyRowFade()
        {
            if (historyRect == null || itemRoot == null)
            {
                return;
            }

            var itemsRect = itemRoot as RectTransform;
            if (itemsRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(itemsRect);
            }

            var panelRect = historyRect.rect;
            var height = panelRect.height;
            if (height <= 1f)
            {
                return;
            }

            for (var index = 0; index < VisibleMessageCount; index++)
            {
                var row = rows[index];
                var group = rowFades[index];
                if (row == null || group == null || !row.gameObject.activeSelf)
                {
                    continue;
                }

                var local = (Vector2)historyRect.InverseTransformPoint(
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

            if (text.textWrappingMode == TextWrappingModes.Normal &&
                text.overflowMode == TextOverflowModes.Overflow &&
                !text.enableAutoSizing)
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
            text.overflowMode = TextOverflowModes.Overflow;
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
            inputField.onFocusSelectAll = false;
            inputField.restoreOriginalTextOnEscape = false;
            inputField.shouldHideSoftKeyboard = true;
            text.richText = false;
            text.parseCtrlCharacters = false;
            text.margin = Vector4.zero;
            placeholder.richText = false;
            placeholder.parseCtrlCharacters = false;
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
