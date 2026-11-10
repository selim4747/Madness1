using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Madness.Mods
{
    /// <summary>
    /// Mono derlemede DLL modlarının kullanabileceği tam yetkili API.
    /// IL2CPP'de DLL yüklenmediği için bu sınıf da sadece Mono modlarından çağrılır.
    /// </summary>
    public static class ModAPI
    {
        public static bool IsFullPower => !ModScriptLoader.IsIL2CPP;

        // --- Event Bus ---
        public static void Subscribe<T>(Action<T> handler, int priority = 0, string owner = "") where T : ModContext
            => ModEventBus.Subscribe(handler, priority, owner);

        public static void UnsubscribeOwner(string owner)
            => ModEventBus.UnsubscribeOwner(owner);

        // --- Content ---
        public static void RegisterContent(string id, UnityEngine.Object asset)
            => ModContentOverride.Register(id, asset);

        public static T GetContent<T>(string id, T fallback = null) where T : UnityEngine.Object
            => ModContentOverride.GetOrDefault(id, fallback);

        public static T LoadAsset<T>(string modId, string assetName) where T : UnityEngine.Object
            => ModAssetLoader.LoadAsset<T>(modId, assetName);

        // --- Scenes ---
        public static bool LoadModScene(string modId, string sceneName = null, bool additive = false)
            => ModSceneLoader.LoadFromMod(modId, sceneName, additive);

        public static void LoadScene(string sceneName, bool additive = false)
            => SceneManager.LoadScene(sceneName, additive ? LoadSceneMode.Additive : LoadSceneMode.Single);

        // --- Save ---
        public static void SetState(string modId, string json, int schema = 1)
            => ModRuntime.EnsureExists().SetState(modId, json, schema);

        public static string GetState(string modId)
            => ModRuntime.EnsureExists().GetState(modId);

        // --- World ---
        public static bool TryGetAnchor(string id, out Vector3 position)
            => ModWorldRegistry.TryGetPosition(id, out position);

        // --- Utility ---
        public static void Log(string modId, string message)
            => Debug.Log("[" + modId + "] " + message);

        public static void LogError(string modId, string message)
            => Debug.LogError("[" + modId + "] " + message);

        /// <summary>
        /// Mono'da true döner. Modlar buna bakarak "tam güç" özelliklerini açabilir.
        /// </summary>
        public static bool CanRunArbitraryCode => IsFullPower;
    }
}
