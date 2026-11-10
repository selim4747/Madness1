using UnityEngine;

namespace Madness.Mods
{
    /// <summary>
    /// IL2CPP derlemede bir kez uyarı basar. Oyun çökmez.
    /// </summary>
    public static class ModIL2CPPNotice
    {
        private static bool shown;

        public static void ShowOnce()
        {
            if (shown) return;
            shown = true;
#if ENABLE_IL2CPP
            Debug.LogWarning(
                "[Mods] IL2CPP modu aktif.\n" +
                "• config.json modları çalışır\n" +
                "• AssetBundle (sahne/prefab/diyalog) çalışır\n" +
                "• totalConversion çalışır\n" +
                "• Dinamik C# DLL yükleme KAPALI (Mono gerekir)\n" +
                "Oyun normal şekilde devam eder.");
#endif
        }
    }
}
