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

        public static class Radius
        {
            public const int Chip = 20;
            public const int IconButton = 20;
        }

        public static class FontSize
        {
            public const float Menu = 40f;
            public const float Quit = 30f;
            public const float Nickname = 24f;
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
