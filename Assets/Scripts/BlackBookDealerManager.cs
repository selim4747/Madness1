// BlackBookDealerManager.cs — "Kara Kitap Satıcısı": ilk 6 gun HER GUN, sonrasinda 6 gunde
// bir (12, 18, 24...) beliren gizemli bir tuccar. FIZIKSEL varligi BlackBookDealerNPC'de —
// bu sinif SADECE MANTIK: gorev havuzu, odul, ve "bugun goruluyor mu" hesabi.
//
// ONEMLI (guncellendi): Gorev degerlendirme + yeni gorev verme ARTIK GUN DEGISINCE OTOMATIK
// OLMAZ — SADECE oyuncu NPC'nin yanina gidip konusunca (Konus() cagrilinca) tetiklenir.
// Oyuncu o gun onu hic ziyaret etmezse, o gun icin hicbir sey olmaz (ne eski degerlendirme
// ne yeni gorev) — bir sonraki gorunme gununde tekrar sansi olur.
//
// Gorev suresi hala "bir SONRAKI gorunme gunu"ne kadardir (ilk 6 gun HER GUN goruldugu icin,
// o donemde verilen bir gorevin suresi sadece "yarin"a kadardir) — TASARIM GEREGI boyle.
//
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System.Collections.Generic;

public class BlackBookDealerManager : MonoBehaviour
{
    public static BlackBookDealerManager Instance { get; private set; }

    [Header("Olasi Gorevler (her gelisinde rastgele biri secilir)")]
    [SerializeField] private List<GorevTanimi> olasiGorevler = new List<GorevTanimi>();

    [Header("Odul — normal dukkanda OLMAYAN ozel kitaplar")]
    [Tooltip("Gorev basariyla tamamlaninca bunlardan rastgele biri stoga eklenir.")]
    [SerializeField] private List<BookData> ozelKitaplar = new List<BookData>();
    [SerializeField] private int odulMiktari = 3;

    /// <summary>UI'lar (orn. bir "Kara Kitap Satıcısı geldi!" paneli) buna abone olabilir.</summary>
    public event System.Action<GorevTanimi> OnDealerGeldiYeniGorev;
    public event System.Action OnGorevBasarili;
    public event System.Action OnGorevBasarisiz;

    /// <summary>Bugun satici GORUNUR mu (BlackBookDealerNPC bunu okuyup kendini gosterir/gizler).</summary>
    public bool BugunGoruldu { get; private set; }

    /// <summary>Bugun oyuncu ONUNLA ZATEN KONUSTU mu — ayni gun icinde tekrar konusulursa
    /// gorev yeniden RASTGELE secilmesin diye.</summary>
    private bool bugunKonusulduMu = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnDayChanged += HandleDayChanged;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnDayChanged -= HandleDayChanged;
    }

    /// <summary>Verilen gunde satici gorunur mu? Ilk 6 gun HER GUN, sonra 6 gunde bir.</summary>
    public bool BugunGelirMi(int gun)
    {
        if (gun <= 6) return true;
        return (gun - 6) % 6 == 0;
    }

    /// <summary>Verilen gunden SONRAKI ilk gorunme gunu (gorev suresi hesabi icin).</summary>
    private int SonrakiGorunmeGunu(int suankiGun)
    {
        if (suankiGun < 6) return suankiGun + 1;
        int altidanBeri = Mathf.Max(0, suankiGun - 6);
        int sonrakiKat = ((altidanBeri / 6) + 1) * 6;
        return 6 + sonrakiKat;
    }

    private void HandleDayChanged(int gun)
    {
        BugunGoruldu = BugunGelirMi(gun);
        bugunKonusulduMu = false; // yeni gun, konusma hakki tazelendi
    }

    /// <summary>BlackBookDealerNPC, oyuncu onunla konustugunda cagirir. Donen satirlar
    /// (sadece metin — konusan ismi NPC scriptinde eklenir) DialogueBox'ta gosterilir.</summary>
    public List<string> Konus()
    {
        List<string> satirlar = new List<string>();

        if (bugunKonusulduMu)
        {
            // Bugun zaten konusulmus — yeniden gorev secmeyiz, sadece mevcut durumu hatirlatiriz.
            if (GorevYoneticisi.Instance != null && GorevYoneticisi.Instance.GorevVarMi)
                satirlar.Add("Hatırlatayım: " + GorevYoneticisi.Instance.AktifGorev.aciklama + ". Bir dahaki gelişimde görüşürüz.");
            else
                satirlar.Add("Şu an senin için bir işim yok. Bir dahaki gelişimi bekle.");
            return satirlar;
        }

        bugunKonusulduMu = true;

        // 1) ONCEKI gorev varsa (bir onceki gelisinde verilmisti), simdi SONUCUNU degerlendir.
        if (GorevYoneticisi.Instance != null && GorevYoneticisi.Instance.GorevVarMi)
        {
            bool basarili = GorevYoneticisi.Instance.TamamlandiMi();

            if (basarili)
            {
                OduluVer();
                PlayerProgressStats.AddDealerOfferAccepted();
                OnGorevBasarili?.Invoke();
                satirlar.Add("Harika iş çıkardın. Sözümde durdum — özel bir kitap rafına eklendi.");
            }
            else
            {
                OnGorevBasarisiz?.Invoke();
                satirlar.Add("Bu seferki işi tamamlayamadın. Belki bir dahaki sefere.");
            }

            GorevYoneticisi.Instance.GoreviTemizle();
        }

        // 2) YENI bir gorev ver — bir SONRAKI gelisine kadar tamamlanmali.
        if (olasiGorevler.Count > 0 && GorevYoneticisi.Instance != null && GameManager.Instance != null)
        {
            GorevTanimi yeniGorev = olasiGorevler[Random.Range(0, olasiGorevler.Count)];
            int sonGun = SonrakiGorunmeGunu(GameManager.Instance.CurrentDay);

            GorevYoneticisi.Instance.GorevBaslat(yeniGorev, sonGun);
            OnDealerGeldiYeniGorev?.Invoke(yeniGorev);
            satirlar.Add("Senin için yeni bir işim var: " + yeniGorev.aciklama + ".");
        }

        if (satirlar.Count == 0)
            satirlar.Add("Şimdilik sana verecek bir işim yok.");

        return satirlar;
    }

    private void OduluVer()
    {
        if (ozelKitaplar.Count == 0) return;

        BookData secilen = ozelKitaplar[Random.Range(0, ozelKitaplar.Count)];
        if (secilen == null) return;

        // Odul artik OZEL RAFA eklenir — normal dukkan stogu degil. Ozel raf henuz satin
        // alinmadiysa (kilitliyse), KitapEkle sessizce 0 doner — odul kaybolmaz, sadece
        // rafi acana kadar bekler (bir sonraki basarili gorevde tekrar denenir).
        if (OzelRafController.Instance != null)
        {
            int eklenen = OzelRafController.Instance.KitapEkle(secilen, odulMiktari);
            if (eklenen == 0 && ToastNotification.Instance != null)
            {
                ToastNotification.Instance.Show(
                    OzelRafController.Instance.IsUnlocked
                        ? "Özel raf dolu, ödül eklenemedi!"
                        : "Ödülü almak için önce Market'ten Özel Raf'ı satın almalısın!",
                    4f);
            }
        }
    }
}