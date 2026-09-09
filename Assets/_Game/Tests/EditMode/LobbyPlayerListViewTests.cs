using System.Collections.Generic;
using Game.Client.Home;
using Game.Client.Lobby;
using Game.Core.Home;
using Game.Core.Lobby;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Architecture.Tests
{
    public sealed class LobbyPlayerListViewTests
    {
        [Test]
        public void EnsureLayout_BuildsBothColumnsWithBoldTitles()
        {
            var canvas = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            try
            {
                var view = canvas.AddComponent<LobbyPlayerListView>();
                view.EnsureLayout();

                Assert.That(view.ParticipantsTitleText, Is.EqualTo(LobbyPlayerListView.ParticipantsTitle));
                Assert.That(view.FriendsTitleText, Is.EqualTo(LobbyPlayerListView.FriendsTitle));
                var participantsTitle = canvas.transform
                    .Find("Columns/Participants/Title")
                    .GetComponent<TMP_Text>();
                var friendsTitle = canvas.transform
                    .Find("Columns/Friends/Title")
                    .GetComponent<TMP_Text>();
                Assert.That(participantsTitle.fontSize, Is.EqualTo(LobbyPlayerListView.TitleFontSize));
                Assert.That(participantsTitle.font, Is.EqualTo(HomeUiFonts.ApplyBold()));
                Assert.That(friendsTitle.fontSize, Is.EqualTo(LobbyPlayerListView.TitleFontSize));
                Assert.That(friendsTitle.font, Is.EqualTo(HomeUiFonts.ApplyBold()));
                Assert.That(view.GetComponent<RectTransform>().sizeDelta.x, Is.EqualTo(LobbyPlayerListView.ModalWidth));
                var participants = canvas.transform.Find("Columns/Participants") as RectTransform;
                var friends = canvas.transform.Find("Columns/Friends") as RectTransform;
                Assert.That(
                    participants.anchorMax.x - participants.anchorMin.x,
                    Is.EqualTo(LobbyPlayerListView.ColumnWidthRatio).Within(0.001f));
                Assert.That(
                    friends.anchorMax.x - friends.anchorMin.x,
                    Is.EqualTo(LobbyPlayerListView.ColumnWidthRatio).Within(0.001f));
                var title = participants.Find("Title") as RectTransform;
                var scroll = participants.Find("Scroll") as RectTransform;
                Assert.That(title.offsetMin.x, Is.EqualTo(LobbyPlayerListView.ColumnInnerPadding));
                Assert.That(title.offsetMax.x, Is.EqualTo(-LobbyPlayerListView.ColumnInnerPadding));
                Assert.That(title.offsetMax.y, Is.EqualTo(-LobbyPlayerListView.ColumnInnerPadding));
                Assert.That(scroll.offsetMin, Is.EqualTo(new Vector2(
                    LobbyPlayerListView.ColumnInnerPadding,
                    LobbyPlayerListView.ColumnInnerPadding)));
                Assert.That(scroll.offsetMax.x, Is.EqualTo(-LobbyPlayerListView.ColumnInnerPadding));
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void SetParticipants_HostSeesKickOnOthersAndLeaderIconOnHost()
        {
            var canvas = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            try
            {
                var view = canvas.AddComponent<LobbyPlayerListView>();
                view.SetParticipants(
                    new[]
                    {
                        new LobbyParticipant("host-1", "방장닉", true),
                        new LobbyParticipant("player-2", "게스트닉", false),
                    },
                    localIsHost: true,
                    localPlayerId: "host-1");

                var hostRow = canvas.transform.Find("Columns/Participants/Scroll/RowRoot/Row_host-1");
                var guestRow = canvas.transform.Find("Columns/Participants/Scroll/RowRoot/Row_player-2");
                Assert.That(hostRow, Is.Not.Null);
                Assert.That(guestRow, Is.Not.Null);
                Assert.That(hostRow.Find("Leader").gameObject.activeSelf, Is.True);
                Assert.That(guestRow.Find("Leader").gameObject.activeSelf, Is.False);
                Assert.That(hostRow.Find("Kick"), Is.Null);
                Assert.That(guestRow.Find("Kick"), Is.Not.Null);

                var kick = guestRow.Find("Kick").GetComponent<TMP_Text>();
                Assert.That(kick.text, Is.EqualTo("강퇴"));
                Assert.That(kick.fontSize, Is.EqualTo(LobbyPlayerListView.KickFontSize));
                Assert.That(kick.color, Is.EqualTo(LobbyPlayerListView.KickColor));
                Assert.That(kick.font, Is.EqualTo(HomeUiFonts.ApplyRegular()));
                var kickRect = guestRow.Find("Kick") as RectTransform;
                Assert.That(kickRect.anchorMin.x, Is.EqualTo(1f));
                Assert.That(kickRect.anchoredPosition.x, Is.EqualTo(-LobbyPlayerListView.ActionRight));

                var name = guestRow.Find("Name").GetComponent<TMP_Text>();
                Assert.That(name.text, Is.EqualTo("게스트닉"));
                Assert.That(name.fontSize, Is.EqualTo(LobbyPlayerListView.NicknameFontSize));
                Assert.That(name.font, Is.EqualTo(HomeUiFonts.ApplyRegular()));
                Assert.That(
                    name.rectTransform.anchoredPosition.x,
                    Is.EqualTo(
                        LobbyPlayerListView.AvatarLeft
                        + LobbyPlayerListView.AvatarSize
                        + LobbyPlayerListView.NicknameLeft));

                var avatar = guestRow.Find("Avatar") as RectTransform;
                Assert.That(avatar.sizeDelta, Is.EqualTo(new Vector2(
                    LobbyPlayerListView.AvatarSize,
                    LobbyPlayerListView.AvatarSize)));
                Assert.That(avatar.anchoredPosition.x, Is.EqualTo(LobbyPlayerListView.AvatarLeft));

                var leader = hostRow.Find("Leader") as RectTransform;
                var hostName = hostRow.Find("Name") as RectTransform;
                Assert.That(
                    leader.anchoredPosition.x,
                    Is.EqualTo(hostName.anchoredPosition.x + hostName.sizeDelta.x + LobbyPlayerListView.LeaderIconGap)
                        .Within(0.5f));
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void SetParticipants_GuestDoesNotSeeKick()
        {
            var canvas = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            try
            {
                var view = canvas.AddComponent<LobbyPlayerListView>();
                view.SetParticipants(
                    new[]
                    {
                        new LobbyParticipant("host-1", "방장닉", true),
                        new LobbyParticipant("player-2", "게스트닉", false),
                    },
                    localIsHost: false,
                    localPlayerId: "player-2");

                var hostRow = canvas.transform.Find("Columns/Participants/Scroll/RowRoot/Row_host-1");
                var guestRow = canvas.transform.Find("Columns/Participants/Scroll/RowRoot/Row_player-2");
                Assert.That(hostRow.Find("Kick"), Is.Null);
                Assert.That(guestRow.Find("Kick"), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void SetParticipants_OthersGetAReportTooltipAndSelfDoesNot()
        {
            var canvas = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            try
            {
                var view = canvas.AddComponent<LobbyPlayerListView>();
                view.SetParticipants(
                    new[]
                    {
                        new LobbyParticipant("host-1", "방장닉", true),
                        new LobbyParticipant("player-2", "게스트닉", false),
                    },
                    localIsHost: false,
                    localPlayerId: "player-2");

                var hostRow = canvas.transform.Find("Columns/Participants/Scroll/RowRoot/Row_host-1");
                var selfRow = canvas.transform.Find("Columns/Participants/Scroll/RowRoot/Row_player-2");
                Assert.That(hostRow.GetComponent<LobbyReportHover>(), Is.Not.Null);
                Assert.That(selfRow.GetComponent<LobbyReportHover>(), Is.Null);
                Assert.That(selfRow.Find("Report"), Is.Null);

                var tooltip = hostRow.Find("Report") as RectTransform;
                Assert.That(tooltip, Is.Not.Null);
                Assert.That(tooltip.gameObject.activeSelf, Is.False);
                Assert.That(tooltip.anchorMin, Is.EqualTo(new Vector2(0.5f, 1f)));
                Assert.That(tooltip.pivot, Is.EqualTo(new Vector2(0.5f, 0f)));
                Assert.That(tooltip.anchoredPosition.y, Is.EqualTo(LobbyPlayerListView.ReportTooltipGap));
                var bridge = tooltip.Find(LobbyPlayerListView.ReportBridgeName) as RectTransform;
                Assert.That(bridge, Is.Not.Null);
                Assert.That(
                    bridge.sizeDelta.y,
                    Is.EqualTo(
                        LobbyPlayerListView.ReportTooltipGap
                        + LobbyPlayerListView.ReportTooltipOverlap));
                Assert.That(bridge.GetComponent<Image>().raycastTarget, Is.True);
                var label = tooltip.Find("Label").GetComponent<TMP_Text>();
                Assert.That(label.text, Is.EqualTo(LobbyPlayerListView.ReportLabel));
                Assert.That(label.fontSize, Is.EqualTo(18f));
                Assert.That(label.font, Is.EqualTo(HomeUiFonts.ApplyRegular()));
                Assert.That(label.color, Is.EqualTo(LobbyPlayerListView.ReportTooltipLabel));
                Assert.That(
                    tooltip.GetComponent<Image>().color,
                    Is.EqualTo(LobbyPlayerListView.ReportTooltipFill));

                hostRow.GetComponent<LobbyReportHover>().ShowTooltip();
                Assert.That(tooltip.gameObject.activeSelf, Is.True);

                var reported = new List<(string Id, string Name)>();
                view.ReportClicked += (id, name) => reported.Add((id, name));
                tooltip.GetComponent<Button>().onClick.Invoke();
                Assert.That(reported, Is.EqualTo(new[] { ("host-1", "방장닉") }));
                Assert.That(tooltip.gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void SetFriends_ShowsNicknamesAndPlusButtons()
        {
            var canvas = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            try
            {
                var view = canvas.AddComponent<LobbyPlayerListView>();
                view.SetFriends(new[]
                {
                    new FriendSummary("f-1", "초대할까말까할까말까", FriendPresence.Online),
                    new FriendSummary("f-2", "오프라인친구", FriendPresence.Offline),
                });

                var friends = canvas.transform.Find("Columns/Friends/Scroll/RowRoot");
                Assert.That(friends.childCount, Is.EqualTo(2));
                Assert.That(
                    friends.Find("Friend_f-1/Name").GetComponent<TMP_Text>().text,
                    Is.EqualTo("초대할까말까할까말까"));
                Assert.That(
                    friends.Find("Friend_f-2/Name").GetComponent<TMP_Text>().text,
                    Is.EqualTo("오프라인친구"));
                for (var index = 0; index < friends.childCount; index++)
                {
                    var add = friends.GetChild(index).Find("Add") as RectTransform;
                    Assert.That(add, Is.Not.Null);
                    Assert.That(add.sizeDelta, Is.EqualTo(new Vector2(
                        LobbyPlayerListView.AddButtonSize,
                        LobbyPlayerListView.AddButtonSize)));
                    Assert.That(add.anchorMin.x, Is.EqualTo(1f));
                    Assert.That(add.anchoredPosition.x, Is.EqualTo(-LobbyPlayerListView.ActionRight));
                    Assert.That(add.GetComponent<Button>(), Is.Not.Null);
                    Assert.That(add.GetComponent<Image>().sprite, Is.Not.Null);
                }

                var invited = new List<(string Id, string Name)>();
                view.InviteClicked += (id, name) => invited.Add((id, name));
                friends.Find("Friend_f-1/Add").GetComponent<Button>().onClick.Invoke();
                Assert.That(invited, Is.EqualTo(new[] { ("f-1", "초대할까말까할까말까") }));
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }
    }
}
