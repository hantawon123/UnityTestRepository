using UnityEngine;

namespace Game.Client.Home
{
    /// <summary>
    /// Every colour and measurement the Home screen draws with, in one place.
    /// </summary>
    /// <remarks>
    /// The screen is built in code rather than authored in the scene, so these
    /// numbers are the only record of the design. Keeping them here means a
    /// revised mock-up is one file to edit, and it lets the menu, the profile
    /// chip and the icon buttons agree on a colour without copying literals
    /// between them.
    /// <para>
    /// Measurements are in the 1920x1080 the mock-up was drawn at, which is also
    /// the canvas reference resolution, so they are literal pixels there and
    /// scale from it everywhere else.
    /// </para>
    /// </remarks>
    public static class HomeStyle
    {
        public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        public static class Palette
        {
            public static readonly Color TextPrimary = FromHex(0xF5F3F1);
            public static readonly Color Accent = FromHex(0xFF7032);

            /// <summary>
            /// Shown only while the background art is missing, so an unwired
            /// scene reads as a dark screen rather than a white one.
            /// </summary>
            public static readonly Color BackgroundFallback = FromHex(0x0B1018);

            /// <summary>
            /// The profile chip, which sits over the lit road and wants to be
            /// darker than the icon buttons beside it.
            /// </summary>
            public static readonly Color ChipFill = FromHex(0x0B1018, 0.6f);

            /// <summary>
            /// The friend and server buttons, which sit over the dark edges of
            /// the picture and only need to lift off them.
            /// </summary>
            public static readonly Color ButtonFill = FromHex(0x5C5C5C, 0.5f);

            /// <summary>
            /// Hovering settles every one of these on the same grey and draws a
            /// hairline around it, so the chip and the two buttons answer alike.
            /// </summary>
            public static readonly Color HoverFill = FromHex(0x5C5C5C, 0.5f);

            public static readonly Color HoverStroke = FromHex(0xF5F3F1);

            public static readonly Color PanelFill = FromHex(0x0B1018, 0.8f);

            public static readonly Color ToggleStroke = FromHex(0xD9D9D9);
            public static readonly Color ToggleOffFill = FromHex(0xFFFFFF);
            public static readonly Color ToggleOffKnob = FromHex(0x747474);
            public static readonly Color ToggleOnFill = FromHex(0xFF9A6A);
            public static readonly Color ToggleOnKnob = FromHex(0xFF7032);

            public static readonly Color InputFill = FromHex(0xF5F3F1, 0.16f);
            public static readonly Color CheckFill = FromHex(0xFFFFFF);
            public static readonly Color CheckLabel = FromHex(0x0B1018);

            public static readonly Color ApplyOffFill = FromHex(0xF5F3F1, 0.16f);
            public static readonly Color ApplyOffLabel = FromHex(0xA8ADB3);
            public static readonly Color ApplyOnFill = FromHex(0xFF7032);
            public static readonly Color ApplyOnLabel = FromHex(0xF5F3F1);

            public static readonly Color MessageRejected = FromHex(0xFF0000);
            public static readonly Color MessageAccepted = FromHex(0x00FF1E);

            /// <summary>
            /// The character counter beside the message. Not given by the
            /// design; the muted grey the rest of the project uses for figures
            /// that are there to be glanced at.
            /// </summary>
            public static readonly Color Counter = FromHex(0xA8ADB3);

            /// <summary>
            /// The plate a region row shows while the pointer is on it.
            /// </summary>
            public static readonly Color RowHover = FromHex(0xF5F3F1, 0.16f);
        }

        public static class Layout
        {
            /// <summary>
            /// The left edge of the menu and the top of its first line, both
            /// measured from the top-left corner of the mock-up.
            /// </summary>
            public const float MenuLeft = 212f;

            public const float MenuTop = 399f;

            /// <summary>
            /// The design gives 30 between one line's box and the next, not
            /// between their tops. A 40 point line stands 48 tall, so the lines
            /// repeat every 78 and the four of them centre on the character.
            /// </summary>
            public const float MenuLineHeight = 48f;

            public const float MenuGap = 30f;

            public const float MenuPitch = MenuLineHeight + MenuGap;

            public const float QuitLeft = 60f;
            public const float QuitBottom = 40f;

            /// <summary>
            /// The chip grows with the name it holds, between these two. Twelve
            /// characters is the longest a nickname gets and the wider number is
            /// what that measures.
            /// </summary>
            public const float ChipMinWidth = 200f;

            public const float ChipMaxWidth = 380f;

            public const float ChipHeight = 60f;
            public const float ChipAvatarDiameter = 42f;
            public const float ChipLeftPadding = 12f;
            public const float ChipAvatarToName = 16f;

            /// <summary>
            /// Not given by the design, which fixes the two widths above
            /// instead. Enough that a name ending at the chip's edge does not
            /// read as clipped.
            /// </summary>
            public const float ChipRightPadding = 20f;

            public const float IconButtonSize = 60f;
            public const float FriendIconSize = 20f;
            public const float ServerIconSize = 24f;

