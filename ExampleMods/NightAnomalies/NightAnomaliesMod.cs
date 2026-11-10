using System;
using Madness.Mods;
using UnityEngine;

namespace MadnessExampleMods
{
    /// <summary>
    /// Mono/itch.io example mod: random night modifiers, elite enemies, a risk contract,
    /// and a paid Shockwave ability. No external art assets are required.
    /// </summary>
    public sealed class NightAnomaliesMod : ModModuleBase
    {
        public override string Id { get { return "night_anomalies"; } }

        private Anomaly anomaly;
        private int contractNights;
        private bool contractActiveThisNight;
        private int elitesThisNight;
        private float shockwaveReadyAt;
        private NightAnomaliesOverlay overlay;

        private enum Anomaly { None, BloodMoon, GoldenHunt, Eclipse }

        protected override void OnRegister(ModRuntime runtime)
        {
            Subscribe<NightContext>(OnNightStarting, priority: 20);
            Subscribe<EnemySpawnContext>(OnEnemySpawning, priority: 20);
            Subscribe<EnemyKilledContext>(OnEnemyKilled, priority: 20);
            Subscribe<DamageContext>(OnDamage, priority: 20);

            GameObject host = new GameObject("Mod_NightAnomalies_UI");
            UnityEngine.Object.DontDestroyOnLoad(host);
            overlay = host.AddComponent<NightAnomaliesOverlay>();
            overlay.Initialize(this);
            Log("Night Anomalies aktif. Gündüz risk kontratı, gece şok dalgası kullanılabilir.");
        }

        protected override void OnUnregister(ModRuntime runtime)
        {
            if (overlay != null) UnityEngine.Object.Destroy(overlay.gameObject);
            overlay = null;
        }

        private void OnNightStarting(NightContext ctx)
        {
            elitesThisNight = 0;
            anomaly = (Anomaly)UnityEngine.Random.Range(1, 4);
            switch (anomaly)
            {
                case Anomaly.BloodMoon:
                    ctx.PoolMultiplier *= 1.2f;
                    ctx.SpawnIntervalMultiplier *= 0.9f;
                    Notify("KANLI AY: Daha kalabalık dalga, elit ihtimali yüksek.");
                    break;
                case Anomaly.GoldenHunt:
                    ctx.PoolMultiplier *= 0.9f;
                    Notify("ALTIN AVI: Daha az düşman; elit ödülleri iki kat.");
                    break;
                case Anomaly.Eclipse:
                    ctx.PoolMultiplier *= 1.1f;
                    ctx.SpawnIntervalMultiplier *= 1.12f;
                    Notify("TUTULMA: Düşmanlar sert vurur; elitler dayanıklı.");
                    break;
            }

            contractActiveThisNight = contractNights > 0;
            if (contractActiveThisNight)
            {
                ctx.PoolMultiplier *= 1.15f;
                contractNights--;
                Notify("KAN YEMİNİ etkin: hasar artışı ve ek risk.");
            }
        }

        private void OnEnemySpawning(EnemySpawnContext ctx)
        {
            float chance = 0.12f;
            if (anomaly == Anomaly.BloodMoon) chance += 0.12f;
            if (anomaly == Anomaly.GoldenHunt) chance += 0.08f;
            if (contractActiveThisNight) chance += 0.10f;
            if (elitesThisNight >= 5 || UnityEngine.Random.value >= chance) return;

            ctx.IsElite = true;
            ctx.HealthMultiplier *= anomaly == Anomaly.Eclipse ? 2.2f : 1.8f;
            ctx.DamageMultiplier *= anomaly == Anomaly.Eclipse ? 1.6f : 1.35f;
            elitesThisNight++;
        }

        private void OnEnemyKilled(EnemyKilledContext ctx)
        {
            if (!ctx.WasElite || EconomyManager.Instance == null) return;
            int reward = anomaly == Anomaly.GoldenHunt ? 60 : 30;
            if (contractActiveThisNight) reward *= 2;
            EconomyManager.Instance.AddGold(reward);
            Notify("Elit düşman yenildi: +" + reward + " altın");
        }

