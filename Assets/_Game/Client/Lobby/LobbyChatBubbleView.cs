using System;
using System.Collections.Generic;
using Game.Client.Match;
using Game.Client.Players;
using Game.Core.Lobby;
using UnityEngine;

namespace Game.Client.Lobby
{
    public interface ILobbyChatBubbleView
    {
        void Show(LobbyChatMessage message);
        void Clear();
    }

    /// <summary>
    /// Playground와 같은 말풍선. 이름표는 캐릭터 위에 따로 유지한다.
    /// </summary>
    public sealed class LobbyChatBubbleView : MonoBehaviour, ILobbyChatBubbleView
    {
        private readonly Dictionary<string, PlayerNameplateView> nameplates =
            new(StringComparer.Ordinal);
        private MatchChatBubbleView bubbles;

        public static LobbyChatBubbleView Create(Transform parent)
        {
            var root = new GameObject("ChatBubbleWorld");
            root.transform.SetParent(parent, false);
            return root.AddComponent<LobbyChatBubbleView>();
        }

        private void Awake()
        {
            StripLegacyAnchors();
            EnsureBubbles();
        }

        public void BindPlayer(string playerId, Transform playerRoot, string displayName)
        {
            if (string.IsNullOrWhiteSpace(playerId) || playerRoot == null)
            {
                return;
            }

            var id = playerId.Trim();
            EnsureBubbles();
            bubbles.BindPlayer(id, playerRoot);

            var plate = PlayerNameplateView.Attach(playerRoot);
            plate.SetNickname(displayName);
            nameplates[id] = plate;
        }

        public void Show(LobbyChatMessage message)
        {
            EnsureBubbles();
            bubbles.Show(message);
        }

        public void Clear()
        {
            EnsureBubbles();
            bubbles.Clear();
        }

        public void ClearBindings()
        {
            Clear();
            foreach (var plate in nameplates.Values)
            {
                if (plate != null)
                {
                    plate.SetNickname(string.Empty);
                }
            }

            nameplates.Clear();
        }

        private void EnsureBubbles()
        {
            if (bubbles != null)
            {
                return;
            }

            bubbles = GetComponentInChildren<MatchChatBubbleView>(true)
                ?? MatchChatBubbleView.Create(transform);
        }

        private void StripLegacyAnchors()
        {
            for (var index = transform.childCount - 1; index >= 0; index--)
            {
                var child = transform.GetChild(index);
                if (child.name.StartsWith("Head_", StringComparison.Ordinal))
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
    }
}
