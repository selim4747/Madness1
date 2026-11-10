using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Madness.Mods
{
    /// <summary>
    /// Mod giriş kodunu yükler.
    /// IL2CPP'de dinamik Assembly.Load çalışmaz — bu durumda sessizce atlanır, oyun çökmez.
    /// Mono derlemede DLL modları yüklenir.
    /// </summary>
    public static class ModScriptLoader
    {
        private static readonly Dictionary<string, IModModule> loadedModules =
            new Dictionary<string, IModModule>(StringComparer.OrdinalIgnoreCase);

        public static bool IsIL2CPP
        {
            get
            {
#if ENABLE_IL2CPP
                return true;
#else
                return false;
#endif
            }
        }

        public static void LoadAll(ModRuntime runtime)
        {
            if (runtime == null) return;
            UnloadAll(runtime);

            if (IsIL2CPP)
            {
                Debug.Log("[ModScriptLoader] IL2CPP tespit edildi. Dinamik DLL yükleme devre dışı. " +
                          "config.json ve AssetBundle modları çalışmaya devam eder.");
                return;
            }

            foreach (ModManifest manifest in runtime.LoadedManifests)
            {
                if (!manifest.enabled) continue;
                try
                {
                    LoadOne(runtime, manifest);
                }
                catch (Exception ex)
                {
                    Debug.LogError("[ModScriptLoader] " + manifest.id + " yüklenemedi: " + ex);
                }
            }
        }

        public static void UnloadAll(ModRuntime runtime)
        {
            if (runtime == null) return;
            foreach (var kv in loadedModules.ToArray())
            {
                try { runtime.UnregisterModule(kv.Value); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
            loadedModules.Clear();
        }

        private static void LoadOne(ModRuntime runtime, ModManifest manifest)
        {
            // IL2CPP guard (çift kontrol)
            if (IsIL2CPP) return;

            string modDir = Path.Combine(runtime.ModsDirectory, manifest.id);
            if (!Directory.Exists(modDir)) return;

            string dllPath = ResolveEntryPath(modDir, manifest.entryScript);
            if (string.IsNullOrEmpty(dllPath) || !File.Exists(dllPath))
                return;

            if (!dllPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning("[ModScriptLoader] Yalnızca .dll desteklenir: " + dllPath);
                return;
            }

#if !ENABLE_IL2CPP
            try
            {
                byte[] bytes = File.ReadAllBytes(dllPath);
                var asm = System.Reflection.Assembly.Load(bytes);
                if (asm == null) return;

                Type moduleType = FindModuleType(asm, manifest.id);
                if (moduleType == null)
                {
                    Debug.LogWarning("[ModScriptLoader] IModModule bulunamadı: " + manifest.id);
                    return;
                }

                IModModule module = Activator.CreateInstance(moduleType) as IModModule;
                if (module == null) return;

                if (runtime.RegisterModule(module))
                {
                    loadedModules[manifest.id] = module;
                    Debug.Log("[ModScriptLoader] Mod yüklendi: " + manifest.id);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[ModScriptLoader] DLL yüklenemedi (" + dllPath + "): " + ex.Message);
            }
#endif
        }

        private static string ResolveEntryPath(string modDir, string entryScript)
        {
            if (!string.IsNullOrWhiteSpace(entryScript))
            {
                string candidate = Path.Combine(modDir, entryScript.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(candidate)) return candidate;
            }
            string[] defaults = { "Mod.dll", "main.dll", "mod.dll" };
            foreach (string name in defaults)
            {
                string p = Path.Combine(modDir, name);
                if (File.Exists(p)) return p;
            }
            return null;
        }

#if !ENABLE_IL2CPP
        private static Type FindModuleType(System.Reflection.Assembly asm, string expectedId)
        {
            Type[] types;
            try { types = asm.GetTypes(); }
            catch (System.Reflection.ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null).ToArray();
            }

            Type best = null;
            foreach (Type t in types)
            {
                if (t == null || t.IsAbstract || t.IsInterface) continue;
                if (!typeof(IModModule).IsAssignableFrom(t)) continue;
                try
                {
                    var probe = Activator.CreateInstance(t) as IModModule;
                    if (probe != null && string.Equals(probe.Id, expectedId, StringComparison.OrdinalIgnoreCase))
                        return t;
                    if (best == null) best = t;
                }
                catch { }
            }
            return best;
        }
#endif
    }
}
