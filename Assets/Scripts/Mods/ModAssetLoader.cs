using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Madness.Mods
{
    /// <summary>
    /// Mod klasöründeki AssetBundle'ları yükler.
    /// Sahne, prefab, ScriptableObject, sprite, ses vb. modun içinde olabilir.
    /// </summary>
    public static class ModAssetLoader
    {
        private static readonly Dictionary<string, AssetBundle> bundles = new Dictionary<string, AssetBundle>(StringComparer.OrdinalIgnoreCase);

        public static AssetBundle LoadBundle(string modId, string bundleFileName = null)
        {
            if (string.IsNullOrWhiteSpace(modId)) return null;
            if (bundles.TryGetValue(modId, out AssetBundle existing) && existing != null)
                return existing;

            var runtime = ModRuntime.EnsureExists();
            string modDir = Path.Combine(runtime.ModsDirectory, modId);
            if (!Directory.Exists(modDir)) return null;

            string path = null;
            if (!string.IsNullOrWhiteSpace(bundleFileName))
                path = Path.Combine(modDir, bundleFileName);
            else
            {
                // varsayılan isimler
                string[] candidates = { "content.bundle", "assets.bundle", "mod.bundle", modId + ".bundle" };
                foreach (var c in candidates)
                {
                    string p = Path.Combine(modDir, c);
                    if (File.Exists(p)) { path = p; break; }
                }
                if (path == null)
                {
                    var files = Directory.GetFiles(modDir, "*.bundle");
                    if (files.Length > 0) path = files[0];
                }
            }

            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                Debug.Log("[ModAssetLoader] " + modId + " için AssetBundle bulunamadı.");
                return null;
            }

            try
            {
                AssetBundle bundle = AssetBundle.LoadFromFile(path);
                if (bundle == null)
                {
                    Debug.LogError("[ModAssetLoader] Bundle yüklenemedi: " + path);
                    return null;
                }
                bundles[modId] = bundle;
                Debug.Log("[ModAssetLoader] Yüklendi: " + modId + " (" + path + ")");
                return bundle;
            }
            catch (Exception ex)
            {
                Debug.LogError("[ModAssetLoader] " + ex);
                return null;
            }
        }

        public static T LoadAsset<T>(string modId, string assetName) where T : UnityEngine.Object
        {
            AssetBundle bundle = LoadBundle(modId);
            if (bundle == null) return null;
            return bundle.LoadAsset<T>(assetName);
        }

        public static string[] GetScenePaths(string modId)
        {
            AssetBundle bundle = LoadBundle(modId);
            return bundle == null ? new string[0] : bundle.GetAllScenePaths();
        }

        public static void Unload(string modId, bool unloadAllLoadedObjects = false)
        {
            if (bundles.TryGetValue(modId, out AssetBundle b) && b != null)
            {
                b.Unload(unloadAllLoadedObjects);
                bundles.Remove(modId);
            }
        }

        public static void UnloadAll()
        {
            foreach (var kv in bundles)
                if (kv.Value != null) kv.Value.Unload(false);
            bundles.Clear();
        }
    }
}