        private void OnDamage(DamageContext ctx)
        {
            if (contractActiveThisNight)
            {
                if (ctx.Target is PlayerStats) ctx.Amount *= 1.25f;
                else if (ctx.Target is EnemyController || ctx.Target is BossController) ctx.Amount *= 1.4f;
            }

            if (anomaly == Anomaly.BloodMoon && ctx.Target is PlayerStats) ctx.Amount *= 1.15f;
            if (anomaly == Anomaly.Eclipse && ctx.Target is PlayerStats) ctx.Amount *= 1.25f;
        }

        public void BuyBloodOath()
        {
            if (contractNights > 0)
            {
                Notify("Kan yemini zaten sırada.");
                return;
            }
            if (EconomyManager.Instance == null || !EconomyManager.Instance.TrySpendGold(150))
            {
                Notify("Kan yemini için 150 altın gerekli.");
                return;
            }
            contractNights = 2;
            Notify("Kan yemini alındı: 2 gece boyunca +%40 hasar, +%25 alınan hasar.");
        }

        public void UseShockwave()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentPhase != GamePhase.Night)
            {
                Notify("Şok dalgası yalnızca gece kullanılabilir.");
                return;
            }
            if (Time.time < shockwaveReadyAt)
            {
                Notify("Şok dalgası beklemede.");
                return;
            }
            PlayerStats player = PlayerStats.Instance;
            if (player == null || player.IsDead) return;
            if (EconomyManager.Instance == null || !EconomyManager.Instance.TrySpendGold(35))
            {
                Notify("Şok dalgası için 35 altın gerekli.");
                return;
            }

            shockwaveReadyAt = Time.time + 18f;
            float radius = 3.25f;
            float damage = player.DamageStat * 2.5f;
            Collider2D[] hits = Physics2D.OverlapCircleAll(player.transform.position, radius);
            int struck = 0;
            for (int i = 0; i < hits.Length; i++)
            {
                EnemyController enemy = hits[i].GetComponent<EnemyController>();
                if (enemy != null && enemy.IsAlive)
                {
                    enemy.TakeDamage(damage);
                    struck++;
                    continue;
                }
                BossController boss = hits[i].GetComponent<BossController>();
                if (boss != null && boss.IsBossActive)
                {
                    boss.TakeDamage(damage);
                    struck++;
                }
            }
            Notify("Şok dalgası: " + struck + " hedef, " + Mathf.RoundToInt(damage) + " hasar.");
        }

        public bool CanUseShockwave { get { return Time.time >= shockwaveReadyAt; } }
        public int ContractNights { get { return contractNights; } }

        private static void Notify(string message)
        {
            if (ToastNotification.Instance != null) ToastNotification.Instance.Show(message, 2.5f);
            else Debug.Log("[NightAnomalies] " + message);
        }
    }

    /// <summary>Built-in IMGUI controls keep this example self-contained and asset-free.</summary>
    public sealed class NightAnomaliesOverlay : MonoBehaviour
    {
        private NightAnomaliesMod mod;
        public void Initialize(NightAnomaliesMod owner) { mod = owner; }

        private void OnGUI()
        {
            if (mod == null || GameManager.Instance == null) return;
            float scale = Mathf.Clamp(Screen.width / 900f, 0.75f, 1.4f);
            Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            float left = 12f;
            float top = 12f;
            float width = 245f;
            if (GameManager.Instance.CurrentPhase == GamePhase.Day)
            {
                GUI.Box(new Rect(left, top, width, 76f), "NIGHT ANOMALIES");
                string oath = mod.ContractNights > 0 ? "Yemin sırada: " + mod.ContractNights + " gece" : "Kan Yemini — 150 altın";
                if (GUI.Button(new Rect(left + 10f, top + 30f, width - 20f, 34f), oath)) mod.BuyBloodOath();
            }
            else if (GameManager.Instance.CurrentPhase == GamePhase.Night)
            {
                GUI.Box(new Rect(left, top, width, 68f), "NIGHT ANOMALIES");
                string text = mod.CanUseShockwave ? "ŞOK DALGASI — 35 altın" : "Şok dalgası beklemede";
                if (GUI.Button(new Rect(left + 10f, top + 29f, width - 20f, 30f), text)) mod.UseShockwave();
            }
            GUI.matrix = old;
        }
    }
}
