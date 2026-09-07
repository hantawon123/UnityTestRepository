using System;
using System.Collections.Generic;
using Game.Client.Home;
using Game.Core.Lobby;
using Game.Core.Rooms;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.Lobby
{
    public interface IPlaySettingsView
    {
        event Action OpenRequested;
        event Action CloseRequested;
        event Action CopyRoomCodeRequested;
        event Action InviteRequested;
        event Action CopyPasswordRequested;
        event Action SaveTitleRequested;

        void SetVisible(bool visible);
        void SetEditable(bool editable);
        void SetDraft(PlaySettingsDraft draft);
        PlaySettingsDraft ReadDraft();

        /// <summary>
        /// Asks to be closed as if the panel's own close button was pressed.
        /// See <see cref="IKeyGuideView.RequestClose"/> for why this goes
        /// through the presenter rather than hiding the panel directly.
        /// </summary>
        void RequestClose();

        /// <summary>
        /// Asks to be opened as if the panel's own open button was pressed, so
        /// the presenter fills the draft and decides editability before the
        /// panel shows. Used by objects in the room that lead to this screen.
        /// </summary>
        void RequestOpen();
    }

    public sealed class PlaySettingsView : MonoBehaviour, IPlaySettingsView
    {
        private const float MapSlotSize = 90f;
        private const float MapSlotSpacing = 12f;

        [SerializeField]
        private Button openButton;

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private Button copyRoomCodeButton;

        [SerializeField]
        private Button inviteButton;

        [SerializeField]
        private Button copyPasswordButton;

        [SerializeField]
        private GameObject panel;

        [SerializeField]
        private Text titleText;
        private InputField titleInput;
        private Button saveTitleButton;
        private string savedTitle = string.Empty;

        [SerializeField]
        private Text roomCodeText;

        [SerializeField]
        private Text passwordMaskedText;

        [SerializeField]
        private Text maxPlayersText;

        [SerializeField]
        private Button maxPlayersMinusButton;

        [SerializeField]
        private Button maxPlayersPlusButton;

        [SerializeField]
        private Text destructionLimitText;

        [SerializeField]
        private Button destructionMinusButton;

        [SerializeField]
        private Button destructionPlusButton;

        [SerializeField]
        private Text mapNameText;

        [SerializeField]
        private Button mapPrevButton;

        [SerializeField]
        private Button mapNextButton;

        [SerializeField]
        private ScrollRect mapScroll;

        [SerializeField]
        private RectTransform mapContent;

        private static readonly float[] SprintOptions = { 0.5f, 1f, 1.5f, 2f, 3f };
        private readonly List<Text> ruleValues = new();
        private readonly List<Button> ruleMinus = new();
        private readonly List<Button> rulePlus = new();

        private string title = string.Empty;
        private string roomCode = string.Empty;
        private bool passwordEnabled;
        private string password = string.Empty;
        private int maxPlayers = RoomSettings.MaxPlayerCount;
        private int destructionLimit = PlaySettingsDraft.DefaultDestructionLimit;
        private int selectedMapIndex;
        private bool editable;
        private MatchRuleSettings matchRules = MatchRuleSettings.Default;
        private IReadOnlyList<LobbyMapOption> maps = LobbyMapCatalog.Maps;
        private readonly List<Image> mapSlotImages = new();
        private readonly List<Button> mapSlotButtons = new();
        private readonly Dictionary<Button, UnityEngine.Events.UnityAction> boundActions = new();

        public event Action OpenRequested;
        public event Action CloseRequested;
        public event Action CopyRoomCodeRequested;
        public event Action InviteRequested;
        public event Action CopyPasswordRequested;
        public event Action SaveTitleRequested;

        private void OnEnable()
        {
            EnsureRuleControls();
            BindRuleControls();
            EnsureTitleInput();
            if (titleInput != null) titleInput.onValueChanged.AddListener(OnTitleChanged);
            Bind(saveTitleButton, () => SaveTitleRequested?.Invoke());
            HomeUiFonts.ApplyLegacy(panel != null ? panel.transform : transform);
            Bind(openButton, () => OpenRequested?.Invoke());
            Bind(closeButton, RequestClose);
            Bind(copyRoomCodeButton, () => CopyRoomCodeRequested?.Invoke());
            Bind(inviteButton, () => InviteRequested?.Invoke());
            Bind(copyPasswordButton, () => CopyPasswordRequested?.Invoke());
            Bind(maxPlayersMinusButton, () => SetMaxPlayers(maxPlayers - 1));
            Bind(maxPlayersPlusButton, () => SetMaxPlayers(maxPlayers + 1));
            Bind(destructionMinusButton, () => SetDestructionLimit(destructionLimit - 1));
            Bind(destructionPlusButton, () => SetDestructionLimit(destructionLimit + 1));
            Bind(mapPrevButton, () => ScrollMaps(-1));
            Bind(mapNextButton, () => ScrollMaps(1));
            BindMapSlots();
        }

        private void OnDisable()
        {
            if (titleInput != null) titleInput.onValueChanged.RemoveListener(OnTitleChanged);
            foreach (var button in ruleMinus) Unbind(button);
            foreach (var button in rulePlus) Unbind(button);
            Unbind(saveTitleButton);
            Unbind(openButton);
            Unbind(closeButton);
            Unbind(copyRoomCodeButton);
            Unbind(inviteButton);
            Unbind(copyPasswordButton);
            Unbind(maxPlayersMinusButton);
            Unbind(maxPlayersPlusButton);
            Unbind(destructionMinusButton);
            Unbind(destructionPlusButton);
            Unbind(mapPrevButton);
            Unbind(mapNextButton);
            UnbindMapSlots();
        }

        public void SetVisible(bool visible)
        {
            if (panel != null)
            {
                panel.SetActive(visible);
            }
        }

        public void RequestOpen() => OpenRequested?.Invoke();

        public void RequestClose()
        {
            if (!editable || RoomSettings.IsValidTitle(ReadDraft().Title)) CloseRequested?.Invoke();
        }

        public void SetEditable(bool value)
        {
            editable = value;
            if (titleInput != null) titleInput.interactable = value;
            RefreshTitleSave();
            RefreshCounters();
            foreach (var button in mapSlotButtons)
                if (button != null) button.interactable = editable;
        }

        public void SetDraft(PlaySettingsDraft draft)
        {
            title = draft.Title;
            savedTitle = title;
            if (titleInput != null) titleInput.SetTextWithoutNotify(title);
            RefreshTitleSave();
            roomCode = draft.RoomCode;
            passwordEnabled = draft.PasswordEnabled;
            password = draft.Password ?? string.Empty;
            matchRules = draft.MatchRules;
            maxPlayers = Mathf.Clamp(
                draft.MaxPlayers,
                RoomSettings.MinPlayerCount,
                RoomSettings.MaxPlayerCount);
            destructionLimit = draft.DestructionLimit ==
                               PlaySettingsDraft.UnlimitedDestructionLimit
                ? PlaySettingsDraft.UnlimitedDestructionLimit
                : Mathf.Clamp(
                    draft.DestructionLimit,
                    PlaySettingsDraft.MinDestructionLimit,
                    PlaySettingsDraft.MaxDestructionLimit);
            selectedMapIndex = LobbyMapCatalog.IndexOf(draft.MapId);

            if (titleText != null)
            {
                titleText.text = title;
            }

            if (roomCodeText != null)
            {
                roomCodeText.text = roomCode;
            }

            EnsureMapSlotsBuilt();
            SetEditable(editable);
            RefreshPasswordMask();
            RefreshCounters();
            RefreshMapSelection(scrollIntoView: true);
        }

        public PlaySettingsDraft ReadDraft()
        {
            var map = maps[Mathf.Clamp(selectedMapIndex, 0, maps.Count - 1)];
            return new PlaySettingsDraft(
                title,
                roomCode,
                passwordEnabled,
                password,
                maxPlayers,
                destructionLimit,
                map.Id,
                matchRules);
        }

        private void OnTitleChanged(string value)
        {
            if (!editable) return;
            title = value;
            RefreshTitleSave();
        }

        private void RefreshTitleSave()
        {
            if (saveTitleButton != null)
                saveTitleButton.interactable = editable && RoomSettings.IsValidTitle(title) &&
                    !string.Equals(title.Trim(), savedTitle, StringComparison.Ordinal);
        }

        private void EnsureTitleInput()
        {
            if (titleInput != null || titleText == null) return;
            var original = titleText.rectTransform;
            var inputRect = new GameObject("Room title input", typeof(RectTransform), typeof(Image))
                .GetComponent<RectTransform>();
            inputRect.SetParent(original.parent, false);
            inputRect.anchorMin = original.anchorMin; inputRect.anchorMax = original.anchorMax;
            inputRect.offsetMin = original.offsetMin;
            inputRect.offsetMax = original.offsetMax - new Vector2(90, 0);
            inputRect.GetComponent<Image>().color = HomeStyle.Palette.InputFill;
            var inputText = Instantiate(titleText, inputRect);
            inputText.name = "Text";
            inputText.rectTransform.anchorMin = Vector2.zero;
            inputText.rectTransform.anchorMax = Vector2.one;
            inputText.rectTransform.offsetMin = new Vector2(8, 0);
            inputText.rectTransform.offsetMax = new Vector2(-8, 0);
            titleInput = inputRect.gameObject.AddComponent<InputField>();
            titleInput.textComponent = inputText;
            titleInput.targetGraphic = inputRect.GetComponent<Image>();
            titleInput.characterLimit = RoomSettings.MaxTitleLength;
            titleInput.SetTextWithoutNotify(title);
            titleInput.interactable = editable;
            var saveRect = new GameObject("Save room title", typeof(RectTransform), typeof(Image), typeof(Button))
                .GetComponent<RectTransform>();
            saveRect.SetParent(original.parent, false);
            saveRect.anchorMin = new Vector2(original.anchorMax.x, original.anchorMin.y);
            saveRect.anchorMax = original.anchorMax;
            saveRect.offsetMin = new Vector2(original.offsetMax.x - 80, original.offsetMin.y);
            saveRect.offsetMax = original.offsetMax;
            saveRect.GetComponent<Image>().color = HomeStyle.Palette.InputFill;
            saveTitleButton = saveRect.GetComponent<Button>();
            var label = Instantiate(titleText, saveRect);
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            label.text = "저장";
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            titleText.gameObject.SetActive(false);
            RefreshTitleSave();
        }

        private void ScrollMaps(int direction)
        {
            if (mapScroll == null || mapContent == null)
            {
                return;
            }

            var step = (MapSlotSize + MapSlotSpacing) / Mathf.Max(1f, mapContent.rect.width);
            mapScroll.horizontalNormalizedPosition = Mathf.Clamp01(
                mapScroll.horizontalNormalizedPosition + (direction * step));
        }

        private void SelectMap(int index)
        {
            if (!editable || index < 0 || index >= maps.Count)
            {
                return;
            }

            selectedMapIndex = index;
            RefreshMapSelection(scrollIntoView: true);
        }

        private void RefreshMapSelection(bool scrollIntoView)
        {
            if (maps.Count == 0)
            {
                return;
            }

            selectedMapIndex = Mathf.Clamp(selectedMapIndex, 0, maps.Count - 1);
            var selected = maps[selectedMapIndex];

            if (mapNameText != null)
            {
                mapNameText.text = selected.DisplayName;
            }

            for (var i = 0; i < mapSlotImages.Count; i++)
            {
                var image = mapSlotImages[i];
                if (image == null)
                {
                    continue;
                }

                var selectedSlot = i == selectedMapIndex;
                image.color = selectedSlot
                    ? new Color(0.92f, 0.92f, 0.95f, 1f)
                    : new Color(0.45f, 0.45f, 0.5f, 1f);

                var outline = image.transform.Find("Selection");
                if (outline != null)
                {
                    outline.gameObject.SetActive(selectedSlot);
                }
            }

            if (scrollIntoView)
            {
                ScrollSelectedIntoView();
            }
        }

        private void ScrollSelectedIntoView()
        {
            if (mapScroll == null || mapContent == null || maps.Count <= 1)
            {
                return;
            }

            var viewport = mapScroll.viewport != null
                ? mapScroll.viewport.rect.width
                : mapScroll.GetComponent<RectTransform>().rect.width;
            var contentWidth = mapContent.rect.width;
            if (contentWidth <= viewport)
            {
                mapScroll.horizontalNormalizedPosition = 0f;
                return;
            }

            var slotCenter = selectedMapIndex * (MapSlotSize + MapSlotSpacing) + (MapSlotSize * 0.5f);
            var target = (slotCenter - (viewport * 0.5f)) / (contentWidth - viewport);
            mapScroll.horizontalNormalizedPosition = Mathf.Clamp01(target);
        }

        private void EnsureMapSlotsBuilt()
        {
            if (mapContent == null)
            {
                return;
            }

            if (mapSlotButtons.Count == maps.Count && mapSlotImages.Count == maps.Count)
            {
                return;
            }

            UnbindMapSlots();
            for (var i = mapContent.childCount - 1; i >= 0; i--)
            {
                var child = mapContent.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            mapSlotImages.Clear();
            mapSlotButtons.Clear();

            var width = (maps.Count * MapSlotSize) + (Mathf.Max(0, maps.Count - 1) * MapSlotSpacing);
            mapContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            mapContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, MapSlotSize);

            for (var i = 0; i < maps.Count; i++)
            {
                var slot = CreateMapSlot(mapContent, i);
                mapSlotImages.Add(slot.GetComponent<Image>());
                mapSlotButtons.Add(slot.GetComponent<Button>());
            }

            BindMapSlots();
        }

        private static RectTransform CreateMapSlot(RectTransform parent, int index)
        {
            var go = new GameObject($"MapSlot{index}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(MapSlotSize, MapSlotSize);
            rect.anchoredPosition = new Vector2(index * (MapSlotSize + MapSlotSpacing), 0f);
            go.GetComponent<Image>().color = new Color(0.45f, 0.45f, 0.5f, 1f);

            var selectionGo = new GameObject("Selection", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            selectionGo.transform.SetParent(go.transform, false);
            var selectionRect = selectionGo.GetComponent<RectTransform>();
            selectionRect.anchorMin = Vector2.zero;
            selectionRect.anchorMax = Vector2.one;
            selectionRect.offsetMin = new Vector2(-4f, -4f);
            selectionRect.offsetMax = new Vector2(4f, 4f);
            selectionRect.SetAsFirstSibling();
            var selectionImage = selectionGo.GetComponent<Image>();
            selectionImage.color = new Color(0.95f, 0.95f, 1f, 1f);
            selectionImage.raycastTarget = false;
            selectionGo.SetActive(false);
            return rect;
        }

        private void BindMapSlots()
        {
            for (var i = 0; i < mapSlotButtons.Count; i++)
            {
                var index = i;
                Bind(mapSlotButtons[i], () => SelectMap(index));
            }
        }

        private void UnbindMapSlots()
        {
            for (var i = 0; i < mapSlotButtons.Count; i++)
            {
                Unbind(mapSlotButtons[i]);
            }
        }

        private void RefreshPasswordMask()
        {
            if (passwordMaskedText == null)
            {
                return;
            }

            if (!passwordEnabled || string.IsNullOrEmpty(password))
            {
                passwordMaskedText.text = "없음";
                return;
            }

            passwordMaskedText.text = new string('*', Mathf.Clamp(password.Length, 4, 12));
        }

        private void SetMaxPlayers(int value)
        {
            if (!editable) return;
            maxPlayers = Mathf.Clamp(
                value,
                RoomSettings.MinPlayerCount,
                RoomSettings.MaxPlayerCount);
            RefreshCounters();
        }

        private void SetDestructionLimit(int value)
        {
            if (!editable) return;
            destructionLimit = destructionLimit ==
                               PlaySettingsDraft.UnlimitedDestructionLimit
                ? PlaySettingsDraft.MaxDestructionLimit
                : value > PlaySettingsDraft.MaxDestructionLimit
                    ? PlaySettingsDraft.UnlimitedDestructionLimit
                    : Mathf.Clamp(
                        value,
                        PlaySettingsDraft.MinDestructionLimit,
                        PlaySettingsDraft.MaxDestructionLimit);
            RefreshCounters();
        }

        private void EnsureRuleControls()
        {
            if (ruleValues.Count != 0 || panel == null || maxPlayersText == null ||
                maxPlayersMinusButton == null || maxPlayersPlusButton == null) return;
            var panelRect = (RectTransform)panel.transform;
            panelRect.sizeDelta += new Vector2(0, 220);
            foreach (RectTransform child in panelRect)
                if (child.anchorMin.y == 0.5f && child.anchorMax.y == 0.5f)
                    child.anchoredPosition += new Vector2(0, 110);
            var names = new[] { "숨기기 시간", "찾기 시간", "달리기 속도", "HP" };
            for (var i = 0; i < names.Length; i++)
            {
                var y = -195 - i * 45;
                var label = Instantiate(maxPlayersText, panelRect);
                label.name = "RuleLabel" + i;
                label.text = names[i];
                label.alignment = TextAnchor.MiddleLeft;
                PlaceRuleControl(label.rectTransform, 40, y, 190);
                var value = Instantiate(maxPlayersText, panelRect);
                value.name = "RuleValue" + i;
                PlaceRuleControl(value.rectTransform, 275, y, 110);
                ruleValues.Add(value);
                var minus = Instantiate(maxPlayersMinusButton, panelRect);
                minus.name = "RuleMinus" + i;
                minus.onClick = new Button.ButtonClickedEvent();
                PlaceRuleControl((RectTransform)minus.transform, 220, y, 40);
                ruleMinus.Add(minus);
                var plus = Instantiate(maxPlayersPlusButton, panelRect);
                plus.name = "RulePlus" + i;
                plus.onClick = new Button.ButtonClickedEvent();
                PlaceRuleControl((RectTransform)plus.transform, 400, y, 40);
                rulePlus.Add(plus);
            }
            RefreshRuleControls();
        }

        private static void PlaceRuleControl(RectTransform rect, float x, float y, float width)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 0.5f);
            rect.pivot = new Vector2(0, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, 36);
        }

        private void BindRuleControls()
        {
            for (var i = 0; i < ruleValues.Count; i++)
            {
                var index = i;
                Bind(ruleMinus[i], () => ChangeRule(index, -1));
                Bind(rulePlus[i], () => ChangeRule(index, 1));
            }
        }

        private void ChangeRule(int index, int direction)
        {
            if (!editable) return;
            var hiding = matchRules.HidingDurationSeconds;
            var searching = matchRules.SearchingDurationMinutes;
            var speed = matchRules.SprintMultiplier;
            var hp = matchRules.StunHitCount;
            switch (index)
            {
                case 0: hiding += direction; break;
                case 1: searching += direction; break;
                case 2:
                    var next = Array.IndexOf(SprintOptions, speed) + direction;
                    if (next < 0 || next >= SprintOptions.Length) return;
                    speed = SprintOptions[next]; break;
                case 3: hp += direction; break;
                default: return;
            }
            if (MatchRuleSettings.TryCreate(hiding, searching, speed, hp, matchRules.CategoryId,
                out var updated, out _)) matchRules = updated;
            RefreshRuleControls();
        }

        private void RefreshRuleControls()
        {
            if (ruleValues.Count == 0) return;
            var values = new[] { matchRules.HidingDurationSeconds, matchRules.SearchingDurationMinutes,
                Array.IndexOf(SprintOptions, matchRules.SprintMultiplier), matchRules.StunHitCount };
            var min = new[] { MatchRuleSettings.MinHidingDurationSeconds, MatchRuleSettings.MinSearchingDurationMinutes,
                0, MatchRuleSettings.MinStunHitCount };
            var max = new[] { MatchRuleSettings.MaxHidingDurationSeconds, MatchRuleSettings.MaxSearchingDurationMinutes,
                SprintOptions.Length - 1, MatchRuleSettings.MaxStunHitCount };
            var labels = new[] { values[0] + "초", values[1] + "분", matchRules.SprintMultiplier + "배", values[3].ToString() };
            for (var i = 0; i < ruleValues.Count; i++)
            {
                ruleValues[i].text = labels[i];
                ruleMinus[i].interactable = editable && values[i] > min[i];
                rulePlus[i].interactable = editable && values[i] < max[i];
            }
        }

        private void RefreshCounters()
        {
            RefreshRuleControls();
            if (maxPlayersText != null)
            {
                maxPlayersText.text = maxPlayers.ToString();
            }

            if (destructionLimitText != null)
            {
                destructionLimitText.text = destructionLimit ==
                                            PlaySettingsDraft.UnlimitedDestructionLimit
                    ? "무한"
                    : destructionLimit.ToString();
            }

            if (maxPlayersMinusButton != null)
            {
                maxPlayersMinusButton.interactable = editable && maxPlayers > RoomSettings.MinPlayerCount;
            }

            if (maxPlayersPlusButton != null)
            {
                maxPlayersPlusButton.interactable = editable && maxPlayers < RoomSettings.MaxPlayerCount;
            }

            if (destructionMinusButton != null)
            {
                destructionMinusButton.interactable =
                    editable && (destructionLimit == PlaySettingsDraft.UnlimitedDestructionLimit ||
                    destructionLimit > PlaySettingsDraft.MinDestructionLimit);
            }

            if (destructionPlusButton != null)
            {
                destructionPlusButton.interactable =
                    editable && destructionLimit != PlaySettingsDraft.UnlimitedDestructionLimit;
            }
        }

        private void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            Unbind(button);
            button.onClick.AddListener(action);
            boundActions[button] = action;
        }

        private void Unbind(Button button)
        {
            if (button == null)
            {
                return;
            }

            if (boundActions.TryGetValue(button, out var action))
            {
                button.onClick.RemoveListener(action);
                boundActions.Remove(button);
            }
        }
    }
}
