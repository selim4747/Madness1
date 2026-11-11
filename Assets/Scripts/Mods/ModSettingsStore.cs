using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Madness.Mods
{
    /// <summary>Mod ayarlarını mod klasöründen ayrı, güvenli ve save bağımsız saklar.</summary>
    public static class ModSettingsStore
    {
        private const string FileName = "settings.json";
        private static readonly Dictionary<string, Dictionary<string, string>> cache =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        private static bool loaded;

        private static string SettingsDirectory
        {
            get { return Path.Combine(Application.persistentDataPath, "Mods"); }
        }

        private static string PathFor(string modId)
        {
            if (!ModRuntime.IsSafeModId(modId)) return null;
            return Path.Combine(SettingsDirectory, modId + ".settings.json");
        }

        public static void LoadAll()
        {
            if (loaded) return;
            loaded = true;
            try { Directory.CreateDirectory(SettingsDirectory); } catch (Exception ex) { Debug.LogWarning("[ModSettings] Klasör oluşturulamadı: " + ex.Message); }
        }

        public static string Get(string modId, string key, string fallback = "")
        {
            LoadAll();
            Dictionary<string, string> values = GetValues(modId);
            string value;
            return values.TryGetValue(key ?? "", out value) ? value : fallback;
        }

        public static bool GetBool(string modId, string key, bool fallback = false)
        {
            bool result;
            return bool.TryParse(Get(modId, key, fallback.ToString()), out result) ? result : fallback;
        }

        public static int GetInt(string modId, string key, int fallback = 0)
        {
            int result;
            return int.TryParse(Get(modId, key, fallback.ToString()), out result) ? result : fallback;
        }

        public static float GetFloat(string modId, string key, float fallback = 0f)
        {
            float result;
            return float.TryParse(Get(modId, key, fallback.ToString(System.Globalization.CultureInfo.InvariantCulture)), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out result) ? result : fallback;
        }

        public static void Set(string modId, string key, string value)
        {
            if (string.IsNullOrWhiteSpace(modId) || string.IsNullOrWhiteSpace(key)) return;
            LoadAll();
            GetValues(modId)[key] = value ?? "";
            Save(modId);
        }

        public static void Reset(string modId, string key)
        {
            LoadAll();
            GetValues(modId).Remove(key ?? "");
            Save(modId);
        }

        public static void Delete(string modId)
        {
            cache.Remove(modId ?? "");
            try
            {
                string path = PathFor(modId);
                if (!string.IsNullOrEmpty(path) && File.Exists(path)) File.Delete(path);
            }
            catch (Exception ex) { Debug.LogWarning("[ModSettings] Ayar dosyası silinemedi: " + ex.Message); }
        }

        private static Dictionary<string, string> GetValues(string modId)
        {
            Dictionary<string, string> values;
            if (cache.TryGetValue(modId ?? "", out values)) return values;
            values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            cache[modId ?? ""] = values;
            try
            {
                string path = PathFor(modId);
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) return values;
                ModSettingsFile file = JsonUtility.FromJson<ModSettingsFile>(File.ReadAllText(path));
                if (file != null && file.values != null)
                    foreach (ModSettingValue item in file.values)
                        if (item != null && !string.IsNullOrWhiteSpace(item.key)) values[item.key] = item.value ?? "";
            }
            catch (Exception ex) { Debug.LogWarning("[ModSettings] Ayarlar okunamadı (" + modId + "): " + ex.Message); }
            return values;
        }

        private static void Save(string modId)
        {
            try
            {
                Directory.CreateDirectory(SettingsDirectory);
                Dictionary<string, string> values = GetValues(modId);
                ModSettingsFile file = new ModSettingsFile();
                foreach (KeyValuePair<string, string> pair in values)
                    file.values.Add(new ModSettingValue { key = pair.Key, value = pair.Value });
                string path = PathFor(modId);
                if (string.IsNullOrEmpty(path)) return;
                string temp = path + ".tmp";
                string backup = path + ".bak";
                File.WriteAllText(temp, JsonUtility.ToJson(file, true));
                if (File.Exists(path))
                {
                    try { File.Replace(temp, path, backup, true); }
                    catch { File.Copy(path, backup, true); File.Delete(path); File.Move(temp, path); }
                }
                else File.Move(temp, path);
            }
            catch (Exception ex) { Debug.LogError("[ModSettings] Ayarlar yazılamadı (" + modId + "): " + ex.Message); }
        }
    }
}
