using System;
using System.Collections.Generic;

namespace Madness.Mods
{
    public static class ModContentRegistry
    {
        private static readonly Dictionary<string, object> content = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public static bool Register<T>(string id, T value)
        {
            if (string.IsNullOrWhiteSpace(id) || value == null) return false;
            if (content.ContainsKey(id)) return false;
            content.Add(id, value);
            return true;
        }

        public static bool Replace<T>(string id, T value)
        {
            if (string.IsNullOrWhiteSpace(id) || value == null) return false;
            content[id] = value;
            return true;
        }

        public static bool TryGet<T>(string id, out T value)
        {
            object raw;
            if (content.TryGetValue(id, out raw) && raw is T)
            {
                value = (T)raw;
                return true;
            }
            value = default(T);
            return false;
        }

        public static bool Remove(string id) { return content.Remove(id); }
        public static void Clear() { content.Clear(); }
    }
}
