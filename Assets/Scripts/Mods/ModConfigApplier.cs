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
            public float dayDurationMultiplier = 1f;
        }

        public static void ApplyAll(ModRuntime runtime)
        {
            if (runtime == null) return;
            foreach (string owner in appliedOwners) ModEventBus.UnsubscribeOwner(owner);
            appliedOwners.Clear();
            foreach (var manifest in runtime.LoadedManifests)
            {
                if (!manifest.enabled) continue;
                string path = Path.Combine(runtime.ModsDirectory, manifest.id, "config.json");
                if (!File.Exists(path)) continue;
                try
                {
                    SimpleConfig cfg = JsonUtility.FromJson<SimpleConfig>(File.ReadAllText(path));
                    if (cfg == null) continue;
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

            if (Math.Abs(cfg.dayDurationMultiplier - 1f) > 0.001f)
            {
                ModEventBus.Subscribe<DayContext>(ctx =>
                {
                    ctx.DurationSeconds *= cfg.dayDurationMultiplier;
                }, 0, owner);
            }
        }
    }
}