            public const float ChipToFriendButton = 16f;
            public const float BottomRightMargin = 60f;
            public const float BottomMargin = 30f;
            public const float ServerRightMargin = 60f;
            public const float ServerTopMargin = 60f;
        }

        /// <summary>
        /// The panel that changes the nickname, hung above the profile chip.
        /// </summary>
        /// <remarks>
        /// Its right edge lines up with the chip's, which is why the margin
        /// here matches where the chip starts, and why its bottom-right corner
        /// is square: the two read as one stack rather than two plates.
        /// </remarks>
        public static class Profile
        {
            public static readonly Vector2 PanelSize = new Vector2(532f, 251f);
            public const float PanelRightMargin = 136f;
            public const float PanelBottomMargin = 108f;

            public const float SidePadding = 30f;

            public const float ToggleRowCentreY = -44f;
            public const float ToggleLeft = 190f;
            public static readonly Vector2 ToggleSize = new Vector2(50f, 30f);
            public const float ToggleStrokeThickness = 3f;

            /// <summary>
            /// How far the knob sits inside the pill. Not given by the design;
            /// chosen so the knob clears the 3 pixel stroke on both sides.
            /// </summary>
            public const float ToggleKnobInset = 5f;

            public const float InputTop = -78f;
            public static readonly Vector2 InputSize = new Vector2(472f, 52f);
            public const float InputTextPadding = 20f;

            public static readonly Vector2 CheckSize = new Vector2(95f, 36f);
            public const float CheckRightInset = 8f;

            public const float MessageTop = -134f;
            public const float MessageHeight = 26f;

            public const float ApplyTop = -167f;
            public static readonly Vector2 ApplySize = new Vector2(472f, 52f);

            public const string TooLongMessage = "최대 12글자 작성가능합니다";
            public const string BadCharacterMessage = "한글/영어/숫자만 작성가능합니다";
            public const string TakenMessage = "이미 존재하는 닉네임입니다";
            public const string AvailableMessage = "사용 가능한 닉네임입니다";

            /// <summary>
            /// Not given by the design, which has no picture for a server that
            /// did not answer. Says to try again rather than to pick another
            /// name, because the name may well be fine.
            /// </summary>
            public const string UnreachableMessage = "확인하지 못했어요. 잠시 후 다시 시도해주세요";
        }

        /// <summary>
        /// The region picker, hung under the globe button it opens from.
        /// </summary>
        /// <remarks>
        /// Its top-right corner is square for the same reason the profile
        /// panel's bottom-right is: the panel and the button that opened it
        /// read as one piece.
        /// </remarks>
        public static class Server
        {
            public static readonly Vector2 PanelSize = new Vector2(240f, 307f);
            public const float PanelRightMargin = 60f;
            public const float PanelTopMargin = 128f;

            public const float SidePadding = 24f;
            public const float VerticalPadding = 20f;

            public const float TitleHeight = 28f;

            public const float RowHeight = 40f;
            public const float RowGap = 10f;
            public const int RowRadius = 12;

            /// <summary>
            /// Not given by the design. The check mark is square-ish and the
            /// row is 40 tall, so this leaves it room without crowding.
            /// </summary>
            public const float CheckSize = 22f;

            /// <summary>
            /// The gap between the title and the first region.
            /// </summary>
            public const float TitleToRows = 12f;

            /// <summary>
            /// Where the first row's top sits, measured down from the panel's
            /// own top: past the padding, the title, and the gap after it.
            /// </summary>
            public const float RowsTop = VerticalPadding + TitleHeight + TitleToRows;
        }

        public static class Radius
        {
            public const int Chip = 20;
            public const int IconButton = 20;
            public const int Panel = 20;

            /// <summary>
            /// Not given by the design. Read off the mock-up, where the field
            /// and the apply button are rounded about a quarter of their height.
            /// </summary>
            public const int Input = 12;

            public const int Check = 8;
        }

        public static class FontSize
        {
            public const float Menu = 40f;
            public const float Quit = 30f;
            public const float Nickname = 24f;
            public const float ToggleLabel = 20f;

            /// <summary>
            /// Not given by the design. Matched to the check button beside it,
            /// which is the only text on that row the design does size.
            /// </summary>
            public const float NicknameInput = 20f;

            public const float Check = 20f;
            public const float Apply = 24f;
            public const float Message = 18f;
            public const float Counter = 18f;
            public const float ServerTitle = 20f;
            public const float Region = 24f;
        }

        /// <summary>
        /// The hairline a hovered chip or button draws around itself. Matches
        /// the room browser's list rows, which the design draws the same way.
        /// </summary>
        public const float HoverStrokeThickness = 1.5f;

        /// <summary>
        /// Reads a design hex such as 0xF5F3F1 as a colour. The palette is
        /// written in sRGB the way the mock-up reports it, and Unity's UI shader
        /// expects exactly that, so no gamma conversion belongs here.
        /// </summary>
        public static Color FromHex(int rgb, float alpha = 1f)
        {
            return new Color(
                ((rgb >> 16) & 0xFF) / 255f,
                ((rgb >> 8) & 0xFF) / 255f,
                (rgb & 0xFF) / 255f,
                alpha);
        }
    }
}
