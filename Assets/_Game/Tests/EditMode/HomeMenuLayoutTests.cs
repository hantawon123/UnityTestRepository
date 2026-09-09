using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Client.Home;
using Game.Core.Home;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// What the Home screen puts on screen, checked against the mock-up.
    /// </summary>
    /// <remarks>
    /// The presenter tests already cover what happens once an action is raised.
    /// What they cannot see is whether the label a player actually clicks is
    /// wired to the action the presenter expects — the menu is built in code,
    /// so nothing else records that "방 만들기" means
    /// <see cref="HomeMenuAction.CreateRoom"/>.
    /// <para>
    /// The layout is built by calling <c>BuildLayout</c> directly rather than by
    /// entering play mode. Edit mode never runs <c>Awake</c>, and the alternative
    /// is a play-mode test that spins up a scene to assert on four labels.
    /// </para>
    /// </remarks>
    public sealed class HomeMenuLayoutTests
    {
        private static readonly HomeMenuAction[] MenuOrder =
        {
            HomeMenuAction.CreateRoom,
            HomeMenuAction.FindRoom,
            HomeMenuAction.Character,
            HomeMenuAction.Settings
        };

        [Test]
        public void Menu_ListsTheFourEntriesTheMockUpDraws()
        {
            using var home = new BuiltHome();

            var labels = new List<string>();
            foreach (var action in MenuOrder)
            {
                labels.Add(home.Label(action.ToString()));
            }

            Assert.That(
                labels,
                Is.EqualTo(new[] { "방 만들기", "게임 찾기", "캐릭터", "환경 설정" }));
        }

        [Test]
        public void Menu_StacksEntriesAtTheDesignedPitch()
        {
            using var home = new BuiltHome();

            for (var index = 0; index < MenuOrder.Length; index++)
            {
                var item = home.Rect(MenuOrder[index].ToString());
                Assert.That(
                    item.anchoredPosition.y,
                    Is.EqualTo(-index * HomeStyle.Layout.MenuPitch).Within(0.01f),
                    $"{MenuOrder[index]} sits off the menu's pitch.");
            }
        }

        [Test]
        public void ClickingAnything_RaisesTheActionBehindIt()
        {
            var expected = new Dictionary<string, HomeMenuAction>
            {
                { HomeMenuAction.CreateRoom.ToString(), HomeMenuAction.CreateRoom },
                { HomeMenuAction.FindRoom.ToString(), HomeMenuAction.FindRoom },
                { HomeMenuAction.Character.ToString(), HomeMenuAction.Character },
                { HomeMenuAction.Settings.ToString(), HomeMenuAction.Settings },
                { "QuitButton", HomeMenuAction.Quit },
                { "ProfileChip", HomeMenuAction.ProfileSettings },
                { "FriendButton", HomeMenuAction.Friends },
                { "ServerButton", HomeMenuAction.ServerSettings }
            };

            using var home = new BuiltHome();
            var raised = new List<HomeMenuAction>();
            home.View.ActionClicked += raised.Add;

            foreach (var pair in expected)
            {
                raised.Clear();
                home.Rect(pair.Key).GetComponent<Button>().onClick.Invoke();
                Assert.That(raised, Is.EqualTo(new[] { pair.Value }), $"{pair.Key} is miswired.");
            }
        }

        [Test]
        public void ProfileChip_GrowsWithTheNameWithinTheDesignedWidths()
        {
            using var home = new BuiltHome();
            var chip = home.Rect("ProfileChip");

            home.View.SetNickname("김");
            var shortest = chip.sizeDelta.x;

            home.View.SetNickname("금오산냥냥이");
            var middling = chip.sizeDelta.x;

            home.View.SetNickname("이건바로열두글자이지렁롱");
            var longest = chip.sizeDelta.x;

            Assert.That(shortest, Is.EqualTo(HomeStyle.Layout.ChipMinWidth).Within(0.01f));
            Assert.That(longest, Is.GreaterThan(middling));
            Assert.That(middling, Is.GreaterThanOrEqualTo(shortest));
            Assert.That(longest, Is.LessThanOrEqualTo(HomeStyle.Layout.ChipMaxWidth));
            Assert.That(chip.sizeDelta.y, Is.EqualTo(HomeStyle.Layout.ChipHeight).Within(0.01f));
        }

        [Test]
        public void ProfileChip_StopsAtTheMaximumWidth()
        {
            using var home = new BuiltHome();
            var chip = home.Rect("ProfileChip");

            // Longer than a nickname is allowed to be, which is the case the
            // ceiling exists for: the chip must not run under the friend button.
            home.View.SetNickname(new string('가', 40));

            Assert.That(
                chip.sizeDelta.x,
                Is.EqualTo(HomeStyle.Layout.ChipMaxWidth).Within(0.01f));
        }

        [Test]
        public void FriendList_WithNobodyInIt_SaysSoUnderBothHeadings()
        {
            using var home = new BuiltHome();
            var view = (IHomeMenuView)home.View;

            view.SetFriends(
                System.Array.Empty<FriendSummary>(), System.Array.Empty<FriendSummary>());

            foreach (var name in new[] { "OnlineEmptyMessage", "OfflineEmptyMessage" })
            {
                var empty = home.Rect(name);
                Assert.That(empty.gameObject.activeSelf, Is.True, name);
                Assert.That(
                    empty.GetComponent<TMPro.TMP_Text>().text, Is.EqualTo("친구가 없어요"), name);
            }
        }

        [Test]
        public void FriendList_KeepsBothHeadingsAndMarksOnlyTheEmptyOne()
        {
            using var home = new BuiltHome();
            var view = (IHomeMenuView)home.View;

            view.SetFriends(
                new[] { new FriendSummary("p1", "가나다", FriendPresence.Online) },
                System.Array.Empty<FriendSummary>());

            Assert.That(
                home.Section("온라인").gameObject.activeSelf,
                Is.True,
                "머리글은 비어 있든 아니든 남는다.");
            Assert.That(home.Section("오프라인").gameObject.activeSelf, Is.True);

            Assert.That(
                home.Rect("OnlineEmptyMessage").gameObject.activeSelf,
                Is.False,
                "친구가 있는 섹션에 없다는 안내가 뜨면 안 된다.");
            Assert.That(home.Rect("OfflineEmptyMessage").gameObject.activeSelf, Is.True);
        }

        /// <summary>
        /// A Home screen assembled in memory, torn down with the test.
        /// </summary>
        /// <summary>
        /// An open panel marks the button that opened it, without the pointer.
        /// </summary>
        /// <remarks>
        /// The outline used to mean "the pointer is here" and nothing else, so
        /// a player with a panel open and the mouse anywhere else had no way to
        /// tell which of the three buttons they were inside.
        /// </remarks>
        [Test]
        public void OpeningAPanel_OutlinesTheButtonThatOpensIt()
        {
            using var home = new BuiltHome();

            foreach (var pair in PanelButtons)
            {
                var stroke = home.Stroke(pair.Key);
                Assert.That(stroke.enabled, Is.False, $"{pair.Key} starts outlined.");

                pair.Value(home.View, true);
                Assert.That(
                    stroke.enabled, Is.True, $"{pair.Key} is not outlined while its panel is up.");

                pair.Value(home.View, false);
                Assert.That(
                    stroke.enabled, Is.False, $"{pair.Key} stays outlined after its panel closes.");
            }
        }

        /// <summary>
        /// Opening one panel does not leave another button outlined.
        /// </summary>
        [Test]
        public void OpeningAPanel_LeavesTheOtherButtonsUnmarked()
        {
            using var home = new BuiltHome();

            home.View.SetFriendListVisible(true);

            Assert.That(home.Stroke("FriendButton").enabled, Is.True);
            Assert.That(home.Stroke("ProfileChip").enabled, Is.False);
            Assert.That(home.Stroke("ServerButton").enabled, Is.False);
        }

        /// <summary>
        /// A search that found somebody never also says nobody was found.
        /// </summary>
        /// <remarks>
        /// The two arrive separately: the rows come from the search, and the
        /// success then clears the last failure. That clear used to assume an
        /// empty result and put the message back over a player who was already
        /// on screen, which is what the panel showed.
        /// </remarks>
        [Test]
        public void ClearingAnErrorAfterAHit_DoesNotSayNobodyWasFound()
        {
            using var home = new BuiltHome();
            home.View.SetFriendSearchVisible(true);
            home.Typed("가짜크런키더블크런치바");

            home.View.SetFriendSearchResults(
                new[]
                {
                    new FriendSearchHit("p1", "가짜크런키더블크런치바", FriendRequestState.None)
                });
            Assert.That(
                home.SearchEmpty().activeSelf, Is.False, "The hit alone already reads as empty.");

            home.View.SetFriendActionError(string.Empty);

            Assert.That(
                home.SearchEmpty().activeSelf,
                Is.False,
                "Clearing the failure put the not-found line back over a player who was found.");
        }

        /// <summary>
        /// A search that found nobody still says so once the error clears.
        /// </summary>
        [Test]
        public void ClearingAnErrorWithNoHits_StillSaysNobodyWasFound()
        {
            using var home = new BuiltHome();
            home.View.SetFriendSearchVisible(true);
            home.Typed("없는사람");

            home.View.SetFriendSearchResults(Array.Empty<FriendSearchHit>());
            home.View.SetFriendActionError(string.Empty);

            Assert.That(home.SearchEmpty().activeSelf, Is.True);
        }

        private static readonly Dictionary<string, Action<HomeMenuView, bool>> PanelButtons =
            new Dictionary<string, Action<HomeMenuView, bool>>
            {
                { "FriendButton", (view, visible) => view.SetFriendListVisible(visible) },
                { "ProfileChip", (view, visible) => view.SetProfileSettingsVisible(visible) },
                { "ServerButton", (view, visible) => view.SetServerSettingsVisible(visible) }
            };

        private sealed class BuiltHome : IDisposable
        {
            private readonly GameObject root;

            public BuiltHome()
            {
                root = new GameObject("HomeMenuViewUnderTest");
                View = root.AddComponent<HomeMenuView>();

                var build = typeof(HomeMenuView).GetMethod(
                    "BuildLayout", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(build, Is.Not.Null, "HomeMenuView.BuildLayout is gone.");
                build.Invoke(View, null);
            }

            public HomeMenuView View { get; }

            public RectTransform Rect(string name)
            {
                foreach (var candidate in root.GetComponentsInChildren<RectTransform>(true))
                {
                    if (candidate.name == name)
                    {
                        return candidate;
                    }
                }

                Assert.Fail($"Home does not draw anything named {name}.");
                return null;
            }

            /// <summary>
            /// A section heading, found by the words it shows rather than by
            /// name: every heading is called "Section".
            /// </summary>
            public RectTransform Section(string label)
            {
                foreach (var candidate in root.GetComponentsInChildren<TMPro.TMP_Text>(true))
                {
                    if (candidate.text == label)
                    {
                        return candidate.rectTransform;
                    }
                }

                Assert.Fail($"Home does not draw a section headed {label}.");
                return null;
            }

            public string Label(string name)
            {
                return Rect(name).GetComponent<TMPro.TMP_Text>().text;
            }

            /// <summary>
            /// The outline drawn under a bottom-bar button.
            /// </summary>
            public Image Stroke(string buttonName)
            {
                var stroke = Rect(buttonName).Find("Stroke");
                Assert.That(stroke, Is.Not.Null, $"{buttonName} draws no Stroke.");
                return stroke.GetComponent<Image>();
            }

            /// <summary>
            /// The line that says a search found nobody.
            /// </summary>
            public GameObject SearchEmpty()
            {
                return Private<TMPro.TMP_Text>("searchEmptyText").gameObject;
            }

            /// <summary>
            /// Puts a query in the search box, which is what tells the panel a
            /// search was asked for at all.
            /// </summary>
            public void Typed(string query)
            {
                Private<TMPro.TMP_InputField>("friendSearchInput").text = query;
            }

            private T Private<T>(string name) where T : class
            {
                var field = typeof(HomeMenuView).GetField(
                    name, BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(field, Is.Not.Null, $"HomeMenuView.{name} is gone.");
                var value = field.GetValue(View) as T;
                Assert.That(value, Is.Not.Null, $"HomeMenuView.{name} was never built.");
                return value;
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
