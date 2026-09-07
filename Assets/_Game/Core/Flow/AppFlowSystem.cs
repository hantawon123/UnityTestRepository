using System;

namespace Game.Core.Flow
{
    public enum AppFlowState
    {
        Home,
        RoomBrowser,
        Lobby,
        InGame,
        Highlight,
        Result,

        /// <summary>
        /// Appended rather than placed beside <see cref="Home"/> so the numbers
        /// the existing states carry do not shift.
        /// </summary>
        CharacterCloset
    }

    public sealed class AppFlowSystem
    {
        public AppFlowState CurrentState { get; private set; } = AppFlowState.Home;

        public event Action<AppFlowState> StateChanged;

        public bool TryTransitionTo(AppFlowState nextState)
        {
            if (!CanTransitionTo(nextState))
            {
                return false;
            }

            CurrentState = nextState;
            StateChanged?.Invoke(nextState);
            return true;
        }

        public bool CanTransitionTo(AppFlowState nextState)
        {
            switch (CurrentState)
            {
                case AppFlowState.Home:
                    return nextState == AppFlowState.CharacterCloset ||
                           nextState == AppFlowState.RoomBrowser ||
                           nextState == AppFlowState.Lobby;

                // The closet is a detour rather than a step forward: the only
                // way on from it is back where it was opened from.
                case AppFlowState.CharacterCloset:
                    return nextState == AppFlowState.Home;
                case AppFlowState.RoomBrowser:
                    return nextState == AppFlowState.Home ||
                           nextState == AppFlowState.Lobby;
                case AppFlowState.Lobby:
                    return nextState == AppFlowState.RoomBrowser ||
                           nextState == AppFlowState.InGame;
                case AppFlowState.InGame:
                    return nextState == AppFlowState.Highlight ||
                           nextState == AppFlowState.Lobby;
                case AppFlowState.Highlight:
                    return nextState == AppFlowState.Result;
                case AppFlowState.Result:
                    return nextState == AppFlowState.Lobby;
                default:
                    return false;
            }
        }

        /// <summary>A terminated room can leave from any phase without replaying intermediate states.</summary>
        public bool TryExitSession(AppFlowState destination = AppFlowState.Home)
        {
            if (destination != AppFlowState.Home && destination != AppFlowState.RoomBrowser) return false;
            if (!IsSessionState(CurrentState)) return false;
            CurrentState = destination;
            StateChanged?.Invoke(CurrentState);
            return true;
        }

        /// <summary>Aligns an existing room with confirmed state, including snapshot rollback.</summary>
        public bool TryRestoreSessionState(AppFlowState restoredState)
        {
            if (!IsSessionState(CurrentState) || !IsSessionState(restoredState)) return false;
            if (CurrentState == restoredState) return true;
            CurrentState = restoredState;
            StateChanged?.Invoke(restoredState);
            return true;
        }

        private static bool IsSessionState(AppFlowState state) =>
            state is AppFlowState.Lobby or AppFlowState.InGame or AppFlowState.Highlight or AppFlowState.Result;
    }
}
