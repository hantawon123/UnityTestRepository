using System;
using System.Collections.Generic;
using Game.Client.Home;
using Game.Core.Lobby;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.Lobby
{
    /// <summary>
    /// Two-column 2-key modal: the live roster on the left and a friend
    /// invite list on the right. Friend actions are chrome only for now.
    /// </summary>
    public sealed class LobbyPlayerListView : MonoBehaviour, ILobbyPlayerListView
    {
        public const string ParticipantsTitle = "게임 참가자 목록";
        public const string FriendsTitle = "친구 목록";
        public const float TitleFontSize = 20f;
        public const float NicknameFontSize = 16f;
        public const float KickFontSize = 16f;
        public const float RowHeight = 40f;
        public const float AvatarSize = 30f;
        public const float AvatarLeft = 10f;
        public const float NicknameLeft = 10f;
        public const float LeaderIconGap = 6f;
        public const float LeaderIconSize = 16f;
        public const float ActionRight = 16f;
        public const float AddButtonSize = 24f;
        public const float PanelPadding = 24f;
        public const float ColumnInnerPadding = 10f;
        public const float TitleHeight = 28f;
        public const float TitleToRows = 12f;
        public const float ModalWidth = 750f;
        public const float ModalHeight = 420f;
        public const float ColumnWidthRatio = 0.4f;
        public const int PanelRadius = 30;
        public const int ColumnRadius = 16;

        public static readonly Color KickColor = new Color(177f / 255f, 177f / 255f, 177f / 255f, 1f);
        public static readonly Color AvatarColor = new Color(0.62f, 0.62f, 0.62f, 1f);

        public static float ColumnWidth => ModalWidth * ColumnWidthRatio;

        public static readonly Vector2 ModalSize = new Vector2(ModalWidth, ModalHeight);

        private static readonly string[] PlaceholderFriends =
        {
            "일이삼사오육칠팔구십일이",
            "초대할까말까할까말까",
            "로비친구하나",
            "로비친구둘",
            "로비친구셋",
            "로비친구넷",
            "로비친구다섯",
            "로비친구여섯"
        };

        private RectTransform participantRowRoot;
        private RectTransform friendRowRoot;
        private TextMeshProUGUI participantsTitle;
        private TextMeshProUGUI friendsTitle;
        private readonly List<GameObject> participantRows = new();
        private readonly List<GameObject> friendRows = new();

        public event Action<string, string> KickClicked;
        public event Action<string, string> TransferClicked;

        public string ParticipantsTitleText =>
            participantsTitle != null ? participantsTitle.text : string.Empty;

        public string FriendsTitleText =>
            friendsTitle != null ? friendsTitle.text : string.Empty;

        private void Awake()
        {
            EnsureLayout();
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void SetParticipants(
            IReadOnlyList<LobbyParticipant> participants,
            bool localIsHost,
            string localPlayerId)
        {
            EnsureLayout();
            ClearRows(participantRows);

            if (participants == null || participants.Count == 0)
            {
                CreateInfoRow(participantRowRoot, participantRows, "참가자가 없습니다.");
                return;
            }

            for (var index = 0; index < participants.Count; index++)
            {
                var participant = participants[index];
                var canKick = localIsHost &&
                    !string.Equals(participant.Id, localPlayerId, StringComparison.Ordinal);
                var row = CreateRow(
                    participantRowRoot,
                    participantRows,
                    $"Row_{participant.Id}",
                    participant.DisplayName,
                    participant.IsHost,
                    canKick,
                    showAdd: false);
                if (!canKick)
                {
                    continue;
                }

                var playerId = participant.Id;
                var displayName = participant.DisplayName;
                var kick = row.Find("Kick")?.GetComponent<Button>();
                if (kick != null)
                {
                    kick.onClick.AddListener(() => KickClicked?.Invoke(playerId, displayName));
                }
            }
        }

        public void EnsureLayout()
        {
            HideLegacyChrome();
            ApplyPanelChrome();
            if (participantsTitle != null &&
                friendsTitle != null &&
                participantRowRoot != null &&
                friendRowRoot != null)
            {
                PlaceColumns();
                return;
            }

            BuildLayout();
        }

        private void HideLegacyChrome()
        {
            HideChild("Title");
            HideChild("BodyText");
            HideChild("Label");
            var leftoverRows = transform.Find("RowRoot");
            if (leftoverRows != null && leftoverRows.Find("Columns") == null)
            {
                leftoverRows.gameObject.SetActive(false);
            }
        }

        private void HideChild(string name)
        {
            var child = transform.Find(name);
            if (child != null)
            {
                child.gameObject.SetActive(false);
            }
        }

        private void ApplyPanelChrome()
        {
            var rect = (RectTransform)transform;
            rect.sizeDelta = ModalSize;

            var fill = GetComponent<Image>();
            if (fill == null)
            {
                fill = gameObject.AddComponent<Image>();
            }

            fill.sprite = HomeUiFonts.Rounded(PanelRadius);
            fill.type = Image.Type.Sliced;
            fill.pixelsPerUnitMultiplier = 1f;
            fill.color = PlaySettingsStyle.Palette.PanelFill;
            fill.raycastTarget = true;
        }

        private void BuildLayout()
        {
            var columns = FindOrCreateRect("Columns", transform);
            Stretch(columns, 0f, PanelPadding, 0f, -PanelPadding);
            var row = columns.GetComponent<HorizontalLayoutGroup>();
            if (row != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(row);
                }
                else
                {
                    DestroyImmediate(row);
                }
            }

            var participants = CreateColumn(columns, "Participants", ParticipantsTitle, out participantsTitle);
            participantRowRoot = participants;
            var friends = CreateColumn(columns, "Friends", FriendsTitle, out friendsTitle);
            friendRowRoot = friends;
            PlaceColumns();
            FillPlaceholderFriends();
        }

        private void PlaceColumns()
        {
            var columns = transform.Find("Columns") as RectTransform;
            if (columns == null)
            {
                return;
            }

            Stretch(columns, 0f, PanelPadding, 0f, -PanelPadding);
            PlaceColumn(columns.Find("Participants") as RectTransform, isLeft: true);
            PlaceColumn(columns.Find("Friends") as RectTransform, isLeft: false);
            InsetColumnContent(columns.Find("Participants") as RectTransform);
            InsetColumnContent(columns.Find("Friends") as RectTransform);
        }

        private static void PlaceColumn(RectTransform column, bool isLeft)
        {
            if (column == null)
            {
                return;
            }

            var side = (1f - (ColumnWidthRatio * 2f)) * 0.25f;
            var minX = isLeft ? side : 1f - side - ColumnWidthRatio;
            column.anchorMin = new Vector2(minX, 0f);
            column.anchorMax = new Vector2(minX + ColumnWidthRatio, 1f);
            column.pivot = new Vector2(0.5f, 0.5f);
            column.offsetMin = Vector2.zero;
            column.offsetMax = Vector2.zero;
            column.anchoredPosition = Vector2.zero;
            column.sizeDelta = Vector2.zero;
        }

        private static void InsetColumnContent(RectTransform column)
        {
            if (column == null)
            {
                return;
            }

            var pad = ColumnInnerPadding;
            var title = column.Find("Title") as RectTransform;
            if (title != null)
            {
                title.anchorMin = new Vector2(0f, 1f);
                title.anchorMax = new Vector2(1f, 1f);
                title.pivot = new Vector2(0.5f, 1f);
                title.offsetMin = new Vector2(pad, -(pad + TitleHeight));
                title.offsetMax = new Vector2(-pad, -pad);
            }

            var scroll = column.Find("Scroll") as RectTransform;
            if (scroll != null)
            {
                scroll.anchorMin = Vector2.zero;
                scroll.anchorMax = Vector2.one;
                scroll.pivot = new Vector2(0.5f, 1f);
                scroll.offsetMin = new Vector2(pad, pad);
                scroll.offsetMax = new Vector2(-pad, -(pad + TitleHeight + TitleToRows));
            }
        }

        private RectTransform CreateColumn(
            Transform parent,
            string name,
            string title,
            out TextMeshProUGUI titleLabel)
        {
            var column = FindOrCreateRect(name, parent);
            var element = column.GetComponent<LayoutElement>();
            if (element != null)
            {
                element.ignoreLayout = true;
            }

            var outline = column.GetComponent<Image>();
            if (outline == null)
            {
                outline = column.gameObject.AddComponent<Image>();
            }

            outline.sprite = HomeUiFonts.Outline(ColumnRadius);
            outline.type = Image.Type.Sliced;
            outline.pixelsPerUnitMultiplier = 1f;
            outline.color = new Color(1f, 1f, 1f, 0.45f);
            outline.raycastTarget = false;

            var titleRect = FindOrCreateRect("Title", column);
            titleLabel = titleRect.GetComponent<TextMeshProUGUI>();
            if (titleLabel == null)
            {
                titleLabel = titleRect.gameObject.AddComponent<TextMeshProUGUI>();
            }

            ApplyTitle(titleLabel, title);

            var scroll = FindOrCreateRect("Scroll", column);
            InsetColumnContent(column);
            if (scroll.GetComponent<RectMask2D>() == null)
            {
                scroll.gameObject.AddComponent<RectMask2D>();
            }

            var content = FindOrCreateRect("RowRoot", scroll);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var layout = content.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 0f;

            var fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollRect = scroll.GetComponent<ScrollRect>();
            if (scrollRect == null)
            {
                scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
            }

            scrollRect.content = content;
            scrollRect.viewport = scroll;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = RowHeight;
            if (name == "Friends")
            {
                scrollRect.verticalScrollbar = CreateScrollbar(scroll);
                scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            }

            return content;
        }

        private static Scrollbar CreateScrollbar(RectTransform viewport)
        {
            var bar = FindOrCreateRect("Scrollbar", viewport);
            bar.anchorMin = new Vector2(1f, 0f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(1f, 0.5f);
            bar.sizeDelta = new Vector2(HomeStyle.Friends.ScrollbarHitWidth, 0f);
            bar.anchoredPosition = Vector2.zero;
            var hit = bar.GetComponent<Image>();
            if (hit == null)
            {
                hit = bar.gameObject.AddComponent<Image>();
            }

            hit.color = Color.clear;
            hit.raycastTarget = true;

            var sliding = FindOrCreateRect("Sliding Area", bar);
            Stretch(sliding, 0f, 0f, 0f, 0f);

            var handle = FindOrCreateRect("Handle", sliding);
            Stretch(handle, 0f, 0f, 0f, 0f);
            var grab = handle.GetComponent<Image>();
            if (grab == null)
            {
                grab = handle.gameObject.AddComponent<Image>();
            }

            grab.color = Color.clear;
            grab.raycastTarget = true;

            var visual = FindOrCreateRect("Visual", handle);
            visual.anchorMin = new Vector2(0.5f, 0f);
            visual.anchorMax = new Vector2(0.5f, 1f);
            visual.pivot = new Vector2(0.5f, 0.5f);
            visual.sizeDelta = new Vector2(HomeStyle.Friends.ScrollbarWidth, 0f);
            visual.anchoredPosition = Vector2.zero;
            var paint = visual.GetComponent<Image>();
            if (paint == null)
            {
                paint = visual.gameObject.AddComponent<Image>();
            }

            paint.sprite = HomeUiFonts.Rounded(HomeStyle.Friends.ScrollbarRadius);
            paint.type = Image.Type.Sliced;
            paint.color = HomeStyle.Palette.ScrollHandle;
            paint.raycastTarget = false;

            var scrollbar = bar.GetComponent<Scrollbar>();
            if (scrollbar == null)
            {
                scrollbar = bar.gameObject.AddComponent<Scrollbar>();
            }

            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = grab;
            scrollbar.transition = Selectable.Transition.None;
            return scrollbar;
        }

        private void FillPlaceholderFriends()
        {
            if (friendRows.Count > 0)
            {
                return;
            }

            for (var index = 0; index < PlaceholderFriends.Length; index++)
            {
                CreateRow(
                    friendRowRoot,
                    friendRows,
                    $"Friend_{index}",
                    PlaceholderFriends[index],
                    showLeader: false,
                    showKick: false,
                    showAdd: true);
            }
        }

        private RectTransform CreateRow(
            RectTransform parent,
            List<GameObject> bucket,
            string name,
            string nickname,
            bool showLeader,
            bool showKick,
            bool showAdd)
        {
            var row = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(parent, false);
            var element = row.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = RowHeight;
            element.minHeight = RowHeight;
            element.flexibleWidth = 1f;

            CreateAvatar(row);
            var nameLabel = CreateNickname(row, nickname);
            CreateLeader(row, nameLabel, showLeader);
            if (showKick)
            {
                CreateKick(row);
            }

            if (showAdd)
            {
                CreateAddButton(row);
            }

            bucket.Add(row.gameObject);
            return row;
        }

        private void CreateInfoRow(RectTransform parent, List<GameObject> bucket, string message)
        {
            var row = new GameObject("InfoRow", typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(parent, false);
            var element = row.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = RowHeight;
            element.minHeight = RowHeight;
            var label = CreateLabel(row, "Label", message, HomeUiFonts.ApplyRegular(), NicknameFontSize);
            Stretch(label.rectTransform, AvatarLeft, 0f, -ActionRight, 0f);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            bucket.Add(row.gameObject);
        }

        private static void CreateAvatar(RectTransform parent)
        {
            var avatar = new GameObject("Avatar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))
                .GetComponent<RectTransform>();
            avatar.SetParent(parent, false);
            PinLeft(avatar, AvatarLeft, new Vector2(AvatarSize, AvatarSize));
            var image = avatar.GetComponent<Image>();
            image.sprite = HomeUiFonts.CircleSprite;
            image.color = AvatarColor;
            image.raycastTarget = false;
            image.preserveAspect = true;
        }

        private static TextMeshProUGUI CreateNickname(RectTransform parent, string nickname)
        {
            var name = CreateLabel(parent, "Name", nickname, HomeUiFonts.ApplyRegular(), NicknameFontSize);
            name.alignment = TextAlignmentOptions.MidlineLeft;
            name.overflowMode = TextOverflowModes.Ellipsis;
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.ForceMeshUpdate();
            var size = new Vector2(
                Mathf.Max(name.preferredWidth, 1f),
                Mathf.Max(name.preferredHeight, NicknameFontSize));
            PinLeft(
                name.rectTransform,
                AvatarLeft + AvatarSize + NicknameLeft,
                size);
            return name;
        }

        private static void CreateLeader(RectTransform parent, TextMeshProUGUI name, bool showLeader)
        {
            var leader = new GameObject("Leader", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))
                .GetComponent<RectTransform>();
            leader.SetParent(parent, false);
            var nameRight = name.rectTransform.anchoredPosition.x + name.rectTransform.sizeDelta.x;
            PinLeft(leader, nameRight + LeaderIconGap, new Vector2(LeaderIconSize, LeaderIconSize));
            var icon = leader.GetComponent<Image>();
            icon.sprite = LobbyPlayerListSprites.Leader;
            icon.color = Color.white;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            leader.gameObject.SetActive(showLeader);
        }

        private static void CreateKick(RectTransform parent)
        {
            var kick = new GameObject("Kick", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI))
                .GetComponent<RectTransform>();
            kick.SetParent(parent, false);
            var label = kick.GetComponent<TextMeshProUGUI>();
            ApplyBody(label, "강퇴");
            label.color = KickColor;
            label.raycastTarget = true;
            label.alignment = TextAlignmentOptions.MidlineRight;
            label.ForceMeshUpdate();
            PinRight(
                kick,
                ActionRight,
                new Vector2(
                    Mathf.Max(label.preferredWidth, 1f),
                    Mathf.Max(label.preferredHeight, KickFontSize)));
            var button = kick.gameObject.AddComponent<Button>();
            button.targetGraphic = label;
            button.transition = Selectable.Transition.None;
        }

        private static void CreateAddButton(RectTransform parent)
        {
            var add = new GameObject("Add", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))
                .GetComponent<RectTransform>();
            add.SetParent(parent, false);
            PinRight(add, ActionRight, new Vector2(AddButtonSize, AddButtonSize));
            var image = add.GetComponent<Image>();
            image.sprite = LobbyPlayerListSprites.Plus;
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = true;
            var button = add.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
        }

        private static void PinLeft(RectTransform rect, float x, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(x, 0f);
            rect.sizeDelta = size;
        }

        private static void PinRight(RectTransform rect, float right, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-right, 0f);
            rect.sizeDelta = size;
        }

        private static void ApplyTitle(TextMeshProUGUI label, string text)
        {
            label.text = text;
            label.font = HomeUiFonts.ApplyBold();
            label.fontSize = TitleFontSize;
            label.fontStyle = FontStyles.Normal;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.margin = Vector4.zero;
        }

        private static void ApplyBody(TextMeshProUGUI label, string text)
        {
            label.text = text;
            label.font = HomeUiFonts.ApplyRegular();
            label.fontSize = KickFontSize;
            label.fontStyle = FontStyles.Normal;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Midline;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
        }

        private static TextMeshProUGUI CreateLabel(
            Transform parent,
            string name,
            string text,
            TMP_FontAsset font,
            float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.font = font != null ? font : HomeUiFonts.ApplyRegular();
            label.fontSize = fontSize;
            label.fontStyle = FontStyles.Normal;
            label.color = Color.white;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            return label;
        }

        private static void ClearRows(List<GameObject> rows)
        {
            for (var index = 0; index < rows.Count; index++)
            {
                if (rows[index] == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(rows[index]);
                }
                else
                {
                    DestroyImmediate(rows[index]);
                }
            }

            rows.Clear();
        }

        private static RectTransform FindOrCreateRect(string name, Transform parent)
        {
            var existing = parent.Find(name) as RectTransform;
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void Stretch(
            RectTransform rect,
            float left,
            float bottom,
            float right,
            float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(right, top);
        }
    }
}
