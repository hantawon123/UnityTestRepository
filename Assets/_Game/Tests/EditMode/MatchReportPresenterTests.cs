using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Game.Client.Match;
using Game.Core.Backend;
using Game.Core.Home;
using Game.Core.Lobby;
using Game.Core.Ports;
using Game.Core.Rooms;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class MatchReportPresenterTests
    {
        [Test]
        public async Task Report_UsesAccountIdAndRejectsInvalidOrDuplicateSubmission()
        {
            var view = new View();
            var gateway = new Gateway();
            using var room = new RoomBrowserSystem();
            room.SetLocalPlayer("room-self");
            room.SetParticipants(new[] {
                new RoomParticipant("room-self", 0, true, "나", "account-self"),
                new RoomParticipant("room-other", 1, false, "상대", "account-other"),
                new RoomParticipant("legacy", 2, false, "이전 클라이언트") });
            var profile = new PlayerProfile("나");
            profile.SetAccountId("account-self");
            using var presenter = new MatchReportPresenter(view, gateway, room, profile);
            presenter.Start();
            view.Show();
            Assert.That(view.Targets.Count, Is.EqualTo(1));
            await presenter.SubmitAsync("room-self", ReportReason.Abuse, "");
            await presenter.SubmitAsync("room-other", (ReportReason)99, "");
            await presenter.SubmitAsync("room-other", ReportReason.Abuse, new string('a', 201));
            Assert.That(gateway.Calls, Is.Zero);
            var pending = presenter.SubmitAsync("room-other", ReportReason.Abuse, " 메모 ").AsTask();
            await presenter.SubmitAsync("room-other", ReportReason.Abuse, "중복");
            Assert.That(gateway.Calls, Is.EqualTo(1));
            Assert.That(gateway.Target, Is.EqualTo("account-other"));
            Assert.That(gateway.Note, Is.EqualTo("메모"));
            gateway.Response.TrySetResult(BackendResult.Success());
            await pending;
            Assert.That(view.Message, Is.EqualTo("신고가 접수되었습니다."));
            await presenter.SubmitAsync("room-other", ReportReason.Other, "중복");
            Assert.That(gateway.Calls, Is.EqualTo(1));
        }

        [Test]
        public async Task Report_CloseAndReopenIgnoresPreviousSuccess()
        {
            var view = new View();
            var gateway = new Gateway();
            using var room = new RoomBrowserSystem();
            room.SetLocalPlayer("self");
            room.SetParticipants(new[] { new RoomParticipant("other", 1, false, "상대", "account-other") });
            var profile = new PlayerProfile("나");
            profile.SetAccountId("account-self");
            using var presenter = new MatchReportPresenter(view, gateway, room, profile);
            presenter.Start();
            view.Show();
            var pending = presenter.SubmitAsync("other", ReportReason.Spam, "").AsTask();
            view.Hide();
            Assert.That(gateway.Token.IsCancellationRequested, Is.True);
            view.Show();
            gateway.Response.TrySetResult(BackendResult.Success());
            await pending;
            Assert.That(view.Message, Is.Empty);
        }

        private sealed class Gateway : IReportGateway
        {
            public int Calls;
            public string Target, Note;
            public CancellationToken Token;
            public readonly UniTaskCompletionSource<BackendResult> Response = new();
            public UniTask<BackendResult> ReportAsync(string playerId, ReportReason reason, string note, CancellationToken cancellation)
            {
                Calls++; Target = playerId; Note = note; Token = cancellation;
                return Response.Task;
            }
        }

        private sealed class View : IMatchReportView
        {
            public event Action OpenRequested;
            public event Action Closed;
            public event Action<string, ReportReason, string> Submitted;
            public IReadOnlyList<RoomParticipant> Targets;
            public string Message;
            public void Show() => OpenRequested?.Invoke();
            public void Hide() => Closed?.Invoke();
            public void Open(IReadOnlyList<RoomParticipant> targets) { Targets = targets; Message = ""; }
            public void SetStatus(string message, bool canSubmit) => Message = message;
        }
    }
}
