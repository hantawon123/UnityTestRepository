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

        /// <summary>
        /// A Home screen assembled in memory, torn down with the test.
        /// </summary>
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

            public string Label(string name)
            {
                return Rect(name).GetComponent<TMPro.TMP_Text>().text;
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
