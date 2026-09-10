using System;
using Game.Core.Settings;
using Game.Network.Session;
using UnityEngine;
using VContainer.Unity;

namespace Game.Bootstrap
{
    /// <summary>
    /// Tells the room what this player wants to be called, and takes down what
    /// everybody else has said about themselves.
    /// </summary>
    /// <remarks>
    /// <see cref="PlayerAvatarNaming"/> holds the three answers a player can
    /// publish. Anything else that arrives is treated as 아직 모름, so a build
    /// that speaks an older dialect cannot cause a name to be shown that its
    /// owner wanted hidden.
    /// </remarks>
    public sealed class NetworkInterfaceSettings : ITickable, IDisposable
    {
        /// <summary>
        /// What travels in <c>PlayerAvatar.NicknameVisibility</c>.
        /// </summary>
        private static class PlayerAvatarNaming
        {
            /// <summary>The owner has not said yet. Nothing is shown.</summary>
            public const int Unsaid = 0;

            /// <summary>The owner is happy to be called by their own name.</summary>
            public const int RealName = 1;

            /// <summary>
            /// 스트리머 모드. The name to use travels beside this, in the same
            /// string that used to carry who was allowed to see the real one.
            /// </summary>
            public const int Pseudonymous = 2;
        }

        private readonly NetworkRunnerService network;
        private readonly InterfaceSettingsSystem settings;
        private readonly InterfacePresentation presentation;
        private double nextRefresh;
        private bool hadSession;

        /// <summary>
        /// The name this player goes by while 스트리머 모드 is on, made once
        /// for as long as they stay in the room.
        /// </summary>
        /// <remarks>
        /// Held rather than derived each tick so that the room can talk to
        /// somebody by the name it saw a moment ago. Dropped with the session,
        /// which is what makes the next visit a different name.
        /// </remarks>
        private string pseudonym;

        public NetworkInterfaceSettings(NetworkRunnerService network, InterfaceSettingsSystem settings,
            InterfacePresentation presentation)
        { this.network = network; this.settings = settings; this.presentation = presentation; }

        public void Tick()
        {
            if (Time.unscaledTimeAsDouble < nextRefresh) return;
            nextRefresh = Time.unscaledTimeAsDouble + 0.25;
            if (!network.HasRoomSession)
            {
                if (hadSession)
                {
                    presentation.ClearPermissions();
                    pseudonym = null;
                }
                hadSession = false;
                return;
            }
            hadSession = true;
            var avatars = network.PlayerAvatars;
            foreach (var avatar in avatars)
            {
                if (avatar == null || avatar.Object == null || !avatar.Object.IsValid) continue;
                if (avatar.IsOwner)
                {
                    avatar.GetComponent<Game.Client.Interactions.PlayerInteractor>()?.SetInterfaceHudVisible(
                        network.IsWaitingForMatch || settings.Current.IsOn(InterfaceOption.InGameUi));

                    var streaming = settings.Current.IsOn(InterfaceOption.StreamerMode);
                    var mode = streaming
                        ? PlayerAvatarNaming.Pseudonymous
                        : PlayerAvatarNaming.RealName;
                    var name = streaming ? EnsurePseudonym(avatar.UserId.ToString()) : string.Empty;
                    if (avatar.NicknameVisibility != mode || avatar.NicknameViewers.ToString() != name)
                        avatar.RPC_SetNicknameVisibility(mode, name);
                }

                switch (avatar.NicknameVisibility)
                {
                    case PlayerAvatarNaming.RealName:
                        presentation.SetPublishedName(avatar.PlayerId, real: true, pseudonym: null);
                        break;
                    case PlayerAvatarNaming.Pseudonymous:
                        presentation.SetPublishedName(
                            avatar.PlayerId, real: false, avatar.NicknameViewers.ToString());
                        break;
                    default:
                        presentation.ClearPublishedName(avatar.PlayerId);
                        break;
                }
            }
        }

        /// <summary>
        /// This visit's pseudonym, made the first time it is wanted.
        /// </summary>
        /// <remarks>
        /// The account id goes into the seed beside a fresh one of our own, so
        /// that two players who join at the same moment are unlikely to be
        /// given the same name. What makes the name change between visits is
        /// the fresh half; the account id on its own would give the same person
        /// the same name for ever, which is a name they could be followed by.
        /// </remarks>
        private string EnsurePseudonym(string userId)
        {
            if (!string.IsNullOrEmpty(pseudonym))
            {
                return pseudonym;
            }

            var seed = Guid.NewGuid().GetHashCode();
            if (!string.IsNullOrEmpty(userId))
            {
                seed ^= userId.GetHashCode();
            }

            pseudonym = Pseudonym.From(seed);
            return pseudonym;
        }

        public void Dispose() => presentation.ClearPermissions(notify: false);
    }
}
