// PersonnelCapacityUpgrade.cs — Es zamanli musteri sinirini artirir
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System;

public class PersonnelCapacityUpgrade : MonoBehaviour
{
    public static PersonnelCapacityUpgrade Instance { get; private set; }

    [Header("Maliyetler (GDD'de kesin)")]
    [SerializeField] private int[] costs = { 300, 600, 1200, 2200, 4000 };

    [Header("Taban")]
    [SerializeField] private int baseSimultaneous = 2;

    public int Level { get; private set; }
    public int MaxLevel { get { return costs.Length; } }
    public bool IsMaxed { get { return Level >= costs.Length; } }

    public event Action OnUpgraded;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        ApplyToSpawner();
    }

    /// <summary>Bir sonraki seviyenin maliyeti (-1 = maksimum), guncel EKONOMİ ENFLASYONU
    /// carpani uygulanmis olarak. UI de, TryUpgrade da AYNI bu metodu cagirir — oyuncu
    /// ekranda gordugunden farkli para asla odemez.</summary>
    public int NextCost
    {
        get
        {
            if (IsMaxed) return -1;

            int ham = costs[Level];
            if (SatinAlmaEnflasyonYoneticisi.Instance != null)
                ham = SatinAlmaEnflasyonYoneticisi.Instance.UygulaVergi(ham);
            return ham;
        }
    }

    public int CurrentSimultaneousLimit
    {
        get { return baseSimultaneous + Level; }
    }

    /// <summary>UpgradePanelUI'daki butondan cagrilir.</summary>
    public bool TryUpgrade()
    {
        if (IsMaxed)
        {
            if (ToastNotification.Instance != null) ToastNotification.Instance.Show("Personel kapasitesi maksimumda.", 2.5f);
            return false;
        }

        // NextCost'u ONCEDEN al — Level artinca deger degisir, harcanan ile gosterilen fiyat
        // (enflasyon carpani dahil) HER ZAMAN ayni kalsin.
        int cost = NextCost;
        if (!EconomyManager.Instance.TrySpendGold(cost))
        {
            if (ToastNotification.Instance != null) ToastNotification.Instance.Show("Yeterli paran yok.", 2.5f);
            return false;
        }

        Level++;
        ApplyToSpawner();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPurchase();

        // EKONOMİ SIKILAŞTIRMA: bu buyuk satin alma bir sonrakini pahalilastirir.
        if (SatinAlmaEnflasyonYoneticisi.Instance != null)
            SatinAlmaEnflasyonYoneticisi.Instance.SatinAlmaKaydet();

        PlayerProgressStats.AddUpgradePurchased();
        if (OnUpgraded != null) OnUpgraded();
        return true;
    }

    private void ApplyToSpawner()
    {
        if (CustomerSpawner.Instance != null)
            CustomerSpawner.Instance.SetSimultaneousLimit(CurrentSimultaneousLimit);
    }

    // ── Kayit koprusu ──

    public void ApplySave(SaveData data)
    {
        Level = Mathf.Clamp(data.personnelCapacityLevel, 0, costs.Length);
        ApplyToSpawner();
    }

    public void WriteToSave(SaveData data)
    {
        data.personnelCapacityLevel = Level;
    }
}
