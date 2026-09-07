using Game.Core.Players;
using UnityEngine;

namespace Game.Client.Character
{
    /// <summary>
    /// Every colour and measurement the closet draws with, taken from the
    /// mock-up.
    /// </summary>
    /// <remarks>
    /// The same arrangement Home uses: the screen is built in code, so this is
    /// the only record of the design, and a revised mock-up is one file to
    /// edit.
    /// <para>
    /// Measurements are pixels at the 1920x1080 the mock-up was drawn at, which
    /// is also the canvas reference resolution, so they are literal pixels
    /// there and scale from it everywhere else.
    /// </para>
    /// </remarks>
    public static class CharacterClosetStyle
    {
        public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        public static class Palette
        {
            public static readonly Color TextPrimary = FromHex(0xFFFDFC);
            public static readonly Color TextMuted = FromHex(0xA8ADB3);
            public static readonly Color Accent = FromHex(0xFF7032);

            /// <summary>Shown only while the background art is missing.</summary>
            public static readonly Color BackgroundFallback = FromHex(0x0B1018);

            public static readonly Color TabFill = FromHex(0x0B1018, 0.8f);
            public static readonly Color TabHoverFill = FromHex(0xF5F3F1, 0.16f);
            public static readonly Color TabSelectedStroke = FromHex(0xFF7032);

            /// <summary>
            /// The selected tab says so twice: an orange ring and orange
            /// lettering. One of the two alone reads as a hover.
            /// </summary>
            public static readonly Color TabSelectedLabel = FromHex(0xFF7032);

            public static readonly Color LockerFill = FromHex(0x0B1018, 0.8f);
            public static readonly Color CellFill = FromHex(0x343845, 0.7f);
            public static readonly Color CellHoverFill = FromHex(0x343845);
            public static readonly Color CellSelectedStroke = FromHex(0xFF7032);
            public static readonly Color ScrollbarHandle = FromHex(0xF5F3F1, 0.8f);

            /// <summary>
            /// The two buttons under the character. What turns them on is the
            /// story after this one; the off colours are what the screen draws
            /// today.
            /// </summary>
            public static readonly Color ResetFill = FromHex(0xF5F3F1);
            public static readonly Color ResetLabel = FromHex(0x231818);
            public static readonly Color ApplyOnFill = FromHex(0xFF7032);
            public static readonly Color ApplyOnLabel = FromHex(0xF5F3F1);
            public static readonly Color ApplyOffFill = FromHex(0x0B1018, 0.5f);
            public static readonly Color ApplyOffLabel = FromHex(0xA8ADB3);

            public static readonly Color BackLabel = FromHex(0xFFFDFC);
        }

        public static class Radius
        {
            public const int Tab = 32;
            public const int Locker = 32;
            public const int Cell = 20;
            public const int Button = 32;
        }

        public static class Back
        {
            public static readonly Vector2 Position = new Vector2(56f, -48f);
            public static readonly Vector2 Size = new Vector2(200f, 48f);
            public const float FontSize = 30f;
            public const string Label = "← 이전";
        }

        public static class Tabs
        {
            public static readonly Vector2 Size = new Vector2(324f, 128f);

            /// <summary>Top left corner of the first tab.</summary>
            public static readonly Vector2 Origin = new Vector2(60f, -281f);

            public const float Gap = 15f;
            public const float FontSize = 30f;

            /// <summary>Between the icon and the word beside it.</summary>
            public const float LabelGap = 60f;

            public const float SelectedStroke = 4f;

            /// <summary>
            /// Each icon at the size the mock-up gives it rather than all four
            /// in one box: they are different shapes, and a shoe fitted to the
            /// square a face wants comes out either tiny or stretched.
            /// </summary>
            public static Vector2 IconSize(AvatarPartCategory category)
            {
                switch (category)
                {
                    case AvatarPartCategory.Hood:
                        return new Vector2(63f, 60f);
                    case AvatarPartCategory.Shoes:
                        return new Vector2(69f, 41f);
                    case AvatarPartCategory.Face:
                        return new Vector2(62f, 62f);
                    default:
                        return new Vector2(62f, 56f);
                }
            }

            public static float IconLeft(AvatarPartCategory category) =>
                category == AvatarPartCategory.Shoes ? 49f : 52f;
        }

        public static class Locker
        {
            public static readonly Vector2 Size = new Vector2(445f, 575f);

            /// <summary>Inset from the right edge and down from the top.</summary>
            public static readonly Vector2 Margin = new Vector2(60f, 272f);

            public const float PaddingVertical = 66f;
            public const float PaddingHorizontal = 43f;
            public const int Columns = 3;
            public const float CellSize = 100f;
            public const float CellGap = 30f;
            public const float ThumbnailInset = 5f;
            public const float SelectedStroke = 4f;

            /// <summary>
            /// The handle down the inside of the right edge, flush with the
            /// padding rather than inset from it.
            /// </summary>
            public const float ScrollbarWidth = 8f;
            public const float ScrollbarInset = 0f;
        }

        public static class Buttons
        {
            public static readonly Vector2 Size = new Vector2(281f, 80f);
            public const float Gap = 45f;
            public const float BottomMargin = 60f;
            public const float FontSize = 40f;

            /// <summary>The circling arrow left of the reset label.</summary>
            public const float IconSize = 30f;
            public const float IconGap = 10f;

            public const string ResetLabel = "초기화";
            public const string ApplyLabel = "적용";
        }

        private static Color FromHex(uint rgb, float alpha = 1f) =>
            new Color(
                ((rgb >> 16) & 0xFF) / 255f,
                ((rgb >> 8) & 0xFF) / 255f,
                (rgb & 0xFF) / 255f,
                alpha);
    }
}
