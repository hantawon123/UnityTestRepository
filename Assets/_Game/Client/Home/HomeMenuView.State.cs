using System;
using System.Collections.Generic;
using Game.Core.Home;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.Home
{
    public sealed partial class HomeMenuView
    {
        [SerializeField]
        [Range(0f, 1f)]
        private float experienceRatio = 0.4f;

        [Header("Art")]
        [SerializeField]
        private Sprite backgroundSprite;

        [SerializeField]
        private Sprite friendIcon;

        [SerializeField]
        private Sprite serverIcon;

        [Header("Fonts")]
        [SerializeField]
        private TMP_FontAsset fontAsset;

        /// <summary>
        /// The menu is drawn in SemiBold where the rest of the screen is not,
        /// so it is a second asset rather than a style flag: TextMeshPro fakes a
        /// bold weight by smearing the glyph, and the mock-up wants the weight
        /// the type designer drew.
        /// </summary>
        [SerializeField]
        private TMP_FontAsset semiBoldFont;

        [SerializeField]
        private TMP_Text nicknameText;

        private RectTransform profileChip;

        private readonly List<Button> menuButtons = new List<Button>();
        private readonly List<FriendRow> onlineRows = new List<FriendRow>();
        private readonly List<FriendRow> offlineRows = new List<FriendRow>();
        private readonly List<SearchRow> searchRows = new List<SearchRow>();
        private TMP_FontAsset koreanFont;
        private GameObject friendListRoot;
        private GameObject friendListBody;
        private GameObject friendSearchBody;
        private GameObject addFriendButton;
        private GameObject closeSearchButton;
        private TMP_Text panelHeaderText;
        private TMP_Text onlineSectionText;
        private TMP_Text offlineSectionText;
        private RectTransform onlineItemsRoot;
        private RectTransform offlineItemsRoot;
        private RectTransform searchItemsRoot;
        private TMP_InputField friendSearchInput;
        private TMP_Text searchEmptyText;
        private Button dismissButton;
        private GameObject profileSettingsRoot;
        private TMP_InputField profileNicknameInput;
        private TMP_Text profileLevelText;
        private TMP_Text appliedFeedbackText;

        public event Action<HomeMenuAction> ActionClicked;

        public event Action FriendListDismissed;

        public event Action ProfileSettingsDismissed;

        public event Action<string> NicknameChangeRequested;

        public event Action<string> NicknameEdited;

        public event Action FriendSearchOpened;

        public event Action FriendSearchClosed;

        public event Action<string> FriendSearchRequested;

        public event Action<string> FriendRequestClicked;

        private void Awake()
        {
            EnsureEventSystem();
            if (nicknameText == null)
            {
                BuildLayout();
            }

            SetFriendListVisible(false);
            SetProfileSettingsVisible(false);
            SetNicknameAppliedFeedbackVisible(false);
        }

        private void OnDestroy()
        {
            ClearButtons(menuButtons);
            for (var index = 0; index < searchRows.Count; index++)
            {
                if (searchRows[index].RequestButton != null)
                {
                    searchRows[index].RequestButton.onClick.RemoveAllListeners();
                }
            }

            if (dismissButton != null)
            {
                dismissButton.onClick.RemoveAllListeners();
            }

            if (friendSearchInput != null)
            {
                friendSearchInput.onSubmit.RemoveAllListeners();
            }

            if (profileNicknameInput != null)
            {
                profileNicknameInput.onValueChanged.RemoveAllListeners();
            }
        }

        public void SetNickname(string nickname)
        {
            if (nicknameText != null)
            {
                nicknameText.text = nickname;

                // The chip is only as wide as the name inside it, so a longer
                // one has to move the chip's own edge before it is drawn.
                nicknameText.ForceMeshUpdate();
                ResizeProfileChip();
            }

            if (profileNicknameInput != null && profileNicknameInput.text != nickname)
            {
                profileNicknameInput.text = nickname;
            }
        }

        public void SetLevel(int level)
        {
            // Home itself no longer shows a level: the chip carries the name
            // alone. The profile panel is the one place it is still written.
            var label = $"Lv.{level}";
            if (profileLevelText != null)
            {
                profileLevelText.text = label;
            }
        }

        public void SetProfileSettingsVisible(bool visible)
        {
            if (profileSettingsRoot == null)
            {
                return;
            }

            profileSettingsRoot.SetActive(visible);
        }

        public void SetNicknameAppliedFeedbackVisible(bool visible)
        {
            if (appliedFeedbackText == null)
            {
                return;
            }

            appliedFeedbackText.gameObject.SetActive(visible);
        }

        public void SetFriendListVisible(bool visible)
        {
            if (friendListRoot == null)
            {
                return;
            }

            SetFriendSearchVisible(false);
            friendListRoot.SetActive(visible);
        }

        public void SetFriends(
            IReadOnlyList<FriendSummary> onlineFriends,
            IReadOnlyList<FriendSummary> offlineFriends)
        {
            if (onlineFriends == null)
            {
                throw new ArgumentNullException(nameof(onlineFriends));
            }

            if (offlineFriends == null)
            {
                throw new ArgumentNullException(nameof(offlineFriends));
            }

            if (onlineSectionText == null || offlineSectionText == null)
            {
                return;
            }

            onlineSectionText.text = $"온라인 {onlineFriends.Count}";
            offlineSectionText.text = $"오프라인 {offlineFriends.Count}";
            BindRows(onlineRows, onlineItemsRoot, onlineFriends);
            BindRows(offlineRows, offlineItemsRoot, offlineFriends);
        }

        public void SetFriendSearchVisible(bool visible)
        {
            if (friendListBody == null || friendSearchBody == null)
            {
                return;
            }

            friendListBody.SetActive(!visible);
            friendSearchBody.SetActive(visible);
            if (panelHeaderText != null)
            {
                panelHeaderText.text = visible ? "친구 검색" : "친구";
            }

            if (addFriendButton != null)
            {
                addFriendButton.SetActive(!visible);
            }

            if (closeSearchButton != null)
            {
                closeSearchButton.SetActive(visible);
            }

            if (visible && friendSearchInput != null)
            {
                friendSearchInput.text = string.Empty;
            }

            if (visible)
            {
                UpdateSearchEmptyHint(Array.Empty<FriendSearchHit>());
            }
        }

        public void SetFriendSearchResults(IReadOnlyList<FriendSearchHit> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            if (searchItemsRoot == null)
            {
                return;
            }

            BindSearchRows(results);
            UpdateSearchEmptyHint(results);
        }

        private void UpdateSearchEmptyHint(IReadOnlyList<FriendSearchHit> results)
        {
            if (searchEmptyText == null)
            {
                return;
            }

            var hasQuery = friendSearchInput != null && !string.IsNullOrWhiteSpace(friendSearchInput.text);
            searchEmptyText.gameObject.SetActive(results.Count == 0);
            if (results.Count > 0)
            {
                return;
            }

            searchEmptyText.text = hasQuery ? "검색 결과가 없습니다" : "아이디를 검색해 보세요";
        }

    }
}

