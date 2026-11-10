using Madness.Mods;
using UnityEngine;

// Bu dosyayı kendi Class Library projenizde derleyip Mod.dll olarak mod klasörüne koyun.
public class MyMod : ModModuleBase
{
    public override string Id => "my_csharp_mod";

    protected override void OnRegister(ModRuntime runtime)
    {
        // Oyuncu %40 daha az hasar alır
        Subscribe<DamageContext>(ctx =>
        {
            if (ctx.Target is PlayerStats)
                ctx.Amount *= 0.6f;
        });

        // Satışlar %20 daha karlı
        Subscribe<SaleContext>(ctx =>
        {
            ctx.Price = Mathf.RoundToInt(ctx.Price * 1.2f);
        });

        Log("C# mod yüklendi — hasar ve satış kancaları aktif");
    }
}
