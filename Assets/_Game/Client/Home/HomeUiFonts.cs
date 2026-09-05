using System;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace Game.Client.Home
{
    public static class HomeUiFonts
    {
        private static TMP_FontAsset koreanFont;
        private static Sprite circleSprite;
        private static Sprite whiteSprite;
        private static Sprite roundedSprite;
        private static Sprite pillSprite;

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

        private const string SemiBoldResource = "Fonts/Paperlogy-6SemiBold";
        private const string LightResource = "Fonts/Paperlogy-3Light";
        private const string RegularResource = "Fonts/Paperlogy-4Regular";
        private static TMP_FontAsset koreanLightFont;
        private static TMP_FontAsset koreanRegularFont;
        private static Font legacyFont;

        public static TMP_FontAsset Apply(TMP_FontAsset fontAsset = null)
        {
            return koreanFont ??= LoadKorean(SemiBoldResource, fontAsset);
        }

        public static TMP_FontAsset ApplyLight(TMP_FontAsset fontAsset = null)
        {
            return koreanLightFont ??= LoadKorean(LightResource, fontAsset);
        }

        public static TMP_FontAsset ApplyRegular(TMP_FontAsset fontAsset = null)
        {
            return koreanRegularFont ??= LoadKorean(RegularResource, fontAsset);
        }

        public static Font Legacy()
        {
            if (legacyFont != null)
            {
                return legacyFont;
            }

            legacyFont = Resources.Load<Font>(SemiBoldResource)
                ?? Resources.Load<Font>(RegularResource);
            return legacyFont;
        }

        public static void ApplyLegacy(Transform root)
        {
            var font = Legacy();
            if (font == null || root == null)
            {
                return;
            }

            var texts = root.GetComponentsInChildren<Text>(true);
            for (var index = 0; index < texts.Length; index++)
            {
                if (texts[index] != null)
                {
                    texts[index].font = font;
                }
            }

            var inputs = root.GetComponentsInChildren<InputField>(true);
            for (var index = 0; index < inputs.Length; index++)
            {
                var input = inputs[index];
                if (input == null)
                {
                    continue;
                }

                if (input.textComponent != null)
                {
                    input.textComponent.font = font;
                }

                if (input.placeholder is Text placeholder)
                {
                    placeholder.font = font;
                }
            }
        }

        public static void ApplyTmp(Transform root)
        {
            var font = Apply();
            if (font == null || root == null)
            {
                return;
            }

            var texts = root.GetComponentsInChildren<TMP_Text>(true);
            for (var index = 0; index < texts.Length; index++)
            {
                var text = texts[index];
                if (text != null && text.font != font)
                {
                    text.font = font;
                    text.fontSharedMaterial = font.material;
                }
            }

            var inputs = root.GetComponentsInChildren<TMP_InputField>(true);
            for (var index = 0; index < inputs.Length; index++)
            {
                var input = inputs[index];
                if (input == null)
                {
                    continue;
                }

                input.fontAsset = font;
                if (input.textComponent != null && input.textComponent.font != font)
                {
                    input.textComponent.font = font;
                    input.textComponent.fontSharedMaterial = font.material;
                }

                if (input.placeholder is TMP_Text placeholder && placeholder.font != font)
                {
                    placeholder.font = font;
                    placeholder.fontSharedMaterial = font.material;
                }
            }
        }

        private static TMP_FontAsset LoadKorean(string resourcePath, TMP_FontAsset fontAsset)
        {
            if (fontAsset != null)
            {
                return fontAsset;
            }

            var baked = Resources.Load<TMP_FontAsset>(resourcePath + " SDF");
            if (baked != null)
            {
                return baked;
            }

            var source = Resources.Load<Font>(resourcePath);
            var loaded = CreateRuntimeKorean(source);
            if (loaded != null)
            {
                return loaded;
            }

            loaded = TMP_Settings.defaultFontAsset;
            if (loaded != null)
            {
                return loaded;
            }

            throw new InvalidOperationException(
                "Korean TMP font is missing. Add Paperlogy under " +
                "Assets/_Game/Content/Resources/Fonts.");
        }

        public static TMP_FontAsset CreateRuntimeKorean(Font source, bool prewarmKorean = false)
        {
            if (source == null)
            {
                return null;
            }

            var loaded = TMP_FontAsset.CreateFontAsset(
                source,
                36,
                5,
                GlyphRenderMode.SDFAA,
                2048,
                2048,
                AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: true);
            if (loaded == null)
            {
                return null;
            }

            loaded.hideFlags = HideFlags.HideAndDontSave;
            loaded.TryAddCharacters(
                "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ .,!?:;-_~/()[]");
            if (prewarmKorean)
            {
                var glyphs = Resources.Load<TextAsset>("Fonts/KoreanGlyphs");
#if UNITY_EDITOR
                if (glyphs == null)
                {
                    glyphs = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(
                        "Assets/_Game/Editor/FontAtlasCharacterSet.txt");
                }
#endif
                if (glyphs != null && !string.IsNullOrEmpty(glyphs.text))
                {
                    loaded.TryAddCharacters(
                        glyphs.text.Replace("\r", string.Empty).Replace("\n", string.Empty));
                }
            }

            return loaded;
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
