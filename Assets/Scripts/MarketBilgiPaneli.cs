// MarketBilgiPaneli.cs — Market'teki her seyi (butonlar + silah kademeleri) aciklayan,
// SADECE dikey kayan (yatay kaymayan) bir bilgi paneli. Kapatilinca Market'e geri doner.
// Silah istatistikleri WeaponInventory.AllWeapons'tan GERCEK degerlerle otomatik uretilir —
// elle yazilmis sayilar degildir, WeaponData asset'lerini degistirdikce burasi da guncel kalir.
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using System.Collections.Generic;

public class MarketBilgiPaneli : MonoBehaviour
{
    public static MarketBilgiPaneli Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button closeButton;

    [Header("Kayan Icerik (Scroll View — SADECE dikey)")]
    [Tooltip("Scroll View'in kendisi. Inspector'da Horizontal=KAPALI, Vertical=ACIK olmali.")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private TextMeshProUGUI icerikYazisi;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (root != null) root.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);

        // Yatay kaymayi kod tarafindan da garanti kapatiyoruz — Inspector'da yanlislikla
        // acik birakilsa bile calisma zamaninda zorla kapatilir.
        if (scrollRect != null)
        {
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
        }

        if (PanelYoneticisi.Instance != null && root != null) PanelYoneticisi.Instance.Kaydet(root);
    }

    public void Open()
    {
        if (PanelYoneticisi.Instance != null) PanelYoneticisi.Instance.DigerleriniKapat(root);
        if (root != null) root.SetActive(true);

        IcerikUret();

        // Her acilista en basa (yukari) sarar — kaldigi yerden degil, hep bastan baslasin.
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
    }

    /// <summary>Kapatilinca Market'e GERI DONER — sadece kapanip bosluga birakmaz.</summary>
    public void Close()
    {
        if (root != null) root.SetActive(false);
        if (MarketUI.Instance != null) MarketUI.Instance.Open();
    }

    private void IcerikUret()
    {
        if (icerikYazisi == null) return;

        StringBuilder sb = new StringBuilder();

        sb.AppendLine("<b>MARKET REHBERİ</b>");
        sb.AppendLine();

        sb.AppendLine("<b>Stok Yenile</b>");
        sb.AppendLine("Tükenen kitap türlerinin stoğunu doldurur. Rafların boş kalmaması için düzenli kullan.");
        sb.AppendLine();

        sb.AppendLine("<b>Dükkan Satın Al</b>");
        sb.AppendLine("Yeni bir dükkan/raf bölümü açar. Daha fazla kitap türü satabilmen için gereklidir.");
        sb.AppendLine();

        sb.AppendLine("<b>Canı Artır</b>");
        sb.AppendLine("Anlık canını bir birim iyileştirir. Gece savaşına hazırlanırken kullan.");
        sb.AppendLine();

        sb.AppendLine("<b>Maksimum Can Artır</b>");
        sb.AppendLine("Toplam can kapasiteni kalıcı olarak artırır. Ne kadar çok canavar öldüreceksen o kadar önemli.");
        sb.AppendLine();

        sb.AppendLine("<b>Öğrenci Masaları</b>");
        sb.AppendLine("Kitap teslimatı bekleyen öğrenci masalarını açman/yönetmen için panel. Kilitli masaları buradan satın alabilirsin.");
        sb.AppendLine();

        sb.AppendLine("<b>Reklamla Stok/Müşteri Artırma</b>");
        sb.AppendLine("Kısa bir reklam izleyerek ekstra stok ya da aynı anda gelebilecek müşteri kapasitesi kazanırsın.");
        sb.AppendLine();

        sb.AppendLine("<b>Raf Paneli</b>");
        sb.AppendLine("Rafların hangi kitap türünü sattığını buradan ayarlarsın.");
        sb.AppendLine();

        sb.AppendLine("――――――――――――――――――");
        sb.AppendLine("<b>SİLAH KADEMELERİ</b>");
        sb.AppendLine();

        if (WeaponInventory.Instance != null && WeaponInventory.Instance.AllWeapons != null)
        {
            WeaponTypeIcinYaz(sb, WeaponType.Pistol, "TABANCA");
            WeaponTypeIcinYaz(sb, WeaponType.Rifle, "TÜFEK");
            WeaponTypeIcinYaz(sb, WeaponType.Shotgun, "POMPALI");
        }
        else
        {
            sb.AppendLine("(Silah verileri şu an yüklenemedi.)");
        }

        icerikYazisi.text = sb.ToString();
    }

    private void WeaponTypeIcinYaz(StringBuilder sb, WeaponType tip, string turkceAdi)
    {
        sb.AppendLine("<b>" + turkceAdi + "</b>");

        List<WeaponData> kademeler = new List<WeaponData>();
        foreach (WeaponData w in WeaponInventory.Instance.AllWeapons)
            if (w != null && w.type == tip) kademeler.Add(w);

        kademeler.Sort((a, b) => ((int)a.tier).CompareTo((int)b.tier));

        for (int i = 0; i < kademeler.Count; i++)
        {
            WeaponData w = kademeler[i];
            float atisHizi = w.fireInterval > 0f ? 1f / w.fireInterval : 0f;

            sb.AppendLine("• <b>" + w.displayName + "</b> — " + w.price + " Para");
            if (!string.IsNullOrEmpty(w.description)) sb.AppendLine("  " + w.description);
            sb.Append("  Hasar çarpanı: x" + w.damageMultiplier.ToString("0.0"));
            sb.Append("  |  Ateş hızı: " + atisHizi.ToString("0.0") + "/sn");
            sb.Append("  |  Menzil: " + w.range.ToString("0.0"));

            if (tip == WeaponType.Shotgun && w.pelletCount > 1)
                sb.Append("  |  Saçma sayısı: " + w.pelletCount);

            sb.AppendLine();
            sb.AppendLine();
        }

        if (kademeler.Count == 0) sb.AppendLine("  (Bu tip için henüz silah verisi yok.)");
        sb.AppendLine();
    }
}