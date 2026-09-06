using System;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Game.Editor
{
    /// <summary>
    /// Bakes the whole Korean character set into the font atlas once and
    /// switches the asset to Static.
    /// </summary>
    /// <remarks>
    /// The font asset shipped as Dynamic. TextMeshPro then adds every newly
    /// displayed glyph to the atlas and writes the asset back to disk, so the
    /// file changes whenever someone opens a screen carrying an unseen
    /// syllable. The atlas is 97% of a 6 MB asset, so each of those writes
    /// lands another 6 MB in the repository and collides with everyone else's
    /// copy on merge. Baking the set up front leaves TextMeshPro nothing to
    /// add, and the file stops moving.
    /// <para>
    /// Baking every Hangul syllable would not fit, and that is what an earlier
    /// builder here concluded before settling on Dynamic. All 11,172 of them
    /// need an atlas so large the glyphs come out mushy. The set below is the
    /// 2,350 of KS X 1001, which is what Korean text actually uses, and those
    /// fit one 2048 square page at a sampling size the screens never exceed.
    /// </para>
    /// <para>
    /// This rewrites the Paperlogy Regular SDF asset in place instead of
    /// recreating it, so existing references keep their guid.
    /// </para>
    /// <para>
    /// Re-run this after replacing
    /// <c>Assets/_Game/Content/Resources/Fonts/Paperlogy-4Regular.ttf</c>
    /// or after editing the character set file. Nothing else needs to run it.
    /// </para>
    /// </remarks>
    public static class FontAtlasBaker
    {
        private const string CharacterSetPath =
            "Assets/_Game/Editor/FontAtlasCharacterSet.txt";

        /// <summary>
        /// Atlas edge in pixels. Unity serialises the texture into the asset as
        /// hexadecimal text at two characters per byte, so a 2048 square alpha
        /// atlas costs about 8 MB of file. Committed once, that is cheaper than
        /// the dynamic atlas rewriting 6 MB on an unpredictable schedule.
        /// </summary>
        private const int AtlasSize = 2048;

        /// <summary>
        /// Size the glyphs are rendered at while packing. Scenes draw this font
        /// between 24 and 52, and a signed distance field stays crisp when
        /// scaled up, so sampling smaller costs little. It buys the room that
        /// matters: measured against this font's syllables, 32 fits roughly
        /// 3,200 of them in one atlas where 40 fits only 2,000, and the set
        /// below needs 2,548.
        /// </summary>
        private const int SamplingPointSize = 32;

        /// <summary>
        /// Distance field spread, held near a tenth of the sampling size so the
        /// gradient resolves. The material's gradient scale must match it.
        /// </summary>
        private const int Padding = 3;

        /// <summary>
        /// A source font legitimately lacks some of the decorative symbols in
        /// the character set, and dropping those is fine. Losing this many
        /// means the atlas ran out of room instead, which would silently ship a
        /// font full of missing syllables.
        /// </summary>
        private const int MissingCharacterLimit = 200;

        private const string PaperlogyRegularMenuPath =
            "Game/Fonts/Bake Paperlogy Regular Atlas";

        private const string PaperlogyRegularSourcePath =
            "Assets/_Game/Content/Resources/Fonts/Paperlogy-4Regular.ttf";

        private const string PaperlogyRegularAssetPath =
            "Assets/_Game/Content/Resources/Fonts/Paperlogy-4Regular SDF.asset";

        [InitializeOnLoadMethod]
        private static void QueuePaperlogyRegularBakeIfMissing()
        {
            EditorApplication.playModeStateChanged -= BakePaperlogyRegularWhenEditMode;
            EditorApplication.playModeStateChanged += BakePaperlogyRegularWhenEditMode;
            EditorApplication.delayCall += BakePaperlogyRegularIfMissing;
        }

        private static void BakePaperlogyRegularWhenEditMode(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.delayCall += BakePaperlogyRegularIfMissing;
            }
        }

        private static void BakePaperlogyRegularIfMissing()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling ||
                EditorApplication.isUpdating)
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PaperlogyRegularAssetPath) != null)
            {
                return;
            }

            Debug.Log("[Fonts] Paperlogy Regular SDF가 없어 Static 아틀라스를 굽습니다.");
            BakePaperlogyRegular();
        }

        [MenuItem(PaperlogyRegularMenuPath)]
        public static void BakePaperlogyRegular()
        {
            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(PaperlogyRegularSourcePath);
            if (sourceFont == null)
            {
                throw new InvalidOperationException(
                    $"No source font at '{PaperlogyRegularSourcePath}'.");
            }

            var characters = ReadCharacterSet();
            var baked = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                SamplingPointSize,
                Padding,
                GlyphRenderMode.SDFAA,
                AtlasSize,
                AtlasSize,
                AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: false);

            if (baked == null)
            {
                throw new InvalidOperationException(
                    $"Could not read '{sourceFont.name}'. Enable Include Font " +
                    "Data in its import settings.");
            }

            try
            {
                baked.TryAddCharacters(
                    characters,
                    out var missing,
                    includeFontFeatures: true);
                RejectOverfilledAtlas(missing);

                baked.name = "Paperlogy-4Regular SDF";
                baked.atlasPopulationMode = AtlasPopulationMode.Static;
                baked.ReadFontAssetDefinition();

                var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    PaperlogyRegularAssetPath);
                if (existing != null)
                {
                    TransferInto(existing, baked);
                    ReportResult(existing, characters, missing);
                    return;
                }

                SaveNewFontAsset(baked, PaperlogyRegularAssetPath);
                ReportResult(baked, characters, missing);
                baked = null;
            }
            finally
            {
                if (baked != null)
                {
                    DiscardBakedAsset(baked);
                }
            }
        }

        private static void SaveNewFontAsset(TMP_FontAsset fontAsset, string assetPath)
        {
            AssetDatabase.CreateAsset(fontAsset, assetPath);
            if (fontAsset.atlasTextures != null)
            {
                for (var index = 0; index < fontAsset.atlasTextures.Length; index++)
                {
                    var atlas = fontAsset.atlasTextures[index];
                    if (atlas == null)
                    {
                        continue;
                    }

                    atlas.name = fontAsset.name + (index == 0 ? " Atlas" : $" Atlas {index}");
                    AssetDatabase.AddObjectToAsset(atlas, fontAsset);
                }
            }

            if (fontAsset.material != null)
            {
                fontAsset.material.name = fontAsset.name + " Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
                if (fontAsset.atlasTextures is { Length: > 0 } &&
                    fontAsset.atlasTextures[0] != null)
                {
                    RefreshMaterial(
                        fontAsset.material,
                        fontAsset,
                        fontAsset.atlasTextures[0]);
                }
            }

            fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
            fontAsset.ReadFontAssetDefinition();
            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Copies the baked tables and atlas pixels onto the live asset.
        /// </summary>
        /// <remarks>
        /// The existing atlas texture and material objects are kept and only
        /// refilled. Scenes and the TMP settings asset reach them by local file
        /// id inside this asset, and replacing the objects would reissue those
        /// ids and break every reference.
        /// </remarks>
        private static void TransferInto(TMP_FontAsset fontAsset,
                                         TMP_FontAsset baked)
        {
            var material = fontAsset.material;
            var atlas = fontAsset.atlasTextures[0];

            // Dynamic mode spilled onto extra pages, and an orphaned page is
            // sitting in the file besides. Neither survives the rebake.
            DestroySpareAtlasPages(fontAsset, atlas);

            var assetName = fontAsset.name;
            EditorUtility.CopySerialized(baked, fontAsset);
            fontAsset.name = assetName;

            var source = baked.atlasTextures[0];
            atlas.Reinitialize(source.width, source.height, source.format, false);
            atlas.SetPixelData(source.GetRawTextureData(), 0);
            atlas.Apply(updateMipmaps: false);
            atlas.name = assetName + " Atlas";

            // CopySerialized pointed these at the throwaway asset's objects.
            fontAsset.atlasTextures = new[] { atlas };
            fontAsset.material = material;

            RefreshMaterial(material, fontAsset, atlas);

            // The source font reference is dropped by this setter, which is
            // what Static means. The guid stays behind so a rerun still finds
            // the font.
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
            fontAsset.ReadFontAssetDefinition();

            EditorUtility.SetDirty(fontAsset);
            EditorUtility.SetDirty(atlas);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void RefreshMaterial(Material material,
                                            TMP_FontAsset fontAsset,
                                            Texture2D atlas)
        {
            material.SetTexture("_MainTex", atlas);
            material.SetFloat("_TextureWidth", atlas.width);
            material.SetFloat("_TextureHeight", atlas.height);
            // The shader reads the distance field across this many pixels.
            material.SetFloat("_GradientScale", Padding + 1);
            material.SetFloat("_WeightNormal", fontAsset.normalStyle);
            material.SetFloat("_WeightBold", fontAsset.boldStyle);

            // Outline and underlay widths are held as fractions of the gradient
            // scale, so they are wrong the moment it changes. TextMeshPro
            // recomputes them the next time it touches the material anyway;
            // doing it here keeps that from surfacing later as a stray edit to
            // a file this whole exercise is meant to hold still.
            ShaderUtilities.UpdateShaderRatios(material);
        }

        private static void DestroySpareAtlasPages(TMP_FontAsset fontAsset,
                                                   Texture2D keep)
        {
            var path = AssetDatabase.GetAssetPath(fontAsset);
            foreach (var stored in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (stored is Texture2D page && page != keep)
                {
                    UnityEngine.Object.DestroyImmediate(
                        page,
                        allowDestroyingAssets: true);
                }
            }
        }

        private static void RejectOverfilledAtlas(string missing)
        {
            if (string.IsNullOrEmpty(missing) ||
                missing.Length <= MissingCharacterLimit)
            {
                return;
            }

            throw new InvalidOperationException(
                $"{missing.Length} characters did not fit a {AtlasSize} square " +
                $"atlas at sampling size {SamplingPointSize}. Lower " +
                "SamplingPointSize or trim the character set, then rerun. The " +
                "font asset was left untouched.");
        }

        /// <summary>
        /// Reads the character set, ignoring the line breaks that keep the file
        /// legible. The space is added here because it cannot survive that.
        /// </summary>
        private static string ReadCharacterSet()
        {
            if (!File.Exists(CharacterSetPath))
            {
                throw new InvalidOperationException(
                    $"No character set at '{CharacterSetPath}'.");
            }

            var text = File.ReadAllText(CharacterSetPath, Encoding.UTF8);
            var characters = new StringBuilder(text.Length + 1);
            characters.Append(' ');

            foreach (var character in text)
            {
                if (!char.IsWhiteSpace(character))
                {
                    characters.Append(character);
                }
            }

            return characters.ToString();
        }

        private static void DiscardBakedAsset(TMP_FontAsset baked)
        {
            if (baked.atlasTextures != null && baked.atlasTextures.Length > 0)
            {
                UnityEngine.Object.DestroyImmediate(baked.atlasTextures[0]);
            }

            if (baked.material != null)
            {
                UnityEngine.Object.DestroyImmediate(baked.material);
            }

            UnityEngine.Object.DestroyImmediate(baked);
        }

        private static void ReportResult(TMP_FontAsset fontAsset,
                                         string requested,
                                         string missing)
        {
            var summary =
                $"[Fonts] Baked {fontAsset.characterTable.Count} of " +
                $"{requested.Length} characters into a {AtlasSize} square " +
                $"atlas at sampling size {SamplingPointSize}. The asset is now " +
                "Static and will stop rewriting itself.";

            if (string.IsNullOrEmpty(missing))
            {
                Debug.Log(summary, fontAsset);
                return;
            }

            // The source font has no glyph for these. Worth seeing, not worth
            // failing over.
            Debug.LogWarning(
                $"{summary}\nNot in the source font ({missing.Length}): " +
                missing,
                fontAsset);
        }
    }
}
