using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Client.Settings
{
    /// <summary>
    /// Which corners of a rectangle are rounded.
    /// </summary>
    /// <remarks>
    /// The settings design rounds one whole side — a tab down its left, a row
    /// down its right — which <see cref="Home.SquareCorner"/> cannot say, so
    /// the corners are named individually here.
    /// </remarks>
    [Flags]
    public enum RoundedCorners
    {
        None = 0,
        TopLeft = 1,
        TopRight = 2,
        BottomRight = 4,
        BottomLeft = 8,

        /// <summary>Both corners down one side, which is how this screen rounds a tab or a row.</summary>
        Left = TopLeft | BottomLeft,

        Right = TopRight | BottomRight,
        All = Left | Right
    }

    /// <summary>
    /// The two shapes the settings screen needs that the shared sprite cache
    /// does not have: a rectangle rounded at chosen corners, and a glow.
    /// </summary>
    /// <remarks>
    /// Generated and cached rather than imported, for the same reason
    /// <see cref="Home.HomeUiFonts.Rounded"/> is: a generated sprite costs a
    /// few kilobytes and no repository space, and cannot drift from
    /// <see cref="SettingsStyle"/> the way an exported PNG does.
    /// </remarks>
    public static class SettingsSprites
    {
        private static readonly Dictionary<int, Sprite> Rounded = new Dictionary<int, Sprite>();
        private static readonly Dictionary<int, Sprite> Glows = new Dictionary<int, Sprite>();

        /// <summary>
        /// A filled rectangle rounded at <paramref name="corners"/>, nine-sliced
        /// so one sprite serves any size.
        /// </summary>
        public static Sprite RoundedRect(int radius, RoundedCorners corners)
        {
            var key = (radius * 16) + (int)corners;
            if (Rounded.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var sprite = BuildRounded(radius, corners);
            Rounded[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// The soft halo a design tool draws as a drop shadow with no offset:
        /// the panel's own rounded outline pushed out by <paramref name="spread"/>,
        /// then faded to nothing over <paramref name="blur"/>.
        /// </summary>
        /// <remarks>
        /// Meant to be drawn on a rectangle that is the panel's grown by
        /// <see cref="GlowMargin"/> on every side, so the fully lit edge lands
        /// exactly <paramref name="spread"/> outside the panel. Nine-sliced,
        /// with the whole faded band inside the border, so stretching the
        /// middle never stretches the fade.
        /// </remarks>
        public static Sprite Glow(int radius, int spread, int blur)
        {
            var key = (radius * 10000) + (spread * 100) + blur;
            if (Glows.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var sprite = BuildGlow(radius, spread, blur);
            Glows[key] = sprite;
            return sprite;
        }

        /// <summary>How far past the panel the glow's rectangle has to reach.</summary>
        public static float GlowMargin(int spread, int blur) => spread + blur;

        private static Sprite BuildRounded(int radius, RoundedCorners corners)
        {
            var size = Mathf.Max((radius * 2) + 4, 8);
            var texture = NewTexture(size);
            var half = size * 0.5f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    // Texture space counts up from the bottom, so the design's
                    // top-left corner is the low-x, high-y quadrant here. A
                    // quadrant whose corner is square is filled solid; the
                    // nine-slice takes each corner from the matching corner of
                    // this texture.
                    var right = x + 0.5f > half;
                    var top = y + 0.5f > half;
                    var coverage = IsRounded(corners, right, top)
                        ? Coverage(x, y, half, radius)
                        : 1f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(coverage)));
                }
            }

            texture.Apply(false, false);
            return Slice(texture, size, Mathf.Min(radius + 1, (size / 2) - 1));
        }

        private static Sprite BuildGlow(int radius, int spread, int blur)
        {
            var margin = spread + blur;
            var shapeRadius = radius + spread;
            var size = ((shapeRadius + blur) * 2) + 4;
            var texture = NewTexture(size);
            var half = size * 0.5f;

            // The lit shape: the panel grown by the spread, sitting one blur
            // plus a pixel inside the texture's edge so the fade has room.
            var extent = half - blur - 1f - shapeRadius;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = Mathf.Abs(x + 0.5f - half) - extent;
                    var dy = Mathf.Abs(y + 0.5f - half) - extent;
                    var outside = new Vector2(Mathf.Max(dx, 0f), Mathf.Max(dy, 0f)).magnitude;
                    var inside = Mathf.Min(Mathf.Max(dx, dy), 0f);
                    var distance = outside + inside - shapeRadius;

                    // Full inside the shape, then eased to nothing across the
                    // blur. Squared so it fades the way a blurred edge does:
                    // quickly at first, then trailing off.
                    var fade = distance <= 0f
                        ? 1f
                        : Mathf.Clamp01(1f - (distance / blur));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, fade * fade));
                }
            }

            texture.Apply(false, false);
            return Slice(texture, size, Mathf.Min(shapeRadius + margin + 1, (size / 2) - 1));
        }

        private static Texture2D NewTexture(int size) =>
            new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

        private static Sprite Slice(Texture2D texture, int size, int border)
        {
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static bool IsRounded(RoundedCorners corners, bool right, bool top)
        {
            var corner = right
                ? (top ? RoundedCorners.TopRight : RoundedCorners.BottomRight)
                : (top ? RoundedCorners.TopLeft : RoundedCorners.BottomLeft);
            return (corners & corner) != 0;
        }

        /// <summary>
        /// How much of a pixel a rounded rectangle covers, from its signed
        /// distance, so the corners stay smooth once the nine-slice stretches
        /// them.
        /// </summary>
        private static float Coverage(int x, int y, float half, float radius)
        {
            var extent = half - radius;
            var dx = Mathf.Abs(x + 0.5f - half) - extent;
            var dy = Mathf.Abs(y + 0.5f - half) - extent;
            var outside = new Vector2(Mathf.Max(dx, 0f), Mathf.Max(dy, 0f)).magnitude;
            var inside = Mathf.Min(Mathf.Max(dx, dy), 0f);
            return Mathf.Clamp01(0.5f - (outside + inside - radius));
        }
    }
}
