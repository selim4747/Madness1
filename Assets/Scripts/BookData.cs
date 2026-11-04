using UnityEngine;

[CreateAssetMenu(fileName = "YeniKitap", menuName = "Delilik/Kitap Verisi")]
public class BookData : ScriptableObject
{
    [Header("Temel Kimlik")]
    public string bookId = "B001";
    public string kitapAdi = "İsimsiz Kitap";
    public KitapTuru kitapTuru;

    [Header("Ekonomi & Sınıflandırma")]
    public Rarity nadirlik; // Oyunun kendi orijinal Rarity sistemine tam entegre
    public int fiyat = 50;
    public int shopNumber = 1;

    [Header("Görsel & Hikaye")]
    public Sprite bookCover;

    [TextArea(3, 5)]
    public string loreBilgisi = "Bu kitabın hikayesi henüz yazılmadı...";

    // NOT: Joker / Kisiye-ozel kitap ayrimi ARTIK BURADA DEGIL — KaraKitapJokerData ve
    // KaraKitapOzelData adli iki AYRI alt sinifta (sadece Kara Kitap Saticisi odulleri
    // icin). Ayarlari (hedef kisi, gorunmeme suresi vb.) çok farkli oldugu icin tek bir
    // bool + liste yerine iki ayri tur kullaniliyor — bkz. KaraKitapJokerData.cs /
    // KaraKitapOzelData.cs.

    // ==========================================
    // MODERN ADAPTÖR (Tüm Sistemlere Kusursuz Veri Sağlar)
    // ==========================================

    public string displayName => kitapAdi;
    public string genre => kitapTuru.ToString();
    public Rarity rarity => nadirlik;
    public string loreSentence => loreBilgisi;
    public float temelFiyat => fiyat;

    public int GetFinalPrice()
    {
        return fiyat;
    }

    public int GetShelfUnlockCost()
    {
        return 500;
    }
}