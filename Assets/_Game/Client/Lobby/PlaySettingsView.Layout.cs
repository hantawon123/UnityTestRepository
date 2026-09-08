using System.Collections.Generic;
using Game.Client.Home;
using Game.Core.Lobby;
using Game.Core.Rooms;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.Lobby
{
    public sealed partial class PlaySettingsView
    {
        private bool layoutBuilt;
        private Text titleCounterText;
        private Text mapNameText;
        private Text categoryText;
        private Button mapPrevButton;
        private Button mapNextButton;
        private Button categoryPrevButton;
        private Button categoryNextButton;
        private Button applyButton;
        private Image mapPreviewImage;
        private RectTransform settingsContent;
        private ScrollRect bodyScroll;

        private int selectedCategoryIndex = PlaySettingsCategoryCatalog.DefaultIndex;

        private void EnsureLayout()
        {
            if (panel == null)
            {
                return;
            }

            if (layoutBuilt && IsCurrentLayout(panel.transform))
            {
                var content = panel.transform.Find("Body/SettingsScroll/Viewport/Content") as RectTransform;
                if (content != null)
                {
                    CacheScrollRefs(content);
                    CacheRoomCodeRefs(content);
                    CacheMapAreaRefs(content);
                    CacheMapScrollRefs();
                }

                return;
            }

            layoutBuilt = true;
            BuildLayout((RectTransform)panel.transform);
        }

        private static bool IsCurrentLayout(Transform root)
        {
            var stamp = root.Find("LayoutStamp");
            return stamp != null &&
                   stamp.TryGetComponent<PlaySettingsLayoutVersion>(out var marker) &&
                   marker.Version == PlaySettingsStyle.LayoutVersion;
        }

        private void CacheScrollRefs(RectTransform content)
        {
            settingsContent = content;
            var scrollTransform = panel.transform.Find("Body/SettingsScroll");
            bodyScroll = scrollTransform != null ? scrollTransform.GetComponent<ScrollRect>() : null;
        }

        private void CacheRoomCodeRefs(RectTransform content)
        {
            var valueTransform = FindDeepChild(content, "RoomCodeValue");
            if (valueTransform != null)
            {
                var text = valueTransform.GetComponentInChildren<Text>();
                if (text != null)
                {
                    roomCodeText = text;
                }

                var hitButton = valueTransform.GetComponent<Button>();
                if (hitButton != null)
                {
                    roomCodeHitButton = hitButton;
                }
            }

            var copyTransform = FindDeepChild(content, "CopyRoomCodeButton");
            if (copyTransform != null)
            {
                var button = copyTransform.GetComponent<Button>();
                if (button != null)
                {
                    copyRoomCodeButton = button;
                }

                var iconTransform = copyTransform.Find("Icon");
                if (iconTransform != null)
                {
                    var icon = iconTransform.GetComponent<Image>();
                    if (icon != null)
                    {
                        copyIconImage = icon;
                    }
                }
            }

            var feedbackTransform = FindDeepChild(content, "CopiedFeedback");
            if (feedbackTransform != null)
            {
                copyFeedbackRoot = feedbackTransform.gameObject;
                var text = feedbackTransform.GetComponent<Text>()
                    ?? feedbackTransform.GetComponentInChildren<Text>();
                if (text != null)
                {
                    copyFeedbackText = text;
                }
            }

            var shiftTransform = FindDeepChild(content, "CopiedFeedbackShift");
            if (shiftTransform != null)
            {
                copyFeedbackShift = shiftTransform.gameObject;
            }
        }

        private static Transform FindDeepChild(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindDeepChild(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private void CacheMapAreaRefs(RectTransform content)
        {
            var mapArea = FindDeepChild(content, "MapArea");
            if (mapArea == null)
            {
                return;
            }

            var mapName = FindDeepChild(mapArea, "MapName");
            if (mapName != null)
            {
                var text = mapName.GetComponent<Text>() ?? mapName.GetComponentInChildren<Text>();
                if (text != null)
                {
                    mapNameText = text;
                }
            }

            var categoryValue = FindDeepChild(mapArea, "CategoryValue");
            if (categoryValue != null)
            {
                var text = categoryValue.GetComponent<Text>() ?? categoryValue.GetComponentInChildren<Text>();
                if (text != null)
                {
                    categoryText = text;
                }
            }

            var categoryPrev = FindDeepChild(mapArea, "CategoryPrev");
            if (categoryPrev != null)
            {
                var button = categoryPrev.GetComponent<Button>();
                if (button != null)
                {
                    categoryPrevButton = button;
                }
            }

            var categoryNext = FindDeepChild(mapArea, "CategoryNext");
            if (categoryNext != null)
            {
                var button = categoryNext.GetComponent<Button>();
                if (button != null)
                {
                    categoryNextButton = button;
                }
            }
        }

        private void RebuildSettingsScrollLayout()
        {
            if (settingsContent == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(settingsContent);
            if (bodyScroll != null)
            {
                bodyScroll.verticalNormalizedPosition = 1f;
            }
        }

        private void BuildLayout(RectTransform root)
        {
            ruleValues.Clear();
            ruleMinus.Clear();
            rulePlus.Clear();
            mapSlotImages.Clear();
            mapSlotButtons.Clear();
            mapScroll = null;
            mapContent = null;

            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            var panelImage = root.GetComponent<Image>() ?? root.gameObject.AddComponent<Image>();
            panelImage.sprite = HomeUiFonts.Rounded(PlaySettingsStyle.PanelRadius);
            panelImage.type = Image.Type.Sliced;
            panelImage.color = PlaySettingsStyle.Palette.PanelFill;
            panelImage.raycastTarget = true;

            var stroke = CreateRect("Stroke", root);
            Stretch(stroke);
            var strokeImage = stroke.gameObject.AddComponent<Image>();
            strokeImage.sprite = HomeUiFonts.Outline(PlaySettingsStyle.PanelRadius, PlaySettingsStyle.BorderWidth);
            strokeImage.type = Image.Type.Sliced;
            strokeImage.color = PlaySettingsStyle.Palette.Border;
            strokeImage.raycastTarget = false;

            root.sizeDelta = PlaySettingsStyle.ModalSize;
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = Vector2.zero;

            var header = CreateRect("Header", root);
            Anchor(header, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
            header.offsetMin = new Vector2(0f, -PlaySettingsStyle.HeaderHeight);
            header.offsetMax = Vector2.zero;
            CreateModalTitle(header, "게임 설정");

            var footer = CreateRect("Footer", root);
            Anchor(footer, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));
            footer.offsetMin = Vector2.zero;
            footer.offsetMax = new Vector2(0f, PlaySettingsStyle.FooterHeight);
            applyButton = CreateApplyButton(footer);

            var body = CreateRect("Body", root);
            Anchor(body, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            body.offsetMin = new Vector2(0f, PlaySettingsStyle.FooterHeight);
            body.offsetMax = new Vector2(0f, -PlaySettingsStyle.HeaderHeight);
            BuildBody(body);

            header.SetAsLastSibling();
            footer.SetAsLastSibling();
            stroke.SetSiblingIndex(0);

            var stamp = CreateRect("LayoutStamp", root);
            stamp.gameObject.SetActive(false);
            stamp.gameObject.AddComponent<PlaySettingsLayoutVersion>().Version =
                PlaySettingsStyle.LayoutVersion;
        }

        private void BuildBody(RectTransform body)
        {
            var scrollArea = CreateRect("SettingsScroll", body);
            Stretch(scrollArea);
            BuildSettingsScroll(scrollArea);
        }

        private void BuildSettingsScroll(RectTransform scrollArea)
        {
            bodyScroll = scrollArea.gameObject.AddComponent<ScrollRect>();
            bodyScroll.horizontal = false;
            bodyScroll.vertical = true;
            bodyScroll.movementType = ScrollRect.MovementType.Clamped;
            bodyScroll.scrollSensitivity = 30f;

            var viewport = CreateRect("Viewport", scrollArea);
            Stretch(viewport);
            var viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = Color.clear;
            viewport.gameObject.AddComponent<RectMask2D>();
            bodyScroll.viewport = viewport;

            settingsContent = CreateRect("Content", viewport);
            settingsContent.anchorMin = new Vector2(0f, 1f);
            settingsContent.anchorMax = new Vector2(1f, 1f);
            settingsContent.pivot = new Vector2(0.5f, 1f);
            settingsContent.anchoredPosition = Vector2.zero;
            settingsContent.sizeDelta = new Vector2(0f, 0f);
            bodyScroll.content = settingsContent;

            var layout = settingsContent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = PlaySettingsStyle.RowSpacing;
            layout.padding = new RectOffset(
                (int)PlaySettingsStyle.SidePadding,
                (int)PlaySettingsStyle.SidePadding,
                (int)PlaySettingsStyle.SidePadding,
                (int)PlaySettingsStyle.SidePadding);

            var fitter = settingsContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var mapArea = CreateLayoutRow(settingsContent, PlaySettingsStyle.MapSectionHeight);
            mapArea.name = "MapArea";
            BuildMapSection(mapArea);
            AddLayoutDivider(settingsContent);
            AddSectionTitle(settingsContent, "방 설정");
            BuildTitleRow(settingsContent);
            BuildRoomCodeRow(settingsContent);
            BuildCounterRow(settingsContent, "인원 설정", out maxPlayersMinusButton, out maxPlayersText,
                out maxPlayersPlusButton);
            BuildCounterRow(settingsContent, "파괴 기능 횟수", out destructionMinusButton, out destructionLimitText,
                out destructionPlusButton);
            BuildRuleRow(settingsContent, "숨기는 시간", 0);
            BuildRuleRow(settingsContent, "찾는 시간", 1);
            BuildRuleRow(settingsContent, "달리는 속도", 2);
            BuildRuleRow(settingsContent, "기절 펀치 횟수", 3);
        }

        private void AddLayoutDivider(RectTransform parent)
        {
            var divider = CreateLayoutRow(parent, 1f);
            divider.name = "SectionDivider";
            divider.gameObject.AddComponent<Image>().color = PlaySettingsStyle.Palette.Divider;
        }

        private RectTransform CreateLayoutRow(RectTransform parent, float height)
        {
            var row = CreateRect("SettingRow", parent);
            var element = row.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
            element.flexibleHeight = 0f;
            row.anchorMin = new Vector2(0f, 1f);
            row.anchorMax = new Vector2(1f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.sizeDelta = new Vector2(0f, height);
            return row;
        }

        private void AddSectionTitle(RectTransform parent, string title)
        {
            var row = CreateLayoutRow(parent, PlaySettingsStyle.Layout.SectionTitleHeight);
            row.name = "SectionTitleRow";

            var label = row.gameObject.AddComponent<Text>();
            label.text = title;
            label.font = ExtraBoldFont();
            label.fontSize = PlaySettingsStyle.FontSize.SectionTitle;
            label.color = PlaySettingsStyle.Palette.Text;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            ApplySingleLine(label);
            Stretch(label.rectTransform);
        }

        private void BuildTitleRow(RectTransform parent)
        {
            var row = CreateLayoutRow(parent, PlaySettingsStyle.RowHeight);
            CreateBodyText(row, "방 제목", new Vector2(0f, 0f),
                new Vector2(PlaySettingsStyle.Layout.LabelAreaRatio, 1f), Vector2.zero, Vector2.zero);

            var field = CreateRect("TitleField", row);
            field.anchorMin = new Vector2(PlaySettingsStyle.Layout.LabelAreaRatio, 0f);
            field.anchorMax = new Vector2(1f, 1f);
            field.offsetMin = Vector2.zero;
            field.offsetMax = Vector2.zero;

            var underline = CreateRect("Underline", field);
            Anchor(underline, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));
            underline.sizeDelta = new Vector2(0f, 1f);
            underline.anchoredPosition = new Vector2(0f, 6f);
            underline.gameObject.AddComponent<Image>().color = PlaySettingsStyle.Palette.Underline;

            titleCounterText = CreateBodyText(field, "0/20", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-4f, -14f), new Vector2(80f, 28f));
            titleCounterText.alignment = TextAnchor.MiddleRight;
            titleCounterText.fontSize = PlaySettingsStyle.FontSize.Counter;

            var inputRect = CreateRect("TitleInput", field);
            Anchor(inputRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f));
            inputRect.offsetMin = new Vector2(0f, 12f);
            inputRect.offsetMax = new Vector2(-90f, 0f);
            var inputBg = inputRect.gameObject.AddComponent<Image>();
            inputBg.color = Color.clear;
            titleText = CreateBodyText(inputRect, string.Empty, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            titleText.alignment = TextAnchor.MiddleLeft;
            titleInput = inputRect.gameObject.AddComponent<InputField>();
            titleInput.textComponent = titleText;
            titleInput.targetGraphic = inputBg;
            titleInput.characterLimit = RoomSettings.MaxTitleLength;
        }

        private void BuildRoomCodeRow(RectTransform parent)
        {
            var row = CreateLayoutRow(parent, PlaySettingsStyle.RowHeight);
            row.name = "RoomCodeRow";

            var roomCodeLabel = CreateBodyText(row, "방 코드", new Vector2(0f, 0f),
                new Vector2(PlaySettingsStyle.Layout.LabelAreaRatio, 1f), Vector2.zero, Vector2.zero);
            ApplySingleLine(roomCodeLabel);

            var group = CreateRect("RoomCodeControls", row);
            group.anchorMin = new Vector2(PlaySettingsStyle.Layout.LabelAreaRatio, 0f);
            group.anchorMax = Vector2.one;
            group.offsetMin = new Vector2(12f, 0f);
            group.offsetMax = new Vector2(-4f, 0f);

            var layout = group.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = PlaySettingsStyle.Layout.RoomCodeControlSpacing;

            var spacer = CreateRect("Spacer", group);
            var spacerElement = spacer.gameObject.AddComponent<LayoutElement>();
            spacerElement.flexibleWidth = 1f;
            spacerElement.minWidth = 0f;

            var feedbackRect = CreateRect("CopiedFeedback", group);
            var feedbackElement = feedbackRect.gameObject.AddComponent<LayoutElement>();
            feedbackElement.preferredWidth = PlaySettingsStyle.Layout.CopiedFeedbackWidth;
            feedbackElement.preferredHeight = PlaySettingsStyle.RowHeight;
            feedbackElement.minWidth = PlaySettingsStyle.Layout.CopiedFeedbackWidth;
            copyFeedbackText = CreateBodyText(
                feedbackRect,
                "복사되었습니다!",
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            copyFeedbackText.alignment = TextAnchor.MiddleRight;
            copyFeedbackText.color = PlaySettingsStyle.Palette.ApplyFill;
            ApplySingleLine(copyFeedbackText);
            copyFeedbackRoot = feedbackRect.gameObject;
            feedbackRect.gameObject.SetActive(false);

            var feedbackShift = CreateRect("CopiedFeedbackShift", group);
            var shiftElement = feedbackShift.gameObject.AddComponent<LayoutElement>();
            shiftElement.preferredWidth = PlaySettingsStyle.Layout.CopiedFeedbackShift;
            shiftElement.minWidth = PlaySettingsStyle.Layout.CopiedFeedbackShift;
            shiftElement.preferredHeight = 1f;
            feedbackShift.gameObject.SetActive(false);
            copyFeedbackShift = feedbackShift.gameObject;

            var valueRect = CreateRect("RoomCodeValue", group);
            var valueElement = valueRect.gameObject.AddComponent<LayoutElement>();
            valueElement.preferredWidth = PlaySettingsStyle.Layout.RoomCodeValueWidth;
            valueElement.preferredHeight = PlaySettingsStyle.RowHeight;

            roomCodeText = CreateBodyText(valueRect, string.Empty, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero);
            roomCodeText.alignment = TextAnchor.MiddleRight;
            roomCodeText.raycastTarget = false;
            ApplySingleLine(roomCodeText);

            var valueHit = valueRect.gameObject.AddComponent<Image>();
            valueHit.color = Color.clear;
            roomCodeHitButton = valueRect.gameObject.AddComponent<Button>();
            roomCodeHitButton.targetGraphic = valueHit;

            var copySize = PlaySettingsStyle.Layout.CopyIconSize;
            var copyRect = CreateRect("CopyRoomCodeButton", group);
            var copyElement = copyRect.gameObject.AddComponent<LayoutElement>();
            copyElement.preferredWidth = copySize;
            copyElement.preferredHeight = copySize;
            copyElement.minWidth = copySize;
            copyElement.minHeight = copySize;

            var copyHit = copyRect.gameObject.AddComponent<Image>();
            copyHit.color = Color.clear;
            copyRoomCodeButton = copyRect.gameObject.AddComponent<Button>();
            copyRoomCodeButton.targetGraphic = copyHit;

            var iconRect = CreateRect("Icon", copyRect);
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(copySize, copySize);
            var icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = LoadCopyIcon();
            icon.color = Color.white;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            copyIconImage = icon;
        }

        private void BuildCounterRow(
            RectTransform parent,
            string label,
            out Button minus,
            out Text value,
            out Button plus)
        {
            var row = CreateLayoutRow(parent, PlaySettingsStyle.RowHeight);
            CreateBodyText(row, label, new Vector2(0f, 0f),
                new Vector2(PlaySettingsStyle.Layout.LabelAreaRatio, 1f), Vector2.zero, Vector2.zero);
            BuildControlGroup(row, out minus, out value, out plus);
        }

        private void BuildRuleRow(RectTransform parent, string label, int ruleIndex)
        {
            var row = CreateLayoutRow(parent, PlaySettingsStyle.RowHeight);
            CreateBodyText(row, label, new Vector2(0f, 0f),
                new Vector2(PlaySettingsStyle.Layout.LabelAreaRatio, 1f), Vector2.zero, Vector2.zero);
            BuildControlGroup(row, out var minus, out var value, out var plus);
            ruleMinus.Add(minus);
            rulePlus.Add(plus);
            ruleValues.Add(value);
        }

        private void BuildControlGroup(
            RectTransform row,
            out Button minus,
            out Text value,
            out Button plus)
        {
            var group = CreateRect("Controls", row);
            group.anchorMin = new Vector2(PlaySettingsStyle.Layout.LabelAreaRatio, 0f);
            group.anchorMax = Vector2.one;
            group.offsetMin = new Vector2(12f, 0f);
            group.offsetMax = new Vector2(-4f, 0f);

            var layout = group.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = PlaySettingsStyle.Layout.ControlSpacing;

            minus = CreateLayoutArrowButton(group, isLeft: true);
            value = CreateLayoutValueText(group);
            plus = CreateLayoutArrowButton(group, isLeft: false);
        }

        private Button CreateLayoutArrowButton(RectTransform parent, bool isLeft)
        {
            var size = PlaySettingsStyle.Layout.ArrowSize;
            var rect = CreateRect(isLeft ? "Prev" : "Next", parent);
            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = size;
            element.preferredHeight = size;
            element.minWidth = size;
            element.minHeight = size;

            var hit = rect.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;

            var iconRect = CreateRect("Icon", rect);
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(size, size);
            var icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = isLeft ? LoadArrowLeftIcon() : LoadArrowRightIcon();
            icon.color = Color.white;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            return button;
        }

        private Text CreateLayoutValueText(RectTransform parent)
        {
            var rect = CreateRect("Value", parent);
            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = PlaySettingsStyle.Layout.ControlValueWidth;
            element.minWidth = PlaySettingsStyle.Layout.ControlValueWidth;

            var label = rect.gameObject.AddComponent<Text>();
            label.text = "0";
            label.font = BodyFont();
            label.fontSize = PlaySettingsStyle.FontSize.Body;
            label.color = PlaySettingsStyle.Palette.Text;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            ApplySingleLine(label);
            return label;
        }

        private void BuildMapSection(RectTransform parent)
        {
            ConfigureVerticalGroup(parent, TextAnchor.UpperCenter, PlaySettingsStyle.Layout.MapColumnSpacing);

            var titleRow = CreateLayoutRow(parent, PlaySettingsStyle.Layout.SectionTitleHeight);
            titleRow.name = "SectionTitles";
            CreateCenteredText(
                CreateHalf(titleRow, "MapTitle", 0f, 0.5f),
                "맵 선택",
                PlaySettingsStyle.FontSize.Body,
                PlaySettingsStyle.Palette.Text);
            CreateCenteredText(
                CreateHalf(titleRow, "CategoryTitle", 0.5f, 1f),
                "카테고리 선택",
                PlaySettingsStyle.FontSize.Body,
                PlaySettingsStyle.Palette.Text);

            var pickerHeight = PlaySettingsStyle.Layout.MapPreviewSize.y;
            var pickerRow = CreateLayoutRow(parent, pickerHeight);
            pickerRow.name = "Pickers";

            var mapPicker = CreateHorizontalPickerRow(CreateHalf(pickerRow, "MapSelect", 0f, 0.5f), pickerHeight);
            Stretch(mapPicker);
            mapPrevButton = CreateLayoutArrowButton(mapPicker, isLeft: true);
            mapPreviewImage = CreateMapPreviewImage(mapPicker);
            mapNextButton = CreateLayoutArrowButton(mapPicker, isLeft: false);

            var categoryPicker = CreateHorizontalPickerRow(
                CreateHalf(pickerRow, "CategorySelect", 0.5f, 1f),
                pickerHeight);
            Stretch(categoryPicker);
            categoryPrevButton = CreateLayoutArrowButton(categoryPicker, isLeft: true);
            categoryPrevButton.gameObject.name = "CategoryPrev";
            categoryText = CreatePickerValueText(
                categoryPicker,
                PlaySettingsCategoryCatalog.Default.Label,
                pickerHeight);
            categoryNextButton = CreateLayoutArrowButton(categoryPicker, isLeft: false);
            categoryNextButton.gameObject.name = "CategoryNext";

            var nameRow = CreateLayoutRow(parent, PlaySettingsStyle.FontSize.MapName + 8f);
            nameRow.name = "MapNameRow";
            mapNameText = CreateCenteredText(
                CreateHalf(nameRow, "MapName", 0f, 0.5f),
                string.Empty,
                PlaySettingsStyle.FontSize.MapName,
                PlaySettingsStyle.Palette.Text);
        }

        private static RectTransform CreateHalf(RectTransform parent, string name, float minX, float maxX)
        {
            var rect = CreateRect(name, parent);
            rect.anchorMin = new Vector2(minX, 0f);
            rect.anchorMax = new Vector2(maxX, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static void ConfigureVerticalGroup(RectTransform rect, TextAnchor alignment, float spacing)
        {
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = alignment;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = spacing;
        }

        private static RectTransform CreateHorizontalPickerRow(RectTransform parent, float height)
        {
            var row = CreateRect("PickerRow", parent);
            var element = row.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = PlaySettingsStyle.Layout.PickerSpacing;
            return row;
        }

        private static Text CreatePickerValueText(RectTransform parent, string text, float rowHeight)
        {
            var row = CreateRect("CategoryValue", parent);
            var element = row.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = rowHeight;
            element.minHeight = rowHeight;
            element.minWidth = PlaySettingsStyle.Layout.CategoryValueMinWidth;
            element.preferredWidth = PlaySettingsStyle.Layout.CategoryValueMinWidth;

            var labelRect = CreateRect("Text", row);
            Stretch(labelRect);
            var label = labelRect.gameObject.AddComponent<Text>();
            label.text = text;
            label.font = BodyFont();
            label.fontSize = PlaySettingsStyle.FontSize.Body;
            label.color = PlaySettingsStyle.Palette.Text;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            ApplySingleLine(label);
            return label;
        }

        private static Text CreateCenteredText(RectTransform parent, string text, int fontSize, Color color)
        {
            Stretch(parent);
            var label = parent.gameObject.AddComponent<Text>();
            label.text = text;
            label.font = BodyFont();
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            ApplySingleLine(label);
            return label;
        }

        private Image CreateMapPreviewImage(RectTransform parent)
        {
            var preview = CreateRect("MapPreview", parent);
            var element = preview.gameObject.AddComponent<LayoutElement>();
            var size = PlaySettingsStyle.Layout.MapPreviewSize;
            element.preferredWidth = size.x;
            element.preferredHeight = size.y;
            element.minWidth = size.x;
            element.minHeight = size.y;
            preview.sizeDelta = size;
            var image = preview.gameObject.AddComponent<Image>();
            image.sprite = HomeUiFonts.Rounded(16);
            image.type = Image.Type.Sliced;
            image.color = PlaySettingsStyle.Palette.MapPreview;
            image.preserveAspect = false;
            image.raycastTarget = false;
            return image;
        }

        private Button CreateApplyButton(RectTransform footer)
        {
            var paddingX = PlaySettingsStyle.ApplyPaddingHorizontal;
            var paddingY = PlaySettingsStyle.ApplyPaddingVertical;
            var fontSize = PlaySettingsStyle.FontSize.Apply;

            var rect = CreateRect("ApplyButton", footer);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            var fill = rect.gameObject.AddComponent<Image>();
            fill.type = Image.Type.Sliced;
            fill.color = PlaySettingsStyle.Palette.ApplyFill;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;

            var labelRect = CreateRect("Text", rect);
            Stretch(labelRect);

            var label = labelRect.gameObject.AddComponent<Text>();
            label.text = "적용하기";
            label.font = MediumFont();
            label.fontSize = fontSize;
            label.color = PlaySettingsStyle.Palette.Text;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;

            var textWidth = Mathf.Max(label.preferredWidth, 128f);
            var textHeight = Mathf.Max(label.preferredHeight, fontSize);
            var height = textHeight + (paddingY * 2f);
            rect.sizeDelta = new Vector2(textWidth + (paddingX * 2f), height);
            rect.anchoredPosition = Vector2.zero;

            var radius = Mathf.Min(
                PlaySettingsStyle.ApplyButtonRadius,
                Mathf.Max(8, Mathf.FloorToInt((height * 0.5f) - 1f)));
            fill.sprite = HomeUiFonts.Rounded(radius);
            return button;
        }

        private void CreateModalTitle(RectTransform parent, string text)
        {
            var rect = CreateRect("Title", parent);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -PlaySettingsStyle.TitleTopPadding);
            rect.sizeDelta = new Vector2(0f, PlaySettingsStyle.FontSize.Header + 8f);

            var label = rect.gameObject.AddComponent<Text>();
            label.text = text;
            label.font = HeaderFont();
            label.fontSize = PlaySettingsStyle.FontSize.Header;
            label.color = PlaySettingsStyle.Palette.Text;
            label.alignment = TextAnchor.UpperCenter;
            label.raycastTarget = false;
        }

        private Text CreateBodyText(
            RectTransform parent,
            string text,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 sizeOrMax)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            if (sizeOrMax == Vector2.zero && offsetMin == Vector2.zero)
            {
                Stretch(rect);
            }
            else if (sizeOrMax.y > 0f && anchorMin == anchorMax)
            {
                rect.pivot = new Vector2(anchorMin.x, 0.5f);
                rect.anchoredPosition = offsetMin;
                rect.sizeDelta = sizeOrMax;
            }
            else
            {
                rect.offsetMin = offsetMin;
                rect.offsetMax = sizeOrMax;
            }

            var label = go.AddComponent<Text>();
            label.text = text;
            label.font = BodyFont();
            label.fontSize = PlaySettingsStyle.FontSize.Body;
            label.color = PlaySettingsStyle.Palette.Text;
            label.alignment = TextAnchor.MiddleLeft;
            label.raycastTarget = false;
            ApplySingleLine(label);
            return label;
        }

        private static void ApplySingleLine(Text label)
        {
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private static Font bodyFont;
        private static Font headerFont;
        private static Font extraBoldFont;
        private static Font mediumFont;
        private static Sprite copyIcon;
        private static Sprite copyCheckIcon;
        private static Sprite arrowLeftIcon;
        private static Sprite arrowRightIcon;

        private static Font BodyFont() =>
            bodyFont ??= Resources.Load<Font>(PlaySettingsStyle.RegularFontResource);

        private static Font HeaderFont() =>
            headerFont ??= Resources.Load<Font>(PlaySettingsStyle.HeaderFontResource)
            ?? BodyFont();

        private static Font ExtraBoldFont() =>
            extraBoldFont ??= Resources.Load<Font>(PlaySettingsStyle.GameStartFontResource)
            ?? HeaderFont();

        private static Font MediumFont()
        {
            if (mediumFont != null)
            {
                return mediumFont;
            }

            mediumFont = Resources.Load<Font>(PlaySettingsStyle.MediumFontResource);
#if UNITY_EDITOR
            if (mediumFont == null)
            {
                mediumFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(
                    "Assets/_Game/Content/Fonts/Paperlogy-5Medium.ttf");
            }
#endif
            return mediumFont ?? BodyFont();
        }

        private static Sprite LoadCopyIcon()
        {
            if (copyIcon != null)
            {
                return copyIcon;
            }

            copyIcon = Resources.Load<Sprite>(PlaySettingsStyle.CopyIconResource);
            if (copyIcon != null)
            {
                return copyIcon;
            }

            var texture = new Texture2D(24, 24, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear
            };

            for (var y = 0; y < 24; y++)
            {
                for (var x = 0; x < 24; x++)
                {
                    var onBack = x >= 4 && x <= 13 && y >= 8 && y <= 17;
                    var onFront = x >= 10 && x <= 19 && y >= 2 && y <= 11;
                    var border = onBack || onFront;
                    if (onFront && onBack)
                    {
                        border = x >= 10 && x <= 13 && y >= 8 && y <= 11;
                    }

                    texture.SetPixel(x, y, border ? Color.white : Color.clear);
                }
            }

            texture.Apply(false, false);
            copyIcon = Sprite.Create(texture, new Rect(0f, 0f, 24f, 24f), new Vector2(0.5f, 0.5f), 100f);
            copyIcon.hideFlags = HideFlags.HideAndDontSave;
            return copyIcon;
        }

        private static Sprite LoadCopyCheckIcon()
        {
            if (copyCheckIcon != null)
            {
                return copyCheckIcon;
            }

            copyCheckIcon = Resources.Load<Sprite>(PlaySettingsStyle.CopyCheckIconResource);
            return copyCheckIcon;
        }

        private static Sprite LoadArrowLeftIcon() =>
            arrowLeftIcon ??= Resources.Load<Sprite>(PlaySettingsStyle.ArrowLeftIconResource);

        private static Sprite LoadArrowRightIcon() =>
            arrowRightIcon ??= Resources.Load<Sprite>(PlaySettingsStyle.ArrowRightIconResource);

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Anchor(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
        }
    }
}
