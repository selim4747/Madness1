// ShopExpansionManager.cs — Raf ve dukkan genisletme
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System;

public class ShopExpansionManager : MonoBehaviour
{
    public static ShopExpansionManager Instance { get; private set; }

    public const int MaxShops = 6;

    public event Action OnExpansionChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>Bir rafi acmanin maliyeti (BookData.shelfIndex uzerinden), guncel EKONOMİ
    /// ENFLASYONU carpani uygulanmis olarak. UI de, TryUnlockShelf da AYNI bu metodu cagirir —
    /// oyuncu ekranda gordugunden farkli para asla odemez.</summary>
    public int GetShelfCost(ShelfController shelf)
    {
        if (shelf == null || shelf.book == null) return -1;

        int ham = shelf.book.GetShelfUnlockCost();   // 150 x raf sirasi
        if (SatinAlmaEnflasyonYoneticisi.Instance != null)
            ham = SatinAlmaEnflasyonYoneticisi.Instance.UygulaVergi(ham);
        return ham;
    }

    /// <summary>Sonraki dukkanin maliyeti: 3000 x 2^(no-2), guncel EKONOMİ ENFLASYONU
    /// carpani uygulanmis olarak.</summary>
    public int GetNextShopCost()
    {
        int next = EconomyManager.Instance.ShopsUnlocked + 1;
        if (next > MaxShops) return -1;

        int ham = 3000 * (int)Mathf.Pow(2f, next - 2);
        if (SatinAlmaEnflasyonYoneticisi.Instance != null)
            ham = SatinAlmaEnflasyonYoneticisi.Instance.UygulaVergi(ham);
        return ham;
    }

    /// <summary>Bu raf su an acilabilir mi? (dukkani satin alinmis olmali)</summary>
    public bool CanUnlockShelf(ShelfController shelf)
    {
        if (shelf == null || shelf.book == null) return false;
        if (shelf.isUnlocked) return false;

        return shelf.book.shopNumber <= EconomyManager.Instance.ShopsUnlocked;
    }

    /// <summary>MarketUI/ShelfUnlockUI'daki "Raf Ac" butonundan cagrilir.</summary>
    public bool TryUnlockShelf(ShelfController shelf)
    {
        if (!CanUnlockShelf(shelf))
        {
            ToastNotification.Instance.Show("Once bu dukkani satin almalisin.", 2.5f);
            return false;
        }

        int cost = GetShelfCost(shelf);
        if (!EconomyManager.Instance.TrySpendGold(cost))
        {
            ToastNotification.Instance.Show("Yeterli paran yok.", 2.5f);
            return false;
        }

        shelf.Unlock();
        AudioManager.Instance.PlayPurchase();

        // EKONOMİ SIKILAŞTIRMA: bu buyuk satin alma bir sonrakini pahalilastirir.
        if (SatinAlmaEnflasyonYoneticisi.Instance != null)
            SatinAlmaEnflasyonYoneticisi.Instance.SatinAlmaKaydet();

        if (OnExpansionChanged != null) OnExpansionChanged();
        return true;
    }

    /// <summary>MarketUI'daki "Dukkan Al" butonundan cagrilir. Sirayla alinir.</summary>
    public bool TryUnlockNextShop()
    {
        int next = EconomyManager.Instance.ShopsUnlocked + 1;
        if (next > MaxShops)
        {
            ToastNotification.Instance.Show("Tum dukkanlar zaten senin.", 2.5f);
            return false;
        }

        int cost = GetNextShopCost();
        if (!EconomyManager.Instance.TrySpendGold(cost))
        {
            ToastNotification.Instance.Show("Yeterli paran yok.", 2.5f);
            return false;
        }

        EconomyManager.Instance.SetShopsUnlocked(next);
        AudioManager.Instance.PlayPurchase();

        // EKONOMİ SIKILAŞTIRMA: bu buyuk satin alma bir sonrakini pahalilastirir.
        if (SatinAlmaEnflasyonYoneticisi.Instance != null)
            SatinAlmaEnflasyonYoneticisi.Instance.SatinAlmaKaydet();

        // Yeni dukkanin HICBIR rafi otomatik acilmaz — teker teker satin alinir
        ToastNotification.Instance.Show(next + ". dukkan acildi! Raflari tek tek acmalisin.", 4f);

        if (OnExpansionChanged != null) OnExpansionChanged();
        return true;
    }
}
