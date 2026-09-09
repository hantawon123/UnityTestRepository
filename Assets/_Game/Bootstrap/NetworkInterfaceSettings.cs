using System;
using System.Collections.Generic;
using Game.Core.Settings;
using Game.Network.Session;
using UnityEngine;
using VContainer.Unity;

namespace Game.Bootstrap
{
    public sealed class NetworkInterfaceSettings : ITickable, IDisposable
    {
        private readonly NetworkRunnerService network;
        private readonly InterfaceSettingsSystem settings;
        private readonly InterfacePresentation presentation;
        private double nextRefresh;
        private bool hadSession;
        public NetworkInterfaceSettings(NetworkRunnerService network, InterfaceSettingsSystem settings,
            InterfacePresentation presentation)
        { this.network = network; this.settings = settings; this.presentation = presentation; }
        public void Tick()
        {
            if (Time.unscaledTimeAsDouble < nextRefresh) return;
            nextRefresh = Time.unscaledTimeAsDouble + 0.25;
            if (!network.HasRoomSession)
            {
                if (hadSession) presentation.ClearPermissions();
                hadSession = false;
                return;
            }
            hadSession = true;
            var avatars = network.PlayerAvatars;
            string localUserId = null;
            foreach (var avatar in avatars)
                if (avatar != null && avatar.Object != null && avatar.Object.IsValid && avatar.IsOwner)
                    localUserId = avatar.UserId.ToString();
            foreach (var avatar in avatars)
            {
                if (avatar == null || avatar.Object == null || !avatar.Object.IsValid) continue;
                if (avatar.IsOwner)
                {
                    avatar.GetComponent<Game.Client.Interactions.PlayerInteractor>()?.SetInterfaceHudVisible(
                        network.IsWaitingForMatch || settings.Current.IsOn(InterfaceOption.InGameUi));
                    var scope = settings.Current.Get(InterfaceOption.OwnNickname);
                    var mode = scope == InterfaceCatalog.Off ? 0 : scope == InterfaceCatalog.FriendsOnly ? 2 : 1;
                    var viewers = new List<string>();
                    if (mode == 2)
                        foreach (var peer in avatars)
                            if (peer != null && peer.Object != null && peer.Object.IsValid && presentation.IsFriend(peer.PlayerId))
                            {
                                var id = peer.UserId.ToString();
                                if (!string.IsNullOrEmpty(id) && !id.Contains(",")) viewers.Add(id);
                            }
                    viewers.Sort(StringComparer.Ordinal);
                    var encoded = string.Join(",", viewers);
                    if (avatar.NicknameVisibility != mode || avatar.NicknameViewers.ToString() != encoded)
                        avatar.RPC_SetNicknameVisibility(mode, encoded);
                }
                var allowed = avatar.NicknameVisibility == 1 || avatar.NicknameVisibility == 2 &&
                    !string.IsNullOrEmpty(localUserId) &&
                    Array.IndexOf(avatar.NicknameViewers.ToString().Split(','), localUserId) >= 0;
                presentation.SetPermission(avatar.PlayerId, allowed);
            }
        }
        public void Dispose() => presentation.ClearPermissions(notify: false);
    }
}
