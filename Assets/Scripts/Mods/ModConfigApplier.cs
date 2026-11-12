using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Madness.Mods
{
    /// <summary>
    /// Kod yazmadan basit mekanik değişiklikleri için config.json desteği.
    /// Örnek: { "nightPoolMultiplier": 2, "goldGainMultiplier": 1.5, "playerDamageTakenMultiplier": 0.5 }
    /// </summary>
    public static class ModConfigApplier
    {
        private static readonly HashSet<string> appliedOwners = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        [Serializable]
        private class SimpleConfig
        {
            public float nightPoolMultiplier = 1f;
            public float spawnIntervalMultiplier = 1f;
            public float goldGainMultiplier = 1f;
            public float goldSpendMultiplier = 1f;
            public float playerDamageTakenMultiplier = 1f;
            public float enemyDamageTakenMultiplier = 1f;
            public float enemyHealthMultiplier = 1f;
            public float enemyDamageMultiplier = 1f;
            public float bossHealthMultiplier = 1f;
            public float bossDamageMultiplier = 1f;
            public float dayDurationMultiplier = 1f;
            public bool disableEnemySpawns = false;
            public bool disableCustomerSpawns = false;
            public bool disableSales = false;
            public bool disableWorldLoads = false;
        }

        public static void ApplyAll(ModRuntime runtime)
        {
            if (runtime == null) return;
            foreach (string owner in appliedOwners) ModEventBus.UnsubscribeOwner(owner);
            appliedOwners.Clear();
            foreach (var manifest in runtime.LoadedManifests)
            {
                if (!manifest.enabled) continue;
                if (!string.IsNullOrEmpty(runtime.GetDependencyError(manifest))) continue;
                string path = Path.Combine(runtime.ModsDirectory, manifest.id, "config.json");
                bool hasManifestSettings = manifest.settings != null && manifest.settings.Count > 0;
                if (!File.Exists(path) && !hasManifestSettings) continue;
                try
                {
                    SimpleConfig cfg = File.Exists(path)
                        ? JsonUtility.FromJson<SimpleConfig>(File.ReadAllText(path))
                        : new SimpleConfig();
                    if (cfg == null) continue;
                    ApplySavedSettings(manifest.id, cfg);
                    string configOwner = manifest.id + "__config";
                    RegisterConfigHooks(configOwner, cfg);
                    appliedOwners.Add(configOwner);
                    Debug.Log("[ModConfig] " + manifest.id + " config uygulandı.");
                }
                catch (Exception ex)
                {
                    Debug.LogError("[ModConfig] " + manifest.id + " config okunamadı: " + ex.Message);
                }
            }
        }

        private static void ApplySavedSettings(string modId, SimpleConfig cfg)
        {
            cfg.nightPoolMultiplier = ModSettingsStore.GetFloat(modId, "nightPoolMultiplier", cfg.nightPoolMultiplier);
            cfg.spawnIntervalMultiplier = ModSettingsStore.GetFloat(modId, "spawnIntervalMultiplier", cfg.spawnIntervalMultiplier);
            cfg.goldGainMultiplier = ModSettingsStore.GetFloat(modId, "goldGainMultiplier", cfg.goldGainMultiplier);
            cfg.goldSpendMultiplier = ModSettingsStore.GetFloat(modId, "goldSpendMultiplier", cfg.goldSpendMultiplier);
            cfg.playerDamageTakenMultiplier = ModSettingsStore.GetFloat(modId, "playerDamageTakenMultiplier", cfg.playerDamageTakenMultiplier);
            cfg.enemyDamageTakenMultiplier = ModSettingsStore.GetFloat(modId, "enemyDamageTakenMultiplier", cfg.enemyDamageTakenMultiplier);
            cfg.enemyHealthMultiplier = ModSettingsStore.GetFloat(modId, "enemyHealthMultiplier", cfg.enemyHealthMultiplier);
            cfg.enemyDamageMultiplier = ModSettingsStore.GetFloat(modId, "enemyDamageMultiplier", cfg.enemyDamageMultiplier);
            cfg.bossHealthMultiplier = ModSettingsStore.GetFloat(modId, "bossHealthMultiplier", cfg.bossHealthMultiplier);
            cfg.bossDamageMultiplier = ModSettingsStore.GetFloat(modId, "bossDamageMultiplier", cfg.bossDamageMultiplier);
            cfg.dayDurationMultiplier = ModSettingsStore.GetFloat(modId, "dayDurationMultiplier", cfg.dayDurationMultiplier);
            cfg.disableEnemySpawns = ModSettingsStore.GetBool(modId, "disableEnemySpawns", cfg.disableEnemySpawns);
            cfg.disableCustomerSpawns = ModSettingsStore.GetBool(modId, "disableCustomerSpawns", cfg.disableCustomerSpawns);
            cfg.disableSales = ModSettingsStore.GetBool(modId, "disableSales", cfg.disableSales);
            cfg.disableWorldLoads = ModSettingsStore.GetBool(modId, "disableWorldLoads", cfg.disableWorldLoads);
        }

        private static void RegisterConfigHooks(string owner, SimpleConfig cfg)
        {
            if (Math.Abs(cfg.nightPoolMultiplier - 1f) > 0.001f || Math.Abs(cfg.spawnIntervalMultiplier - 1f) > 0.001f)
            {
                ModEventBus.Subscribe<NightContext>(ctx =>
                {
                    ctx.PoolMultiplier *= cfg.nightPoolMultiplier;
                    ctx.SpawnIntervalMultiplier *= cfg.spawnIntervalMultiplier;
                }, 0, owner);
            }

            if (Math.Abs(cfg.goldGainMultiplier - 1f) > 0.001f || Math.Abs(cfg.goldSpendMultiplier - 1f) > 0.001f)
            {
                ModEventBus.Subscribe<GoldContext>(ctx =>
                {
                    if (ctx.Reason == "add") ctx.Amount = Mathf.RoundToInt(ctx.Amount * cfg.goldGainMultiplier);
                    else if (ctx.Reason == "spend") ctx.Amount = Mathf.RoundToInt(ctx.Amount * cfg.goldSpendMultiplier);
                }, 0, owner);
            }

            if (Math.Abs(cfg.playerDamageTakenMultiplier - 1f) > 0.001f || Math.Abs(cfg.enemyDamageTakenMultiplier - 1f) > 0.001f)
            {
                ModEventBus.Subscribe<DamageContext>(ctx =>
                {
                    if (ctx.Target is PlayerStats)
                        ctx.Amount *= cfg.playerDamageTakenMultiplier;
                    else if (ctx.Target is EnemyController || ctx.Target is BossController)
                        ctx.Amount *= cfg.enemyDamageTakenMultiplier;
                }, 0, owner);
            }

            if (Math.Abs(cfg.enemyHealthMultiplier - 1f) > 0.001f || Math.Abs(cfg.enemyDamageMultiplier - 1f) > 0.001f)
            {
                ModEventBus.Subscribe<EnemySpawnContext>(ctx =>
                {
                    ctx.HealthMultiplier *= cfg.enemyHealthMultiplier;
                    ctx.DamageMultiplier *= cfg.enemyDamageMultiplier;
                }, 0, owner);
            }

            if (Math.Abs(cfg.bossHealthMultiplier - 1f) > 0.001f || Math.Abs(cfg.bossDamageMultiplier - 1f) > 0.001f)
            {
                ModEventBus.Subscribe<BossSpawnContext>(ctx =>
                {
                    ctx.HealthMultiplier *= cfg.bossHealthMultiplier;
                    ctx.DamageMultiplier *= cfg.bossDamageMultiplier;
                }, 0, owner);
            }

            if (Math.Abs(cfg.dayDurationMultiplier - 1f) > 0.001f)
            {
                ModEventBus.Subscribe<DayContext>(ctx =>
                {
                    ctx.DurationSeconds *= cfg.dayDurationMultiplier;
                }, 0, owner);
            }

            if (cfg.disableEnemySpawns)
                ModEventBus.Subscribe<EnemySpawnContext>(ctx => ctx.Cancelled = true, 100, owner);
            if (cfg.disableEnemySpawns)
                ModEventBus.Subscribe<BossSpawnContext>(ctx => ctx.Cancelled = true, 100, owner);
            if (cfg.disableCustomerSpawns)
                ModEventBus.Subscribe<CustomerSpawnContext>(ctx => ctx.Cancelled = true, 100, owner);
            if (cfg.disableSales)
                ModEventBus.Subscribe<SaleContext>(ctx => ctx.Cancelled = true, 100, owner);
            if (cfg.disableWorldLoads)
                ModEventBus.Subscribe<WorldLoadContext>(ctx => ctx.CancelLoad = true, 100, owner);
        }
    }
}
