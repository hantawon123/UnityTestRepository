using System.Linq;
using Game.Client.Lobby;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    /// <summary>
    /// 로비 씬의 서벽 공구판(Basement_ToolBoard)을 작전 계획판으로 만든다.
    /// 공구판 렌더러 바운드를 재서 살짝 큰 BoxCollider와
    /// <see cref="LobbyPlanBoardInteractable"/>을 가진 씬 오브젝트를 놓는다.
    /// 환경 프리팹은 건드리지 않으므로 라이트맵 재베이크가 필요 없다.
    /// </summary>
    public static class LobbyPlanBoardSetupMenu
    {
        private const string MenuPath = "Game/Lobby/Place Plan Board (ToolBoard)";
        private const string LobbyScenePath = "Assets/_Game/Content/Scenes/Lobby.unity";
        private const string EnvironmentRootName = "LobbyBasementEnvironment";
        private const string BoardSourceName = "Basement_ToolBoard";
        private const string BoardObjectName = "LobbyPlanBoard";

        // 공구판 자체 콜라이더보다 먼저 맞아야 조준 대상이 되므로 높이·폭을 조금 키운다.
        private const float ColliderPadding = 0.03f;

        // 서벽(x -3.10~-3.00 콜라이더)에 걸린 판이라, 방 쪽(+X) 면은 벽 콜라이더 안쪽 면에서
        // 1 cm만 나오게 한다. 더 나오면 위 모서리가 발판이 되어 2.5 m짜리 벽 콜라이더를
        // 뛰어넘는 디딤대가 될 수 있다. 조준 광선은 벽보다 이 판을 먼저 맞으므로 문제없다.
        private const float WallOuterX = -3.10f;
        private const float RoomFaceX = -2.99f;

        [MenuItem(MenuPath)]
        public static void PlacePlanBoard()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != LobbyScenePath)
            {
                if (!EditorUtility.DisplayDialog("Lobby Plan Board",
                        $"활성 씬이 Lobby가 아닙니다.\n{LobbyScenePath} 를 열고 진행할까요?", "열기", "취소") ||
                    !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    return;
                }

                scene = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);
            }

            var root = GameObject.Find(EnvironmentRootName);
            var source = root != null
                ? root.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(t => t.name.Trim() == BoardSourceName)
                : null;
            if (source == null)
            {
                EditorUtility.DisplayDialog("Lobby Plan Board",
                    $"'{EnvironmentRootName}/**/{BoardSourceName}' 을 찾지 못했습니다.", "확인");
                return;
            }

            var renderers = source.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).ToArray();
            if (renderers.Length == 0)
            {
                EditorUtility.DisplayDialog("Lobby Plan Board", "공구판에 렌더러가 없습니다.", "확인");
                return;
            }

            var bounds = renderers[0].bounds;
            foreach (var r in renderers)
            {
                bounds.Encapsulate(r.bounds);
            }
            bounds.Expand(new Vector3(0f, ColliderPadding * 2f, ColliderPadding * 2f));
            // 깊이는 벽 콜라이더 바깥 면에서 방 쪽 1 cm까지로 고정한다.
            var min = bounds.min;
            var max = bounds.max;
            min.x = WallOuterX;
            max.x = RoomFaceX;
            bounds.SetMinMax(min, max);

            var existing = GameObject.Find(BoardObjectName);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
            }

            var board = new GameObject(BoardObjectName);
            Undo.RegisterCreatedObjectUndo(board, "Place Lobby Plan Board");
            board.transform.position = bounds.center;
            var box = board.AddComponent<BoxCollider>();
            box.size = bounds.size;
            board.AddComponent<LobbyPlanBoardInteractable>();

            Selection.activeGameObject = board;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"[Lobby] Plan board placed over {BoardSourceName}: center {bounds.center}, " +
                      $"size {bounds.size.x:F2} x {bounds.size.y:F2} x {bounds.size.z:F2} m. 씬 저장됨.");
        }
    }
}
