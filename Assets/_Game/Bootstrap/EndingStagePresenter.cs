using System;
using System.Collections.Generic;
using Game.Client.Match;
using Game.Client.Players;
using Game.Core.Lobby;
using Game.Core.Match;
using Game.Network.Players;
using UnityEngine;
using VContainer.Unity;

namespace Game.Bootstrap
{
    /// <summary>
    /// 결과 화면 동안 유치장 무대에 플레이어를 세운다. 탈출한 사람은 철창 앞에서 철창을 보고,
    /// 체포된 사람은 철창 안에서 카메라를 본다.
    /// </summary>
    /// <remarks>
    /// The real avatars stay where the match left them; what stands on the
    /// stage is a <see cref="ReplayVisual"/> copy of each one, the same trick
    /// the highlight replay uses. Copies carry the player's own look without
    /// touching networked transforms, and disposing them brings the originals
    /// back for the replay that follows.
    /// </remarks>
    public sealed class EndingStagePresenter : IStartable, ITickable, IDisposable
    {
        private readonly NetworkResultLobbyReturnController result;
        private readonly RoomBrowserSystem room;
        private readonly EndingStage stage;
        private readonly List<ReplayVisual> visuals = new();
        private bool built;

        public EndingStagePresenter(
            NetworkResultLobbyReturnController result,
            RoomBrowserSystem room,
            EndingStage stage)
        {
            this.result = result ?? throw new ArgumentNullException(nameof(result));
            this.room = room ?? throw new ArgumentNullException(nameof(room));
            this.stage = stage ?? throw new ArgumentNullException(nameof(stage));
        }

        public void Start()
        {
            if (!stage.IsWired)
            {
                Debug.LogWarning("[Ending] EndingStage is not wired; the result screen keeps the text-only view.", stage);
                return;
            }

            stage.ShowCamera();
            TryBuild();
        }

        public void Tick()
        {
            // The result normally arrives before this scene loads, but a late
            // client can see the scene first. Keep trying until the line-up is known.
            if (!built) TryBuild();
        }

        public void Dispose()
        {
            foreach (var visual in visuals) visual.Dispose();
            visuals.Clear();
            stage.HideCamera();
        }

        private void TryBuild()
        {
            if (!stage.IsWired || !result.HasMatchResult) return;
            var participants = room.MatchParticipants.CurrentValue;
            if (participants == null || participants.Count == 0) return;

            var avatars = FindAvatars();
            if (avatars.Count == 0) return;

            var placements = EndingStageLayout.Assign(
                participants,
                result.LastWinnerPlayerIndices,
                stage.EscapeSlotCount,
                stage.ArrestSlotCount);

            foreach (var placement in placements)
            {
                if (!avatars.TryGetValue(placement.PlayerId, out var avatar)) continue;
                var slot = stage.Slot(placement.Escaped, placement.Slot);
                if (slot == null) continue;

                var visual = new ReplayVisual(avatar.transform, stage.VisualsRoot);
                visual.Target.SetPositionAndRotation(slot.position, slot.rotation);
                if (visual.Animator != null) visual.Animator.speed = 1f;
                visual.SetPlaying(true);
                visuals.Add(visual);
            }

            built = visuals.Count > 0;
            if (built)
            {
                Debug.Log($"[Ending] Staged {visuals.Count} of {participants.Count} players " +
                          $"(escaped {CountEscaped(placements)}).");
            }
        }

        private static int CountEscaped(IReadOnlyList<EndingStagePlacement> placements)
        {
            var count = 0;
            foreach (var p in placements) if (p.Escaped) count++;
            return count;
        }

        private static Dictionary<string, PlayerAvatar> FindAvatars()
        {
            var map = new Dictionary<string, PlayerAvatar>(StringComparer.Ordinal);
            foreach (var avatar in UnityEngine.Object.FindObjectsByType<PlayerAvatar>(
                         FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                var id = avatar.PlayerId;
                if (!string.IsNullOrEmpty(id)) map.TryAdd(id, avatar);
            }
            return map;
        }
    }
}
