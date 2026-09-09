using System;
using System.Collections;
using System.Collections.Generic;
using Game.Client.Home;
using Game.Client.Rooms;
using Game.Core.Lobby;
using Game.Core.Rooms;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
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
        event Action StartRequested;

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
        private Button roomCodeHitButton;
        private Image copyIconImage;
        private Text copyFeedbackText;
        private GameObject copyFeedbackRoot;
        private GameObject copyFeedbackShift;
        private Coroutine copyFeedbackRoutine;
        private bool copyCooldownActive;
        private Text titleText;
        private InputField titleInput;
        private Text roomCodeText;
        private Text maxPlayersText;
        private Button maxPlayersMinusButton;
        private Button maxPlayersPlusButton;
        private Text destructionLimitText;
        private Button destructionMinusButton;
        private Button destructionPlusButton;

        private GameObject overlayRoot;
        private TextMeshProUGUI gameStartLabel;
        private Button gameStartButton;

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
        private IReadOnlyList<PlaySettingsMapOption> mapOptions = PlaySettingsMapCatalog.All;
        private readonly Dictionary<Button, UnityEngine.Events.UnityAction> boundActions = new();
        private int openedOnFrame = int.MinValue;

        public event Action OpenRequested;
        public event Action CloseRequested;
        public event Action CopyRoomCodeRequested;
        public event Action InviteRequested;
        public event Action CopyPasswordRequested;
        public event Action StartRequested;

        private void OnEnable()
        {
            EnsureOverlay();
            EnsureLayout();
            BindRuleControls();
            if (titleInput != null) titleInput.onValueChanged.AddListener(OnTitleChanged);
            Bind(openButton, () => OpenRequested?.Invoke());
            Bind(closeButton, RequestClose);
            Bind(gameStartButton, RequestStart);
            Bind(copyRoomCodeButton, RequestCopyRoomCode);
            Bind(roomCodeHitButton, RequestCopyRoomCode);
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
            Unbind(gameStartButton);
            Unbind(copyRoomCodeButton);
            Unbind(roomCodeHitButton);
            Unbind(maxPlayersMinusButton);
            Unbind(maxPlayersPlusButton);
            Unbind(destructionMinusButton);
            Unbind(destructionPlusButton);
            Unbind(mapPrevButton);
            Unbind(mapNextButton);
            UnbindMapSlots();
            Unbind(categoryPrevButton);
            Unbind(categoryNextButton);
            StopCopyFeedback(resetVisuals: true);
        }

        private void Update()
        {
            if (overlayRoot == null || !overlayRoot.activeInHierarchy)
            {
                return;
            }

            if (Time.frameCount <= openedOnFrame)
            {
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
            {
                RequestClose();
            }
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
            SetGameStartLabelVisible(visible);
            if (visible)
            {
                openedOnFrame = Time.frameCount;
                BringOverlayForward();
                RebuildSettingsScrollLayout();
                EnsureMapUiReady();

                RefreshCounters();
                RefreshCategory();
                RefreshMapSelection(scrollIntoView: false);
                RefreshTitleCounter();
                RefreshRoomCode();
            }
            else
            {
                StopCopyFeedback(resetVisuals: true);
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

        private void RequestStart()
        {
            if (!editable || RoomSettings.IsValidTitle(ReadDraft().Title)) StartRequested?.Invoke();
        }

        private void RequestCopyRoomCode()
        {
            if (copyCooldownActive || string.IsNullOrWhiteSpace(roomCode))
            {
                return;
            }

            CopyRoomCodeRequested?.Invoke();
            StartCopyFeedback();
        }

        private void StartCopyFeedback()
        {
            StopCopyFeedback(resetVisuals: false);
            copyCooldownActive = true;
            SetCopyControlsInteractable(false);
            if (copyFeedbackRoot != null)
            {
                copyFeedbackRoot.SetActive(true);
            }

            if (copyFeedbackShift != null)
            {
                copyFeedbackShift.SetActive(true);
            }

            if (copyIconImage != null)
            {
                copyIconImage.sprite = LoadCopyCheckIcon() ?? LoadCopyIcon();
            }

            if (isActiveAndEnabled)
            {
                copyFeedbackRoutine = StartCoroutine(CopyFeedbackRoutine());
            }
            else
            {
                StopCopyFeedback(resetVisuals: true);
            }
        }

        private IEnumerator CopyFeedbackRoutine()
        {
            yield return new WaitForSecondsRealtime(PlaySettingsStyle.Layout.CopyFeedbackDuration);
            copyFeedbackRoutine = null;
            StopCopyFeedback(resetVisuals: true);
        }

        private void StopCopyFeedback(bool resetVisuals)
        {
            if (copyFeedbackRoutine != null)
            {
                StopCoroutine(copyFeedbackRoutine);
                copyFeedbackRoutine = null;
            }

            copyCooldownActive = false;
            if (!resetVisuals)
            {
                return;
            }

            if (copyFeedbackRoot != null)
            {
                copyFeedbackRoot.SetActive(false);
            }

            if (copyFeedbackShift != null)
            {
                copyFeedbackShift.SetActive(false);
            }

            if (copyIconImage != null)
            {
                copyIconImage.sprite = LoadCopyIcon();
            }

            SetCopyControlsInteractable(true);
        }

        private void SetCopyControlsInteractable(bool interactable)
        {
            if (copyRoomCodeButton != null)
            {
                copyRoomCodeButton.interactable = interactable;
            }

            if (roomCodeHitButton != null)
            {
                roomCodeHitButton.interactable = interactable;
            }
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
            if (titleInput != null) titleInput.SetTextWithoutNotify(title);
            RefreshTitleCounter();
            roomCode = draft.RoomCode;
            passwordEnabled = draft.PasswordEnabled;
            password = draft.Password ?? string.Empty;
            matchRules = draft.MatchRules;
            selectedCategoryIndex = PlaySettingsCategoryCatalog.IndexOf(matchRules.CategoryId);
            if (selectedCategoryIndex < 0)
            {
                // The picker only lists ready categories. An id it cannot name
                // is still the room's rule — rewriting it here would drop
                // food/fruit (and anything else not on the chip yet) the
                // moment a host changed player cap.
                selectedCategoryIndex = PlaySettingsCategoryCatalog.DefaultIndex;
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
            selectedMapIndex = PlaySettingsMapCatalog.IndexOf(draft.MapId);
            if (selectedMapIndex < 0)
            {
                selectedMapIndex = PlaySettingsMapCatalog.DefaultIndex;
            }

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
            var map = mapOptions[Mathf.Clamp(selectedMapIndex, 0, mapOptions.Count - 1)];
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
                if (dedupeRoot != null && overlayRoot != null)
                {
                    var adoptedOverlay = (RectTransform)overlayRoot.transform;
                    StyleBackButton(adoptedOverlay);
                    EnsureGameStartLabel(dedupeRoot, adoptedOverlay);
                    BringOverlayForward();
                }

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

            var existingOverlay = hudRoot.Find("PlaySettingsOverlay") as RectTransform;
            if (existingOverlay != null)
            {
                overlayRoot = existingOverlay.gameObject;
                EnsureOverlayScrim(existingOverlay);
                panelTransform.SetParent(existingOverlay, false);
                RemoveDuplicateOverlays(hudRoot, overlayRoot);
                StyleBackButton(existingOverlay);
                EnsureGameStartLabel(hudRoot, existingOverlay);
                overlayRoot.SetActive(panel.activeSelf);
                SetBackButtonVisible(overlayRoot.activeSelf);
                SetGameStartLabelVisible(overlayRoot.activeSelf);
                return;
            }

            RemoveDuplicateOverlays(hudRoot, null);

            overlayRoot = new GameObject("PlaySettingsOverlay", typeof(RectTransform));
            var createdOverlay = (RectTransform)overlayRoot.transform;
            createdOverlay.SetParent(hudRoot, false);
            createdOverlay.SetSiblingIndex(panelTransform.GetSiblingIndex());
            StretchRect(createdOverlay);

            EnsureOverlayScrim(createdOverlay);

            panelTransform.SetParent(createdOverlay, false);
            StyleBackButton(createdOverlay);
            EnsureGameStartLabel(hudRoot, createdOverlay);

            overlayRoot.SetActive(panel.activeSelf);
            SetBackButtonVisible(overlayRoot.activeSelf);
            SetGameStartLabelVisible(overlayRoot.activeSelf);
        }

        private bool TryAdoptOverlay(RectTransform panelTransform)
        {
            var parent = panelTransform.parent as RectTransform;
            if (parent != null && parent.name == "PlaySettingsOverlay")
            {
                overlayRoot = parent.gameObject;
                EnsureOverlayScrim(parent);
                return true;
            }

            return false;
        }

        private static void RemoveLegacyBackdrop(RectTransform overlayTransform)
        {
            var legacy = overlayTransform.Find("Backdrop");
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

        private static void EnsureOverlayScrim(RectTransform overlayTransform)
        {
            RemoveLegacyBackdrop(overlayTransform);
            EnsureOverlayCanvas(overlayTransform);

            var dimTransform = overlayTransform.Find("Dim") as RectTransform;
            if (dimTransform == null)
            {
                var dimGo = new GameObject("Dim", typeof(RectTransform), typeof(Image));
                dimTransform = dimGo.GetComponent<RectTransform>();
                dimTransform.SetParent(overlayTransform, false);
                StretchRect(dimTransform);
            }

            dimTransform.SetSiblingIndex(0);
            var dimImage = dimTransform.GetComponent<Image>();
            dimImage.color = PlaySettingsStyle.Overlay.Scrim;
            dimImage.raycastTarget = true;
        }

        private static void EnsureOverlayCanvas(RectTransform overlayTransform)
        {
            if (overlayTransform == null)
            {
                return;
            }

            var canvas = overlayTransform.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = overlayTransform.gameObject.AddComponent<Canvas>();
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = PlaySettingsStyle.Overlay.SortingOrder;

            if (overlayTransform.GetComponent<GraphicRaycaster>() == null)
            {
                overlayTransform.gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private void BringOverlayForward()
        {
            if (overlayRoot == null)
            {
                return;
            }

            var overlayTransform = (RectTransform)overlayRoot.transform;
            EnsureOverlayCanvas(overlayTransform);
            overlayTransform.SetAsLastSibling();
            if (closeButton != null)
            {
                closeButton.transform.SetAsLastSibling();
            }

            if (gameStartLabel != null)
            {
                gameStartLabel.transform.SetAsLastSibling();
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

        private void StyleBackButton(RectTransform overlay)
        {
            if (closeButton == null || overlay == null)
            {
                return;
            }

            var rect = closeButton.GetComponent<RectTransform>();
            rect.SetParent(overlay, false);
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

        private void EnsureGameStartLabel(RectTransform hudRoot, RectTransform overlay)
        {
            if (overlay == null)
            {
                return;
            }

            if (gameStartLabel == null)
            {
                var existingStartLabel = overlay.Find("GameStartLabel") ?? hudRoot?.Find("GameStartLabel");
                if (existingStartLabel != null)
                {
                    gameStartLabel = existingStartLabel.GetComponent<TextMeshProUGUI>();
                }

                if (gameStartLabel == null)
                {
                    CreateGameStartLabel(overlay);
                }
            }

            if (gameStartLabel != null)
            {
                gameStartLabel.transform.SetParent(overlay, false);
            }

            EnsureGameStartButton();
        }

        private void CreateGameStartLabel(RectTransform overlay)
        {
            var labelGo = new GameObject("GameStartLabel", typeof(RectTransform));
            var rect = labelGo.GetComponent<RectTransform>();
            rect.SetParent(overlay, false);
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = PlaySettingsStyle.Overlay.GameStartPosition;
            rect.sizeDelta = PlaySettingsStyle.Overlay.GameStartSize;

            gameStartLabel = labelGo.AddComponent<TextMeshProUGUI>();
            var font = HomeUiFonts.ApplyExtraBold();
            if (font != null)
            {
                gameStartLabel.font = font;
                if (font.material != null)
                {
                    gameStartLabel.fontSharedMaterial = font.material;
                }
            }

            gameStartLabel.fontSize = PlaySettingsStyle.FontSize.GameStart;
            gameStartLabel.text = "게임시작";
            gameStartLabel.alignment = TextAlignmentOptions.BottomRight;
            gameStartLabel.color = Color.white;
            gameStartLabel.raycastTarget = false;
            gameStartLabel.textWrappingMode = TextWrappingModes.NoWrap;
            gameStartLabel.overflowMode = TextOverflowModes.Overflow;
            labelGo.SetActive(false);
        }

        private void EnsureGameStartButton()
        {
            if (gameStartLabel == null)
            {
                return;
            }

            gameStartLabel.raycastTarget = true;
            gameStartButton = gameStartLabel.GetComponent<Button>();
            if (gameStartButton == null)
            {
                gameStartButton = gameStartLabel.gameObject.AddComponent<Button>();
            }

            if (gameStartButton == null)
            {
                return;
            }

            gameStartButton.targetGraphic = gameStartLabel;
            gameStartButton.transition = Selectable.Transition.None;
        }

        private void SetGameStartLabelVisible(bool visible)
        {
            if (gameStartLabel == null)
            {
                return;
            }

            gameStartLabel.gameObject.SetActive(visible);
            if (visible)
            {
                gameStartLabel.transform.SetAsLastSibling();
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
