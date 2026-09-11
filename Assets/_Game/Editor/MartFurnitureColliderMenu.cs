using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Game.Client.Interactions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Game.Editor
{
    /// <summary>
    /// 마트 가구 콜라이더를 실제 모양으로 바꿔 선반 칸 안의 상품에 조준 광선이 닿게 한다.
    /// </summary>
    /// <remarks>
    /// 문제: Synty 가구(진열대·냉장고·진열 스탠드·카트)는 convex 메시 콜라이더라 볼록 껍질이 선반 칸을 통째로 덮고,
    /// 분해 도구가 만든 구조물 조각(계산대 선반·벽 냉장고 본체)은 바운드 크기 BoxCollider였다. 둘 다 안쪽 상품을 가린다.
    /// <list type="bullet">
    /// <item>우리 생성 구조물 프리팹(<c>Gen_*</c>, CarryableItem 없음): BoxCollider → non-convex MeshCollider(자기 메시). 프리팹 에셋을 고친다.</item>
    /// <item>Synty 가구 인스턴스(경계 안, Carryable 아님): MeshCollider의 convex를 끈다(인스턴스 오버라이드, 팩 에셋은 그대로).</item>
    /// <item>움직이지 않는 가구라 non-convex 메시 콜라이더가 허용된다. 벽·바닥 BoxCollider는 그대로 둔다.</item>
    /// <item><b>Reachability Report</b>: 상품마다 네 방향에서 눈높이 광선을 쏘아 몇 개가 닿는지, 무엇이 가리는지 집계한다.</item>
    /// </list>
    /// </remarks>
    public static class MartFurnitureColliderMenu
    {
        private const string MenuRoot = "Game/Match Map/Carryable/";
        private const string GeneratedFolder = "Assets/_Game/Content/MatchMap/GeneratedProps";
        private const string BoundaryName = "Boundary";

        [MenuItem(MenuRoot + "4. Fix Furniture Colliders (Non-Convex)")]
        public static void FixFurnitureColliders()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[MartCollider] Play 모드를 끝낸 뒤 실행하세요.");
                return;
            }

            var scene = SceneManager.GetActiveScene();
            var prefabsFixed = FixGeneratedStructurePrefabs();
            var instancesFixed = FixSceneFurnitureInstances(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[MartCollider] 생성 구조물 프리팹 {prefabsFixed}개 → 메시 콜라이더, 가구 인스턴스 {instancesFixed}개 convex 해제. 씬 저장됨.");
        }

        /// <summary>CarryableItem이 없는 생성 프리팹(구조물)의 BoxCollider를 자기 메시의 non-convex MeshCollider로 바꾼다.</summary>
        public static int FixGeneratedStructurePrefabs()
        {
            var fixedCount = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { GeneratedFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null || asset.GetComponent<CarryableItem>() != null)
                {
                    continue; // 상품은 그대로(들 수 있는 물건은 convex/box여야 한다)
                }

                var filter = asset.GetComponent<MeshFilter>();
                var box = asset.GetComponent<BoxCollider>();
                var existing = asset.GetComponent<MeshCollider>();
                if (filter == null || filter.sharedMesh == null || (box == null && (existing == null || !existing.convex)))
                {
                    continue;
                }

                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach (var collider in root.GetComponents<BoxCollider>())
                    {
                        Object.DestroyImmediate(collider);
                    }

                    var mesh = root.GetComponent<MeshFilter>().sharedMesh;
                    var meshCollider = root.GetComponent<MeshCollider>();
                    if (meshCollider == null)
                    {
                        meshCollider = root.AddComponent<MeshCollider>();
                    }

                    meshCollider.sharedMesh = mesh;
                    meshCollider.convex = false;
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    fixedCount++;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();
            return fixedCount;
        }

        /// <summary>
        /// 경계 안의 Carryable이 아닌 가구 인스턴스에서 메시 콜라이더를 <b>시각 메시 + non-convex</b>로 바꾼다(오버라이드).
        /// </summary>
        /// <remarks>
        /// Synty 가구 프리팹의 MeshCollider는 시각 메시가 아니라 <c>Models/Collision/Convex/*_Convex.asset</c>(닫힌 껍질)을 쓴다.
        /// convex만 꺼도 앞면이 막힌 채라 선반 칸·냉동고 안의 상품에 광선이 닿지 않는다. 같은 오브젝트의 MeshFilter 메시
        /// (Read/Write 켜 둠)를 콜라이더에 쓰면 실제 열린 모양대로 막힌다.
        /// </remarks>
        public static int FixSceneFurnitureInstances(Scene scene)
        {
            var hasBoundary = TryGetBoundary(scene, out var boundary);
            var changed = 0;
            foreach (var collider in Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (collider.gameObject.scene != scene)
                {
                    continue;
                }

                if (collider.GetComponentInParent<CarryableItem>() != null || collider.GetComponentInParent<Rigidbody>() != null)
                {
                    continue; // 들 수 있는 물건·움직이는 것은 convex 유지
                }

                if (hasBoundary && !Inside(boundary, collider.bounds.center))
                {
                    continue;
                }

                var filter = collider.GetComponent<MeshFilter>();
                var visualMesh = filter != null ? filter.sharedMesh : null;
                var needsMesh = visualMesh != null && collider.sharedMesh != visualMesh;
                if (!collider.convex && !needsMesh)
                {
                    continue;
                }

                Undo.RecordObject(collider, "Fix Furniture Collider");
                if (needsMesh)
                {
                    collider.sharedMesh = visualMesh;
                }

                collider.convex = false;
                changed++;
            }

            return changed;
        }

        // ------------------------------------------------------------------ 도달 보고

        [MenuItem(MenuRoot + "5. Reachability Report")]
        public static void ReachabilityReport()
        {
            var scene = SceneManager.GetActiveScene();
            Physics.SyncTransforms();
            var report = BuildReachability(scene, out var reachable, out var blocked);
            Debug.Log($"[MartCollider] 조준 도달 검사: 닿음 {reachable}개 / 막힘 {blocked}개\n{report}");
        }

        /// <summary>상품마다 네 방향에서 눈높이(1.6 m) 1.2 m 거리에서 광선을 쏘아, 첫 충돌이 그 상품인지 본다.</summary>
        public static string BuildReachability(Scene scene, out int reachable, out int blocked)
        {
            reachable = 0;
            blocked = 0;
            var blockers = new Dictionary<string, int>(StringComparer.Ordinal);
            var hits = new RaycastHit[16];
            var directions = new[] { Vector3.left, Vector3.right, Vector3.back, Vector3.forward };
            foreach (var item in Object.FindObjectsByType<CarryableItem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (item.gameObject.scene != scene)
                {
                    continue;
                }

                var renderer = item.GetComponentInChildren<Renderer>();
                if (renderer == null)
                {
                    continue;
                }

                var target = renderer.bounds.center;
                var seen = false;
                string firstBlocker = null;
                foreach (var direction in directions)
                {
                    var eye = target + (direction * 1.2f);
                    eye.y = 1.6f;
                    var count = Physics.RaycastNonAlloc(new Ray(eye, (target - eye).normalized), hits, 2.5f,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                    if (count == 0)
                    {
                        continue;
                    }

                    var first = hits.Take(count).OrderBy(h => h.distance).First();
                    if (first.collider.GetComponentInParent<CarryableItem>() == item)
                    {
                        seen = true;
                        break;
                    }

                    firstBlocker ??= System.Text.RegularExpressions.Regex.Replace(first.collider.gameObject.name, @"\s*\(\d+\)|\s\d+$", string.Empty);
                }

                if (seen)
                {
                    reachable++;
                }
                else
                {
                    blocked++;
                    if (firstBlocker != null)
                    {
                        blockers[firstBlocker] = blockers.GetValueOrDefault(firstBlocker) + 1;
                    }
                }
            }

            var sb = new StringBuilder();
            foreach (var pair in blockers.OrderByDescending(p => p.Value).Take(15))
            {
                sb.AppendLine($"  막는 콜라이더 {pair.Key} x{pair.Value}");
            }

            return sb.ToString();
        }

        // ------------------------------------------------------------------ 공용

        private static bool TryGetBoundary(Scene scene, out Bounds boundary)
        {
            boundary = default;
            var found = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var box in root.GetComponentsInChildren<BoxCollider>(true))
                {
                    if (box.transform.parent == null || box.transform.parent.name != BoundaryName)
                    {
                        continue;
                    }

                    if (!found)
                    {
                        boundary = box.bounds;
                        found = true;
                    }
                    else
                    {
                        boundary.Encapsulate(box.bounds);
                    }
                }
            }

            return found;
        }

        private static bool Inside(Bounds boundary, Vector3 point)
        {
            return point.x > boundary.min.x && point.x < boundary.max.x &&
                   point.z > boundary.min.z && point.z < boundary.max.z &&
                   point.y < boundary.max.y;
        }
    }
}
