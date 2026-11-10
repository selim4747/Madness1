using UnityEngine;

namespace Madness.Mods
{
    /// <summary>
    /// totalConversion = true olan mod varsa oyun başlangıcında onun sahnesini yükler.
    /// Böylece mod tamamen yeni bir oyun gibi davranabilir.
    /// </summary>
    public static class ModTotalConversion
    {
        public static bool TryStart()
        {
            var runtime = ModRuntime.EnsureExists();
            foreach (var m in runtime.LoadedManifests)
            {
                if (!m.enabled || !m.totalConversion) continue;
                if (string.IsNullOrWhiteSpace(m.mainScene) && string.IsNullOrWhiteSpace(m.worldScene))
                    continue;

                string scene = !string.IsNullOrWhiteSpace(m.mainScene) ? m.mainScene : m.worldScene;
                Debug.Log("[TotalConversion] " + m.id + " başlatılıyor → " + scene);
                return ModSceneLoader.LoadFromMod(m.id, scene, false);
            }
            return false;
        }

        public static ModManifest GetActive()
        {
            var runtime = ModRuntime.EnsureExists();
            foreach (var m in runtime.LoadedManifests)
                if (m.enabled && m.totalConversion) return m;
            return null;
        }
    }
}
