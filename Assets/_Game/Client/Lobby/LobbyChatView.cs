using System;
using System.Collections.Generic;
using Game.Client.Match;
using Game.Core.Lobby;
using UnityEngine;

namespace Game.Client.Lobby
{
    public interface ILobbyChatView
    {
        event Action<string> SendRequested;

        void SetMessages(IReadOnlyList<LobbyChatMessage> messages);
        void ClearInput();
        void Deactivate();
    }

    /// <summary>
    /// Playground 채팅 HUD와 같은 레이아웃. 입력창과 목록은 항상 보이고,
    /// Enter는 포커스만 켜고 끈다.
    /// </summary>
    public sealed class LobbyChatView : MonoBehaviour, ILobbyChatView
    {
        private MatchChatView hud;

        public event Action<string> SendRequested;

        public bool IsActivated => hud != null && hud.IsActivated;

        private void Awake() => EnsureHud();

        private void OnDestroy()
        {
            if (hud == null)
            {
                return;
            }

            hud.SendRequested -= HandleSend;
            hud = null;
        }

        public void SetMessages(IReadOnlyList<LobbyChatMessage> messages)
        {
            EnsureHud();
            hud.SetMessages(messages);
        }

        public void ClearInput()
        {
            EnsureHud();
            hud.ClearInput();
        }

        public void Deactivate()
        {
            EnsureHud();
            hud.Deactivate();
        }

        private void HandleSend(string text) => SendRequested?.Invoke(text);

        private void EnsureHud()
        {
            if (hud != null)
            {
                return;
            }

            hud = MatchChatView.AttachTo(gameObject, keepChromeVisible: true);
            hud.SendRequested += HandleSend;
        }
    }
}
