using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.Home
{
    /// <summary>
    /// The nickname panel: the search-allow toggle, the field with its
    /// duplicate check, and the apply button under them.
    /// </summary>
    /// <remarks>
    /// Kept apart from the rest of the screen because the rules are its own.
    /// What may be typed, how long it may be and when apply lights up are all
    /// decided here; the presenter is told only when the player asks for
    /// something that leaves the screen.
    /// </remarks>
    public sealed partial class HomeMenuView
    {
        /// <summary>
        /// Raised when the player asks whether the typed name is free.
        /// </summary>
        public event Action<string> NicknameDuplicateCheckRequested;

        /// <summary>
        /// Raised when the search-allow toggle is flipped, with its new state.
        /// </summary>
        public event Action<bool> NicknameSearchAllowedChanged;

        private void CreateProfileSettingsRoot(RectTransform canvas)
        {
            var root = CreateRect("ProfileSettingsRoot", canvas);
            root.gameObject.SetActive(false);
            SetAnchor(root, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            // The panel has no close button, so the rest of the screen is the
            // way out. Barely-there rather than fully clear: a transparent
            // graphic takes no clicks, and Home must not be dimmed behind it.
            var dismissRect = CreateRect("DismissArea", root);
            SetAnchor(dismissRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            dismissRect.offsetMin = Vector2.zero;
            dismissRect.offsetMax = Vector2.zero;
            var dismissImage = AddImage(
                dismissRect, new Color(0f, 0f, 0f, 0.01f), raycastTarget: true);
            var dismiss = dismissRect.gameObject.AddComponent<Button>();
            dismiss.targetGraphic = dismissImage;
            dismiss.transition = Selectable.Transition.None;
            dismiss.navigation = new Navigation { mode = Navigation.Mode.None };
            dismiss.onClick.AddListener(() => ProfileSettingsDismissed?.Invoke());
            menuButtons.Add(dismiss);

            var panel = CreateRect("ProfileSettingsPanel", root);
            SetAnchor(panel, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f));
            panel.anchoredPosition = new Vector2(
                -HomeStyle.Profile.PanelRightMargin, HomeStyle.Profile.PanelBottomMargin);
            panel.sizeDelta = HomeStyle.Profile.PanelSize;

            var fill = AddImage(
                panel,
                HomeStyle.Palette.PanelFill,
                HomeUiFonts.Rounded(HomeStyle.Radius.Panel, squareBottomRight: true),
                raycastTarget: true);
            fill.type = Image.Type.Sliced;
            fill.pixelsPerUnitMultiplier = 1f;

            CreateSearchAllowToggle(panel);
            CreateNicknameField(panel);
            CreateNicknameMessageRow(panel);
            CreateApplyButton(panel);

            profileSettingsRoot = root.gameObject;
            SetNicknameSearchAllowed(false);
            ClearNicknameMessage();
        }

        private void CreateSearchAllowToggle(RectTransform panel)
        {
            var label = CreateRect("SearchAllowLabel", panel);
            SetAnchor(label, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f));
            label.anchoredPosition = new Vector2(
                HomeStyle.Profile.SidePadding, HomeStyle.Profile.ToggleRowCentreY);
            label.sizeDelta = new Vector2(HomeStyle.Profile.ToggleLeft, 30f);
            var labelText = AddText(
                label,
                "닉네임 검색 허용",
                HomeStyle.FontSize.ToggleLabel,
                FontStyles.Normal,
                TextAlignmentOptions.MidlineLeft);
            ApplyMenuFont(labelText);
            labelText.color = HomeStyle.Palette.TextPrimary;

            var toggle = CreateRect("SearchAllowToggle", panel);
            SetAnchor(toggle, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f));
            toggle.anchoredPosition = new Vector2(
                HomeStyle.Profile.ToggleLeft, HomeStyle.Profile.ToggleRowCentreY);
            toggle.sizeDelta = HomeStyle.Profile.ToggleSize;

            searchAllowFill = AddImage(
                toggle,
                HomeStyle.Palette.ToggleOffFill,
                HomeUiFonts.PillSprite,
                raycastTarget: true);
            searchAllowFill.type = Image.Type.Sliced;
            searchAllowFill.pixelsPerUnitMultiplier = 1f;

            var strokeRect = CreateRect("Stroke", toggle);
            SetAnchor(strokeRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            strokeRect.offsetMin = Vector2.zero;
            strokeRect.offsetMax = Vector2.zero;
            var stroke = AddImage(
                strokeRect,
                HomeStyle.Palette.ToggleStroke,
                HomeUiFonts.Outline(
                    Mathf.RoundToInt(HomeStyle.Profile.ToggleSize.y * 0.5f),
                    HomeStyle.Profile.ToggleStrokeThickness));
            stroke.type = Image.Type.Sliced;
            stroke.pixelsPerUnitMultiplier = 1f;

            var knobDiameter = HomeStyle.Profile.ToggleSize.y
                - (HomeStyle.Profile.ToggleKnobInset * 2f);
            var knob = CreateRect("Knob", toggle);
            SetAnchor(knob, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
            knob.sizeDelta = new Vector2(knobDiameter, knobDiameter);
            searchAllowKnob = knob;
            searchAllowKnobImage = AddImage(
                knob, HomeStyle.Palette.ToggleOffKnob, HomeUiFonts.CircleSprite);

            var button = toggle.gameObject.AddComponent<Button>();
            button.targetGraphic = searchAllowFill;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(ToggleSearchAllowed);
            menuButtons.Add(button);
        }

        private void CreateNicknameField(RectTransform panel)
        {
            var field = CreateRect("NicknameField", panel);
            SetAnchor(field, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            field.anchoredPosition = new Vector2(
                HomeStyle.Profile.SidePadding, HomeStyle.Profile.InputTop);
            field.sizeDelta = HomeStyle.Profile.InputSize;

            var background = AddImage(
                field,
                HomeStyle.Palette.InputFill,
                HomeUiFonts.Rounded(HomeStyle.Radius.Input),
                raycastTarget: true);
            background.type = Image.Type.Sliced;
            background.pixelsPerUnitMultiplier = 1f;

            var viewport = CreateRect("TextArea", field);
            SetAnchor(viewport, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            viewport.offsetMin = new Vector2(HomeStyle.Profile.InputTextPadding, 4f);
            viewport.offsetMax = new Vector2(
                -(HomeStyle.Profile.CheckSize.x
                    + (HomeStyle.Profile.CheckRightInset * 2f)),
                -4f);
            viewport.gameObject.AddComponent<RectMask2D>();

            var textRect = CreateRect("Text", viewport);
            SetAnchor(textRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = AddText(
                textRect,
                string.Empty,
                HomeStyle.FontSize.NicknameInput,
                FontStyles.Normal,
                TextAlignmentOptions.MidlineLeft,
                raycastTarget: true);
            text.color = HomeStyle.Palette.TextPrimary;

            // TMP_InputField reads its parts as it wakes, so the object is kept
            // switched off until every one of them is in place.
            field.gameObject.SetActive(false);
            var input = field.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = viewport;
            input.textComponent = text;
            input.fontAsset = koreanFont;
            input.pointSize = HomeStyle.FontSize.NicknameInput;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.customCaretColor = true;
            input.caretColor = HomeStyle.Palette.TextPrimary;
            input.selectionColor = HomeStyle.Palette.Accent;

            // The limit is enforced below rather than by characterLimit, which
            // drops the extra keystroke without a word about why.
            input.characterLimit = 0;
            input.onValueChanged.AddListener(OnNicknameEdited);
            field.gameObject.SetActive(true);
            profileNicknameInput = input;

            CreateDuplicateCheckButton(field);
        }

        private void CreateDuplicateCheckButton(RectTransform field)
        {
            var check = CreateRect("DuplicateCheckButton", field);
            SetAnchor(check, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
            check.anchoredPosition = new Vector2(-HomeStyle.Profile.CheckRightInset, 0f);
            check.sizeDelta = HomeStyle.Profile.CheckSize;

            var fill = AddImage(
                check,
                HomeStyle.Palette.CheckFill,
                HomeUiFonts.Rounded(HomeStyle.Radius.Check),
                raycastTarget: true);
            fill.type = Image.Type.Sliced;
            fill.pixelsPerUnitMultiplier = 1f;

            var labelRect = CreateRect("Label", check);
            SetAnchor(labelRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var label = AddText(
                labelRect,
                "중복확인",
                HomeStyle.FontSize.Check,
                FontStyles.Normal,
                TextAlignmentOptions.Center);
            ApplyMenuFont(label);
            label.color = HomeStyle.Palette.CheckLabel;

            var button = check.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(OnDuplicateCheckClicked);
            menuButtons.Add(button);
        }

        /// <summary>
        /// The row under the field: why the name was refused on the left, how
        /// many characters have been typed on the right.
        /// </summary>
        private void CreateNicknameMessageRow(RectTransform panel)
        {
            var messageRect = CreateRect("NicknameMessage", panel);
            SetAnchor(messageRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            messageRect.anchoredPosition = new Vector2(
                HomeStyle.Profile.SidePadding, HomeStyle.Profile.MessageTop);
            messageRect.sizeDelta = new Vector2(
                HomeStyle.Profile.InputSize.x * 0.7f, HomeStyle.Profile.MessageHeight);
            nicknameMessageText = AddText(
                messageRect,
                string.Empty,
                HomeStyle.FontSize.Message,
                FontStyles.Normal,
                TextAlignmentOptions.MidlineLeft);

            var counterRect = CreateRect("NicknameCounter", panel);
            SetAnchor(counterRect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));
            counterRect.anchoredPosition = new Vector2(
                -HomeStyle.Profile.SidePadding, HomeStyle.Profile.MessageTop);
            counterRect.sizeDelta = new Vector2(120f, HomeStyle.Profile.MessageHeight);
            nicknameCounterText = AddText(
                counterRect,
                $"0/{HomeStyle.Profile.MaxNicknameLength}",
                HomeStyle.FontSize.Counter,
                FontStyles.Normal,
                TextAlignmentOptions.MidlineRight);
            nicknameCounterText.color = HomeStyle.Palette.Counter;
        }

        private void CreateApplyButton(RectTransform panel)
        {
            var apply = CreateRect("ApplyButton", panel);
            SetAnchor(apply, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            apply.anchoredPosition = new Vector2(
                HomeStyle.Profile.SidePadding, HomeStyle.Profile.ApplyTop);
            apply.sizeDelta = HomeStyle.Profile.ApplySize;

            applyFill = AddImage(
                apply,
                HomeStyle.Palette.ApplyOffFill,
                HomeUiFonts.Rounded(HomeStyle.Radius.Input),
                raycastTarget: true);
            applyFill.type = Image.Type.Sliced;
            applyFill.pixelsPerUnitMultiplier = 1f;

            var labelRect = CreateRect("Label", apply);
            SetAnchor(labelRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            applyLabel = AddText(
                labelRect,
                "적용하기",
                HomeStyle.FontSize.Apply,
                FontStyles.Normal,
                TextAlignmentOptions.Center);
            ApplyMenuFont(applyLabel);

            applyButton = apply.gameObject.AddComponent<Button>();
            applyButton.targetGraphic = applyFill;
            applyButton.transition = Selectable.Transition.None;
            applyButton.onClick.AddListener(OnChangeNicknameClicked);
            menuButtons.Add(applyButton);
            SetNicknameApplyEnabled(false);
        }

        /// <summary>
        /// Filters what was typed and reports on it, without letting anything
        /// the rules forbid stay in the field.
        /// </summary>
        /// <remarks>
        /// Rejecting by rewriting the field re-enters this handler, so the
        /// second pass is skipped rather than allowed to fight the first.
        /// </remarks>
        private void OnNicknameEdited(string value)
        {
            if (isRewritingNickname)
            {
                return;
            }

            var accepted = Filter(value, out var hadBadCharacter, out var wasTooLong);
            if (accepted != value)
            {
                isRewritingNickname = true;
                profileNicknameInput.text = accepted;
                profileNicknameInput.caretPosition = accepted.Length;
                isRewritingNickname = false;
            }

            if (hadBadCharacter)
            {
                ShowNicknameMessage(
                    HomeStyle.Profile.BadCharacterMessage, HomeStyle.Palette.MessageRejected);
            }
            else if (wasTooLong)
            {
                ShowNicknameMessage(
                    HomeStyle.Profile.TooLongMessage, HomeStyle.Palette.MessageRejected);
            }
            else
            {
                ClearNicknameMessage();
            }

            UpdateNicknameCounter(accepted);

            // Anything typed after a check invalidates it: what was found free
            // was the old spelling.
            SetNicknameApplyEnabled(false);
            NicknameEdited?.Invoke(accepted);
        }

        /// <summary>
        /// Keeps the Hangul, Latin letters and digits, up to the length limit.
        /// </summary>
        /// <remarks>
        /// Counted in characters rather than bytes, and Hangul syllables are
        /// one character each in UTF-16, so twelve of them is twelve here.
        /// </remarks>
        private static string Filter(string value, out bool hadBadCharacter, out bool wasTooLong)
        {
            hadBadCharacter = false;
            wasTooLong = false;
            var accepted = new StringBuilder(value.Length);

            foreach (var character in value)
            {
                if (!IsAllowed(character))
                {
                    hadBadCharacter = true;
                    continue;
                }

                if (accepted.Length == HomeStyle.Profile.MaxNicknameLength)
                {
                    wasTooLong = true;
                    continue;
                }

                accepted.Append(character);
            }

            return accepted.ToString();
        }

        private static bool IsAllowed(char character)
        {
            if (character >= '0' && character <= '9')
            {
                return true;
            }

            if ((character >= 'a' && character <= 'z') || (character >= 'A' && character <= 'Z'))
            {
                return true;
            }

            // Complete syllables, and the jamo an IME shows mid-composition.
            return (character >= '가' && character <= '힣')
                || (character >= 'ᄀ' && character <= 'ᇿ')
                || (character >= '㄰' && character <= '㆏');
        }

        private void OnDuplicateCheckClicked()
        {
            var nickname = profileNicknameInput != null
                ? profileNicknameInput.text
                : string.Empty;
            if (nickname.Length < HomeStyle.Profile.MinNicknameLength)
            {
                ShowNicknameMessage(
                    $"최소 {HomeStyle.Profile.MinNicknameLength}글자 이상 작성해주세요",
                    HomeStyle.Palette.MessageRejected);
                SetNicknameApplyEnabled(false);
                return;
            }

            NicknameDuplicateCheckRequested?.Invoke(nickname);
        }

        /// <summary>
        /// Answers the check: says whether the name is free and opens or shuts
        /// the apply button to match.
        /// </summary>
        public void SetNicknameAvailability(bool available)
        {
            ShowNicknameMessage(
                available ? HomeStyle.Profile.AvailableMessage : HomeStyle.Profile.TakenMessage,
                available
                    ? HomeStyle.Palette.MessageAccepted
                    : HomeStyle.Palette.MessageRejected);
            SetNicknameApplyEnabled(available);
        }

        public void SetNicknameSearchAllowed(bool allowed)
        {
            isSearchAllowed = allowed;
            if (searchAllowFill == null || searchAllowKnob == null)
            {
                return;
            }

            searchAllowFill.color = allowed
                ? HomeStyle.Palette.ToggleOnFill
                : HomeStyle.Palette.ToggleOffFill;
            searchAllowKnobImage.color = allowed
                ? HomeStyle.Palette.ToggleOnKnob
                : HomeStyle.Palette.ToggleOffKnob;

            var travel = HomeStyle.Profile.ToggleSize.x
                - searchAllowKnob.sizeDelta.x
                - (HomeStyle.Profile.ToggleKnobInset * 2f);
            searchAllowKnob.anchoredPosition = new Vector2(
                HomeStyle.Profile.ToggleKnobInset + (allowed ? travel : 0f), 0f);
        }

        private void ToggleSearchAllowed()
        {
            SetNicknameSearchAllowed(!isSearchAllowed);
            NicknameSearchAllowedChanged?.Invoke(isSearchAllowed);
        }

        private void SetNicknameApplyEnabled(bool enabled)
        {
            if (applyButton == null)
            {
                return;
            }

            applyButton.interactable = enabled;
            applyFill.color = enabled
                ? HomeStyle.Palette.ApplyOnFill
                : HomeStyle.Palette.ApplyOffFill;
            applyLabel.color = enabled
                ? HomeStyle.Palette.ApplyOnLabel
                : HomeStyle.Palette.ApplyOffLabel;
        }

        private void UpdateNicknameCounter(string value)
        {
            if (nicknameCounterText != null)
            {
                nicknameCounterText.text =
                    $"{value.Length}/{HomeStyle.Profile.MaxNicknameLength}";
            }
        }

        private void ShowNicknameMessage(string message, Color color)
        {
            if (nicknameMessageText == null)
            {
                return;
            }

            nicknameMessageText.text = message;
            nicknameMessageText.color = color;
        }

        private void ClearNicknameMessage()
        {
            ShowNicknameMessage(string.Empty, HomeStyle.Palette.MessageRejected);
        }

        private void OnChangeNicknameClicked()
        {
            NicknameChangeRequested?.Invoke(
                profileNicknameInput != null ? profileNicknameInput.text : string.Empty);
        }
    }
}
