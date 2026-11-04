// SadikMusteriYoneticisi.cs — Musteri-Oyuncu ILISKI puani takip eder. Oyuncu bir musteriyle
// KONUSTUKCA (diyalog), o musterinin dukkana TEKRAR GELME SANSI artar. ARTIK COK KADEMELI:
// tek bir "mudavim" esigi yerine, sirali bir KADEME LISTESI var (Tanidik / Mudavim / Sirdas
// gibi) — her kademeye ulasinca kutlama mesaji + (varsa) altin odulu tetiklenir.
//
// ZAMANLA SOGUMA: bir musteriyle uzun sure (X gun) KONUSULMAZSA, iliski puani gunde bir
// miktar azalir — oyuncuyu duzenli ilgilenmeye iter. Sadece GERCEK sohbetler (SohbetEdildi)
// bu sayaci sifirlar; disaridan gelen puan etkileri (orn. dedikodu) sayaci SIFIRLAMAZ.
//
// COK KARAKTER OLCEKLENIR: her karakter ScriptableObject asset'i olarak degil, KENDI ISMI
// (musteriIsmi) ile bir STRING anahtarli sozlukte takip edilir — yani onceden hicbir yerde
// kayitli olmasi gerekmez, kac karakter eklersen ekle otomatik calisir.
//
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class SadakatKademesi
{
    [Tooltip("Kademe adi (UI/toast'ta gosterilir). Orn: 'Tanidik', 'Mudavim', 'Sirdas'.")]
    public string adi = "Müdavim";

    [Tooltip("Bu kademeye ulasmak icin gereken TOPLAM iliski puani.")]
    public int esikPuan = 30;

    [Tooltip("Bu kademeye ULASINCA bir seferlik verilecek altin odulu (0 = odul yok).")]
    public int odulAltin = 0;

    [TextArea(1, 2)]
    [Tooltip("Kademeye ulasinca gosterilecek toast mesaji. '{isim}' otomatik musteri ismiyle degistirilir.")]
    public string kutlamaMesaji = "{isim} artık müdavim müşterin! Daha sık uğrayacak.";
}

public class SadikMusteriYoneticisi : MonoBehaviour
{
    public static SadikMusteriYoneticisi Instance { get; private set; }

    [Header("İlişki Puanı Kazanımı")]
    [Tooltip("Her SOHBET (diyalog) puani ne kadar iliski puani kazandirir.")]
    [SerializeField] private int sohbetPuani = 5;

    [Header("Sadakat Kademeleri (puana göre KÜÇÜKTEN BÜYÜĞE sıralı olmalı)")]
    [SerializeField]
    private List<SadakatKademesi> kademeler = new List<SadakatKademesi>();

    /// <summary>Component ILK EKLENDIGINDE (ya da Inspector'da component basligina sag tik >
    /// Reset ile) varsayilan 3 kademeyi doldurur. Alan tanimina DOGRUDAN gomulmedi — Unity'nin
    /// sahnede ONCEDEN var olan bir component'in verisini bu yeni ic-ice sinifla (SadakatKademesi)
    /// uzlastirirken serialization hatasi vermesine (ExtensionOfNativeClass) sebep oluyordu.</summary>
    private void Reset()
    {
        kademeler = new List<SadakatKademesi>
        {
            new SadakatKademesi { adi = "Tanıdık", esikPuan = 10, odulAltin = 0,  kutlamaMesaji = "{isim} artık seni tanıyor." },
            new SadakatKademesi { adi = "Müdavim", esikPuan = 30, odulAltin = 20, kutlamaMesaji = "{isim} artık müdavim müşterin! Daha sık uğrayacak." },
            new SadakatKademesi { adi = "Sırdaş",  esikPuan = 60, odulAltin = 50, kutlamaMesaji = "{isim} sana sırdaş oldu — seninle özel şeyler paylaşıyor." },
        };
    }

    [Header("Gelme Şansı Ağırlığı")]
    [Tooltip("Iliski puani basina, o karakterin SECILME agirligina eklenecek yuzde. Orn: 0.02 = her puan +%2 agirlik. 20 puanlik biri, hic konusulmamis birine gore %40 daha olasi secilir.")]
    [SerializeField] private float puanBasiAgirlikCarpani = 0.02f;

    [Tooltip("Bir karakterin agirligi ne kadar buyurse buyusun, en fazla bu katina kadar cikabilir (asiri baskinlik olmasin diye guvenlik siniri).")]
    [SerializeField] private float maxAgirlikCarpani = 4f;

    [Header("Zamanla Soğuma")]
    [Tooltip("Soguma sistemi acik mi?")]
    [SerializeField] private bool sogumaAktif = true;

    [Tooltip("Bir musteriyle KAC GUN konusulmazsa soguma baslar (o gune kadar dokunulmaz).")]
    [SerializeField] private int sogumaBaslamaGunSayisi = 2;

    [Tooltip("Soguma basladiktan sonra, HER GUN kac puan azalsin.")]
    [SerializeField] private int sogumaGundeMiktar = 2;

