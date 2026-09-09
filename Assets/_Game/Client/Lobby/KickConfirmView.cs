using System;
using System.Collections;
using Game.Client.Character;
using Game.Client.Home;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Client.Lobby
{
    /// <summary>
    /// The kick question, drawn like the settings confirmation: the same
    /// plate, the same buttons, no subtitle.
    /// </summary>
    public sealed class KickConfirmView : MonoBehaviour, ILobbyConfirmView
    {
        public const string RootName = "KickConfirm";
        public const string CancelLabel = "취소";
        public const string ConfirmLabel = "강퇴하기";
        public const int SortingOrder = PlaySettingsStyle.Overlay.SortingOrder + 20;

        public static string FormatTitle(string displayName) =>
            $"{displayName} 님을\n강퇴하시겠습니까?";

        private GameObject root;
        private KickConfirmEscape driver;
        private RawImage backdropImage;
        private TMP_Text title;
        private RenderTexture backdrop;
        private Coroutine backdropRoutine;

        public event Action Confirmed;
        public event Action Cancelled;

        public void Show(string message)
        {
            EnsureLayout();
            if (title != null)
            {
                title.text = message ?? string.Empty;
            }

            if (root != null)
            {
                root.SetActive(true);
            }

            BeginBackdrop();
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }

            EndBackdrop();
        }

        private void OnDestroy()
        {
            EndBackdrop();
        }

        public void EnsureLayout()
        {
            if (root != null)
            {
                return;
            }

            var host = GetComponentInParent<Canvas>()?.transform ?? transform;
            var overlay = new GameObject(
                RootName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(GraphicRaycaster));
            overlay.transform.SetParent(host, false);
            var rect = (RectTransform)overlay.transform;
            Stretch(rect);

            var canvas = overlay.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = SortingOrder;

            var blurRect = CreateRect("Backdrop", rect);
            Stretch(blurRect);
            backdropImage = blurRect.gameObject.AddComponent<RawImage>();
            backdropImage.raycastTarget = false;
            backdropImage.enabled = false;

            var dimRect = CreateRect("Dim", rect);
            Stretch(dimRect);
            AddImage(dimRect, CharacterClosetStyle.Palette.Dim, raycastTarget: true);

            CreatePanel(rect);

            overlay.SetActive(false);
            root = overlay;
            driver = overlay.AddComponent<KickConfirmEscape>();
            driver.Bind(this);
        }

        private void CreatePanel(RectTransform overlay)
        {
            var plate = CreateRect("Panel", overlay);
            SetAnchor(plate, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            plate.anchoredPosition = Vector2.zero;
            plate.sizeDelta = CharacterClosetStyle.Modal.PanelSize;
            AddImage(
                plate,
                CharacterClosetStyle.Palette.ModalFill,
                HomeUiFonts.Rounded(CharacterClosetStyle.Modal.PanelRadius),
                raycastTarget: true);

            var titleHeight = CharacterClosetStyle.Modal.TitleFontSize * 1.4f * 2f;
            title = CreateText(
                "Title",
                plate,
                string.Empty,
                CharacterClosetStyle.Modal.TitleFontSize,
                CharacterClosetStyle.Palette.ModalTitle,
                TextAlignmentOptions.Center);
            var titleRect = title.rectTransform;
            SetAnchor(titleRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
            titleRect.anchoredPosition = new Vector2(0f, -CharacterClosetStyle.Modal.TitleTop);
            titleRect.sizeDelta = new Vector2(0f, titleHeight);

            var buttonTop = CharacterClosetStyle.Modal.TitleTop
                            + titleHeight
                            + CharacterClosetStyle.Modal.ButtonGapAbove;
            var half = (CharacterClosetStyle.Modal.ButtonSize.x
                        + CharacterClosetStyle.Modal.ButtonGap) * 0.5f;

            CreateButton(
                plate,
                "DeclineButton",
                new Vector2(-half, -buttonTop),
                CancelLabel,
                CharacterClosetStyle.Palette.DeclineFill,
                CharacterClosetStyle.Palette.DeclineHoverFill,
                CharacterClosetStyle.Palette.DeclineLabel,
                () => Cancelled?.Invoke());

            CreateButton(
                plate,
                "AcceptButton",
                new Vector2(half, -buttonTop),
                ConfirmLabel,
                CharacterClosetStyle.Palette.AcceptFill,
                CharacterClosetStyle.Palette.AcceptHoverFill,
                CharacterClosetStyle.Palette.AcceptLabel,
                () => Confirmed?.Invoke());

            CreateCloseButton(plate);
        }

        private void CreateButton(
            RectTransform plate,
            string name,
            Vector2 position,
            string label,
            Color fillColor,
            Color hoverColor,
            Color labelColor,
            Action clicked)
        {
            var rect = CreateRect(name, plate);
            SetAnchor(rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            rect.anchoredPosition = position;
            rect.sizeDelta = CharacterClosetStyle.Modal.ButtonSize;

            var fill = AddImage(
                rect,
                fillColor,
                HomeUiFonts.Rounded(CharacterClosetStyle.Modal.ButtonRadius),
                raycastTarget: true);

            var text = CreateText(
                "Label",
                rect,
                label,
                CharacterClosetStyle.Modal.ButtonFontSize,
                labelColor,
                TextAlignmentOptions.Center);
            Stretch(text.rectTransform);

            var hover = rect.gameObject.AddComponent<HomeHoverHighlight>();
            hover.Bind(fill, null, fillColor, hoverColor);

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => clicked());
        }

        private void CreateCloseButton(RectTransform plate)
        {
            var rect = CreateRect("CloseButton", plate);
            SetAnchor(rect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));
            rect.anchoredPosition = new Vector2(
                -CharacterClosetStyle.Modal.CloseOffset.x,
                -CharacterClosetStyle.Modal.CloseOffset.y);
            rect.sizeDelta = new Vector2(
                CharacterClosetStyle.Modal.CloseSize, CharacterClosetStyle.Modal.CloseSize);

            var image = AddImage(rect, CharacterClosetStyle.Palette.CloseIcon, raycastTarget: true);
            image.sprite = BuildCloseIcon();
            image.type = Image.Type.Simple;
            image.preserveAspect = true;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => Cancelled?.Invoke());
        }

        private void BeginBackdrop()
        {
            if (backdropImage == null || driver == null || !Application.isPlaying)
            {
                return;
            }

            EndBackdrop();
            backdropImage.enabled = false;
            backdropRoutine = driver.StartCoroutine(CaptureBackdrop());
        }

        private void EndBackdrop()
        {
            if (backdropRoutine != null)
            {
                if (driver != null)
                {
                    driver.StopCoroutine(backdropRoutine);
                }

                backdropRoutine = null;
            }

            if (backdrop == null)
            {
                return;
            }

            if (backdropImage != null)
            {
                backdropImage.texture = null;
            }

            backdrop.Release();
            Destroy(backdrop);
            backdrop = null;
        }

        private IEnumerator CaptureBackdrop()
        {
            yield return null;
            yield return new WaitForEndOfFrame();

            if (backdrop != null)
            {
                if (backdropImage != null)
                {
                    backdropImage.texture = null;
                }

                backdrop.Release();
                Destroy(backdrop);
                backdrop = null;
            }

            backdrop = ScreenBlur.Capture(
                CharacterClosetStyle.Modal.BackdropHalvings,
                CharacterClosetStyle.Modal.BackdropBlur);
            if (backdropImage != null)
            {
                backdropImage.texture = backdrop;
                backdropImage.uvRect = ScreenBlur.UvRect;
                backdropImage.enabled = true;
            }

            backdropRoutine = null;
        }

        internal void RaiseCancelled() => Cancelled?.Invoke();

        private static Sprite BuildCloseIcon()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear
            };

            var center = (size - 1) * 0.5f;
            var radius = center - 2f;
            var arm = 11f;
            var thickness = 3.2f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    if ((dx * dx) + (dy * dy) > radius * radius)
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    var onX = Mathf.Abs(dx - dy) <= thickness && Mathf.Abs(dx) <= arm
                        || Mathf.Abs(dx + dy) <= thickness && Mathf.Abs(dx) <= arm;
                    texture.SetPixel(x, y, onX ? new Color(0.14f, 0.14f, 0.14f, 1f) : Color.white);
                }
            }

            texture.Apply(false, false);
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var created = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            created.SetParent(parent, false);
            return created;
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
                image.pixelsPerUnitMultiplier = 1f;
            }

            return image;
        }

        private static TMP_Text CreateText(
            string name,
            Transform parent,
            string value,
            float size,
            Color color,
            TextAlignmentOptions alignment)
        {
            var rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = HomeUiFonts.Apply();
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.richText = false;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            SetAnchor(rect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetAnchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
        }

        /// <summary>
        /// Ticks on the overlay so Escape still closes the panel when this
        /// view lives on a hidden scene object.
        /// </summary>
        private sealed class KickConfirmEscape : MonoBehaviour
        {
            private KickConfirmView owner;

            public void Bind(KickConfirmView view) => owner = view;

            private void Update()
            {
                if (owner == null || !gameObject.activeSelf)
                {
                    return;
                }

                var keyboard = Keyboard.current;
                if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                {
                    owner.RaiseCancelled();
                }
            }
        }
    }
}
