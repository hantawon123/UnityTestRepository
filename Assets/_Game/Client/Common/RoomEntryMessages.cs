using Game.Core.Rooms;

namespace Game.Client.Common
{
    /// <summary>
    /// Which way into a room a refusal came back from.
    /// </summary>
    /// <remarks>
    /// The same failure asks for different things depending on how the player
    /// got it. A room that is not there is worth refreshing the list over when
    /// it was picked from the list, and worth re-reading the code over when a
    /// code was typed.
    /// </remarks>
    public enum RoomEntrySource
    {
        RoomList,
        RoomCode,

        /// <summary>
        /// The create-room form on the home screen. Opening a room is entering
        /// it too, so the same failures come back — but a room that is full or
        /// gone means something different when the player is the one making it.
        /// </summary>
        RoomCreate,
    }

    /// <summary>
    /// What the player is told when a room refuses them.
    /// </summary>
    /// <remarks>
    /// The heading never changes: from the player's side every one of these is
    /// the same event, which is that they tried to get into a room and did not.
    /// The line under it says what to do about it.
    /// </remarks>
    public static class RoomEntryMessages
    {
        public const string Title = "게임 접속 오류";

        /// <summary>
        /// For the failures a player can do nothing about, and for anything new
        /// that arrives before it has wording of its own.
        /// </summary>
        public const string Generic = "잠시 문제가 발생했어요. 다시 접속 시도 해주세요.";

        public static string Describe(RoomEntryFailure failure, RoomEntrySource source)
        {
            switch (failure)
            {
                case RoomEntryFailure.NotFound:
                    if (source == RoomEntrySource.RoomCreate)
                    {
                        // The room was made and then lost before this client
                        // could get into it. Nothing to refresh and no code to
                        // check: the only thing to do is make it again.
                        return "방을 만들지 못했어요. 다시 시도해 주세요.";
                    }

                    return source == RoomEntrySource.RoomCode
                        ? "그런 방이 없어요. 코드를 다시 확인해 주세요."
                        : "사라진 방이에요. 목록을 새로고침 해주세요.";

                case RoomEntryFailure.Full:
                    if (source == RoomEntrySource.RoomCreate)
                    {
                        return "방을 만들지 못했어요. 다시 시도해 주세요.";
                    }

                    return source == RoomEntrySource.RoomCode
                        ? "방이 가득 찼어요."
                        : "방이 가득 찼어요. 다른 방을 골라주세요.";

                case RoomEntryFailure.Closed:
                    return source == RoomEntrySource.RoomCreate
                        ? "방을 만들지 못했어요. 다시 시도해 주세요."
                        : "이미 게임이 시작된 방이에요.";

                case RoomEntryFailure.InvalidRequest when source == RoomEntrySource.RoomCreate:
                    // The form checks the name and the player count before it
                    // sends, so reaching this means the two disagree. Said
                    // plainly rather than blamed on the player.
                    return "방 설정을 확인해 주세요.";


                case RoomEntryFailure.InvalidCode:
                    return "방 코드를 다시 확인해 주세요.";

                case RoomEntryFailure.AlreadyInRoom:
                    return "이미 다른 방에 들어가 있어요.";

                default:
                    return Generic;
            }
        }
    }
}
