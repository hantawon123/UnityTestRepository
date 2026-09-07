using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core.Backend;
using Game.Core.Home;
using Game.Core.Lobby;
using Game.Core.Ports;
using Game.Core.Rooms;
using VContainer.Unity;

namespace Game.Client.Match
{
    public interface IMatchReportView
    {
        event Action OpenRequested;
        event Action Closed;
        event Action<string, ReportReason, string> Submitted;
        void Open(IReadOnlyList<RoomParticipant> targets);
        void SetStatus(string message, bool canSubmit);
    }

    public sealed class MatchReportPresenter : IStartable, IDisposable
    {
        private readonly IMatchReportView view;
        private readonly IReportGateway gateway;
        private readonly RoomBrowserSystem room;
        private readonly PlayerProfile profile;
        private CancellationTokenSource request;
        private bool busy;
        private bool sent;

        public MatchReportPresenter(IMatchReportView view, IReportGateway gateway,
            RoomBrowserSystem room, PlayerProfile profile)
        {
            this.view = view;
            this.gateway = gateway;
            this.room = room;
            this.profile = profile;
        }

        public void Start()
        {
            view.OpenRequested += Open;
            view.Closed += Close;
            view.Submitted += Submit;
        }

        private void Open()
        {
            Close();
            request = new CancellationTokenSource();
            view.Open(room.Participants.CurrentValue.Where(p =>
                p.PlayerId != room.LocalPlayerId.CurrentValue &&
                !string.IsNullOrEmpty(p.AccountId) && p.AccountId != profile.AccountId).ToArray());
        }

        private void Close()
        {
            request?.Cancel();
            request?.Dispose();
            request = null;
            busy = sent = false;
        }

        private void Submit(string playerId, ReportReason reason, string note) =>
            SubmitAsync(playerId, reason, note).Forget();

        public async UniTask SubmitAsync(string playerId, ReportReason reason, string note)
        {
            if (request == null || busy || sent) return;
            var target = room.Participants.CurrentValue.FirstOrDefault(p => p.PlayerId == playerId);
            note = note?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(profile.AccountId) || string.IsNullOrEmpty(target.AccountId) ||
                playerId == room.LocalPlayerId.CurrentValue || target.AccountId == profile.AccountId ||
                !Enum.IsDefined(typeof(ReportReason), reason) || note.Length > 200)
            {
                view.SetStatus("신고 대상과 사유를 확인해 주세요. 메모는 200자까지 입력할 수 있습니다.", true);
                return;
            }

            var lifetime = request;
            busy = true;
            view.SetStatus("신고를 접수하고 있습니다…", false);
            try
            {
                var result = await gateway.ReportAsync(target.AccountId, reason, note, lifetime.Token);
                if (request != lifetime || lifetime.IsCancellationRequested) return;
                sent = result.Ok;
                view.SetStatus(result.Ok ? "신고가 접수되었습니다." : Describe(result.Failure), !sent);
            }
            catch (OperationCanceledException) { }
            catch (Exception)
            {
                if (request == lifetime && !lifetime.IsCancellationRequested)
                    view.SetStatus("신고를 전송하지 못했습니다. 잠시 후 다시 시도해 주세요.", true);
            }
            finally
            {
                if (request == lifetime) busy = false;
            }
        }

        private static string Describe(BackendFailure failure) => failure switch
        {
            BackendFailure.NotSignedIn or BackendFailure.AccountNotFound => "로그인 상태를 확인해 주세요.",
            BackendFailure.TargetNotFound => "신고 대상을 찾을 수 없습니다. 화면을 다시 열어 주세요.",
            BackendFailure.InvalidRequest => "신고 사유와 메모를 확인해 주세요.",
            _ => "신고를 전송하지 못했습니다. 잠시 후 다시 시도해 주세요."
        };

        public void Dispose()
        {
            view.OpenRequested -= Open;
            view.Closed -= Close;
            view.Submitted -= Submit;
            Close();
        }
    }
}
