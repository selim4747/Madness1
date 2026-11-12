using Madness.Mods;
using UnityEngine;

namespace MadnessExampleMods
{
    /// <summary>
    /// ExampleMods klasöründeki modları yerleşik kataloğa kaydeder.
    /// Hepsi varsayılan KAPALI başlar; oyuncu Mod panelinden açar.
    /// </summary>
    public static class ExampleBuiltInMods
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            TryRegister("harder_nights", "Harder Nights", "1.0.0", () => new HarderNightsMod());
            TryRegister("my_csharp_mod", "My C# Mod", "1.0.0", () => new MyCSharpMod());
            TryRegister("full_power_example", "Full Power Example", "1.0.0", () => new FullPowerExampleMod());
        }

        // Bir kaydın hatası diğerlerini ve oyunu etkilemez.
        private static void TryRegister(string id, string name, string version, System.Func<IModModule> factory)
        {
            try { BuiltInModCatalog.Register(id, name, version, factory); }
            catch (System.Exception ex) { Debug.LogError("[ExampleBuiltInMods] " + id + " kaydedilemedi: " + ex.Message); }
        }
    }

    /// <summary>ExampleMods/harder_nights/config.json değerlerinin kod karşılığı.</summary>
    public sealed class HarderNightsMod : ModModuleBase
    {
        public override string Id { get { return "harder_nights"; } }

        protected override void OnRegister(ModRuntime runtime)
        {
            Subscribe<NightContext>(ctx =>
            {
                ctx.PoolMultiplier *= 1.5f;
                ctx.SpawnIntervalMultiplier *= 0.8f;
            });
            Subscribe<EnemySpawnContext>(ctx =>
            {
                ctx.HealthMultiplier *= 1.25f;
                ctx.DamageMultiplier *= 1.1f;
            });
        }
    }

    /// <summary>ExampleMods/csharp_example/MyMod.cs</summary>
    public sealed class MyCSharpMod : ModModuleBase
    {
        public const float SalePriceMultiplier = 29.0f;
        public override string Id { get { return "my_csharp_mod"; } }

        protected override void OnRegister(ModRuntime runtime)
        {
            Subscribe<DamageContext>(ctx =>
            {
                if (ctx.Target is PlayerStats) ctx.Amount *= 0.6f;
            });
            Subscribe<SaleContext>(ctx =>
            {
                ctx.Price = Mathf.RoundToInt(ctx.Price * MyCSharpMod.SalePriceMultiplier);
            });
            Log("C# mod yüklendi — hasar ve satış kancaları aktif");
        }
    }

    /// <summary>ExampleMods/full_power_example/FullPowerMod.cs</summary>
    public sealed class FullPowerExampleMod : ModModuleBase
    {
        public override string Id { get { return "full_power_example"; } }

        protected override void OnRegister(ModRuntime runtime)
        {
            Subscribe<DamageContext>(ctx =>
            {
                if (ctx.Target is PlayerStats) ctx.Amount *= 0.3f;
            });
            Subscribe<SaleContext>(ctx =>
            {
                ctx.Price = Mathf.RoundToInt(ctx.Price * 2f);
            });
            Subscribe<NightContext>(ctx =>
            {
                ctx.PoolMultiplier = 3f;
                ctx.SpawnIntervalMultiplier = 0.4f;
            });
            SetState("{\"started\":true}");
            Log("Tam yetkili örnek mod yüklendi.");
        }
    }
}
