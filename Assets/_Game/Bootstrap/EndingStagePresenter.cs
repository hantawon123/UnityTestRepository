using System;
using System.Collections.Generic;
using Game.Client.Interactions;
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
        private readonly IResultView view;
        private readonly List<ReplayVisual> visuals = new();
        private bool built;
        private bool backdropHidden;
        private PlayerMovement lockedMovement;
        private PlayerInteractor lockedInteractor;

        public EndingStagePresenter(
            NetworkResultLobbyReturnController result,
            RoomBrowserSystem room,
            EndingStage stage,
            IResultView view)
        {
            this.result = result ?? throw new ArgumentNullException(nameof(result));
            this.room = room ?? throw new ArgumentNullException(nameof(room));
            this.stage = stage ?? throw new ArgumentNullException(nameof(stage));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public void Start()
        {
            if (!stage.IsWired)
            {
                Debug.LogWarning("[Ending] EndingStage is not wired; the result screen keeps the text-only view.", stage);
                return;
            }

            // The text screen was built to sit over the match with an opaque
            // backdrop. With a stage behind it, the backdrop would hide the stage.
            view.SetBackdropVisible(false);
            backdropHidden = true;
            stage.ShowCamera();
            LockLocalInput();
            TryBuild();
        }

        public void Tick()
        {
            // The result normally arrives before this scene loads, but a late
            // client can see the scene first. Keep trying until the line-up is known.
            if (!built) TryBuild();
            // The local avatar can replicate in after Start; keep it held still
            // while the stage is up, the same lock the lobby menu uses.
            if (lockedMovement == null) LockLocalInput();
        }

        public void Dispose()
        {
            foreach (var visual in visuals) visual.Dispose();
            visuals.Clear();
            stage.HideCamera();
            if (backdropHidden) view.SetBackdropVisible(true);
            UnlockLocalInput();
        }

        /// <remarks>
        /// The camera is fixed on the stage, so walking the hidden avatar around
        /// the match map only moves the copy's source out from under it. Locking
        /// here is client-side; the authority stops judging inputs on its own.
        /// </remarks>
        private void LockLocalInput()
        {
            foreach (var avatar in UnityEngine.Object.FindObjectsByType<PlayerAvatar>(
                         FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!avatar.IsOwner) continue;
                lockedMovement = avatar.GetComponent<PlayerMovement>();
                lockedInteractor = avatar.GetComponent<PlayerInteractor>();
                if (lockedMovement != null) lockedMovement.IsMovementLocked = true;
                if (lockedInteractor != null) lockedInteractor.IsInputLocked = true;
                return;
            }
        }

        private void UnlockLocalInput()
        {
            if (lockedMovement != null) lockedMovement.IsMovementLocked = false;
            if (lockedInteractor != null) lockedInteractor.IsInputLocked = false;
            lockedMovement = null;
            lockedInteractor = null;
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
