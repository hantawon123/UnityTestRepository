using System;
using System.Collections.Generic;
using Game.Client.Home;
using Game.Core.Players;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Game.Client.Character
{
    /// <summary>
    /// The closet: the character in the middle, the categories down the left,
    /// the locker on the right.
    /// </summary>
    /// <remarks>
    /// Built in code like the rest of the screens, so
    /// <see cref="CharacterClosetStyle"/> is the only record of the design.
    /// <para>
    /// One canvas while there is no character, two once there is. A
    /// screen-space overlay always draws over the world, so a model that has
    /// to be seen changing cannot share a canvas with the art behind it — see
    /// <see cref="CreateBackground"/>. Home never faces this because its
    /// character is part of the picture.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed partial class CharacterClosetView : MonoBehaviour, ICharacterClosetView
    {
        [Header("Art")]
        [SerializeField]
        private Sprite backgroundSprite;

        [SerializeField]
        [Tooltip("The circling arrow left of the reset label. Optional.")]
        private Sprite resetIcon;

        [Header("Fonts")]
        [SerializeField]
        [Tooltip("SemiBold, for the arrow and the category tabs.")]
        private TMP_FontAsset fontAsset;

        [SerializeField]
        [Tooltip("Medium, for the two buttons under the character. Falls back " +
                 "to the font above when it is not assigned.")]
        private TMP_FontAsset buttonFontAsset;

        [Header("Preview")]
        [SerializeField]
        [Tooltip("Renders the character, once there is one. With a camera the " +
                 "background is drawn on a plane behind the model; without " +
                 "one it is drawn straight onto the controls canvas.")]
        private Camera previewCamera;

        [SerializeField]
        [Tooltip("The character standing in the middle of the screen. The " +
                 "screen is complete without one; the middle is simply empty.")]
        private AvatarAppearanceApplier previewCharacter;

        private TMP_FontAsset font;
        private RectTransform controlsRoot;
        private Image resetFill;
        private TMP_Text resetLabel;
        private Image applyFill;
        private TMP_Text applyLabel;
        private Image resetIconImage;
        private readonly List<Button> buttons = new List<Button>();

        public event Action BackRequested;

        public event Action<AvatarPartCategory> CategorySelected;

        public event Action<AvatarPartCategory, string> PartSelected;

        /// <summary>
        /// Dresses the preview. Does nothing without a character assigned,
        /// which is the case in tests and in a scene still being put together.
        /// </summary>
        public void ShowPreview(AvatarAppearance appearance)
        {
            if (previewCharacter != null)
            {
                previewCharacter.Apply(appearance);
            }
        }

        private void Awake()
        {
            EnsureEventSystem();
            if (controlsRoot == null)
            {
                BuildLayout();
            }
        }

        private void OnDestroy()
        {
            foreach (var button in buttons)
            {
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                }
            }

            buttons.Clear();
        }

        private void BuildLayout()
        {
            font = HomeUiFonts.Apply(fontAsset);
            controlsRoot = CreateControlsCanvas();
            CreateBackground(controlsRoot);
            CreateBackButton(controlsRoot);
            CreateTabRail(controlsRoot);
            CreateLocker(controlsRoot);
            CreateActionBar(controlsRoot);
        }

        /// <summary>
        /// The frontend coordinator keeps one event system alive across the
        /// screens and turns the rest off, so this is only ever the one that
        /// gets used when the closet is played on its own.
        /// </summary>
        private void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.transform.SetParent(transform, false);
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        /// <summary>
        /// The room the screen sits in.
        /// </summary>
        /// <remarks>
        /// Where it is drawn depends on whether there is a character to stand
        /// in front of it. With a preview camera it goes on a canvas that
        /// camera owns, on a plane just inside its far clip, so the model ends
        /// up between the art and the controls. Without one there is nothing
        /// to keep in front of it, so it is the first thing on the controls
        /// canvas — one canvas, exactly as Home does it.
        /// </remarks>
        private void CreateBackground(RectTransform controls)
        {
            var parent = controls;
            if (previewCamera != null)
            {
                var canvasObject = new GameObject(
                    "ClosetBackgroundCanvas", typeof(RectTransform));
                canvasObject.layer = LayerMask.NameToLayer("UI");
                canvasObject.transform.SetParent(transform, false);

                var canvas = canvasObject.AddComponent<Canvas>();
                canvas.sortingOrder = 0;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = previewCamera;
                canvas.planeDistance = Mathf.Max(1f, previewCamera.farClipPlane - 1f);

                AddScaler(canvasObject);
                parent = canvasObject.GetComponent<RectTransform>();
                parent.anchorMin = Vector2.zero;
                parent.anchorMax = Vector2.one;
                parent.offsetMin = Vector2.zero;
                parent.offsetMax = Vector2.zero;
            }

            var art = CreateRect("Background", parent);
            SetAnchor(art, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            art.anchoredPosition = Vector2.zero;
            art.sizeDelta = CharacterClosetStyle.ReferenceResolution;

            var image = art.gameObject.AddComponent<Image>();
            image.sprite = backgroundSprite;
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            image.color = backgroundSprite != null
                ? Color.white
                : CharacterClosetStyle.Palette.BackgroundFallback;

            if (previewCamera == null)
            {
                // Behind the controls, which are already on this canvas.
                art.SetAsFirstSibling();
            }

            var fitter = art.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = backgroundSprite != null && backgroundSprite.rect.height > 0f
                ? backgroundSprite.rect.width / backgroundSprite.rect.height
                : CharacterClosetStyle.ReferenceResolution.x
                  / CharacterClosetStyle.ReferenceResolution.y;
        }

        private RectTransform CreateControlsCanvas()
        {
            var canvasObject = new GameObject("ClosetCanvas", typeof(RectTransform));
            canvasObject.layer = LayerMask.NameToLayer("UI");
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvas.additionalShaderChannels =
                AdditionalCanvasShaderChannels.TexCoord1
                | AdditionalCanvasShaderChannels.Normal
                | AdditionalCanvasShaderChannels.Tangent;

            AddScaler(canvasObject);
            canvasObject.AddComponent<GraphicRaycaster>();

            var rect = canvasObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static void AddScaler(GameObject canvasObject)
        {
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = CharacterClosetStyle.ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        private void CreateBackButton(RectTransform canvas)
        {
            var rect = CreateRect("BackButton", canvas);
            SetAnchor(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            rect.anchoredPosition = CharacterClosetStyle.Back.Position;
            rect.sizeDelta = CharacterClosetStyle.Back.Size;

            // A barely-there graphic takes the click; the arrow and the word
            // are drawn by the label, which takes none.
            var hit = AddImage(rect, new Color(0f, 0f, 0f, 0.01f), raycastTarget: true);

            var label = CreateText(
                "Label",
                rect,
                CharacterClosetStyle.Back.Label,
                CharacterClosetStyle.Back.FontSize,
                CharacterClosetStyle.Palette.BackLabel,
                TextAlignmentOptions.Left);
            SetAnchor(label, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            label.offsetMin = Vector2.zero;
            label.offsetMax = Vector2.zero;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => BackRequested?.Invoke());
            buttons.Add(button);
        }

        /// <summary>
        /// Reset and apply, under the character.
        /// </summary>
        /// <remarks>
        /// Drawn but not wired. What turns them on is whether the draft differs
        /// from what was applied, which is the next story; building the bar now
        /// means that story adds a rule rather than a layout.
        /// </remarks>
        private void CreateActionBar(RectTransform canvas)
        {
            var size = CharacterClosetStyle.Buttons.Size;
            var half = (size.x + CharacterClosetStyle.Buttons.Gap) * 0.5f;

            CreateButtonPlate(
                "ResetButton",
                canvas,
                new Vector2(-half, CharacterClosetStyle.Buttons.BottomMargin),
                CharacterClosetStyle.Buttons.ResetLabel,
                CharacterClosetStyle.Palette.ResetFill,
                CharacterClosetStyle.Palette.ResetLabel,
                resetIcon,
                buttonFontAsset,
                out resetFill,
                out resetLabel);

            CreateButtonPlate(
                "ApplyButton",
                canvas,
                new Vector2(half, CharacterClosetStyle.Buttons.BottomMargin),
                CharacterClosetStyle.Buttons.ApplyLabel,
                CharacterClosetStyle.Palette.ApplyOffFill,
                CharacterClosetStyle.Palette.ApplyOffLabel,
                null,
                buttonFontAsset,
                out applyFill,
                out applyLabel);

            ShowActionsEnabled(false);
        }

        /// <summary>
        /// Paints the two buttons for whether there is anything to apply.
        /// </summary>
        /// <remarks>
        /// Private, and only ever called with <c>false</c>, until the story
        /// that decides when there is something to apply. The colours are here
        /// rather than in that story so the bar the mock-up shows is the bar
        /// this screen draws today.
        /// </remarks>
        private void ShowActionsEnabled(bool enabled)
        {
            if (resetFill != null)
            {
                resetFill.color = enabled
                    ? CharacterClosetStyle.Palette.ResetFill
                    : CharacterClosetStyle.Palette.ApplyOffFill;
            }

            if (resetLabel != null)
            {
                resetLabel.color = enabled
                    ? CharacterClosetStyle.Palette.ResetLabel
                    : CharacterClosetStyle.Palette.ApplyOffLabel;
            }

            if (resetIconImage != null)
            {
                resetIconImage.color = enabled
                    ? CharacterClosetStyle.Palette.ResetLabel
                    : CharacterClosetStyle.Palette.ApplyOffLabel;
            }

            if (applyFill != null)
            {
                applyFill.color = enabled
                    ? CharacterClosetStyle.Palette.ApplyOnFill
                    : CharacterClosetStyle.Palette.ApplyOffFill;
            }

            if (applyLabel != null)
            {
                applyLabel.color = enabled
                    ? CharacterClosetStyle.Palette.ApplyOnLabel
                    : CharacterClosetStyle.Palette.ApplyOffLabel;
            }
        }

        private RectTransform CreateButtonPlate(
            string name,
            RectTransform canvas,
            Vector2 position,
            string label,
            Color fillColor,
            Color labelColor,
            Sprite icon,
            TMP_FontAsset labelFont,
            out Image fill,
            out TMP_Text text)
        {
            var rect = CreateRect(name, canvas);
            SetAnchor(rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            rect.anchoredPosition = position;
            rect.sizeDelta = CharacterClosetStyle.Buttons.Size;

            fill = AddImage(
                rect,
                fillColor,
                HomeUiFonts.Rounded(CharacterClosetStyle.Radius.Button),
                raycastTarget: false);

            var labelRect = CreateText(
                "Label",
                rect,
                label,
                CharacterClosetStyle.Buttons.FontSize,
                labelColor,
                TextAlignmentOptions.Center,
                labelFont);
            SetAnchor(labelRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            text = labelRect.GetComponent<TMP_Text>();
            if (icon != null)
            {
                resetIconImage = CreateButtonIcon(rect, icon, text, labelColor);
            }

            return rect;
        }

        /// <summary>
        /// The glyph beside a button label.
        /// </summary>
        /// <remarks>
        /// Placed against the measured width of the centred label rather than
        /// at a fixed inset, so the icon and the word stay a pair whatever the
        /// plate is sized to.
        /// </remarks>
        private Image CreateButtonIcon(
            RectTransform plate, Sprite icon, TMP_Text label, Color color)
        {
            var size = CharacterClosetStyle.Buttons.IconSize;
            var gap = CharacterClosetStyle.Buttons.IconGap;
            label.ForceMeshUpdate();
            var shift = (size + gap) * 0.5f;
            label.rectTransform.anchoredPosition = new Vector2(shift, 0f);

            var rect = CreateRect("Icon", plate);
            SetAnchor(
                rect,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f));
            rect.anchoredPosition = new Vector2(
                shift - ((label.preferredWidth + size + gap) * 0.5f), 0f);
            rect.sizeDelta = new Vector2(size, size);

            var image = AddImage(rect, color);
            image.sprite = icon;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            return image;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.gameObject.layer = LayerMask.NameToLayer("UI");
            rect.SetParent(parent, false);
            return rect;
        }

        private static Image AddImage(
            RectTransform rect, Color color, Sprite sprite = null, bool raycastTarget = false)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;

                // The rounded sprites are generated at one pixel per unit of
                // radius, so the slice must not be rescaled by the canvas.
                image.pixelsPerUnitMultiplier = 1f;
            }

            return image;
        }

        /// <param name="face">
        /// The weight this piece of the design is drawn in. Null takes the
        /// screen's own, which is the SemiBold everything but the buttons uses.
        /// </param>
        private RectTransform CreateText(
            string name,
            Transform parent,
            string value,
            float size,
            Color color,
            TextAlignmentOptions alignment,
            TMP_FontAsset face = null)
        {
            var rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = face != null ? face : font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.richText = false;
            text.raycastTarget = false;
            return rect;
        }

        private static void SetAnchor(
            RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
        }
    }
}
