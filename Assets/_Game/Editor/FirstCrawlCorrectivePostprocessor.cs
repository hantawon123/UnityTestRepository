using UnityEditor;
using UnityEngine;

// Non-crawl clips explicitly release the pose-space corrections, including
// controllers whose states have Write Defaults disabled.
public sealed class FirstCrawlCorrectivePostprocessor : AssetPostprocessor
{
    private void OnPostprocessAnimation(GameObject root, AnimationClip clip)
    {
        if (!assetPath.StartsWith("Assets/Scenes/CharacterTest/First/FirstPlayerCapsule_") ||
            assetPath.Contains("_Crawl_")) return;
        foreach (var side in new[] { "L", "R" })
            clip.SetCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.Crawl_Follow_" + side,
                AnimationCurve.Constant(0f, Mathf.Max(clip.length, 0.01f), 0f));
    }
}
