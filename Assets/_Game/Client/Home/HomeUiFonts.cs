using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Game.Client.Home
{
    public static class HomeUiFonts
    {
        private static TMP_FontAsset koreanFont;
        private static Sprite circleSprite;
        private static Sprite whiteSprite;
        private static Sprite roundedSprite;
        private static Sprite pillSprite;

        private static readonly Dictionary<int, Sprite> RoundedSprites =
            new Dictionary<int, Sprite>();

        private static readonly Dictionary<int, Sprite> OutlineSprites =
            new Dictionary<int, Sprite>();

        /// <summary>
        /// A filled rounded rectangle at the given radius, nine-sliced so one
        /// sprite serves both a 60 pixel button and a 380 pixel chip.
        /// </summary>
        /// <remarks>
        /// Generated per radius and cached rather than imported: a generated
        /// sprite costs a few kilobytes and no repository space, and it cannot
        /// drift from <see cref="HomeStyle"/> the way an exported PNG does.
        /// </remarks>
        public static Sprite Rounded(int radius, bool squareBottomRight = false)
        {
            var key = squareBottomRight ? -radius : radius;
            if (RoundedSprites.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var sprite = BuildRoundedSprite(radius, 0f, squareBottomRight);
            RoundedSprites[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// The border of a rounded rectangle, for the hairline a hovered chip
        /// draws. Kept apart from the fill so one can change without the other.
        /// </summary>
        public static Sprite Outline(int radius, float thickness = 1f)
        {
            var key = (radius * 100) + Mathf.RoundToInt(thickness * 10f);
            if (OutlineSprites.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var sprite = BuildRoundedSprite(radius, thickness);
            OutlineSprites[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Builds the rounded rectangle. A <paramref name="thickness"/> above
        /// zero hollows it out, leaving a border that many pixels wide.
        /// </summary>
        /// <remarks>
        /// Alpha comes from a signed distance rather than an inside test, so the
        /// corners stay smooth once the nine-slice stretches them. A thresholded
        /// corner shows its steps at chip size.
        /// </remarks>
        private static Sprite BuildRoundedSprite(
            int radius, float thickness, bool squareBottomRight = false)
        {
            var size = Mathf.Max((radius * 2) + 4, 8);
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var half = size * 0.5f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    // Texture space counts up from the bottom, so the design's
                    // bottom-right corner is the low-y, high-x quadrant here.
                    // Filling that quadrant solid leaves it square while the
                    // other three keep their radius, and the nine-slice takes
                    // each corner from the matching corner of this texture.
                    var isSquareCorner = squareBottomRight && x + 0.5f > half && y + 0.5f < half;
                    var coverage = isSquareCorner
                        ? 1f
                        : RoundedCoverage(x, y, half, radius, 0f);
                    if (thickness > 0f && !isSquareCorner)
                    {
                        coverage -= RoundedCoverage(x, y, half, radius, thickness);
                    }

                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(coverage)));
                }
            }

            texture.Apply(false, false);

            // One pixel past the radius, so the stretched middle never eats into
            // a corner.
            var border = Mathf.Min(radius + 1, (size / 2) - 1);
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

        private static float RoundedCoverage(int x, int y, float half, float radius, float inset)
        {
            var effectiveRadius = Mathf.Max(radius - inset, 0f);
            var extent = half - inset - effectiveRadius;
            var dx = Mathf.Abs(x + 0.5f - half) - extent;
            var dy = Mathf.Abs(y + 0.5f - half) - extent;
            var outside = new Vector2(Mathf.Max(dx, 0f), Mathf.Max(dy, 0f)).magnitude;
            var inside = Mathf.Min(Mathf.Max(dx, dy), 0f);
            return Mathf.Clamp01(0.5f - (outside + inside - effectiveRadius));
        }

        public static Sprite WhiteSprite
        {
            get
            {
                if (whiteSprite != null)
                {
                    return whiteSprite;
                }

                var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                var pixels = new Color[16];
                for (var index = 0; index < pixels.Length; index++)
                {
                    pixels[index] = Color.white;
                }

                texture.SetPixels(pixels);
                texture.Apply(false, false);
                whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
                whiteSprite.hideFlags = HideFlags.HideAndDontSave;
                return whiteSprite;
            }
        }

        public static Sprite RoundedSprite
        {
            get
            {
                if (roundedSprite != null)
                {
                    return roundedSprite;
                }

                const int size = 64;
                const int radius = 16;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Bilinear
                };

                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        texture.SetPixel(x, y, IsInsideRoundedRect(x, y, size, radius)
                            ? Color.white
                            : Color.clear);
                    }
                }

                texture.Apply(false, false);
                roundedSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, size, size),
                    new Vector2(0.5f, 0.5f),
                    100f,
                    0,
                    SpriteMeshType.FullRect,
                    new Vector4(radius, radius, radius, radius));
                roundedSprite.hideFlags = HideFlags.HideAndDontSave;
                return roundedSprite;
            }
        }

        public static Sprite PillSprite
        {
            get
            {
                if (pillSprite != null)
                {
                    return pillSprite;
                }

                const int size = 64;
                const int radius = 30;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Bilinear
                };

                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        texture.SetPixel(x, y, IsInsideRoundedRect(x, y, size, radius)
                            ? Color.white
                            : Color.clear);
                    }
                }

                texture.Apply(false, false);
                pillSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, size, size),
                    new Vector2(0.5f, 0.5f),
                    100f,
                    0,
                    SpriteMeshType.FullRect,
                    new Vector4(radius, radius, radius, radius));
                pillSprite.hideFlags = HideFlags.HideAndDontSave;
                return pillSprite;
            }
        }

        public static Sprite CircleSprite
        {
            get
            {
                if (circleSprite != null)
                {
                    return circleSprite;
                }

                const int size = 64;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Bilinear
                };

                var center = (size - 1) * 0.5f;
                var radius = center - 1f;
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        var dx = x - center;
                        var dy = y - center;
                        texture.SetPixel(x, y, (dx * dx) + (dy * dy) <= radius * radius
                            ? Color.white
                            : Color.clear);
                    }
                }

                texture.Apply(false, false);
                circleSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, size, size),
                    new Vector2(0.5f, 0.5f),
                    100f);
                circleSprite.hideFlags = HideFlags.HideAndDontSave;
                return circleSprite;
            }
        }

        public static TMP_FontAsset Apply(TMP_FontAsset fontAsset = null)
        {
            if (koreanFont != null)
            {
                return koreanFont;
            }

            koreanFont = fontAsset != null ? fontAsset : TMP_Settings.defaultFontAsset;
            if (koreanFont == null)
            {
                throw new InvalidOperationException(
                    "Korean TMP font is missing. Assign a Paperlogy SDF asset " +
                    "or set it as TMP Settings default font.");
            }

            return koreanFont;
        }

        private static bool IsInsideRoundedRect(int x, int y, int size, int radius)
        {
            var innerMin = radius;
            var innerMax = size - radius;
            if (x >= innerMin && x < innerMax)
            {
                return true;
            }

            if (y >= innerMin && y < innerMax)
            {
                return true;
            }

            var cornerX = x < innerMin ? innerMin : innerMax;
            var cornerY = y < innerMin ? innerMin : innerMax;
            var dx = x - cornerX;
            var dy = y - cornerY;
            return (dx * dx) + (dy * dy) <= radius * radius;
        }
    }
}
