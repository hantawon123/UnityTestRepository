using System;
using System.Collections.Generic;
using Game.Client.Character;
using Game.Client.Home;
using Game.Client.Rooms;
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
        /// through the presenter rather than hiding the panel directly: Esc has
        /// to back out of this panel, and the presenter tracks whether it is
        /// open. Hiding from outside would leave that flag saying open.
        /// </summary>
        void RequestClose();

        /// <summary>
        /// Asks to be opened as if the panel's own open button was pressed, so
        /// the presenter fills the draft and decides editability before the
        /// panel shows. Used by objects in the room that lead to this screen.
        /// </summary>
        void RequestOpen();
    }

    public sealed partial class PlaySettingsView : MonoBehaviour, IPlaySettingsView
    {
        [SerializeField]
        private Button openButton;

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private GameObject panel;

        private Button copyRoomCodeButton;
        private Text titleText;
        private InputField titleInput;
        private string savedTitle = string.Empty;
        private Text roomCodeText;
        private Text maxPlayersText;
        private Button maxPlayersMinusButton;
        private Button maxPlayersPlusButton;
        private Text destructionLimitText;
        private Button destructionMinusButton;
        private Button destructionPlusButton;

        private GameObject overlayRoot;

        private static readonly float[] SprintOptions = { 0.5f, 1f, 1.5f, 2f, 3f };        private readonly List<Text> ruleValues = new();
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
        private readonly Dictionary<Button, UnityEngine.Events.UnityAction> boundActions = new();

        public event Action OpenRequested;
        public event Action CloseRequested;
        public event Action CopyRoomCodeRequested;
        public event Action InviteRequested;
        public event Action CopyPasswordRequested;
        public event Action SaveTitleRequested;

        private void OnEnable()
        {
            EnsureOverlay();
            EnsureLayout();
            BindRuleControls();
            if (titleInput != null) titleInput.onValueChanged.AddListener(OnTitleChanged);
            Bind(openButton, () => OpenRequested?.Invoke());
            Bind(closeButton, RequestClose);
            Bind(copyRoomCodeButton, () => CopyRoomCodeRequested?.Invoke());
            Bind(applyButton, RequestClose);
            Bind(maxPlayersMinusButton, () => SetMaxPlayers(maxPlayers - 1));
            Bind(maxPlayersPlusButton, () => SetMaxPlayers(maxPlayers + 1));
            Bind(destructionMinusButton, () => SetDestructionLimit(destructionLimit - 1));
            Bind(destructionPlusButton, () => SetDestructionLimit(destructionLimit + 1));
            Bind(mapPrevButton, () => StepMapSelection(-1));
            Bind(mapNextButton, () => StepMapSelection(1));
            Bind(categoryPrevButton, () => SelectCategory(-1));
            Bind(categoryNextButton, () => SelectCategory(1));
        }

        private void OnDisable()
        {
            if (titleInput != null) titleInput.onValueChanged.RemoveListener(OnTitleChanged);
            foreach (var button in ruleMinus) Unbind(button);
            foreach (var button in rulePlus) Unbind(button);
            Unbind(applyButton);
            Unbind(openButton);
            Unbind(closeButton);
            Unbind(copyRoomCodeButton);
            Unbind(maxPlayersMinusButton);
            Unbind(maxPlayersPlusButton);
            Unbind(destructionMinusButton);
            Unbind(destructionPlusButton);
            Unbind(mapPrevButton);
            Unbind(mapNextButton);
            UnbindMapSlots();
            Unbind(categoryPrevButton);
            Unbind(categoryNextButton);
        }

        public void SetVisible(bool visible)
        {
            EnsureOverlay();
            EnsureLayout();
            if (overlayRoot != null)
            {
                overlayRoot.SetActive(visible);
            }

            if (panel != null)
            {
                panel.SetActive(visible);
            }

            SetBackButtonVisible(visible);

            if (visible)
            {
                RebuildSettingsScrollLayout();
                EnsureMapUiReady();

                RefreshCounters();
                RefreshCategory();
                RefreshMapSelection(scrollIntoView: false);
                RefreshTitleCounter();
                RefreshRoomCode();
            }
        }

        private void RefreshRoomCode()
        {
            if (roomCodeText == null && settingsContent != null)
            {
                CacheRoomCodeRefs(settingsContent);
            }

            if (roomCodeText != null)
            {
                roomCodeText.text = roomCode;
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
            foreach (var button in mapSlotButtons)
            {
                if (button != null)
                {
                    button.interactable = value;
                }
            }

            RefreshCounters();
            RefreshCategory();
            RefreshMapSelection(scrollIntoView: false);
        }

        public void SetDraft(PlaySettingsDraft draft)
        {
            EnsureLayout();
            title = draft.Title;
            savedTitle = title;
            if (titleInput != null) titleInput.SetTextWithoutNotify(title);
            RefreshTitleCounter();
            roomCode = draft.RoomCode;
            passwordEnabled = draft.PasswordEnabled;
            password = draft.Password ?? string.Empty;
            matchRules = draft.MatchRules;
            selectedCategoryIndex = PlaySettingsCategoryCatalog.IndexOf(matchRules.CategoryId);
            if (selectedCategoryIndex < 0)
            {
                selectedCategoryIndex = PlaySettingsCategoryCatalog.DefaultIndex;
                NormalizeCategoryRules();
            }

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

            RefreshRoomCode();

            EnsureMapUiReady();
            SetEditable(editable);
            RefreshCounters();
            RefreshCategory();
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
            RefreshTitleCounter();
        }

        private void RefreshTitleCounter()
        {
            if (titleCounterText != null)
            {
                titleCounterText.text = $"{title.Length}/{RoomSettings.MaxTitleLength}";
            }
        }

        private void SelectCategory(int direction)
        {
            var options = PlaySettingsCategoryCatalog.All;
            if (!editable || options.Count <= 1)
            {
                return;
            }

            selectedCategoryIndex = (selectedCategoryIndex + direction + options.Count) % options.Count;
            NormalizeCategoryRules();
            RefreshCategory();
        }

        private void NormalizeCategoryRules()
        {
            var categoryId = PlaySettingsCategoryCatalog.GetOption(selectedCategoryIndex).Id;
            if (MatchRuleSettings.TryCreate(
                    matchRules.HidingDurationSeconds,
                    matchRules.SearchingDurationMinutes,
                    matchRules.SprintMultiplier,
                    matchRules.StunHitCount,
                    categoryId,
                    out var updated,
                    out _))
            {
                matchRules = updated;
            }
        }

        private void RefreshCategory()
        {
            if (categoryText == null && settingsContent != null)
            {
                CacheMapAreaRefs(settingsContent);
            }

            if (categoryText != null)
            {
                categoryText.text = PlaySettingsCategoryCatalog.GetOption(selectedCategoryIndex).Label;
            }

            var hasMultipleOptions = PlaySettingsCategoryCatalog.All.Count > 1;
            if (categoryPrevButton != null)
            {
                categoryPrevButton.interactable = editable && hasMultipleOptions;
            }

            if (categoryNextButton != null)
            {
                categoryNextButton.interactable = editable && hasMultipleOptions;
            }
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
            var labels = new[]
            {
                values[0] + "초",
                values[1] + "분",
                matchRules.SprintMultiplier + "배",
                values[3] + "회"
            };
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
                maxPlayersText.text = maxPlayers + "명";
            }

            if (destructionLimitText != null)
            {
                destructionLimitText.text = destructionLimit ==
                                            PlaySettingsDraft.UnlimitedDestructionLimit
                    ? "무한"
                    : destructionLimit + "회";
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

        private void EnsureOverlay()
        {
            if (panel == null)
            {
                return;
            }

            var panelTransform = (RectTransform)panel.transform;
            if (TryAdoptOverlay(panelTransform))
            {
                var dedupeRoot = panelTransform.parent != null
                    ? panelTransform.parent.parent as RectTransform
                    : null;
                RemoveDuplicateOverlays(dedupeRoot, overlayRoot);
                return;
            }

            if (overlayRoot != null)
            {
                return;
            }

            var hudRoot = panelTransform.parent as RectTransform;
            if (hudRoot == null)
            {
                return;
            }

            var existing = hudRoot.Find("PlaySettingsOverlay") as RectTransform;
            if (existing != null)
            {
                overlayRoot = existing.gameObject;
                RemoveLegacyBackdrop(existing);
                panelTransform.SetParent(existing, false);
                RemoveDuplicateOverlays(hudRoot, overlayRoot);
                StyleBackButton(hudRoot, existing);
                overlayRoot.SetActive(panel.activeSelf);
                SetBackButtonVisible(overlayRoot.activeSelf);
                return;
            }

            RemoveDuplicateOverlays(hudRoot, null);

            overlayRoot = new GameObject("PlaySettingsOverlay", typeof(RectTransform));
            var overlayRect = (RectTransform)overlayRoot.transform;
            overlayRect.SetParent(hudRoot, false);
            overlayRect.SetSiblingIndex(panelTransform.GetSiblingIndex());
            StretchRect(overlayRect);

            var dimGo = new GameObject("Dim", typeof(RectTransform), typeof(Image));
            dimGo.transform.SetParent(overlayRect, false);
            StretchRect(dimGo.GetComponent<RectTransform>());
            dimGo.GetComponent<Image>().color = CharacterClosetStyle.Palette.Dim;

            panelTransform.SetParent(overlayRect, false);
            StyleBackButton(hudRoot, overlayRect);

            overlayRoot.SetActive(panel.activeSelf);
            SetBackButtonVisible(overlayRoot.activeSelf);
        }

        private bool TryAdoptOverlay(RectTransform panelTransform)
        {
            var parent = panelTransform.parent as RectTransform;
            if (parent != null && parent.name == "PlaySettingsOverlay")
            {
                overlayRoot = parent.gameObject;
                RemoveLegacyBackdrop(parent);
                return true;
            }

            return false;
        }

        private static void RemoveLegacyBackdrop(RectTransform overlayRect)
        {
            var legacy = overlayRect.Find("Backdrop");
            if (legacy == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(legacy.gameObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(legacy.gameObject);
            }
        }

        private static void RemoveDuplicateOverlays(RectTransform hudRoot, GameObject keep)
        {
            if (hudRoot == null)
            {
                return;
            }

            for (var i = hudRoot.childCount - 1; i >= 0; i--)
            {
                var child = hudRoot.GetChild(i);
                if (child.name != "PlaySettingsOverlay" || child.gameObject == keep)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(child.gameObject);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private void StyleBackButton(RectTransform hudRoot, RectTransform overlay)
        {
            if (closeButton == null || hudRoot == null)
            {
                return;
            }

            var rect = closeButton.GetComponent<RectTransform>();
            rect.SetParent(hudRoot, false);
            rect.SetSiblingIndex(overlay.GetSiblingIndex() + 1);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = RoomBrowserStyle.Layout.BackButtonPosition;
            rect.sizeDelta = RoomBrowserStyle.Layout.BackButtonSize;

            var image = closeButton.GetComponent<Image>();
            if (image != null)
            {
                image.color = Color.clear;
            }

            var label = closeButton.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = "← 이전";
                label.fontSize = Mathf.RoundToInt(RoomBrowserStyle.FontSize.Back);
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.white;
                label.raycastTarget = false;
                var labelRect = label.rectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
            }

            closeButton.gameObject.name = "BackButton";
        }

        private void SetBackButtonVisible(bool visible)
        {
            if (closeButton == null)
            {
                return;
            }

            closeButton.gameObject.SetActive(visible);
            if (visible)
            {
                closeButton.transform.SetAsLastSibling();
            }
        }

        private static void StretchRect(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}
