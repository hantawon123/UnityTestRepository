using Game.Client.Interactions;
using Game.Client.Lobby;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public sealed class LobbyPlanBoardInteractableTests
    {
        private GameObject boardObject;
        private GameObject propObject;

        [TearDown]
        public void TearDown()
        {
            if (boardObject != null) Object.DestroyImmediate(boardObject);
            if (propObject != null) Object.DestroyImmediate(propObject);
        }

        [Test]
        public void Bind_ShowsOutlineOnExternalProp_UnbindHidesIt()
        {
            propObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var propRenderer = propObject.GetComponent<MeshRenderer>();

            boardObject = new GameObject("LobbyPlanBoard");
            boardObject.AddComponent<BoxCollider>();
            var outline = boardObject.AddComponent<InteractableFocusOutline>();
            using (var serialized = new SerializedObject(outline))
            {
                var sources = serialized.FindProperty("sourceRenderers");
                sources.arraySize = 1;
                sources.GetArrayElementAtIndex(0).objectReferenceValue = propRenderer;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            var board = boardObject.AddComponent<LobbyPlanBoardInteractable>();
            using (var serialized = new SerializedObject(board))
            {
                serialized.FindProperty("outline").objectReferenceValue = outline;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            Assert.That(board.IsOutlineVisible, Is.False);

            board.Bind(() => true, () => { });

            Assert.That(board.IsOutlineVisible, Is.True);
            var generated = FindGeneratedOutline(propObject.transform);
            Assert.That(generated, Is.Not.Null, "실루엣은 지정한 소품 렌더러 밑에 생겨야 한다.");
            Assert.That(generated.enabled, Is.True);
            Assert.That(boardObject.transform.childCount, Is.EqualTo(0),
                "판 오브젝트 자체에는 실루엣이 생기지 않아야 한다.");

            board.Unbind();

            Assert.That(board.IsOutlineVisible, Is.False);
            Assert.That(generated.enabled, Is.False);
        }

        // 외부 소품에 붙인 실루엣을 컴포넌트 파괴 시 함께 치우는 동작은 OnDestroy에 있어
        // 에디트 모드 테스트로는 검증할 수 없다(에디트 모드에서는 OnDestroy가 호출되지 않음).

        [Test]
        public void WithoutOutline_BindStillWorks()
        {
            boardObject = new GameObject("LobbyPlanBoard");
            boardObject.AddComponent<BoxCollider>();
            var board = boardObject.AddComponent<LobbyPlanBoardInteractable>();

            Assert.DoesNotThrow(() => board.Bind(() => true, () => { }));
            Assert.That(board.IsBound, Is.True);
            Assert.That(board.IsOutlineVisible, Is.False);
        }

        private static Renderer FindGeneratedOutline(Transform prop)
        {
            foreach (var renderer in prop.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.gameObject != prop.gameObject && renderer.name.StartsWith("["))
                {
                    return renderer;
                }
            }

            return null;
        }
    }
}
