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
        public string ModsDirectory { get; private set; }
        public IReadOnlyList<ModManifest> LoadedManifests { get { return loadedManifests; } }
        public event Action OnModsChanged;

        private readonly List<ModManifest> loadedManifests = new List<ModManifest>();
        private readonly List<IModModule> modules = new List<IModModule>();
        private readonly List<ModSaveEntry> states = new List<ModSaveEntry>();
        private readonly List<IModModule> builtInModules = new List<IModModule>();
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
            string[] files = Directory.GetFiles(ModsDirectory, "manifest.json", SearchOption.AllDirectories)
                .Where(file => !IsImportArtifact(file)).ToArray();
            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    ModManifest manifest = JsonUtility.FromJson<ModManifest>(File.ReadAllText(files[i]));
                    if (manifest == null || string.IsNullOrWhiteSpace(manifest.id))
                    {
                        Debug.LogWarning("[ModRuntime] Kimliksiz manifest atlandı: " + files[i]);
                        continue;
                    }
                    if (manifest.apiVersion != CurrentApiVersion.ToString())
                    {
                        Debug.LogWarning("[ModRuntime] API uyumsuzluğu nedeniyle atlandı: " + manifest.id);
                        continue;
                    }
                    if (!loadedManifests.Any(m => string.Equals(m.id, manifest.id, StringComparison.OrdinalIgnoreCase)))
                        loadedManifests.Add(manifest);
                }
                catch (Exception ex)
                {
                    Debug.LogError("[ModRuntime] Manifest okunamadı: " + files[i] + " - " + ex.Message);
                }
            }
            loadedManifests.Sort((a, b) => a.loadOrder.CompareTo(b.loadOrder));
        }

        private bool IsImportArtifact(string file)
        {
            string normalized = file.Replace('\\', '/');
            return normalized.Contains("/.staging/") || normalized.Contains("/.backup/");
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

            if (AllowDllMods)
            {
                try { ModScriptLoader.LoadAll(this); }
                catch (Exception ex) { Debug.LogError("[ModRuntime] DLL modları yüklenemedi: " + ex); }
            }

            if (OnModsChanged != null) OnModsChanged();
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
