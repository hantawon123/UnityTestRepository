using System.Collections.Generic;
using System.Linq;
using Game.Client.Interactions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    /// <summary>
    /// 마트 매치 맵(Supermarket) 조명 준비·베이크 메뉴. 로비의 <see cref="LobbyLightingSetupMenu"/>를 마트 규모에 맞게 옮긴 것.
    /// </summary>
    /// <remarks>
    /// 로비 도구를 그대로 못 쓰는 이유: (1) 경계 안 52×75 m로 로비(13×12 m)의 25배라 라이트맵 해상도를 낮춰야 하고,
    /// (2) 씬이 한 루트 아래가 아니라 평평(루트 9천여 개)하고 경계가 6면이 아닌 박스 15개 합집합이며,
    /// (3) 고정 소품에 ContributeGI가 하나도 없어 굽기 전에 표시해야 한다.
    /// <list type="number">
    /// <item>Mark Static: 경계 안 고정 소품(Carryable·Rigidbody 아님)에 ContributeGI·ReflectionProbeStatic·OccludeeStatic.</item>
    /// <item>Convert Lights: 태양광 Mixed, 스팟·포인트 Baked(부드러운 그림자), 창고 구역(x &lt; -40) 포인트 세기 절반.</item>
    /// <item>Setup: 조명 설정 에셋(8 texels/m, 4096), 라이트 프로브 격자, 리플렉션 프로브 격자.</item>
    /// <item>Post-Process: 로비 프로필 복제 → 마트 볼륨, Synty 데모 볼륨 제거, 카메라 PP + SMAA.</item>
    /// <item>Bake / Clear / Report.</item>
    /// </list>
    /// 재베이크 규칙은 로비와 같다: 고정물(선반·벽·전등·Static 플래그) 변경 뒤 반드시 5번 다시 실행. 들 수 있는 상품은 동적이라 무관.
    /// </remarks>
    public static class MartLightingSetupMenu
    {
        private const string MenuRoot = "Game/Match Map/Lighting/";
        private const string ScenePath = "Assets/_Game/Content/Scenes/Supermarket.unity";
        private const string LightingFolder = "Assets/_Game/Content/Lighting";
        private const string LightingSettingsPath = LightingFolder + "/MartLighting.lighting";
        private const string LobbyPostProfilePath = LightingFolder + "/LobbyPostProcess.asset";
        private const string PostProfilePath = LightingFolder + "/MartPostProcess.asset";
        private const string EnvironmentRootName = "MartEnvironment";
        private const string BoundaryName = "Boundary";
        private const string ProbeGroupName = "MartLightProbes";
        private const string ReflectionRootName = "Probes";
        private const string PostVolumeName = "Mart Post Volume";
        private const string SyntyDemoVolumeName = "Global Volume";

        // 라이트 프로브: 로비(1.5 m)보다 넓게. 52×75 m → 약 15×22×3 = 1,000개 안팎.
        private const float ProbeSpacing = 3.5f;
        private static readonly float[] ProbeHeights = { 0.4f, 1.6f, 3.2f };
        private const float ProbeWallMargin = 0.6f;
        private const float ProbeSolidRadius = 0.2f;

        // 리플렉션 프로브 격자(가로×세로 칸 수). 칸마다 Baked 박스 프로브 하나.
        private const int ReflectionColumns = 3;
        private const int ReflectionRows = 4;
        private const int ReflectionResolution = 128;

        /// <summary>이 x보다 서쪽은 창고·하역장 → 숨기기 유리 구역으로 어둡게.</summary>
        private const float WarehouseMaxX = -40f;
        private const float WarehouseIntensityScale = 0.5f;

        private const int CarryableLayer = 7;

        // ------------------------------------------------------------------ 1. 정적 플래그

        [MenuItem(MenuRoot + "1. Mark Fixed Props Static For Baking")]
        public static void MarkStaticForBaking()
        {
            var scene = EnsureSceneOpen();
            if (!scene.IsValid())
            {
                return;
            }

            var hasBoundary = TryGetBoundary(out var boundary);
            var marked = 0;
            var skippedOutside = 0;
            var skippedMovable = 0;
            const StaticEditorFlags bakeFlags =
                StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic | StaticEditorFlags.OccludeeStatic;

            foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (renderer.gameObject.scene != scene || renderer.gameObject.name.StartsWith("["))
                {
                    continue;
                }

                if (IsMovable(renderer.transform))
                {
                    skippedMovable++;
                    continue;
                }

                if (hasBoundary && !boundary.Contains(renderer.bounds.center))
                {
                    skippedOutside++;
                    continue;
                }

                var flags = GameObjectUtility.GetStaticEditorFlags(renderer.gameObject);
                if ((flags & bakeFlags) == bakeFlags)
                {
                    continue;
                }

                Undo.RecordObject(renderer.gameObject, "Mark Static For Baking");
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, flags | bakeFlags);
                marked++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Mart Lighting] ContributeGI 표시 {marked}개 (경계 밖 제외 {skippedOutside}, 움직이는 것 제외 {skippedMovable}). 씬 저장됨.");
        }

        // ------------------------------------------------------------------ 2. 조명 모드

        [MenuItem(MenuRoot + "2. Convert Lights (Sun Mixed, Others Baked, Warehouse Dim)")]
        public static void ConvertLights()
        {
            var scene = EnsureSceneOpen();
            if (!scene.IsValid())
            {
                return;
            }

            var mixed = 0;
            var baked = 0;
            var dimmed = 0;
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (light.gameObject.scene != scene)
                {
                    continue;
                }

                Undo.RecordObject(light, "Convert Mart Lights");
                if (light.type == LightType.Directional)
                {
                    light.lightmapBakeType = LightmapBakeType.Mixed;
                    light.shadows = LightShadows.Soft;
                    mixed++;
                    continue;
                }

                light.lightmapBakeType = LightmapBakeType.Baked;
                light.shadows = LightShadows.Soft; // Baked 라이트도 shadows가 None이면 베이크 그림자가 안 생긴다
                baked++;

                if (light.type == LightType.Point && light.transform.position.x < WarehouseMaxX && !light.name.EndsWith(" (Dim)"))
                {
                    light.intensity *= WarehouseIntensityScale;
                    light.name += " (Dim)"; // 두 번 실행해도 다시 반으로 줄지 않게 표시
                    dimmed++;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Mart Lighting] Mixed {mixed}, Baked {baked}, 창고 어둡게 {dimmed}. 씬 저장됨.");
        }

        // ------------------------------------------------------------------ 3. 설정·프로브

        [MenuItem(MenuRoot + "3. Setup Lighting Settings, Light Probes, Reflection Probes")]
        public static void SetupLighting()
        {
            var scene = EnsureSceneOpen();
            if (!scene.IsValid())
            {
                return;
            }

            if (!TryGetBoundary(out var boundary))
            {
                EditorUtility.DisplayDialog("Mart Lighting",
                    $"'{EnvironmentRootName}/{BoundaryName}' 아래 BoxCollider가 없습니다. 경계를 먼저 배치하세요.", "확인");
                return;
            }

            var settings = GetOrCreateLightingSettings();
            Lightmapping.lightingSettings = settings;

            var probeCount = BuildLightProbes(boundary);
            var reflectionCount = BuildReflectionProbes(boundary);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Mart Lighting] Setup done. settings={LightingSettingsPath} ({settings.lightmapResolution} texels/m, max {settings.lightmapMaxSize}), " +
                      $"lightProbes={probeCount}, reflectionProbes={reflectionCount}. 다음: 4번(포스트프로세스) → 5번(Bake).");
            LogReport();
        }

        // ------------------------------------------------------------------ 4. 포스트프로세스

        [MenuItem(MenuRoot + "4. Setup Post-Process Volume And Camera")]
        public static void SetupPostProcess()
        {
            var scene = EnsureSceneOpen();
            if (!scene.IsValid())
            {
                return;
            }

            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PostProfilePath);
            if (profile == null)
            {
                if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(LobbyPostProfilePath) == null)
                {
                    Debug.LogError($"[Mart Lighting] 로비 프로필 {LobbyPostProfilePath} 이 없어 복제할 수 없습니다.");
                    return;
                }

                AssetDatabase.CopyAsset(LobbyPostProfilePath, PostProfilePath);
                profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PostProfilePath);
                Debug.Log($"[Mart Lighting] {LobbyPostProfilePath} → {PostProfilePath} 복제.");
            }

            // Synty 데모 씬에서 따라온 전역 볼륨(팩 Demo 폴더 프로필 참조)은 제거한다.
            foreach (var volume in Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (volume.gameObject.scene != scene || volume.name == PostVolumeName)
                {
                    continue;
                }

                var path = volume.sharedProfile != null ? AssetDatabase.GetAssetPath(volume.sharedProfile) : string.Empty;
                if (volume.name == SyntyDemoVolumeName || path.StartsWith("Assets/Synty/"))
                {
                    var removedName = volume.name;
                    Undo.DestroyObjectImmediate(volume.gameObject);
                    Debug.Log($"[Mart Lighting] 데모 볼륨 제거: {removedName} ({path})");
                }
            }

            var volumeObject = GameObject.Find(PostVolumeName);
            if (volumeObject == null)
            {
                volumeObject = new GameObject(PostVolumeName);
                Undo.RegisterCreatedObjectUndo(volumeObject, "Create Mart Post Volume");
                SceneManager.MoveGameObjectToScene(volumeObject, scene);
            }

            var postVolume = volumeObject.GetComponent<Volume>();
            if (postVolume == null)
            {
                postVolume = volumeObject.AddComponent<Volume>();
            }

            postVolume.isGlobal = true;
            postVolume.priority = 0f;
            postVolume.sharedProfile = profile;

            var camera = Camera.main;
            if (camera != null && camera.gameObject.scene == scene)
            {
                var data = camera.GetUniversalAdditionalCameraData();
                Undo.RecordObject(data, "Mart Camera Post-Process");
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                data.antialiasingQuality = AntialiasingQuality.Medium;
            }
            else
            {
                Debug.LogWarning("[Mart Lighting] 씬에 Main Camera가 없어 카메라 PP 설정은 건너뜀.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Mart Lighting] 포스트프로세스 설정 완료: {PostVolumeName} → {profile.name}, 카메라 PP + SMAA Medium. 씬 저장됨.");
        }

        // ------------------------------------------------------------------ 5~7. 베이크·정리·보고

        [MenuItem(MenuRoot + "5. Bake Lighting (Lightmaps + Reflection + Light Probes)")]
        public static void Bake()
        {
            var scene = EnsureSceneOpen();
            if (!scene.IsValid())
            {
                return;
            }

            if (Lightmapping.isRunning)
            {
                Debug.LogWarning("[Mart Lighting] 이미 베이크가 진행 중입니다.");
                return;
            }

            if (Lightmapping.lightingSettings == null ||
                AssetDatabase.GetAssetPath(Lightmapping.lightingSettings) != LightingSettingsPath)
            {
                Lightmapping.lightingSettings = GetOrCreateLightingSettings();
            }

            LogReport();
            Lightmapping.bakeCompleted -= OnBakeCompleted;
            Lightmapping.bakeCompleted += OnBakeCompleted;

            var started = Lightmapping.BakeAsync();
            Debug.Log(started
                ? "[Mart Lighting] Bake started. 완료되면 콘솔에 결과가 출력되고 씬이 저장됩니다."
                : "[Mart Lighting] Bake failed to start. Lighting 창의 오류를 확인하세요.");
        }

        [MenuItem(MenuRoot + "6. Clear Baked Data")]
        public static void ClearBaked()
        {
            var scene = EnsureSceneOpen();
            if (!scene.IsValid())
            {
                return;
            }

            Lightmapping.Clear();
            Lightmapping.ClearLightingDataAsset();
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[Mart Lighting] Baked data cleared.");
        }

        [MenuItem(MenuRoot + "Report Lighting State")]
        public static void Report()
        {
            LogReport();
        }

        private static void OnBakeCompleted()
        {
            Lightmapping.bakeCompleted -= OnBakeCompleted;
            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            var dataAsset = Lightmapping.lightingDataAsset;
            Debug.Log($"[Mart Lighting] Bake completed. lightmaps={LightmapSettings.lightmaps.Length}, " +
                      $"lightingData={(dataAsset != null ? AssetDatabase.GetAssetPath(dataAsset) : "none")}. 씬 저장됨.");
        }

        // ------------------------------------------------------------------ 내부

        private static Scene EnsureSceneOpen()
        {
            var active = SceneManager.GetActiveScene();
            if (active.path == ScenePath)
            {
                return active;
            }

            if (!EditorUtility.DisplayDialog("Mart Lighting",
                    $"활성 씬이 Supermarket이 아닙니다.\n{ScenePath} 를 열고 진행할까요?", "열기", "취소"))
            {
                return default;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return default;
            }

            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static bool IsMovable(Transform transform)
        {
            return transform.GetComponentInParent<CarryableItem>(true) != null ||
                   transform.GetComponentInParent<Rigidbody>(true) != null;
        }

        /// <summary>경계 박스 15개의 합집합. 로비처럼 6면 이름이 정해져 있지 않으므로 바운드로 잰다.</summary>
        private static bool TryGetBoundary(out Bounds boundary)
        {
            boundary = default;
            var root = GameObject.Find(EnvironmentRootName);
            var boundaryRoot = root != null ? root.transform.Find(BoundaryName) : null;
            if (boundaryRoot == null)
            {
                return false;
            }

            var found = false;
            foreach (var box in boundaryRoot.GetComponentsInChildren<BoxCollider>(true))
            {
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

            return found;
        }

        private static LightingSettings GetOrCreateLightingSettings()
        {
            var existing = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingSettingsPath);
            if (existing != null)
            {
                return existing;
            }

            if (!AssetDatabase.IsValidFolder(LightingFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Game/Content", "Lighting");
            }

            var settings = new LightingSettings
            {
                name = "MartLighting",
                bakedGI = true,
                realtimeGI = false,
                lightmapper = LightingSettings.Lightmapper.ProgressiveGPU,
                mixedBakeMode = MixedLightingMode.Shadowmask,
                directionalityMode = LightmapsMode.CombinedDirectional,
                // 해상도: 경계 안 52×75 m + 고정 소품 1만 3천 개. 로비(20)의 절반 이하, 최대 크기는 두 배.
                lightmapResolution = 8f,
                lightmapMaxSize = 4096,
                lightmapPadding = 2,
                lightmapCompression = LightmapCompression.NormalQuality,
                // 샘플/바운스: 넓은 맵이라 로비(32/512/3)보다 가볍게 시작. 얼룩이 보이면 올린다.
                directSampleCount = 32,
                indirectSampleCount = 256,
                environmentSampleCount = 128,
                maxBounces = 2,
                lightProbeSampleCountMultiplier = 2f,
                ao = true,
                aoMaxDistance = 0.5f,
                aoExponentDirect = 0f,
                aoExponentIndirect = 1f,
                filteringMode = LightingSettings.FilterMode.Auto,
            };

            AssetDatabase.CreateAsset(settings, LightingSettingsPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Mart Lighting] Created {LightingSettingsPath}");
            return settings;
        }

        /// <summary>경계 안 격자에 라이트 프로브를 놓는다. 벽·선반 속(콜라이더 안)은 뺀다. 상품(Carryable 레이어)은 무시.</summary>
        private static int BuildLightProbes(Bounds boundary)
        {
            var existing = GameObject.Find(ProbeGroupName);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
            }

            var go = new GameObject(ProbeGroupName);
            Undo.RegisterCreatedObjectUndo(go, "Build Mart Light Probes");
            var group = go.AddComponent<LightProbeGroup>();

            var minX = boundary.min.x + ProbeWallMargin;
            var maxX = boundary.max.x - ProbeWallMargin;
            var minZ = boundary.min.z + ProbeWallMargin;
            var maxZ = boundary.max.z - ProbeWallMargin;
            var countX = Mathf.Max(2, Mathf.RoundToInt((maxX - minX) / ProbeSpacing) + 1);
            var countZ = Mathf.Max(2, Mathf.RoundToInt((maxZ - minZ) / ProbeSpacing) + 1);
            var floorY = Mathf.Max(0f, boundary.min.y);
            var hits = new Collider[8];
            var layerMask = Physics.DefaultRaycastLayers & ~(1 << CarryableLayer);

            var positions = new List<Vector3>();
            for (var ix = 0; ix < countX; ix++)
            {
                var x = Mathf.Lerp(minX, maxX, ix / (float)(countX - 1));
                for (var iz = 0; iz < countZ; iz++)
                {
                    var z = Mathf.Lerp(minZ, maxZ, iz / (float)(countZ - 1));
                    foreach (var h in ProbeHeights)
                    {
                        var p = new Vector3(x, floorY + h, z);
                        if (p.y > boundary.max.y - 0.3f)
                        {
                            continue;
                        }

                        var count = Physics.OverlapSphereNonAlloc(p, ProbeSolidRadius, hits, layerMask, QueryTriggerInteraction.Ignore);
                        var insideSolid = false;
                        for (var i = 0; i < count; i++)
                        {
                            if (hits[i].transform.parent != null && hits[i].transform.parent.name == BoundaryName)
                            {
                                continue; // 경계 박스는 막힘으로 치지 않음
                            }

                            insideSolid = true;
                            break;
                        }

                        if (!insideSolid)
                        {
                            positions.Add(p);
                        }
                    }
                }
            }

            group.probePositions = positions.ToArray();
            return positions.Count;
        }

        /// <summary>경계를 가로×세로 칸으로 나눠 칸마다 Baked 박스 리플렉션 프로브를 놓는다.</summary>
        private static int BuildReflectionProbes(Bounds boundary)
        {
            var root = GameObject.Find(EnvironmentRootName);
            var probesRoot = root.transform.Find(ReflectionRootName);
            if (probesRoot == null)
            {
                var go = new GameObject(ReflectionRootName);
                Undo.RegisterCreatedObjectUndo(go, "Create Probes Root");
                go.transform.SetParent(root.transform, false);
                probesRoot = go.transform;
            }

            foreach (var old in probesRoot.GetComponentsInChildren<ReflectionProbe>(true).ToArray())
            {
                Undo.DestroyObjectImmediate(old.gameObject);
            }

            var cell = new Vector3(boundary.size.x / ReflectionColumns, boundary.size.y, boundary.size.z / ReflectionRows);
            var floorY = Mathf.Max(0f, boundary.min.y);
            var height = boundary.max.y - floorY;
            var created = 0;
            for (var cx = 0; cx < ReflectionColumns; cx++)
            {
                for (var cz = 0; cz < ReflectionRows; cz++)
                {
                    var center = new Vector3(
                        boundary.min.x + cell.x * (cx + 0.5f),
                        floorY + Mathf.Min(1.8f, height * 0.5f),
                        boundary.min.z + cell.z * (cz + 0.5f));
                    var go = new GameObject($"ReflectionProbe_{cx}_{cz}");
                    Undo.RegisterCreatedObjectUndo(go, "Create Reflection Probe");
                    go.transform.SetParent(probesRoot, false);
                    go.transform.position = center;
                    var probe = go.AddComponent<ReflectionProbe>();
                    probe.mode = ReflectionProbeMode.Baked;
                    probe.resolution = ReflectionResolution;
                    probe.size = new Vector3(cell.x + 1f, height, cell.z + 1f);
                    probe.center = new Vector3(0f, floorY + height * 0.5f - center.y, 0f);
                    probe.boxProjection = true;
                    probe.importance = 1;
                    created++;
                }
            }

            return created;
        }

        private static void LogReport()
        {
            var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var byMode = lights.GroupBy(l => l.lightmapBakeType).Select(g => $"{g.Key}={g.Count()}");
            var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var contributeGI = renderers.Count(r =>
                GameObjectUtility.AreStaticEditorFlagsSet(r.gameObject, StaticEditorFlags.ContributeGI));
            var probes = GameObject.Find(ProbeGroupName)?.GetComponent<LightProbeGroup>();
            var reflection = Object.FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var settingsName = Lightmapping.TryGetLightingSettings(out var settings) && settings != null ? settings.name : "default";

            Debug.Log("[Mart Lighting] Report: " +
                      $"lights={lights.Length} ({string.Join(", ", byMode)}), " +
                      $"renderers={renderers.Length} contributeGI={contributeGI}, " +
                      $"lightProbes={(probes != null ? probes.probePositions.Length : 0)}, " +
                      $"reflectionProbes={reflection.Length}, lightmaps={LightmapSettings.lightmaps.Length}, settings={settingsName}");
        }
    }
}
