// CharacterUpgradeManager.cs — Can / Hasar / Hiz yukseltmeleri
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System;

public class CharacterUpgradeManager : MonoBehaviour
{
    public static CharacterUpgradeManager Instance { get; private set; }

    [Header("Maliyet egrisi (playtest ile ayarlanir)")]
    [SerializeField] private int baseCost = 200;
    [SerializeField] private float costExponent = 1.5f;
    [SerializeField] private int maxLevel = 10;

    [Header("Seviye basina kazanim")]
    [SerializeField] private int healthPerLevel = 2;
    [SerializeField] private float damagePerLevel = 0.3f;
    [SerializeField] private float speedPerLevel = 0.1f;

    public int HealthLevel { get; private set; }
    public int DamageLevel { get; private set; }
    public int SpeedLevel { get; private set; }

    public event Action OnUpgraded;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public int GetLevel(StatType stat)
    {
        switch (stat)
        {
            case StatType.Health: return HealthLevel;
            case StatType.Damage: return DamageLevel;
            case StatType.Speed: return SpeedLevel;
            default: return 0;
        }
    }

    public bool IsMaxed(StatType stat)
    {
        return GetLevel(stat) >= maxLevel;
    }

    /// <summary>Bir sonraki seviyenin maliyeti, guncel EKONOMİ ENFLASYONU carpani uygulanmis
    /// olarak. UI de, TryUpgrade da AYNI bu metodu cagirir — oyuncu ekranda gordugunden
    /// farkli para asla odemez.</summary>
    public int GetCost(StatType stat)
    {
        int level = GetLevel(stat);
        if (level >= maxLevel) return -1;

        int ham = Mathf.RoundToInt(baseCost * Mathf.Pow(level + 1, costExponent));
        if (SatinAlmaEnflasyonYoneticisi.Instance != null)
            ham = SatinAlmaEnflasyonYoneticisi.Instance.UygulaVergi(ham);
        return ham;
    }

    /// <summary>UpgradePanelUI'daki butondan cagrilir.</summary>
    public bool TryUpgrade(StatType stat)
    {
        if (IsMaxed(stat))
        {
            if (ToastNotification.Instance != null) ToastNotification.Instance.Show("Bu stat zaten maksimum seviyede.", 2.5f);
            return false;
        }

        int cost = GetCost(stat);
        if (!EconomyManager.Instance.TrySpendGold(cost))
        {
            if (ToastNotification.Instance != null) ToastNotification.Instance.Show("Yeterli paran yok.", 2.5f);
            return false;
        }

        Apply(stat);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPurchase();

        // EKONOMİ SIKILAŞTIRMA: bu buyuk satin alma bir sonrakini pahalilastirir.
        if (SatinAlmaEnflasyonYoneticisi.Instance != null)
            SatinAlmaEnflasyonYoneticisi.Instance.SatinAlmaKaydet();

        PlayerProgressStats.AddUpgradePurchased();
        if (OnUpgraded != null) OnUpgraded();
        return true;
    }

    private void Apply(StatType stat)
    {
        switch (stat)
        {
            case StatType.Health:
                HealthLevel++;
                PlayerStats.Instance.IncreaseMaxHealth(healthPerLevel);
                break;

            case StatType.Damage:
                DamageLevel++;
                PlayerStats.Instance.IncreaseDamage(damagePerLevel);
                break;

            case StatType.Speed:
                SpeedLevel++;
                PlayerStats.Instance.IncreaseSpeed(speedPerLevel);
                break;
        }
    }

    // ── Kayit koprusu ──

    public void ApplySave(SaveData data)
    {
        HealthLevel = data.healthLevel;
        DamageLevel = data.damageLevel;
        SpeedLevel = data.speedLevel;
        // Stat degerlerinin kendisi PlayerStats.ApplySave'de yuklenir
    }

    public void WriteToSave(SaveData data)
    {
        data.healthLevel = HealthLevel;
        data.damageLevel = DamageLevel;
        data.speedLevel = SpeedLevel;
    }
}
