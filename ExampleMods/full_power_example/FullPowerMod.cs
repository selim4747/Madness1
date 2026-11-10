using Madness.Mods;
using UnityEngine;

/// <summary>
/// Mono derlemede çalışan tam yetkili örnek mod.
/// Yeni mekanik ekleyebilir, mevcut her şeyi değiştirebilir.
/// </summary>
public class FullPowerMod : ModModuleBase
{
    public override string Id => "full_power_example";

    protected override void OnRegister(ModRuntime runtime)
    {
        if (!ModAPI.CanRunArbitraryCode)
        {
            Log("IL2CPP tespit edildi — bu mod atlandı.");
            return;
        }

        // 1. Mevcut mekanikleri değiştir
        Subscribe<DamageContext>(ctx =>
        {
            if (ctx.Target is PlayerStats)
                ctx.Amount *= 0.3f; // oyuncu çok az hasar alır
        });

        Subscribe<SaleContext>(ctx =>
        {
            ctx.Price = Mathf.RoundToInt(ctx.Price * 2f); // satışlar 2 katı
        });

        Subscribe<NightContext>(ctx =>
        {
            ctx.PoolMultiplier = 3f;
            ctx.SpawnIntervalMultiplier = 0.4f;
        });

        // 2. Yeni içerik kaydet (AssetBundle'dan)
        var yeniPrefab = LoadAsset<GameObject>("YeniKarakter");
        if (yeniPrefab != null)
            RegisterContent("player_prefab", yeniPrefab);

        // 3. Kendi kaydını tut
        SetState("{\"started\":true}");

        Log("Tam yetkili mod yüklendi — tüm sınırlar açık.");
    }
}
