using System;
using System.Collections.Generic;

namespace Madness.Mods
{
    public static class ModContentRegistry
    {
        private sealed class Entry { public string Owner; public object Value; }
        private static readonly Dictionary<string, Entry> content = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
        public static bool Register<T>(string id, T value) => Register("anonymous", id, value);
        public static bool Register<T>(string owner, string id, T value)
        {
            if (string.IsNullOrWhiteSpace(id) || value == null || content.ContainsKey(id)) return false;
            content.Add(id, new Entry { Owner = owner ?? "anonymous", Value = value }); return true;
        }
        public static bool Replace<T>(string id, T value) => Replace("anonymous", id, value);
        public static bool Replace<T>(string owner, string id, T value)
        {
            if (string.IsNullOrWhiteSpace(id) || value == null) return false;
            content[id] = new Entry { Owner = owner ?? "anonymous", Value = value }; return true;
        }
        public static bool TryGet<T>(string id, out T value)
        {
            Entry raw;
            if (content.TryGetValue(id ?? "", out raw) && raw.Value is T) { value=(T)raw.Value; return true; }
            value=default(T); return false;
        }
        public static void RemoveOwner(string owner)
        {
            if (string.IsNullOrEmpty(owner)) return;
            var keys=new List<string>(); foreach(var kv in content) if(string.Equals(kv.Value.Owner,owner,StringComparison.OrdinalIgnoreCase)) keys.Add(kv.Key);
            foreach(string key in keys) content.Remove(key);
        }
        public static bool Remove(string id) => content.Remove(id);
        public static void Clear() => content.Clear();
    }
}
