using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Madness.Mods
{
    /// <summary>
    /// Oyunla birlikte derlenen C# modları. Her mod derlemede bulunur; oyuncu seçimleri
    /// yalnızca etkinleştirmeyi kontrol eder. IL2CPP'de dışarıdan DLL çalıştırılmaz.
    /// Mod geliştiricileri kayıtlarını BeforeSceneLoad aşamasında yapmalıdır.
    /// </summary>
    public static class BuiltInModCatalog
    {
        private const string PreferencePrefix = "Madness.BuiltInMod.";
        private sealed class Entry
        {
            public string Id;
            public string Name;
            public Func<IModModule> Factory;
        }

        private static readonly Dictionary<string, Entry> Entries =
            new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
        public static void Register(string id, string name, string version, Func<IModModule> factory)
        {
            if (string.IsNullOrWhiteSpace(id) || factory == null)
                throw new ArgumentException("Yerleşik mod kimliği ve factory zorunludur.");
            // Editörde "Domain Reload" kapalıyken tekrar kayıt oyunu bozmasın: aynı kimlik üzerine yazılır.
            Entries[id] = new Entry
            {
                Id = id,
                Name = string.IsNullOrWhiteSpace(name) ? id : name,
                Factory = factory
            };
        }

        public static IReadOnlyList<string> GetAvailableIds()
            => Entries.Values.Select(e => e.Id).OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray();

        public static string GetDisplayName(string id)
            => Entries.TryGetValue(id ?? "", out Entry entry) ? entry.Name : id;

        public static bool Contains(string id)
            => Entries.ContainsKey(id ?? "");

        public static bool IsEnabled(string id)
        {
            if (!Entries.ContainsKey(id ?? "")) return false;
            // İlk açılışta hiçbir mod seçili değildir; oyuncu özellikle açarsa uygulanır.
            return PlayerPrefs.GetInt(PreferencePrefix + id, 0) != 0;
        }

        public static bool SetEnabled(string id, bool enabled)
        {
            if (!Entries.ContainsKey(id ?? "")) return false;
            PlayerPrefs.SetInt(PreferencePrefix + id, enabled ? 1 : 0);
            PlayerPrefs.Save();
            ModRuntime runtime = ModRuntime.Instance;
            if (runtime != null) runtime.ReloadModSelection();
            return true;
        }

        internal static IEnumerable<IModModule> CreateEnabledModules()
        {
            foreach (Entry entry in Entries.Values.OrderBy(e => e.Id, StringComparer.OrdinalIgnoreCase))
            {
                if (!IsEnabled(entry.Id)) continue;
                IModModule module = null;
                try { module = entry.Factory(); }
                catch (Exception ex) { Debug.LogError("[BuiltInMods] Factory hatası (" + entry.Id + "): " + ex); }
                if (module == null)
                {
                    Debug.LogError("[BuiltInMods] Mod örneği oluşturulamadı: " + entry.Id);
                    continue;
                }
                if (!string.Equals(module.Id, entry.Id, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogError("[BuiltInMods] Katalog kimliği ile IModModule.Id uyuşmuyor: " + entry.Id);
                    continue;
                }
                yield return module;
            }
        }
    }
}
