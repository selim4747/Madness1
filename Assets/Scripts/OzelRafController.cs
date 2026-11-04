// OzelRafController.cs — Kara Kitap Satici odullerinin durdugu OZEL raf. Normal ShelfController'dan
// farkli: TEK kitap turu degil, AYNI ANDA BIRDEN FAZLA farkli kitap tutabilir (max 30 toplam).
// Marketten satin alinip acilir. Iki tur ozel kitap barindirir:
//   - JOKER (KaraKitapJokerData): HERHANGI bir musteriye satilabilir (sevdigi tur stokta yoksa devreye girer)
//   - KISIYE OZEL (KaraKitapOzelData): SADECE hedefMusteriIsmi'ndeki TEK NPC alabilir, fiyat onun icin engel degildir
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System;
using System.Collections.Generic;

public class OzelRafController : MonoBehaviour
{
    public static OzelRafController Instance { get; private set; }

    public const int MaxKapasite = 30;

    [Header("Kilit / Satin Alma")]
    [SerializeField] private int acilisMaliyeti = 2000;
    public bool IsUnlocked { get; private set; } = false;

    [Header("Konum")]
    [Tooltip("Musterilerin yaklasacagi nokta. Bos birakilirsa bu objenin kendi konumu kullanilir.")]
    [SerializeField] private Transform yaklasmaNoktasi;
    public Vector3 ApproachPosition { get { return yaklasmaNoktasi != null ? yaklasmaNoktasi.position : transform.position; } }

    public event Action OnStokDegisti;
    public event Action OnKilitAcildi;

    private readonly Dictionary<BookData, int> stok = new Dictionary<BookData, int>();
    public IReadOnlyDictionary<BookData, int> Stok { get { return stok; } }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public int ToplamKitapSayisi()
    {
        int toplam = 0;
        foreach (int adet in stok.Values) toplam += adet;
        return toplam;
    }

    public int GetAcilisMaliyeti() { return acilisMaliyeti; }

    /// <summary>MarketUI'daki "Ozel Raf Satin Al" butonundan cagrilir.</summary>
    public bool TrySatinAl()
    {
        if (IsUnlocked)
        {
            if (ToastNotification.Instance != null) ToastNotification.Instance.Show("Özel raf zaten senin.", 2.5f);
            return false;
        }

        if (EconomyManager.Instance == null || !EconomyManager.Instance.TrySpendGold(acilisMaliyeti))
        {
            if (ToastNotification.Instance != null) ToastNotification.Instance.Show("Yeterli paran yok.", 2.5f);
            return false;
        }

        IsUnlocked = true;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPurchase();
        if (ToastNotification.Instance != null) ToastNotification.Instance.Show("Özel raf açıldı! Kara Kitap Satıcısı ödülleri artık buraya eklenecek.", 4f);

        OnKilitAcildi?.Invoke();
        OnStokDegisti?.Invoke();
        return true;
    }

    /// <summary>Kilitliyse ya da kapasite doluysa sessizce basarisiz olur (BlackBookDealerManager
    /// gibi otomatik odul sistemleri icin) — kapasiteye sigan kismi ekler, sigmayanini KAYBETMEZ,
    /// sadece eklemez (return degeri gercekten kac tane eklendigini soyler).</summary>
    public int KitapEkle(BookData kitap, int miktar)
    {
        if (!IsUnlocked || kitap == null || miktar <= 0) return 0;

        int mevcutToplam = ToplamKitapSayisi();
        int eklenebilir = Mathf.Min(miktar, MaxKapasite - mevcutToplam);
        if (eklenebilir <= 0) return 0;

        if (!stok.ContainsKey(kitap)) stok[kitap] = 0;
        stok[kitap] += eklenebilir;

        OnStokDegisti?.Invoke();
        return eklenebilir;
    }

    /// <summary>Verilen musteri icin en uygun ozel kitabi bulur. Once ONA OZEL (KaraKitapOzelData)
    /// kitaba bakar (varsa oncelik ondadir — daha anlamli bir deneyim), sonra genel JOKER
    /// (KaraKitapJokerData) kitaba. Hicbiri yoksa null doner.</summary>
    public BookData EnUygunKitabiBul(NPCKimligi musteriKimlik)
    {
        if (!IsUnlocked) return null;

        // 1) Bu KISIYE OZEL kitap
        foreach (KeyValuePair<BookData, int> kv in stok)
        {
            if (kv.Value <= 0) continue;
            KaraKitapOzelData ozel = kv.Key as KaraKitapOzelData;
            if (ozel != null && musteriKimlik != null && ozel.hedefMusteriIsmi == musteriKimlik.musteriIsmi)
            {
                return kv.Key;
            }
        }

        // 2) Herkese acik JOKER kitap
        foreach (KeyValuePair<BookData, int> kv in stok)
        {
            if (kv.Value <= 0) continue;
            if (kv.Key is KaraKitapJokerData) return kv.Key;
        }

        return null;
    }

    /// <summary>Belirtilen kitaptan bir adet dusurur, dusurulen kitabi dondurur (yoksa null).</summary>
    public BookData TryTakeOne(BookData hangisi)
    {
        if (hangisi == null || !stok.ContainsKey(hangisi) || stok[hangisi] <= 0) return null;

        stok[hangisi]--;
        OnStokDegisti?.Invoke();
        return hangisi;
    }

    /// <summary>2. Duzey ogrenci calisma sonrasi kitabi geri koyarsa (tutarlilik icin).</summary>
    public void ReturnOne(BookData hangisi)
    {
        if (hangisi == null) return;
        if (!stok.ContainsKey(hangisi)) stok[hangisi] = 0;
        stok[hangisi]++;
        OnStokDegisti?.Invoke();
    }

    // ── Kayit koprusu ──

    public void ApplySave(SaveData data)
    {
        IsUnlocked = data.ozelRafAcikMi;
        // NOT: Stok icerigi (hangi kitaptan kac tane) su an KAYDEDILMIYOR — gun ici bir
        // oturumda dolar, gun sonu ozetinde/normal akiste tuketilmesi beklenir. Kalici
        // stok takibi istenirse ayrica eklenebilir.
    }

    public void WriteToSave(SaveData data)
    {
        data.ozelRafAcikMi = IsUnlocked;
    }
}