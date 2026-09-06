using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Client.Interactions
{
    /// <summary>조준 중인 집을 수 있는 물건에 주황 2px 실루엣을 그린다.</summary>
    [DisallowMultipleComponent]
    public sealed class InteractableFocusOutline : MonoBehaviour
    {
        public static readonly Color FocusColor = new(1f, 154f / 255f, 106f / 255f, 1f);
        public const float FocusPixels = 2f;

        private const string ShaderResourceName = "AssignedItemOutline";

        private readonly List<Renderer> outlineRenderers = new();
        private Material outlineMaterial;
        private bool built;

        public Color Color => FocusColor;

        public float PixelWidth => FocusPixels;

        public bool IsVisible { get; private set; }

        public void SetVisible(bool visible)
        {
            if (visible)
            {
                EnsureBuilt();
            }

            IsVisible = visible;
            for (var index = 0; index < outlineRenderers.Count; index++)
            {
                outlineRenderers[index].enabled = visible;
            }
        }

        private void EnsureBuilt()
        {
            if (built)
            {
                return;
            }

            built = true;
            var shader = Resources.Load<Shader>(ShaderResourceName);
            if (shader == null)
            {
                Debug.LogError(
                    $"Interactable focus outline shader resource '{ShaderResourceName}' is missing.",
                    this);
                return;
            }

            outlineMaterial = new Material(shader)
            {
                name = $"{name} Focus Outline (Runtime)",
                hideFlags = HideFlags.DontSave,
            };
            outlineMaterial.SetColor("_OutlineColor", FocusColor);
            outlineMaterial.SetFloat("_OutlineWidth", 0.001f);
            outlineMaterial.SetFloat("_OutlinePixels", FocusPixels);

            var sources = GetComponentsInChildren<Renderer>(includeInactive: true);
            for (var index = 0; index < sources.Length; index++)
            {
                var source = sources[index];
                if (ItemOutlineRenderers.IsGenerated(source))
                {
                    continue;
                }

                switch (source)
                {
                    case MeshRenderer meshRenderer:
                        CreateMeshOutline(meshRenderer);
                        break;
                    case SkinnedMeshRenderer skinnedRenderer:
                        CreateSkinnedOutline(skinnedRenderer);
                        break;
                }
            }
        }

        private void CreateMeshOutline(MeshRenderer source)
        {
            var sourceFilter = source.GetComponent<MeshFilter>();
            if (sourceFilter == null || sourceFilter.sharedMesh == null)
            {
                return;
            }

            var child = CreateOutlineChild(source.transform);
            child.AddComponent<MeshFilter>().sharedMesh = sourceFilter.sharedMesh;
            var renderer = child.AddComponent<MeshRenderer>();
            Configure(renderer, source.sharedMaterials.Length);
        }

        private void CreateSkinnedOutline(SkinnedMeshRenderer source)
        {
            if (source.sharedMesh == null)
            {
                return;
            }

            var child = CreateOutlineChild(source.transform);
            var renderer = child.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = source.sharedMesh;
            renderer.rootBone = source.rootBone;
            renderer.bones = source.bones;
            renderer.localBounds = source.localBounds;
            renderer.updateWhenOffscreen = source.updateWhenOffscreen;
            Configure(renderer, source.sharedMaterials.Length);
        }

        private GameObject CreateOutlineChild(Transform parent)
        {
            var child = new GameObject("[Interactable Focus Outline]")
            {
                layer = parent.gameObject.layer,
                hideFlags = HideFlags.DontSave,
            };
            child.transform.SetParent(parent, worldPositionStays: false);
            return child;
        }

        private void Configure(Renderer renderer, int materialCount)
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            var materials = new Material[Mathf.Max(1, materialCount)];
            for (var index = 0; index < materials.Length; index++)
            {
                materials[index] = outlineMaterial;
            }

            renderer.sharedMaterials = materials;
            renderer.enabled = false;
            outlineRenderers.Add(renderer);
        }

        private void OnDestroy()
        {
            if (outlineMaterial == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(outlineMaterial);
            }
            else
            {
                DestroyImmediate(outlineMaterial);
            }
        }
    }
}
