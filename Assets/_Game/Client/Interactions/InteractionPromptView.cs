using Game.Client.Home;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.Interactions
{
    /// <summary>조준한 대상 위에 상호작용 키와 동작 문구를 띄운다.</summary>
    public sealed class InteractionPromptView : MonoBehaviour
    {
        public const float LabelFontSize = 18f;
        public const float KeyFontSize = 18f;
        public const float KeyBoxSize = 32f;
        public static readonly Color KeyBoxColor = new(0.12f, 0.12f, 0.12f, 1f);

        private const int SortingOrder = 220;
        private const float WorldLift = 0.08f;

        private Canvas canvas;
        private RectTransform root;
        private Image keyBox;
        private TMP_Text keyLabel;
        private TMP_Text actionLabel;
        private Transform follow;
        private Camera followCamera;

        public Image KeyBox => keyBox;

        public TMP_Text KeyLabel => keyLabel;

        public TMP_Text ActionLabel => actionLabel;

        public bool IsVisible => root != null && root.gameObject.activeSelf;

        public static InteractionPromptView Create()
        {
            var root = new GameObject(
                "Interaction Prompt",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            var view = root.AddComponent<InteractionPromptView>();
            view.Build();
            return view;
        }

        public void Show(string key, string action, Transform target)
        {
            EnsureBuilt();
            follow = target;
            keyLabel.text = key ?? string.Empty;
            actionLabel.text = action ?? string.Empty;
            root.gameObject.SetActive(true);
            RefreshPosition();
        }

        public void Hide()
        {
            follow = null;
            if (root != null)
            {
                root.gameObject.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            if (IsVisible)
            {
                RefreshPosition();
            }
        }

        private void EnsureBuilt()
        {
            if (root == null)
            {
                Build();
            }
        }

        private void Build()
        {
            canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            canvas.pixelPerfect = true;

            var raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                raycaster.enabled = false;
            }

            var font = HomeUiFonts.Apply();

            root = CreateRect("Prompt", transform);
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0f);
            root.sizeDelta = new Vector2(160f, 58f);

            var layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.LowerCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = 6f;

            var fitter = root.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var keyRow = CreateRect("KeyRow", root);
            var keyRowLayout = keyRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            keyRowLayout.childAlignment = TextAnchor.MiddleCenter;
            keyRowLayout.childControlWidth = true;
            keyRowLayout.childControlHeight = true;
            keyRowLayout.childForceExpandWidth = false;
            keyRowLayout.childForceExpandHeight = false;

            var keyBoxRect = CreateRect("KeyBox", keyRow);
            keyBox = keyBoxRect.gameObject.AddComponent<Image>();
            keyBox.sprite = HomeUiFonts.RoundedSprite;
            keyBox.type = Image.Type.Sliced;
            keyBox.pixelsPerUnitMultiplier = 2.4f;
            keyBox.color = KeyBoxColor;
            keyBox.raycastTarget = false;
            var keyBoxLayout = keyBoxRect.gameObject.AddComponent<LayoutElement>();
            keyBoxLayout.preferredWidth = KeyBoxSize;
            keyBoxLayout.preferredHeight = KeyBoxSize;
            keyBoxLayout.minWidth = KeyBoxSize;
            keyBoxLayout.minHeight = KeyBoxSize;
            keyBoxLayout.flexibleWidth = 0f;

            keyLabel = CreateLabel("Key", keyBoxRect, font, KeyFontSize, FontStyles.Normal);
            var keyRect = keyLabel.rectTransform;
            keyRect.anchorMin = Vector2.zero;
            keyRect.anchorMax = Vector2.one;
            keyRect.offsetMin = Vector2.zero;
            keyRect.offsetMax = Vector2.zero;

            actionLabel = CreateLabel("Action", root, font, LabelFontSize, FontStyles.Normal);

            root.gameObject.SetActive(false);
        }

        private void RefreshPosition()
        {
            if (follow == null || root == null)
            {
                return;
            }

            if (followCamera == null || !followCamera.isActiveAndEnabled)
            {
                followCamera = Camera.main;
            }

            if (followCamera == null)
            {
                return;
            }

            var world = ResolveAnchor(follow);
            var screen = followCamera.WorldToScreenPoint(world);
            if (screen.z <= 0f)
            {
                root.gameObject.SetActive(false);
                return;
            }

            if (!root.gameObject.activeSelf)
            {
                root.gameObject.SetActive(true);
            }

            root.position = screen;
        }

        private static Vector3 ResolveAnchor(Transform target)
        {
            if (target.TryGetComponent<Collider>(out var targetCollider) && targetCollider.enabled)
            {
                var bounds = targetCollider.bounds;
                return bounds.center + (Vector3.up * (bounds.extents.y + WorldLift));
            }

            var renderers = target.GetComponentsInChildren<Renderer>();
            Bounds? combined = null;
            for (var index = 0; index < renderers.Length; index++)
            {
                if (ItemOutlineRenderers.IsGenerated(renderers[index]))
                {
                    continue;
                }

                if (!combined.HasValue)
                {
                    combined = renderers[index].bounds;
                    continue;
                }

                var bounds = combined.Value;
                bounds.Encapsulate(renderers[index].bounds);
                combined = bounds;
            }

            if (combined.HasValue)
            {
                var bounds = combined.Value;
                return bounds.center + (Vector3.up * (bounds.extents.y + WorldLift));
            }

            return target.position + (Vector3.up * 0.35f);
        }

        private static RectTransform CreateRect(string objectName, Transform parent)
        {
            var created = new GameObject(objectName, typeof(RectTransform));
            created.transform.SetParent(parent, false);
            return created.GetComponent<RectTransform>();
        }

        private static TMP_Text CreateLabel(
            string objectName,
            Transform parent,
            TMP_FontAsset font,
            float fontSize,
            FontStyles style)
        {
            var created = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            created.transform.SetParent(parent, false);
            var text = created.GetComponent<TextMeshProUGUI>();
            text.font = font;
            if (font != null)
            {
                text.fontSharedMaterial = font.material;
            }

            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }
    }
}
