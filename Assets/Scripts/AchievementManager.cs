// AchievementManager.cs — Basarim takibi ve odul dagitimi
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System.Collections.Generic;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    [Header("Tum basarimlar")]
    [SerializeField] private List<AchievementData> allAchievements = new List<AchievementData>();

    [Header("Kontrol sikligi")]
    [SerializeField] private float checkInterval = 1f;

    private readonly HashSet<string> unlocked = new HashSet<string>();
    private float timer;

    public List<AchievementData> AllAchievements { get { return allAchievements; } }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Resources/Achievements klasorundeki TUM basarimlar otomatik yuklenir — elle
        // surukleme geregi kalmaz. Inspector'dan elle eklediklerin varsa, TEKRARLANMADAN
        // (ayni achievementId iki kez eklenmez) bunlarin ustune eklenir.
        AchievementData[] otomatikYuklenen = Resources.LoadAll<AchievementData>("Achievements");
        if (otomatikYuklenen != null && otomatikYuklenen.Length > 0)
        {
            HashSet<string> mevcutIdler = new HashSet<string>();
            for (int i = 0; i < allAchievements.Count; i++)
                if (allAchievements[i] != null) mevcutIdler.Add(allAchievements[i].achievementId);

            for (int i = 0; i < otomatikYuklenen.Length; i++)
            {
                if (otomatikYuklenen[i] != null && !mevcutIdler.Contains(otomatikYuklenen[i].achievementId))
                {
                    allAchievements.Add(otomatikYuklenen[i]);
                    mevcutIdler.Add(otomatikYuklenen[i].achievementId);
                }
            }
        }
    }

    private void Update()
    {
        // Her karede degil, saniyede bir kontrol — mobilde gereksiz yuk olmasin
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = checkInterval;

        CheckAll();
    }

    private void CheckAll()
    {
        for (int i = 0; i < allAchievements.Count; i++)
        {
            AchievementData a = allAchievements[i];
            if (a == null) continue;
            if (unlocked.Contains(a.achievementId)) continue;

            if (PlayerProgressStats.GetStatValue(a.trackedStat) >= a.targetValue)
                Unlock(a);
        }
    }

    public bool IsUnlocked(AchievementData a)
    {
        return a != null && unlocked.Contains(a.achievementId);
    }

    public int GetProgress(AchievementData a)
    {
        return a == null ? 0 : PlayerProgressStats.GetStatValue(a.trackedStat);
    }

    [Header("Tum Basarimlar Tamamlaninca (Meta Odul)")]
    [SerializeField] private int metaGoldReward = 5000;
    private bool metaTamamlandi = false;
    public bool MetaTamamlandiMi { get { return metaTamamlandi; } }

    private void Unlock(AchievementData a)
    {
        unlocked.Add(a.achievementId);
        if (EconomyManager.Instance != null) EconomyManager.Instance.AddGold(a.goldReward);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayAchievement();

        // Buyuk kutlama ekrani varsa onu kullan (daha carpici); yoksa eski Toast'a duser —
        // boylece kutlama paneli kurulmadiysa bile hicbir sey bozulmaz.
        if (AchievementCelebrationUI.Instance != null) AchievementCelebrationUI.Instance.Goster(a);
        else if (ToastNotification.Instance != null) ToastNotification.Instance.Show("Basarim Acildi: " + a.title, 5f);

        // META ODUL: TUM basarimlar (gizli olanlar dahil) tamamlandiysa, bir kez tetiklenir.
        if (!metaTamamlandi && TumBasarimlarTamamlandiMi())
        {
            metaTamamlandi = true;
            if (EconomyManager.Instance != null) EconomyManager.Instance.AddGold(metaGoldReward);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAchievement();

            if (AchievementCelebrationUI.Instance != null) AchievementCelebrationUI.Instance.GosterMeta(metaGoldReward);
            else if (ToastNotification.Instance != null) ToastNotification.Instance.Show("★ TÜM BAŞARIMLAR TAMAMLANDI! ★", 6f);
        }
    }

    private bool TumBasarimlarTamamlandiMi()
    {
        for (int i = 0; i < allAchievements.Count; i++)
        {
            if (allAchievements[i] == null) continue;
            if (!unlocked.Contains(allAchievements[i].achievementId)) return false;
        }
        return allAchievements.Count > 0;
    }

    // ── Kayit koprusu ──

    public void ApplySave(SaveData data)
    {
        unlocked.Clear();
        for (int i = 0; i < data.unlockedAchievementIds.Count; i++)
            unlocked.Add(data.unlockedAchievementIds[i]);

        metaTamamlandi = data.metaBasarimTamamlandi;
    }

    public void WriteToSave(SaveData data)
    {
        data.unlockedAchievementIds.Clear();
        foreach (string id in unlocked) data.unlockedAchievementIds.Add(id);

        data.metaBasarimTamamlandi = metaTamamlandi;
    }
}