using Madness.Mods;
using UnityEngine;

// Bu dosyayı kendi Class Library projenizde derleyip Mod.dll olarak mod klasörüne koyun.
public class MyMod : ModModuleBase
{
    public const float SalePriceMultiplier = 29.0f;
    public override string Id => "my_csharp_mod";

    protected override void OnRegister(ModRuntime runtime)
    {
        float salePriceMultiplier = ModAPI.GetSettingFloat(Id, "salePriceMultiplier", SalePriceMultiplier);
        // Oyuncu %40 daha az hasar alır
        Subscribe<DamageContext>(ctx =>
        {
            if (ctx.Target is PlayerStats)
                ctx.Amount *= 0.6f;
        });

        // Satışlar %20 daha karlı
        Subscribe<SaleContext>(ctx =>
        {
            ctx.Price = Mathf.RoundToInt(ctx.Price * salePriceMultiplier);
        });

        Log("C# mod yüklendi — hasar ve satış kancaları aktif");
    }
}
