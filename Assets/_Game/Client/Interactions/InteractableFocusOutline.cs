using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Client.Interactions
{
    /// <summary>조준 중인 집을 수 있는 물건에 주황 2px 실루엣을 그린다.</summary>
    /// <remarks>
    /// 기본으로는 자기 자식 렌더러에 실루엣을 씌운다. 씬에 이미 놓인 환경 소품
    /// (여러 개를 하나의 상호작용 대상으로 묶는 경우)에도 같은 실루엣을 쓰려면
    /// <see cref="sourceRenderers"/>에 그 렌더러들을 직접 지정한다. 지정된 것이
    /// 하나라도 있으면 자식 검색은 하지 않는다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class InteractableFocusOutline : MonoBehaviour
    {
        public static readonly Color FocusColor = new(1f, 154f / 255f, 106f / 255f, 1f);
        public const float FocusPixels = 2f;

        private const string ShaderResourceName = "AssignedItemOutline";

        [Tooltip("실루엣을 씌울 렌더러. 비어 있으면 이 오브젝트의 자식 렌더러를 쓴다.")]
        [SerializeField]
        private Renderer[] sourceRenderers;

        [Tooltip("sourceRenderers와 같은 순서의 원본 메시. 정적 배칭된 소품은 런타임에 " +
                 "MeshFilter가 결합 메시를 가리키므로, 에디터에서 저장한 원본을 대신 쓴다.")]
        [SerializeField]
        private Mesh[] sourceMeshes;

        [Tooltip("실루엣 색. 집는 소품은 기본 주황, 상시 표시하는 설치물은 다른 색으로 구분한다.")]
        [SerializeField]
        private Color outlineColor = FocusColor;

        [Tooltip("실루엣 두께(화면 픽셀).")]
        [SerializeField]
        [Min(0.5f)]
        private float pixelWidth = FocusPixels;

        private readonly List<Renderer> outlineRenderers = new();
        private Material outlineMaterial;
        private bool built;

        public Color Color => outlineColor;

        public float PixelWidth => pixelWidth;

        public bool IsVisible { get; private set; }

        /// <summary>실루엣 대상으로 지정된 렌더러 수. 0이면 자식 렌더러를 쓴다.</summary>
        public int SourceCount => sourceRenderers?.Length ?? 0;

        public void SetVisible(bool visible)
        {
            if (visible)
            {
                EnsureBuilt();
            }

            IsVisible = visible;
            for (var index = 0; index < outlineRenderers.Count; index++)
            {
                // 외부 소품에 붙인 복사본은 씬 언로드 때 이 컴포넌트보다 먼저 사라질 수 있다.
                var renderer = outlineRenderers[index];
                if (renderer != null)
                {
                    renderer.enabled = visible;
                }
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
            outlineMaterial.SetColor("_OutlineColor", outlineColor);
            outlineMaterial.SetFloat("_OutlineWidth", 0.001f);
            outlineMaterial.SetFloat("_OutlinePixels", pixelWidth);

            var sources = SourceCount > 0
                ? sourceRenderers
                : GetComponentsInChildren<Renderer>(includeInactive: true);
            for (var index = 0; index < sources.Length; index++)
            {
                var source = sources[index];
                if (source == null || ItemOutlineRenderers.IsGenerated(source))
                {
                    continue;
                }

                switch (source)
                {
                    case MeshRenderer meshRenderer:
                        CreateMeshOutline(meshRenderer, StoredMesh(index));
                        break;
                    case SkinnedMeshRenderer skinnedRenderer:
                        CreateSkinnedOutline(skinnedRenderer);
                        break;
                }
            }
        }

        /// <summary>지정 소품용으로 저장된 원본 메시. 자식 렌더러 모드나 미저장이면 null.</summary>
        private Mesh StoredMesh(int index)
        {
            return SourceCount > 0 && sourceMeshes != null && index < sourceMeshes.Length
                ? sourceMeshes[index]
                : null;
        }

        /// <remarks>
        /// 정적 배칭이 켜진 소품은 플레이 중 <c>MeshFilter.sharedMesh</c>가 씬 전체를 합친
        /// "Combined Mesh"가 되어, 그대로 복사하면 엉뚱한 곳에 다른 물건의 실루엣이 그려진다.
        /// 저장된 원본 메시가 있으면 그것을 우선한다.
        /// </remarks>
        private void CreateMeshOutline(MeshRenderer source, Mesh storedMesh)
        {
            var sourceFilter = source.GetComponent<MeshFilter>();
            var mesh = storedMesh != null ? storedMesh : sourceFilter != null ? sourceFilter.sharedMesh : null;
            if (mesh == null)
            {
                return;
            }

            var child = CreateOutlineChild(source.transform);
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = child.AddComponent<MeshRenderer>();
            Configure(renderer, mesh.subMeshCount);
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

        /// <remarks>
        /// 원본 렌더러의 자식으로 붙인다. 원본이 정적(static)이어도 새 자식은
        /// 정적 플래그를 받지 않으므로 라이트맵·배칭에 섞이지 않는다.
        /// </remarks>
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
            // 외부 렌더러에 붙인 실루엣은 이 컴포넌트가 사라져도 남으므로 함께 치운다.
            // 자기 자식에 붙인 것은 오브젝트와 함께 사라진다.
            for (var index = 0; index < outlineRenderers.Count; index++)
            {
                var renderer = outlineRenderers[index];
                if (renderer == null || renderer.transform.IsChildOf(transform))
                {
                    continue;
                }

                DestroyObject(renderer.gameObject);
            }

            outlineRenderers.Clear();

            if (outlineMaterial != null)
            {
                DestroyObject(outlineMaterial);
            }
        }

        private static void DestroyObject(Object target)
        {
            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
