using System;
using System.Collections.Generic;
using Game.Client.Common;
using Game.Client.Home;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace Game.Bootstrap
{
    /// <summary>
    /// Keeps the frontend screens loaded and swaps their scene roots.
    /// Gameplay scenes remain owned by Fusion and unload them normally.
    /// </summary>
    /// <remarks>
    /// Home and the room browser are kept warm in each other's company because
    /// the player crosses between them constantly and both are cheap. The
    /// closet is not preloaded: it carries a lit room and a character, and it
    /// is opened far less often than it is passed by. Nor is the settings
    /// screen, which is cheap but rarer still.
    /// </remarks>
    internal sealed class FrontendSceneCoordinator : IStartable, IDisposable
    {
        private const string Home = UnityHomeApplicationHost.HomeSceneName;
        private const string Room = UnityHomeApplicationHost.RoomBrowserSceneName;
        private const string Closet = UnityHomeApplicationHost.CharacterClosetSceneName;
        private const string Settings = UnityHomeApplicationHost.SettingsSceneName;

        private static readonly string[] Frontends = { Home, Room, Closet, Settings };

        private string desiredScene;
        private readonly Dictionary<string, AsyncOperation> loads =
            new Dictionary<string, AsyncOperation>(StringComparer.Ordinal);

        private double switchStartedAt = -1d;
        private readonly EventSystem sharedEventSystem;

        public FrontendSceneCoordinator(EventSystem sharedEventSystem)
        {
            this.sharedEventSystem = sharedEventSystem;
        }

        public void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            var active = SceneManager.GetActiveScene();
            if (!IsFrontend(active))
            {
                sharedEventSystem.gameObject.SetActive(false);
                return;
            }

            DisableDuplicateEventSystems(active);
            ActivateSharedEventSystem();
            desiredScene = active.name;
            SetRootsActive(active, true);
            EnsureCounterpartLoaded(active.name);
        }

        public void Dispose()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        public void OpenHome() => Open(Home);

        public void OpenRoomBrowser() => Open(Room);

        public void OpenCharacterCloset() => Open(Closet);

        public void OpenSettings() => Open(Settings);

        private void Open(string sceneName)
        {
            desiredScene = sceneName;
            switchStartedAt = Time.realtimeSinceStartupAsDouble;
            Debug.Log(
                $"[SceneTiming] Frontend switch requested: " +
                $"{SceneManager.GetActiveScene().name} -> {sceneName}.");

            if (!TryShow(sceneName))
            {
                EnsureLoaded(sceneName);
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!IsFrontend(scene))
            {
                foreach (var frontend in Frontends)
                {
                    SetRootsActive(GetLoadedScene(frontend), false);
                }

                sharedEventSystem.gameObject.SetActive(false);
                return;
            }

            DisableDuplicateEventSystems(scene);
            ActivateSharedEventSystem();
            ClearLoad(scene.name);

            if (!AnyOtherFrontendLoaded(scene.name))
            {
                // This frontend was entered from a gameplay/session scene.
                desiredScene = scene.name;
            }

            if (!TryShow(desiredScene))
            {
                SetRootsActive(scene, false);
            }

            EnsureCounterpartLoaded(desiredScene);
        }

        private void OnSceneUnloaded(Scene scene)
        {
            if (!IsFrontend(scene))
            {
                return;
            }

            ClearLoad(scene.name);
        }

        private bool TryShow(string sceneName)
        {
            var target = GetLoadedScene(sceneName);
            if (!target.IsValid())
            {
                return false;
            }

            SetRootsActive(target, true);
            SceneManager.SetActiveScene(target);
            foreach (var frontend in Frontends)
            {
                if (!string.Equals(frontend, sceneName, StringComparison.Ordinal))
                {
                    SetRootsActive(GetLoadedScene(frontend), false);
                }
            }

            ActivateSharedEventSystem();

            if (switchStartedAt >= 0d)
            {
                Debug.Log(
                    $"[SceneTiming] Frontend switch completed: scene={sceneName}, " +
                    $"elapsed={Time.realtimeSinceStartupAsDouble - switchStartedAt:F3}s.");
                switchStartedAt = -1d;
            }

            EnsureCounterpartLoaded(sceneName);
            return true;
        }

        /// <summary>
        /// Keeps Home and the room browser warm for each other. The closet is
        /// deliberately absent: it is loaded when it is asked for, and asking
        /// for Home does not pay for a room and a character nobody is looking
        /// at.
        /// </summary>
        private void EnsureCounterpartLoaded(string visibleScene)
        {
            if (string.Equals(visibleScene, Home, StringComparison.Ordinal))
            {
                EnsureLoaded(Room);
            }
            else if (string.Equals(visibleScene, Room, StringComparison.Ordinal))
            {
                EnsureLoaded(Home);
            }
        }

        private bool AnyOtherFrontendLoaded(string sceneName)
        {
            foreach (var frontend in Frontends)
            {
                if (!string.Equals(frontend, sceneName, StringComparison.Ordinal) &&
                    GetLoadedScene(frontend).IsValid())
                {
                    return true;
                }
            }

            return false;
        }

        private void EnsureLoaded(string sceneName)
        {
            if (GetLoadedScene(sceneName).IsValid() || GetLoad(sceneName) != null)
            {
                return;
            }

            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (operation == null)
            {
                Debug.LogError($"[SceneTiming] Could not load frontend scene '{sceneName}'.");
                return;
            }

            operation.priority = 100;
            SetLoad(sceneName, operation);
            Debug.Log($"[SceneTiming] Frontend additive load requested: scene={sceneName}.");
        }

        private AsyncOperation GetLoad(string sceneName) =>
            loads.TryGetValue(sceneName, out var operation) ? operation : null;

        private void SetLoad(string sceneName, AsyncOperation operation) =>
            loads[sceneName] = operation;

        private void ClearLoad(string sceneName) => loads.Remove(sceneName);

        private static Scene GetLoadedScene(string sceneName)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            return scene.IsValid() && scene.isLoaded ? scene : default;
        }

        private static bool IsFrontend(Scene scene)
        {
            if (!scene.IsValid())
            {
                return false;
            }

            foreach (var frontend in Frontends)
            {
                if (string.Equals(scene.name, frontend, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <remarks>
        /// A root marked <see cref="FrontendPersistentRoot"/> is skipped. It
        /// belongs to the application rather than to the screen whose scene
        /// happens to hold it, and switching it off with that screen would hide
        /// a notice the player is meant to answer from wherever they are.
        /// </remarks>
        private static void SetRootsActive(Scene scene, bool active)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.GetComponent<FrontendPersistentRoot>() != null)
                {
                    continue;
                }

                root.SetActive(active);
            }
        }

        private void DisableDuplicateEventSystems(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var eventSystem in root.GetComponentsInChildren<EventSystem>(true))
                {
                    if (eventSystem == sharedEventSystem)
                    {
                        continue;
                    }

                    eventSystem.enabled = false;
                    var inputModule = eventSystem.GetComponent<BaseInputModule>();
                    if (inputModule != null)
                    {
                        inputModule.enabled = false;
                    }
                }
            }
        }

        private void ActivateSharedEventSystem()
        {
            sharedEventSystem.gameObject.SetActive(true);
            sharedEventSystem.enabled = true;
            EventSystem.current = sharedEventSystem;
        }
    }
}
