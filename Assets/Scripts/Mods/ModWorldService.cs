using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Madness.Mods
{
    [DefaultExecutionOrder(-900)]
    public sealed class ModWorldService : MonoBehaviour
    {
        public static ModWorldService Instance { get; private set; }
        public string ActiveWorldId { get; private set; } = "base";
        public string ActiveSceneName { get; private set; } = "";
        public event Action<string> OnWorldLoaded;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        public static ModWorldService EnsureExists()
        {
            if (Instance != null) return Instance;
            GameObject go = new GameObject("ModWorldService");
            return go.AddComponent<ModWorldService>();
        }

        public bool TryLoadSceneWorld(string worldId, string sceneName, bool additive = false)
        {
            if (string.IsNullOrWhiteSpace(sceneName)) return false;
            WorldLoadContext context = ModEventBus.Publish(new WorldLoadContext
            {
                WorldId = worldId ?? sceneName,
                SceneName = sceneName,
                Additive = additive
            });
            if (context.CancelLoad) return false;
            StartCoroutine(LoadScene(context));
            return true;
        }

        private IEnumerator LoadScene(WorldLoadContext context)
        {
            AsyncOperation operation = context.Additive
                ? SceneManager.LoadSceneAsync(context.SceneName, LoadSceneMode.Additive)
                : SceneManager.LoadSceneAsync(context.SceneName, LoadSceneMode.Single);
            if (operation == null) yield break;
            yield return operation;
            ActiveWorldId = context.WorldId;
            ActiveSceneName = context.SceneName;
            if (OnWorldLoaded != null) OnWorldLoaded(ActiveWorldId);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (string.IsNullOrEmpty(ActiveSceneName)) ActiveSceneName = scene.name;
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Instance = null;
        }
    }
}
