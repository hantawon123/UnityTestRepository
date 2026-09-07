using Game.Client;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    public static class CharacterTestPreviewSetup
    {
        private const string PreviewPath = "Assets/Scenes/CharacterTest/First/FirstPlayerCapsule_Idle_Breathing_2s.fbx";
        private const string ControllerPath = "Assets/Scenes/CharacterTest/First/FirstCharacterPreview.controller";
        private const string ScenePath = "Assets/Scenes/CharacterTest.unity";
        private const string PreviewName = "FirstPlayerCapsule";
        private const string Tag = "first-motion-preview-v2";
        private const string IdleState = "Idle_Breathing";
        private static readonly MotionDefinition[] MotionDefinitions =
        {
            new("Assets/Scenes/CharacterTest/First/FirstPlayerCapsule_Walk_Wide_Clean.fbx", "Walk_Wide_Clean", 24, true),
            new("Assets/Scenes/CharacterTest/First/FirstPlayerCapsule_Run_SideArms.fbx", "Run_SideArms", 20, true),
            new("Assets/Scenes/CharacterTest/First/FirstPlayerCapsule_Jump_Cute.fbx", "Jump_Cute", 41, false),
            new("Assets/Scenes/CharacterTest/First/FirstPlayerCapsule_Fall_Flutter.fbx", "Fall_Flutter", 48, true),
            new("Assets/Scenes/CharacterTest/First/FirstPlayerCapsule_Land_Matched.fbx", "Land_Matched", 20, false),
            new("Assets/Scenes/CharacterTest/First/FirstPlayerCapsule_Crouch_Idle_KneesUp.fbx", "Crouch_Idle_KneesUp", 60, true, true),
            new("Assets/Scenes/CharacterTest/First/FirstPlayerCapsule_Crouch_Walk_Forward_KneesUp.fbx", "Crouch_Walk_Forward_KneesUp", 36, true, true),
            new("Assets/Scenes/CharacterTest/First/FirstPlayerCapsule_Stand_To_Crouch_KneesUp.fbx", "Stand_To_Crouch_KneesUp", 24, false, true),
            new("Assets/Scenes/CharacterTest/First/FirstPlayerCapsule_Crouch_To_Stand_KneesUp.fbx", "Crouch_To_Stand_KneesUp", 24, false, true),
            new("Assets/Scenes/CharacterTest/First/FirstPlayerCapsule_Crouch_Walk_Left_KneesUp.fbx", "Crouch_Walk_Left_KneesUp", 36, true, true),
            new("Assets/Scenes/CharacterTest/First/FirstPlayerCapsule_Crouch_Walk_Right_KneesUp.fbx", "Crouch_Walk_Right_KneesUp", 36, true, true),
            new("Assets/Scenes/CharacterTest/First/FirstPlayerCapsule_Crouch_Walk_Back_KneesUp.fbx", "Crouch_Walk_Back_KneesUp", 36, true, true),
            new("Assets/Scenes/CharacterTest/First/FirstPlayerCapsule_Pickup_Low.fbx", "Pickup_Low", 60, false, true),
            new("Assets/Scenes/CharacterTest/First/FirstPlayerCapsule_Carry_Idle.fbx", "Carry_Idle", 60, true, true),
            new("Assets/Scenes/CharacterTest/First/FirstPlayerCapsule_PutDown_Low.fbx", "PutDown_Low", 60, false, true),
            new("Assets/Scenes/CharacterTest/First/FirstPlayerCapsule_Prone_Start.fbx", "Prone_Start", 24, false),
            new("Assets/Scenes/CharacterTest/First/FirstPlayerCapsule_Prone_Idle.fbx", "Prone_Idle", 60, true),
            new("Assets/Scenes/CharacterTest/First/FirstPlayerCapsule_Crawl_Forward.fbx", "Crawl_Forward", 36, true),
            new("Assets/Scenes/CharacterTest/First/FirstPlayerCapsule_Crawl_Back.fbx", "Crawl_Back", 36, true),
            new("Assets/Scenes/CharacterTest/First/FirstPlayerCapsule_Crawl_Left.fbx", "Crawl_Left", 36, true),
            new("Assets/Scenes/CharacterTest/First/FirstPlayerCapsule_Crawl_Right.fbx", "Crawl_Right", 36, true),
            new("Assets/Scenes/CharacterTest/First/FirstPlayerCapsule_Prone_End.fbx", "Prone_End", 24, false),
        };

        [InitializeOnLoadMethod]
        private static void BuildAfterReload()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    ApplyBlenderPreview();
                }
            };
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (scene.path == ScenePath && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                ApplyBlenderPreview();
            }
        }

        [MenuItem("Game/Setup/Apply CharacterTest Blender Preview")]
        public static void ApplyFromMenu()
        {
            if (!ApplyBlenderPreview())
            {
                EditorUtility.DisplayDialog(
                    "CharacterTest",
                    "First 캐릭터 FBX가 없습니다.",
                    "OK");
                return;
            }

            EditorUtility.DisplayDialog(
                "CharacterTest",
                "First 캐릭터의 전체 동작을 연결했습니다. Play 하세요.",
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
            foreach (var motion in MotionDefinitions)
            {
                var motionImporter = AssetImporter.GetAtPath(motion.Path) as ModelImporter;
                if (motionImporter == null)
                {
                    return false;
                }

                ConfigureMotion(motionImporter, motion);
            }

            ConfigureController();
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
            importer.importBlendShapes = true;
            importer.importAnimation = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.clipAnimations = new[]
            {
                new ModelImporterClipAnimation
                {
                    name = IdleState,
                    takeName = importer.defaultClipAnimations.First().takeName,
                    firstFrame = 0,
                    lastFrame = 60,
                    loopTime = true,
                    keepOriginalOrientation = true,
                    keepOriginalPositionY = true,
                    keepOriginalPositionXZ = true,
                }
            };
            importer.userData = Tag;
            importer.SaveAndReimport();
        }

        private static void ConfigureMotion(ModelImporter importer, MotionDefinition motion)
        {
            if (importer.userData == Tag &&
                importer.animationType == ModelImporterAnimationType.Generic &&
                importer.avatarSetup == ModelImporterAvatarSetup.CreateFromThisModel &&
                importer.clipAnimations.Length == 1 &&
                importer.clipAnimations[0].name == motion.State &&
                Mathf.Approximately(importer.clipAnimations[0].lastFrame, motion.LastFrame) &&
                importer.clipAnimations[0].loopTime == motion.Loop &&
                importer.importBlendShapes == motion.ImportBlendShapes)
            {
                return;
            }

            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.importAnimation = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importBlendShapes = motion.ImportBlendShapes;
            var takeName = "Scene";
            var takes = importer.defaultClipAnimations;
            if (takes != null && takes.Length > 0)
            {
                takeName = takes[0].takeName;
            }

            importer.clipAnimations = new[]
            {
                new ModelImporterClipAnimation
                {
                    name = motion.State,
                    takeName = takeName,
                    firstFrame = 0,
                    lastFrame = motion.LastFrame,
                    loopTime = motion.Loop,
                    keepOriginalOrientation = true,
                    keepOriginalPositionY = true,
                    keepOriginalPositionXZ = true,
                }
            };
            importer.userData = Tag;
            importer.SaveAndReimport();
        }

        private static void ConfigureController()
        {
            var clip = AssetDatabase.LoadAllAssetsAtPath(PreviewPath)
                .OfType<AnimationClip>().First(c => c.name == IdleState);
            var bellyBinding = AnimationUtility.GetCurveBindings(clip)
                .FirstOrDefault(b => b.propertyName == "blendShape.Belly_Breath");
            var bellyCurve = AnimationUtility.GetEditorCurve(clip, bellyBinding);
            if (bellyCurve == null || bellyCurve.Evaluate(0.8f) - bellyCurve.Evaluate(0f) < 90f)
            {
                throw new System.InvalidOperationException("First idle FBX must contain animated belly expansion (0 to 100).");
            }

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            var machine = controller.layers[0].stateMachine;
            var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == IdleState)
                ?? machine.AddState(IdleState);
            state.motion = clip;
            state.writeDefaultValues = false;
            foreach (var motion in MotionDefinitions)
            {
                var clipMotion = AssetDatabase.LoadAllAssetsAtPath(motion.Path)
                    .OfType<AnimationClip>().FirstOrDefault(clip => clip.name == motion.State);
                if (clipMotion == null)
                {
                    Debug.LogWarning($"CharacterTest: missing clip {motion.State} at {motion.Path}");
                    continue;
                }
                var motionState = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == motion.State)
                    ?? machine.AddState(motion.State);
                motionState.motion = clipMotion;
                motionState.writeDefaultValues = false;
            }
            machine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssetIfDirty(controller);
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
            HideNamed("BlenderPreview");

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

            var driver = preview.GetComponent<CharacterTestPreviewDriver>();
            if (driver == null)
            {
                driver = preview.AddComponent<CharacterTestPreviewDriver>();
            }

            driver.ConfigureMotions(new[] { IdleState }.Concat(MotionDefinitions.Select(motion => motion.State)).ToArray());
            EditorUtility.SetDirty(driver);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);

            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static GameObject FindNamed(string name)
        {
            foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
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

        private sealed class MotionDefinition
        {
            public MotionDefinition(string path, string state, float lastFrame, bool loop, bool importBlendShapes = false)
            {
                Path = path;
                State = state;
                LastFrame = lastFrame;
                Loop = loop;
                ImportBlendShapes = importBlendShapes;
            }

            public string Path { get; }
            public string State { get; }
            public float LastFrame { get; }
            public bool Loop { get; }
            public bool ImportBlendShapes { get; }
        }
    }
}
