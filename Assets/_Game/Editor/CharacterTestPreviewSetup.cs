using Game.Client;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    public static class CharacterTestPreviewSetup
    {
        private const string PreviewPath = "Assets/Scenes/CharacterTest/FromBlender/BlenderPreview.fbx";
        private const string ControllerPath = "Assets/Scenes/CharacterTest/CharacterTestPreview.controller";
        private const string ScenePath = "Assets/Scenes/CharacterTest.unity";
        private const string PreviewName = "BlenderPreview";
        private const string Tag = "blender-preview-v2";

        [InitializeOnLoadMethod]
        private static void BuildAfterReload()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    ApplyBlenderPreview();
                }
            };
        }

        [MenuItem("Game/Setup/Apply CharacterTest Blender Preview")]
        public static void ApplyFromMenu()
        {
            if (!ApplyBlenderPreview())
            {
                EditorUtility.DisplayDialog(
                    "CharacterTest",
                    "FromBlender/BlenderPreview.fbx 가 없습니다.",
                    "OK");
                return;
            }

            EditorUtility.DisplayDialog(
                "CharacterTest",
                "블렌더 캐릭터를 올렸습니다. Play 하세요.",
                "OK");
        }

        public static bool ApplyBlenderPreview()
        {
            var importer = AssetImporter.GetAtPath(PreviewPath) as ModelImporter;
            if (importer == null)
            {
                return false;
            }

            ConfigurePreview(importer);
            AssignToScene();
            return true;
        }

        private static void ConfigurePreview(ModelImporter importer)
        {
            if (importer.userData == Tag &&
                importer.animationType == ModelImporterAnimationType.Generic &&
                importer.avatarSetup == ModelImporterAvatarSetup.CreateFromThisModel)
            {
                return;
            }

            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.userData = Tag;
            importer.SaveAndReimport();
        }

        private static void AssignToScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PreviewPath);
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            if (prefab == null || controller == null)
            {
                return;
            }

            HideNamed("PlayerCapsule");
            HideNamed("PlayerCapsule_BakedIdle");

            var preview = FindNamed(PreviewName);
            if (preview == null)
            {
                preview = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                preview.name = PreviewName;
                preview.transform.SetPositionAndRotation(
                    new Vector3(0f, 0f, -3.59f),
                    Quaternion.identity);
            }

            preview.SetActive(true);

            var animator = preview.GetComponent<Animator>();
            if (animator == null)
            {
                animator = preview.GetComponentInChildren<Animator>(true);
            }

            if (animator != null)
            {
                animator.enabled = true;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.runtimeAnimatorController = controller;
            }

            if (preview.GetComponent<CharacterTestPreviewDriver>() == null)
            {
                preview.AddComponent<CharacterTestPreviewDriver>();
            }

            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static GameObject FindNamed(string name)
        {
            foreach (var transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (transform.parent == null && transform.name == name)
                {
                    return transform.gameObject;
                }
            }

            return null;
        }

        private static void HideNamed(string name)
        {
            var found = FindNamed(name);
            if (found != null)
            {
                found.SetActive(false);
            }
        }
    }
}
