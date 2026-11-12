using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Madness.Mods
{
    [DefaultExecutionOrder(-1000)]
    public sealed class ModRuntime : MonoBehaviour
    {
        public static ModRuntime Instance { get; private set; }
        public const int CurrentApiVersion = 1;

        public static bool IsSafeModId(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length > 64 || id != id.Trim() || id.Contains("..")) return false;
            for (int i = 0; i < id.Length; i++)
            {
                char c = id[i];
                if (!(char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.')) return false;
            }
            return true;
        }
        public string ModsDirectory { get; private set; }
        public IReadOnlyList<ModManifest> LoadedManifests { get { return loadedManifests; } }
        public IReadOnlyList<ModDiagnostic> Diagnostics { get { return diagnostics; } }
        public event Action OnModsChanged;

        private readonly List<ModManifest> loadedManifests = new List<ModManifest>();
        private readonly List<IModModule> modules = new List<IModModule>();
        private readonly List<ModSaveEntry> states = new List<ModSaveEntry>();
        private readonly List<IModModule> builtInModules = new List<IModModule>();
        private readonly List<ModDiagnostic> diagnostics = new List<ModDiagnostic>();
        private bool initialized;

        // false yapılırsa mod klasöründeki .dll dosyaları hiç yüklenmez (config.json ve gömülü modlar çalışır).
        // Not: .dll mod, oyunun yetkileriyle kod çalıştırır; sadece güvendiğin modları yükle.
        public static bool AllowDllMods = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        public static ModRuntime EnsureExists()
        {
            if (Instance != null) return Instance;
            GameObject go = new GameObject("ModRuntime");
            return go.AddComponent<ModRuntime>();
        }

        public void Initialize()
        {
            if (initialized) return;
            initialized = true;
            ModsDirectory = Path.Combine(Application.persistentDataPath, "Mods");
            try
            {
                Directory.CreateDirectory(ModsDirectory);
                ScanManifests();
                ModIL2CPPNotice.ShowOnce();
                ReloadModSelection();
            }
            catch (Exception ex)
            {
                Debug.LogError("[ModRuntime] Mod klasörü başlatılamadı: " + ex);
            }
        }

        private void ScanManifests()
        {
            loadedManifests.Clear();
            diagnostics.Clear();
            string[] files = Directory.GetFiles(ModsDirectory, "manifest.json", SearchOption.AllDirectories)
                .Where(file => !IsImportArtifact(file)).ToArray();
            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    ModManifest manifest = JsonUtility.FromJson<ModManifest>(File.ReadAllText(files[i]));
                    if (manifest == null || string.IsNullOrWhiteSpace(manifest.id))
                    {
                        ReportDiagnostic("", "Kimliksiz manifest atlandı: " + files[i], true);
                        Debug.LogWarning("[ModRuntime] Kimliksiz manifest atlandı: " + files[i]);
                        continue;
                    }
                    if (manifest.apiVersion != CurrentApiVersion.ToString())
                    {
                        ReportDiagnostic(manifest.id, "API sürümü uyumsuz: " + manifest.apiVersion, true);
                        Debug.LogWarning("[ModRuntime] API uyumsuzluğu nedeniyle atlandı: " + manifest.id);
                        continue;
                    }
                    if (!IsSafeModId(manifest.id))
                    {
                        ReportDiagnostic(manifest.id, "Güvensiz mod ID nedeniyle atlandı.", true);
                        continue;
                    }
                    if (loadedManifests.Any(m => string.Equals(m.id, manifest.id, StringComparison.OrdinalIgnoreCase)))
                    {
                        ReportDiagnostic(manifest.id, "Aynı ID'ye sahip birden fazla manifest bulundu; mod çakışması.", true);
                        continue;
                    }
                    loadedManifests.Add(manifest);
                }
                catch (Exception ex)
                {
                    ReportDiagnostic("", "Manifest okunamadı: " + ex.Message, true);
                    Debug.LogError("[ModRuntime] Manifest okunamadı: " + files[i] + " - " + ex.Message);
                }
            }
            loadedManifests.Sort((a, b) => a.loadOrder.CompareTo(b.loadOrder));
        }

        private bool IsImportArtifact(string file)
        {
            string normalized = file.Replace('\\', '/');
            return normalized.Contains("/.staging/") || normalized.Contains("/.backup/") || normalized.Contains("/.trash/");
        }

        public void RescanMods()
        {
            Initialize();
            ScanManifests();
            ReloadModSelection();
        }

        /// <summary>
        /// Mod seçimini baştan uygular: gömülü modları (oyuncunun seçtiklerini), config.json
        /// ayarlarını ve (izin varsa) .dll modlarını yeniden kurar. Bir modun hatası oyunu durdurmaz.
        /// </summary>
        public void ReloadModSelection()
        {
            Initialize();

            // Harici modları önce kaldır; aynı ID'li built-in modun callback'leri yanlışlıkla silinmesin.
            ModScriptLoader.UnloadAll(this);
            ModContentPackageLoader.UnloadAll();
            for (int i = builtInModules.Count - 1; i >= 0; i--)
            {
                try { UnregisterModule(builtInModules[i]); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
            builtInModules.Clear();

            try
            {
                foreach (IModModule module in BuiltInModCatalog.CreateEnabledModules())
                {
                    if (RegisterModule(module)) builtInModules.Add(module);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[ModRuntime] Gömülü modlar kurulamadı: " + ex);
            }

            try { ModConfigApplier.ApplyAll(this); }
            catch (Exception ex) { Debug.LogError("[ModRuntime] config.json uygulanamadı: " + ex); }

            try { ModContentPackageLoader.LoadAll(this); }
            catch (Exception ex) { Debug.LogError("[ModRuntime] Mod Creator içerikleri yüklenemedi: " + ex); }

            if (AllowDllMods)
            {
                try { ModScriptLoader.LoadAll(this); }
                catch (Exception ex) { Debug.LogError("[ModRuntime] DLL modları yüklenemedi: " + ex); }
            }

            if (OnModsChanged != null) OnModsChanged();
        }

        public void ReportDiagnostic(string modId, string message, bool error)
        {
            diagnostics.Add(new ModDiagnostic { modId = modId ?? "", message = message ?? "", error = error });
            if (error) Debug.LogWarning("[ModRuntime] " + (string.IsNullOrEmpty(modId) ? "" : modId + ": ") + message);
        }

        public bool IsBuiltIn(string modId) { return BuiltInModCatalog.Contains(modId); }

        public string GetModDirectory(string modId)
        {
            if (!IsSafeModId(modId)) return null;
            string path = Path.Combine(ModsDirectory, modId);
            return Directory.Exists(path) ? path : null;
        }

        public string GetDependencyError(ModManifest manifest)
        {
            if (manifest == null) return "Mod manifesti yok.";
            return GetDependencyError(manifest, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        }

        private string GetDependencyError(ModManifest manifest, HashSet<string> visiting)
        {
            if (manifest.dependencies == null) return null;
            if (!visiting.Add(manifest.id)) return "Bağımlılık döngüsü: " + manifest.id;
            for (int i = 0; i < manifest.dependencies.Count; i++)
            {
                string dependency = manifest.dependencies[i];
                if (string.IsNullOrWhiteSpace(dependency)) continue;
                ModManifest found = loadedManifests.FirstOrDefault(m => string.Equals(m.id, dependency, StringComparison.OrdinalIgnoreCase));
                if (found == null) return "Eksik bağımlılık: " + dependency;
                if (!found.enabled) return "Kapalı bağımlılık: " + dependency;
                string nested = GetDependencyError(found, new HashSet<string>(visiting, StringComparer.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(nested)) return nested;
            }
            return null;
        }

        public string GetModStatus(ModManifest manifest)
        {
            if (manifest == null) return "Bilinmiyor";
            if (!manifest.enabled) return "Kapalı";
            string dependencyError = GetDependencyError(manifest);
            if (!string.IsNullOrEmpty(dependencyError)) return "Bağımlılık hatası";
            if (!AllowDllMods && !string.IsNullOrWhiteSpace(manifest.entryScript)) return "DLL devre dışı";
            if (!string.IsNullOrWhiteSpace(manifest.entryScript))
            {
                string dir = GetModDirectory(manifest.id);
                string path = string.IsNullOrEmpty(dir) ? null : Path.Combine(dir, manifest.entryScript.Replace('/', Path.DirectorySeparatorChar));
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) return "DLL bulunamadı";
                if (ModScriptLoader.IsIL2CPP) return "IL2CPP desteklenmiyor";
                if (ModScriptLoader.IsLoaded(manifest.id)) return "Yüklendi";
                for (int i = diagnostics.Count - 1; i >= 0; i--)
                    if (string.Equals(diagnostics[i].modId, manifest.id, StringComparison.OrdinalIgnoreCase) && diagnostics[i].error) return "Yükleme hatası";
                return "Hazır";
            }
            return "Hazır";
        }

        public bool SetExternalModEnabled(string modId, bool enabled)
        {
            if (IsBuiltIn(modId)) return false;
            ModManifest manifest = loadedManifests.FirstOrDefault(m => string.Equals(m.id, modId, StringComparison.OrdinalIgnoreCase));
            string path = GetManifestPath(modId);
            if (manifest == null || string.IsNullOrEmpty(path)) return false;
            manifest.enabled = enabled;
            if (!WriteManifest(path, manifest)) return false;
            RescanMods();
            return true;
        }

        public bool SetModLoadOrder(string modId, int loadOrder)
        {
            if (IsBuiltIn(modId)) return false;
            ModManifest manifest = loadedManifests.FirstOrDefault(m => string.Equals(m.id, modId, StringComparison.OrdinalIgnoreCase));
            string path = GetManifestPath(modId);
            if (manifest == null || string.IsNullOrEmpty(path)) return false;
            manifest.loadOrder = Mathf.Clamp(loadOrder, -10000, 10000);
            if (!WriteManifest(path, manifest)) return false;
            RescanMods();
            return true;
        }

        public bool DeleteExternalMod(string modId, out string message)
        {
            message = "";
            if (IsBuiltIn(modId)) { message = "Yerleşik modlar silinemez."; return false; }
            string source = GetModDirectory(modId);
            if (string.IsNullOrEmpty(source)) { message = "Mod klasörü bulunamadı."; return false; }
            try
            {
                ModScriptLoader.UnloadAll(this);
                string trash = Path.Combine(ModsDirectory, ".trash");
                Directory.CreateDirectory(trash);
                Directory.Move(source, Path.Combine(trash, modId + "_" + DateTime.UtcNow.Ticks));
                ModSettingsStore.Delete(modId);
                RescanMods();
                message = "Mod geri dönüşüm klasörüne taşındı.";
                return true;
            }
            catch (Exception ex) { message = "Mod silinemedi: " + ex.Message; return false; }
        }

        private string GetManifestPath(string modId)
        {
            if (string.IsNullOrWhiteSpace(ModsDirectory) || string.IsNullOrWhiteSpace(modId)) return null;
            string[] files = Directory.GetFiles(ModsDirectory, "manifest.json", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                if (IsImportArtifact(files[i])) continue;
                try
                {
                    ModManifest item = JsonUtility.FromJson<ModManifest>(File.ReadAllText(files[i]));
                    if (item != null && string.Equals(item.id, modId, StringComparison.OrdinalIgnoreCase)) return files[i];
                }
                catch { }
            }
            return null;
        }

        private bool WriteManifest(string path, ModManifest manifest)
        {
            try
            {
                string temp = path + ".tmp";
                string backup = path + ".bak";
                File.WriteAllText(temp, JsonUtility.ToJson(manifest, true));
                if (File.Exists(path))
                {
                    try { File.Replace(temp, path, backup, true); }
                    catch { File.Copy(path, backup, true); File.Delete(path); File.Move(temp, path); }
                }
                else File.Move(temp, path);
                return true;
            }
            catch (Exception ex) { ReportDiagnostic(manifest == null ? "" : manifest.id, "Manifest yazılamadı: " + ex.Message, true); return false; }
        }

        public bool RegisterModule(IModModule module)
        {
            if (module == null || string.IsNullOrWhiteSpace(module.Id)) return false;
            if (modules.Any(m => string.Equals(m.Id, module.Id, StringComparison.OrdinalIgnoreCase)))
            {
                Debug.LogWarning("[ModRuntime] Aynı ID zaten yüklü, mod atlandı: " + module.Id);
                return false;
            }
            modules.Add(module);
            try { module.Register(this); return true; }
            catch (Exception ex)
            {
                Debug.LogError("[ModRuntime] " + module.Id + " kaydedilemedi: " + ex);
                CleanupModuleOwner(module.Id); modules.Remove(module); return false;
            }
        }

        public void UnregisterModule(IModModule module)
        {
            if (module == null || !modules.Contains(module)) return;
            try { module.Unregister(this); } catch (Exception ex) { Debug.LogException(ex); }
            CleanupModuleOwner(module.Id); modules.Remove(module);
        }

        private void CleanupModuleOwner(string owner)
        {
            ModEventBus.UnsubscribeOwner(owner);
            ModContentOverride.RemoveOwner(owner);
            ModContentRegistry.RemoveOwner(owner);
        }

        public void SetState(string modId, string stateJson, int schemaVersion = 1)
        {
            if (string.IsNullOrWhiteSpace(modId)) return;
            ModSaveEntry entry = states.FirstOrDefault(s => s.modId == modId);
            if (entry == null)
            {
                entry = new ModSaveEntry { modId = modId };
                states.Add(entry);
            }
            entry.schemaVersion = schemaVersion;
            entry.stateJson = string.IsNullOrEmpty(stateJson) ? "{}" : stateJson;
        }

        public string GetState(string modId)
        {
            ModSaveEntry entry = states.FirstOrDefault(s => s.modId == modId);
            return entry == null ? "{}" : entry.stateJson;
        }

        public void ApplySave(SaveData data)
        {
            states.Clear();
            if (data == null || data.modStates == null) return;
            foreach (ModSaveEntry entry in data.modStates)
                if (entry != null && !string.IsNullOrWhiteSpace(entry.modId)) states.Add(entry);
            foreach (IModModule module in modules)
            {
                IModSaveAware aware = module as IModSaveAware;
                if (aware == null) continue;
                try { aware.OnSaveLoaded(this); } catch (Exception ex) { Debug.LogException(ex); }
            }
        }

        public void WriteToSave(SaveData data)
        {
            if (data == null) return;
            foreach (IModModule module in modules)
            {
                IModSaveAware aware = module as IModSaveAware;
                if (aware == null) continue;
                try { aware.OnBeforeSave(this); } catch (Exception ex) { Debug.LogException(ex); }
            }
            data.modStates = new List<ModSaveEntry>(states);
        }

        public void RegisterBuiltInModules(IEnumerable<IModModule> builtIns)
        {
            if (builtIns == null) return;
            foreach (IModModule module in builtIns) RegisterModule(module);
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            for (int i = modules.Count - 1; i >= 0; i--)
            {
                try { modules[i].Unregister(this); } catch (Exception ex) { Debug.LogException(ex); }
            }
            ModEventBus.ClearAll();
            Instance = null;
        }
    }
}
