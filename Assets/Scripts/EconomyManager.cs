// EconomyManager.cs — Merkezi para sistemi
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using Madness.Mods;
using System;
using TMPro; // TextMeshPro kullanıyorsan bu şart! (Eski text ise UnityEngine.UI yap)

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    [Header("Arayüz (UI) Bağlantısı")]
    [Tooltip("Sol üstteki Altın yazısını buraya sürükle!")]
    public TextMeshProUGUI paraText;

    public int Gold { get; private set; }
    public int ShopsUnlocked { get; private set; } = 1;

    public event Action<int> OnGoldChanged;
    public event Action<int> OnShopsUnlockedChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // 1. DAHİYANE DOKUNUŞ: Kendi fırlattığı "Para Değişti" sinyalini kendisi dinler
        OnGoldChanged += EkraniGuncelle;

        // Oyun başlarken ekranda 0 veya kayıttan gelen mevcut parayı yazdırır
        EkraniGuncelle(Gold);
    }

    private void OnDestroy()
    {
        // Sahne kapanırken sinyali temizler (Motor çökmesin diye)
        if (Instance == this)
        {
            OnGoldChanged -= EkraniGuncelle;
        }
    }

    // --- YENİ EKLENEN KISIM: Ekrana parayı yazan metot ---
    private void EkraniGuncelle(int guncelPara)
    {
        if (paraText != null)
        {
            paraText.text = "Altın: " + guncelPara.ToString();
        }
    }

    // =========================================================
    // AŞAĞISI SENİN ORİJİNAL KODUNDUR, HİÇBİR ŞEY SİLİNMEDİ!
    // =========================================================

    public bool CanAfford(int amount)
    {
        return amount >= 0 && Gold >= amount;
    }

    public bool TrySpendGold(int amount)
    {
        GoldContext context = ModEventBus.Publish(new GoldContext { Amount = amount, Reason = "spend" });
        if (context.Cancelled) return false;
        amount = Mathf.Max(0, context.Amount);
        if (!CanAfford(amount)) return false;
        Gold -= amount;
        if (OnGoldChanged != null) OnGoldChanged(Gold);
        return true;
    }

    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        GoldContext context = ModEventBus.Publish(new GoldContext { Amount = amount, Reason = "add" });
        if (context.Cancelled) return;
        amount = Mathf.Max(0, context.Amount);
        if (amount <= 0) return;
        Gold += amount;
        PlayerProgressStats.AddGoldToday(amount);   // basarim sayaci beslenir

        if (OnGoldChanged != null) OnGoldChanged(Gold);
    }

    public void SetShopsUnlocked(int count)
    {
        ShopsUnlocked = Mathf.Clamp(count, 1, 6);
        if (OnShopsUnlockedChanged != null) OnShopsUnlockedChanged(ShopsUnlocked);
    }

    // ── Kayit koprusu ──

    public void ApplySave(SaveData data)
    {
        Gold = data.gold;
        ShopsUnlocked = data.shopsUnlocked;

        if (OnGoldChanged != null) OnGoldChanged(Gold);
        if (OnShopsUnlockedChanged != null) OnShopsUnlockedChanged(ShopsUnlocked);
    }

    public void WriteToSave(SaveData data)
    {
        data.gold = Gold;
        data.shopsUnlocked = ShopsUnlocked;
    }
}