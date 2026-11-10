using System;
using System.Collections.Generic;
using UnityEngine;

namespace Madness.Mods
{
    /// <summary>
    /// Modların karakter, diyalog, prefab, ScriptableObject vb. içerikleri
    /// ID ile değiştirmesini sağlar.
    /// </summary>
    public static class ModContentOverride
    {
        private static readonly Dictionary<string, UnityEngine.Object> overrides =
            new Dictionary<string, UnityEngine.Object>(StringComparer.OrdinalIgnoreCase);

        public static void Register(string id, UnityEngine.Object asset)
        {
            if (string.IsNullOrWhiteSpace(id) || asset == null) return;
            overrides[id] = asset;
        }

        public static bool TryGet<T>(string id, out T asset) where T : UnityEngine.Object
        {
            UnityEngine.Object raw;
            if (overrides.TryGetValue(id, out raw) && raw is T)
            {
                asset = (T)raw;
                return true;
            }
            asset = null;
            return false;
        }

        public static T GetOrDefault<T>(string id, T fallback) where T : UnityEngine.Object
        {
            T result;
            return TryGet(id, out result) ? result : fallback;
        }

        public static void Clear() { overrides.Clear(); }

        // Kolay yardımcılar
        public static GameObject GetPrefab(string id, GameObject fallback = null)
            => GetOrDefault(id, fallback);

        public static Sprite GetSprite(string id, Sprite fallback = null)
            => GetOrDefault(id, fallback);
    }
}