    /// <summary>Bir musteri YENI bir kademeye ulasinca tetiklenir (UI/kutlama icin).</summary>
    public event Action<string, SadakatKademesi> OnKademeYukseldi;

    /// <summary>Geriye donuk uyumluluk icin korunuyor — "Mudavim" adli kademeye (bulunamazsa
    /// 2. kademeye) ulasinca tetiklenir.</summary>
    public event Action<string> OnMudavimOldu;

    private readonly Dictionary<string, int> iliskiPuanlari = new Dictionary<string, int>();
    private readonly Dictionary<string, int> ulasilanKademeIndeksi = new Dictionary<string, int>(); // -1 = hicbiri
    private readonly Dictionary<string, int> sonKonusulanGun = new Dictionary<string, int>();

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

    /// <summary>Her yeni gun: uzun suredir konusulmayan musterilerin puani yavasca duser.</summary>
    private void HandleDayChanged(int gun)
    {
        if (!sogumaAktif || iliskiPuanlari.Count == 0) return;

        List<string> isimler = new List<string>(iliskiPuanlari.Keys);
        foreach (string isim in isimler)
        {
            if (iliskiPuanlari[isim] <= 0) continue;

            if (!sonKonusulanGun.ContainsKey(isim))
            {
                // Bu isim icin sayac hic baslamamis (orn. sadece disaridan/dedikodu puani almis) —
                // simdi baslat, bu gun soguma UYGULANMAZ (haksiz cezaya ugramasin).
                sonKonusulanGun[isim] = gun;
                continue;
            }

            int gecenGun = gun - sonKonusulanGun[isim];
            if (gecenGun >= sogumaBaslamaGunSayisi)
                iliskiPuanlari[isim] = Mathf.Max(0, iliskiPuanlari[isim] - sogumaGundeMiktar);
        }
    }

    public int GetIliskiPuani(string musteriIsmi)
    {
        if (string.IsNullOrEmpty(musteriIsmi)) return 0;
        int puan;
        return iliskiPuanlari.TryGetValue(musteriIsmi, out puan) ? puan : 0;
    }

    /// <summary>Su an ulastigi EN YUKSEK kademenin index'i (-1 = hicbir kademeye ulasmadi).</summary>
    public int GetKademeIndeksi(string musteriIsmi)
    {
        int puan = GetIliskiPuani(musteriIsmi);
        for (int i = kademeler.Count - 1; i >= 0; i--)
        {
            if (kademeler[i] != null && puan >= kademeler[i].esikPuan) return i;
        }
        return -1;
    }

    /// <summary>Su anki kademenin ADI (hicbir kademeye ulasmadiysa bos string).</summary>
    public string GetKademeAdi(string musteriIsmi)
    {
        int idx = GetKademeIndeksi(musteriIsmi);
        return idx >= 0 ? kademeler[idx].adi : "";
    }

    private int MudavimKademeIndeksi()
    {
        for (int i = 0; i < kademeler.Count; i++)
        {
            if (kademeler[i] != null && kademeler[i].adi != null &&
                kademeler[i].adi.IndexOf("üdavim", StringComparison.OrdinalIgnoreCase) >= 0)
                return i;
        }
        return kademeler.Count > 1 ? 1 : 0; // isimle bulunamazsa 2. kademeyi kabul et (eski davranis)
    }

    public bool MudavimMi(string musteriIsmi)
    {
        return GetKademeIndeksi(musteriIsmi) >= MudavimKademeIndeksi();
    }

    /// <summary>MusteriSohbetRadari/BahceSohbetRadari GERCEKTEN bir diyalog gosterdiginde cagirir.
    /// Soguma sayacini da SIFIRLAR (gercek bir sohbet oldugu icin).</summary>
    public void SohbetEdildi(string musteriIsmi)
    {
        if (string.IsNullOrEmpty(musteriIsmi)) return;
        if (GameManager.Instance != null) sonKonusulanGun[musteriIsmi] = GameManager.Instance.CurrentDay;
        PuanEkle(musteriIsmi, sohbetPuani);
    }

    /// <summary>DISARIDAN (orn. DedikoduYoneticisi) puan etkisi eklemek icin — sohbet SAYILMAZ,
    /// soguma sayacini SIFIRLAMAZ.</summary>
    public void DisaridanPuanEkle(string musteriIsmi, int miktar)
    {
        PuanEkle(musteriIsmi, miktar);
    }

