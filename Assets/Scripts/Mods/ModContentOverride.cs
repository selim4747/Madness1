using System;
using System.Collections.Generic;
using UnityEngine;

namespace Madness.Mods
{
    public static class ModContentOverride
    {
        private sealed class Entry { public string Owner; public UnityEngine.Object Asset; }
        private static readonly Dictionary<string, Entry> overrides =
            new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);

        public static void Register(string id, UnityEngine.Object asset) => Register("anonymous", id, asset);
        public static void Register(string owner, string id, UnityEngine.Object asset)
        {
            if (string.IsNullOrWhiteSpace(id) || asset == null) return;
            overrides[id] = new Entry { Owner = owner ?? "anonymous", Asset = asset };
        }

        public static bool TryGet<T>(string id, out T asset) where T : UnityEngine.Object
        {
            Entry entry;
            if (overrides.TryGetValue(id ?? "", out entry) && entry.Asset is T)
            { asset = (T)entry.Asset; return true; }
            asset = null; return false;
        }

        public static T GetOrDefault<T>(string id, T fallback) where T : UnityEngine.Object
        { T result; return TryGet(id, out result) ? result : fallback; }
        public static void RemoveOwner(string owner)
        {
            if (string.IsNullOrEmpty(owner)) return;
            var keys = new List<string>();
            foreach (var kv in overrides) if (string.Equals(kv.Value.Owner, owner, StringComparison.OrdinalIgnoreCase)) keys.Add(kv.Key);
            foreach (string key in keys) overrides.Remove(key);
        }
        public static void Clear() => overrides.Clear();
        public static GameObject GetPrefab(string id, GameObject fallback = null) => GetOrDefault(id, fallback);
        public static Sprite GetSprite(string id, Sprite fallback = null) => GetOrDefault(id, fallback);
    }
}
