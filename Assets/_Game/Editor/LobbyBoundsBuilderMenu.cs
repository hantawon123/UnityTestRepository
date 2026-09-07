using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// 로비 환경 루트 주변에 보이지 않는 두꺼운 경계 콜라이더 6면을 생성한다.
    /// 벽 모듈 콜라이더가 있어도 물리 버그(고속 충돌, 끼임 튕김)로 밖으로 나가는 것을 막는 안전망.
    /// </summary>
    public static class LobbyBoundsBuilderMenu
    {
        private const string MenuPath = "Game/Lobby/Build Boundary Colliders";
        private const string DefaultRootName = "LobbyBasementEnvironment";
        private const string BoundaryName = "Boundary";

        // 벽 두께(m): 두꺼울수록 고속 관통에 안전
        private const float Thickness = 2.0f;
        // 실측 바운드 바깥으로 띄우는 여유(m)
        private const float Padding = 0.05f;

        [MenuItem(MenuPath)]
        public static void BuildBoundaryColliders()
        {
            var root = Selection.activeGameObject;
            if (root == null)
            {
                root = GameObject.Find(DefaultRootName);
            }
            if (root == null)
            {
                EditorUtility.DisplayDialog(
                    "Lobby Bounds",
                    $"환경 루트를 선택하거나 씬에 '{DefaultRootName}' 오브젝트를 두세요.",
                    "확인");
                return;
            }

            // 기존 경계 제거 (데칼/경계 자신은 측정에서 제외)
            var old = root.transform.Find(BoundaryName);
            if (old != null)
            {
                Undo.DestroyObjectImmediate(old.gameObject);
            }

            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(r => r.enabled && !r.name.Contains("Decal"))
                .ToArray();
            if (renderers.Length == 0)
            {
                Debug.LogWarning("[Lobby] 측정할 렌더러가 없습니다.");
                return;
            }

            var bounds = renderers[0].bounds;
            foreach (var r in renderers)
            {
                bounds.Encapsulate(r.bounds);
            }

            var boundary = new GameObject(BoundaryName);
            Undo.RegisterCreatedObjectUndo(boundary, "Build Lobby Boundary");
            boundary.transform.SetParent(root.transform, false);
            boundary.transform.position = bounds.center;
            boundary.isStatic = true;

            var min = bounds.min - Vector3.one * Padding;
            var max = bounds.max + Vector3.one * Padding;
            var size = max - min;
            var center = (min + max) * 0.5f;
            var t = Thickness;

            // 각 면: 바운드 바깥에 두께 t짜리 판, 모서리가 겹치도록 다른 축은 두께만큼 확장
            AddWall(boundary, "West",  new Vector3(min.x - t * 0.5f, center.y, center.z), new Vector3(t, size.y + 2 * t, size.z + 2 * t));
            AddWall(boundary, "East",  new Vector3(max.x + t * 0.5f, center.y, center.z), new Vector3(t, size.y + 2 * t, size.z + 2 * t));
            AddWall(boundary, "South", new Vector3(center.x, center.y, min.z - t * 0.5f), new Vector3(size.x + 2 * t, size.y + 2 * t, t));
            AddWall(boundary, "North", new Vector3(center.x, center.y, max.z + t * 0.5f), new Vector3(size.x + 2 * t, size.y + 2 * t, t));
            AddWall(boundary, "Floor", new Vector3(center.x, min.y - t * 0.5f, center.z), new Vector3(size.x + 2 * t, t, size.z + 2 * t));
            AddWall(boundary, "Ceiling", new Vector3(center.x, max.y + t * 0.5f, center.z), new Vector3(size.x + 2 * t, t, size.z + 2 * t));

            Selection.activeGameObject = boundary;
            Debug.Log($"[Lobby] Boundary built around {root.name}: size {size.x:F1} x {size.y:F1} x {size.z:F1} m, thickness {t} m.");
        }

        private static void AddWall(GameObject parent, string name, Vector3 worldCenter, Vector3 worldSize)
        {
            var go = new GameObject($"Bound_{name}");
            go.transform.SetParent(parent.transform, false);
            go.transform.position = worldCenter;
            go.isStatic = true;
            var box = go.AddComponent<BoxCollider>();
            box.size = worldSize;
        }
    }
}