    private void PuanEkle(string musteriIsmi, int miktar)
    {
        if (string.IsNullOrEmpty(musteriIsmi) || miktar == 0) return;

        int oncekiKademe = GetKademeIndeksi(musteriIsmi);

        if (!iliskiPuanlari.ContainsKey(musteriIsmi)) iliskiPuanlari[musteriIsmi] = 0;
        iliskiPuanlari[musteriIsmi] = Mathf.Max(0, iliskiPuanlari[musteriIsmi] + miktar);

        // DEBUG: her puan kazanimini Console'a yazdir.
        Debug.Log($"💛 [İLİŞKİ] {musteriIsmi}: +{miktar} puan (toplam: {iliskiPuanlari[musteriIsmi]})");

        int yeniKademe = GetKademeIndeksi(musteriIsmi);
        if (yeniKademe <= oncekiKademe) return;

        int mudavimIdx = MudavimKademeIndeksi();

        // Arada birden fazla kademe atlanmis olabilir (buyuk puan siçramasi) — hepsini bildir.
        for (int i = Mathf.Max(0, oncekiKademe + 1); i <= yeniKademe; i++)
        {
            SadakatKademesi kademe = kademeler[i];
            if (kademe == null) continue;

            if (kademe.odulAltin > 0 && EconomyManager.Instance != null)
                EconomyManager.Instance.AddGold(kademe.odulAltin);

            if (i == mudavimIdx)
            {
                PlayerProgressStats.AddRegularCustomer();
                OnMudavimOldu?.Invoke(musteriIsmi);
            }

            OnKademeYukseldi?.Invoke(musteriIsmi, kademe);

            if (ToastNotification.Instance != null && !string.IsNullOrEmpty(kademe.kutlamaMesaji))
                ToastNotification.Instance.Show(kademe.kutlamaMesaji.Replace("{isim}", musteriIsmi), 3.5f);
        }
    }

    /// <summary>Bu karakterin SECILME agirligi carpani. 1.0 = normal (hic iliski yok).
    /// CustomerController karakter secerken, uniform rastgele yerine BUNUNLA agirlikli secer.</summary>
    public float GetSecimAgirligi(string musteriIsmi)
    {
        int puan = GetIliskiPuani(musteriIsmi);
        float carpan = 1f + (puan * puanBasiAgirlikCarpani);
        return Mathf.Min(carpan, maxAgirlikCarpani);
    }

    /// <summary>Agirlikli rastgele secim — cok sayida karakter icin de dogrusal (O(n)) calisir,
    /// performans sorunu olmaz. CustomerController.Baslat() bunu kullanir.</summary>
    public NPCKimligi AgirlikliSec(List<NPCKimligi> adaylar)
    {
        if (adaylar == null || adaylar.Count == 0) return null;
        if (adaylar.Count == 1) return adaylar[0];

        float toplamAgirlik = 0f;
        for (int i = 0; i < adaylar.Count; i++)
        {
            if (adaylar[i] == null) continue;
            toplamAgirlik += GetSecimAgirligi(adaylar[i].musteriIsmi);
        }

        if (toplamAgirlik <= 0f) return adaylar[UnityEngine.Random.Range(0, adaylar.Count)];

        float rastgeleNokta = UnityEngine.Random.Range(0f, toplamAgirlik);
        float birikmis = 0f;

        for (int i = 0; i < adaylar.Count; i++)
        {
            if (adaylar[i] == null) continue;
            birikmis += GetSecimAgirligi(adaylar[i].musteriIsmi);
            if (rastgeleNokta <= birikmis) return adaylar[i];
        }

        return adaylar[adaylar.Count - 1]; // guvenlik yedegi (float yuvarlama payi icin)
    }

    // ── Kayit koprusu ──

    public void ApplySave(SaveData data)
    {
        iliskiPuanlari.Clear();
        ulasilanKademeIndeksi.Clear();
        sonKonusulanGun.Clear();
        if (data.musteriIliskileri == null) return;

        for (int i = 0; i < data.musteriIliskileri.Count; i++)
        {
            IliskiKaydi kayit = data.musteriIliskileri[i];
            if (kayit == null || string.IsNullOrEmpty(kayit.isim)) continue;

            iliskiPuanlari[kayit.isim] = kayit.puan;
            ulasilanKademeIndeksi[kayit.isim] = kayit.ulasilanKademe;

            // Eski kayitlarda bu alan yoktu (varsayilan -1 gelir) — o durumda soguma sayacini
            // simdiki gunle baslat (gecmise donuk ceza uygulanmasin).
            sonKonusulanGun[kayit.isim] = kayit.sonKonusulanGun >= 0
                ? kayit.sonKonusulanGun
                : (GameManager.Instance != null ? GameManager.Instance.CurrentDay : 0);
        }
    }

    public void WriteToSave(SaveData data)
    {
        data.musteriIliskileri.Clear();
        foreach (KeyValuePair<string, int> kv in iliskiPuanlari)
        {
            IliskiKaydi kayit = new IliskiKaydi();
            kayit.isim = kv.Key;
            kayit.puan = kv.Value;
            kayit.ulasilanKademe = GetKademeIndeksi(kv.Key);
            kayit.sonKonusulanGun = sonKonusulanGun.ContainsKey(kv.Key) ? sonKonusulanGun[kv.Key] : -1;
            data.musteriIliskileri.Add(kayit);
        }
    }
}

[Serializable]
public class IliskiKaydi
{
    public string isim;
    public int puan;
    public int ulasilanKademe = -1;
    public int sonKonusulanGun = -1;
}