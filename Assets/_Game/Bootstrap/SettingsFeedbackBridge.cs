using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Client.Settings;
using Game.Core.Backend;
using Game.Core.Ports;
using UnityEngine;
using VContainer.Unity;

namespace Game.Bootstrap
{
    /// <summary>
    /// Carries what a player wrote in 설정 → 피드백 보내기 to the server.
    /// </summary>
    /// <remarks>
    /// The same shape <see cref="HomeProfileBridge"/> uses: the screen raises an
    /// event, this sends it on, and the answer decides what the screen says
    /// next. The panel's own state — what is typed, whether 보내기 is armed —
    /// stays with <see cref="SettingsPresenter"/>, which knows nothing about
    /// the network.
    /// <para>
    /// <b>Failure leaves what was written alone.</b> Somebody who filled the box
    /// and lost it to a timeout does not write it again, so a refusal re-arms
    /// 보내기 and says why, and nothing clears the box. Only a send that the
    /// server confirmed closes the panel.
    /// </para>
    /// <para>
    /// Sign-in is not a dependency here, unlike the profile bridge. That one
    /// runs at startup and would race sign-in; this one runs when a player
    /// presses a button on a screen two navigations deep. If somehow nothing is
    /// signed in, the gateway refuses before sending
    /// (<see cref="BackendFailure.NotSignedIn"/>) and that reads on screen as
    /// what it is.
    /// </para>
    /// </remarks>
    public sealed class SettingsFeedbackBridge : IStartable, IDisposable
    {
        private readonly ISettingsView view;
        private readonly IFeedbackGateway feedback;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();

        /// <summary>
        /// Whether a send is in flight.
        /// </summary>
        /// <remarks>
        /// The presenter also darkens 보내기 on press, so this is the second
        /// guard rather than the first. It is here because the button comes back
        /// alive as soon as the player types another character, and typing while
        /// a slow send is in flight must not queue a second one.
        /// </remarks>
        private bool sending;

        public SettingsFeedbackBridge(ISettingsView view, IFeedbackGateway feedback)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));
        }

        public void Start()
        {
            view.FeedbackSubmitted += OnFeedbackSubmitted;
        }

        public void Dispose()
        {
            view.FeedbackSubmitted -= OnFeedbackSubmitted;
            lifetime.Cancel();
            lifetime.Dispose();
        }

        private void OnFeedbackSubmitted(string message)
        {
            SendAsync(message).Forget();
        }

        private async UniTaskVoid SendAsync(string message)
        {
            if (sending || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            sending = true;

            try
            {
                var result = await feedback.SendAsync(message, lifetime.Token);

                if (lifetime.IsCancellationRequested)
                {
                    return;
                }

                if (result.Ok)
                {
                    // Down first, then the thanks over the screen behind it.
                    view.FeedbackSent();
                    view.ShowNotice(SettingsStyle.FeedbackNoticeTitle, SettingsStyle.FeedbackSentMessage);
                    return;
                }

                // The screen is going away or already gone; there is nobody to tell.
                if (result.Failure == BackendFailure.Cancelled)
                {
                    return;
                }

                view.SetFeedbackSubmitEnabled(true);
                view.ShowNotice(
                    SettingsStyle.FeedbackNoticeTitle,
                    $"{Explain(result.Failure)}. {SettingsStyle.FeedbackKeptMessage}");

                Debug.LogWarning($"[Feedback] Refused: {result.Failure}.");
            }
            finally
            {
                sending = false;
            }
        }

        /// <summary>
        /// What to say about a refusal.
        /// </summary>
        /// <remarks>
        /// Only the failures a player can do something about get their own line.
        /// The rest share one message: a code the player cannot act on is noise,
        /// and the code itself goes to the log instead.
        /// </remarks>
        private static string Explain(BackendFailure failure)
        {
            switch (failure)
            {
                case BackendFailure.NotSignedIn:
                    return SettingsStyle.FeedbackNotSignedInMessage;

                case BackendFailure.Offline:
                case BackendFailure.Timeout:
                    return SettingsStyle.FeedbackOfflineMessage;

                case BackendFailure.InvalidRequest:
                    return SettingsStyle.FeedbackTooLongMessage;

                default:
                    return SettingsStyle.FeedbackFailedMessage;
            }
        }
    }
}
