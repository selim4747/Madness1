using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class KasaEtkilesim : MonoBehaviour
{
    public static KasaEtkilesim Instance { get; private set; }

    // ==========================================
    // SİLİNEN HER ŞEY GERİ GELDİ (DİĞER KODLAR PATLAMASIN DİYE)
    // ==========================================
    [Header("Holdingin Varlıkları")]
    public List<GameObject> tumRaflar = new List<GameObject>();
    public List<BookData> oyundakiTumKitaplar = new List<BookData>();

    [Header("Canlı Veri")]
    public List<BookData> kesfedilenKitaplar = new List<BookData>();
    public Dictionary<BookData, int> kitapStoklari = new Dictionary<BookData, int>();
    public int bugunkuKazanc = 0;

    // ==========================================
    // PC EKRANI VE ARAYÜZ
    // ==========================================
    [Header("Bilgisayar (Ana Ekran)")]
    public GameObject pcAnaEkran;

    [Header("Yaklaşınca görünen PC butonu (genel etkileşim butonundan AYRI)")]
    [Tooltip("Oyuncu kasaya yaklasinca gorunur, uzaklasinca kaybolur. PC ekranini SADECE bu buton acar — cakisma olmasin diye genel etkilesim zincirine hic girmez.")]
    public GameObject pcButonu;

    [Header("Masaüstü Programları (genişletilebilir liste)")]
    [Tooltip("Ansiklopedi, Basarimlar, ve ilerde eklenecek 4./5. program vs. — sirayla buraya ekle")]
    public GameObject[] programlar;

    [Header("Vezne Programı (Bildirimler)")]
    public TextMeshProUGUI musteriBilgiYazisi;

    [Header("Sistemi Kapat (Gece Fazı)")]
    public Button mesaiBitirButonu;
    public TextMeshProUGUI mesaiBitirButonuYazisi;
    private float mesaiSayaci = 0f;
    private float mesaiIcinGerekenSure = 120f;
    private bool mesaiKilidiAcik = false;

    [Header("Kasa Donanımları")]
    public AudioSource kasaSesKaynagi;
    public AudioClip satisSesi;
    public ParticleSystem paraEfekti;

    public event Action OnSatisYapildi;
    private bool oyuncuYakin = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(this);
    }

    void Start()
    {
        if (mesaiBitirButonu != null) mesaiBitirButonu.interactable = false;
        if (mesaiBitirButonuYazisi != null)
            mesaiBitirButonuYazisi.text = "Mesai Devam Ediyor (" + Mathf.CeilToInt(mesaiIcinGerekenSure) + " sn)";

        // METOD GERİ GELDİ!
        RaflaraKitapDagitVeStokla();

        // Her yeni gun basladiginda mesai kilidi sifirlansin — yoksa bir kere acilinca
        // sonsuza kadar acik kalip, sonraki gunlerde 120 saniyelik bekleme hic olmuyordu.
        if (GameManager.Instance != null) GameManager.Instance.OnDayChanged += HandleNewDay;

        // PanelYoneticisi'ne kaydol: hem ana ekran (hub) hem her bir program ayri ayri
        // kaydedilir — boylece BookCatalogUI/AchievementsPanelUI gibi DISARIDAN acilan
        // panellerle de dogru sekilde birbirini kapatirlar.
        if (PanelYoneticisi.Instance != null)
        {
            if (pcAnaEkran != null) PanelYoneticisi.Instance.Kaydet(pcAnaEkran);
            if (programlar != null)
                for (int i = 0; i < programlar.Length; i++)
                    if (programlar[i] != null) PanelYoneticisi.Instance.Kaydet(programlar[i]);
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnDayChanged -= HandleNewDay;
    }

    private void HandleNewDay(int day)
    {
        mesaiSayaci = 0f;
        mesaiKilidiAcik = false;

        if (mesaiBitirButonu != null) mesaiBitirButonu.interactable = false;
        if (mesaiBitirButonuYazisi != null)
            mesaiBitirButonuYazisi.text = "Mesai Devam Ediyor (" + Mathf.CeilToInt(mesaiIcinGerekenSure) + " sn)";
    }

    void Update()
    {
        if (!mesaiKilidiAcik)
        {
            mesaiSayaci += Time.deltaTime;

            if (mesaiBitirButonuYazisi != null)
            {
                int kalanSaniye = Mathf.Max(0, Mathf.CeilToInt(mesaiIcinGerekenSure - mesaiSayaci));
                mesaiBitirButonuYazisi.text = "Mesai Devam Ediyor (" + kalanSaniye + " sn)";
            }

            if (mesaiSayaci >= mesaiIcinGerekenSure)
            {
                mesaiKilidiAcik = true;
                if (mesaiBitirButonu != null) mesaiBitirButonu.interactable = true;
                if (mesaiBitirButonuYazisi != null) mesaiBitirButonuYazisi.text = "Geceye Geç";
            }
        }

        if (oyuncuYakin && pcAnaEkran != null && pcAnaEkran.activeSelf)
        {
            VezneBildiriminiGuncelle();
        }
    }

    // ==========================================
    // ESKİ RAF DİZME METODU
    // ==========================================
    void RaflaraKitapDagitVeStokla()
    {
        if (oyundakiTumKitaplar == null || oyundakiTumKitaplar.Count == 0) return;
        if (tumRaflar == null || tumRaflar.Count == 0) return;

        List<BookData> normalKitaplar = new List<BookData>();
        foreach (var b in oyundakiTumKitaplar)
        {
            if (b != null && b.nadirlik == Rarity.Common) normalKitaplar.Add(b);
        }

        for (int i = 0; i < normalKitaplar.Count; i++)
        {
            BookData gecici = normalKitaplar[i];
            int rastgeleIndex = UnityEngine.Random.Range(i, normalKitaplar.Count);
            normalKitaplar[i] = normalKitaplar[rastgeleIndex];
            normalKitaplar[rastgeleIndex] = gecici;
        }

        List<KitapTuru> atananTurler = new List<KitapTuru>();

        for (int i = 0; i < tumRaflar.Count; i++)
        {
            GameObject rafObj = tumRaflar[i];
            if (rafObj == null) continue;

            if (i < 3)
            {
                rafObj.SetActive(true);
                ShelfController rafScript = rafObj.GetComponentInChildren<ShelfController>();

                if (rafScript != null && normalKitaplar.Count > 0)
                {
                    BookData secilenKitap = normalKitaplar[UnityEngine.Random.Range(0, normalKitaplar.Count)];

                    foreach (var kb in normalKitaplar)
                    {
                        if (!atananTurler.Contains(kb.kitapTuru))
                        {
                            secilenKitap = kb;
                            break;
                        }
                    }

                    atananTurler.Add(secilenKitap.kitapTuru);
                    rafScript.KitapAta(secilenKitap);

                    if (!kitapStoklari.ContainsKey(secilenKitap)) kitapStoklari[secilenKitap] = 0;
                    kitapStoklari[secilenKitap] += UnityEngine.Random.Range(20, 36);
                    if (!kesfedilenKitaplar.Contains(secilenKitap)) kesfedilenKitaplar.Add(secilenKitap);
                }
            }
            else
            {
                rafObj.SetActive(false);
            }
        }
    }

    // ==========================================
    // İŞLETİM SİSTEMİ (PC) KONTROLLERİ
    // ==========================================

    /// <summary>Vezne ekraninda detayli musteri/kitap bilgisi gosterilsin mi.
    /// Panel icindeki "Detay" butonuyla acilip kapanir.</summary>
    private bool detayModu = false;

    /// <summary>Panel icindeki "Detay" butonuna baglanir — kisa/detayli gorunum arasi gecis yapar.</summary>
    public void DetayModunuAcKapat()
    {
        detayModu = !detayModu;
        VezneBildiriminiGuncelle();
    }

    public void VezneBildiriminiGuncelle()
    {
        if (musteriBilgiYazisi == null) return;

        if (CounterController.Instance == null || !CounterController.Instance.HasWaitingCustomer)
        {
            musteriBilgiYazisi.text = "SİSTEM DURUMU: Bekleyen Müşteri Yok.\nDiğer programları kullanabilirsiniz.";
            return;
        }

        CustomerController m = CounterController.Instance.SiradakiMusteri;
        int sayi = CounterController.Instance.WaitingCount;

        // Musterinin GERCEKTEN odeyecegi tutar (kisilik carpani dahil)
        int odenecek = 0;
        int orijinalDeger = 0;
        if (m != null && m.CarriedBook != null)
        {
            orijinalDeger = m.CarriedBook.GetFinalPrice();
            odenecek = orijinalDeger;
            if (m.SecilenKimlik != null)
                odenecek = Mathf.RoundToInt(orijinalDeger * Mathf.Max(0.1f, m.SecilenKimlik.odemeCarpani));
        }

        // ── NORMAL GORUNUM: sadece "musteri var" + alinacak para ──
        if (!detayModu)
        {
            musteriBilgiYazisi.text = "SİSTEM UYARISI: Kasada " + sayi + " Müşteri Var!\n" +
                                       (odenecek > 0 ? "Alınacak: " + odenecek + " Para" : "Elinde kitap yok.");
            return;
        }

        // ── DETAY GORUNUMU: butona basilinca ──
        if (m == null)
        {
            musteriBilgiYazisi.text = "Sıradaki müşteri bilgisi alınamadı.";
            return;
        }

        string isim = m.SecilenKimlik != null ? m.SecilenKimlik.musteriIsmi : "Bilinmeyen";
        string meslek = (m.SecilenKimlik != null && !string.IsNullOrEmpty(m.SecilenKimlik.meslek))
                        ? m.SecilenKimlik.meslek : "—";

        if (m.CarriedBook != null)
        {
            musteriBilgiYazisi.text =
                "MÜŞTERİ: " + isim + "\n" +
                "Meslek: " + meslek + "\n\n" +
                "KİTAP: " + m.CarriedBook.kitapAdi + "\n" +
                "Tür: " + m.CarriedBook.kitapTuru + "\n" +
                "Orijinal Değer: " + orijinalDeger + " Para\n" +
                "Verdiği Para: " + odenecek + " Para";
        }
        else
        {
            musteriBilgiYazisi.text =
                "MÜŞTERİ: " + isim + "\n" +
                "Meslek: " + meslek + "\n\n" +
                "Elinde kitap yok — aradığını bulamadı.";
        }
    }

    /// <summary>Butonun kendisi acik/kapali durumunu TERSE cevirir (toggle). Hareket kilidini
    /// de buna gore ayarlar.</summary>
    public void BilgisayariAcKapat()
    {
        if (pcAnaEkran != null)
        {
            pcAnaEkran.SetActive(!pcAnaEkran.activeSelf);

            if (pcAnaEkran.activeSelf)
            {
                VezneBildiriminiGuncelle();
                if (PlayerController.Instance != null) PlayerController.Instance.SetMovementLocked(true);
            }
            else
            {
                TumProgramlariKapat();
                if (PlayerController.Instance != null) PlayerController.Instance.SetMovementLocked(false);
            }
        }
    }

    /// <summary>SADECE kapatir (toggle degil). "Kapat" butonunu buna bagla.</summary>
    public void KapatButonu()
    {
        if (pcAnaEkran != null) pcAnaEkran.SetActive(false);
        TumProgramlariKapat();
        if (PlayerController.Instance != null) PlayerController.Instance.SetMovementLocked(false);
    }

    /// <summary>Belirtilen index'teki programi acar/kapatir (toggle). Digerleri otomatik kapanir.
    /// Program butonlarini buna baglayacaksin: ProgramAc(0), ProgramAc(1), ProgramAc(2)...
    /// Yeni bir program eklemek icin kod degistirmene gerek yok — sadece Inspector'daki
    /// "Programlar" listesine yeni objeyi ekle, listedeki sirasi onun index'i olur.</summary>
    public void ProgramAc(int index)
    {
        if (programlar == null || index < 0 || index >= programlar.Length) return;

        bool zatenAcikMi = programlar[index] != null && programlar[index].activeSelf;

        // Merkezi yonetici uzerinden kapatilir — boylece sadece programlar[] listesindekiler
        // degil, BookCatalogUI/AchievementsPanelUI gibi DISARIDAN acilmis paneller de kapanir.
        if (!zatenAcikMi && programlar[index] != null && PanelYoneticisi.Instance != null)
            PanelYoneticisi.Instance.DigerleriniKapat(programlar[index]);
        else
            TumProgramlariKapat();

        if (!zatenAcikMi && programlar[index] != null)
            programlar[index].SetActive(true);
    }

    /// <summary>"Geri Don" butonuna baglanmali (eger su an baska bir seye bagliysa DEGISTIR).
    /// Tum programlari kapatir VE ana ekrani (hub) tekrar gosterir — programlar artik hub'i
    /// da kapattigi icin, geri donerken hub'i elle tekrar acmak gerekiyor.</summary>
    public void GeriDon()
    {
        TumProgramlariKapat();
        if (pcAnaEkran != null) pcAnaEkran.SetActive(true);
    }

    private void TumProgramlariKapat()
    {
        if (programlar == null) return;
        for (int i = 0; i < programlar.Length; i++)
            if (programlar[i] != null) programlar[i].SetActive(false);
    }

    // ==========================================
    // ESKİ METOTLAR GERİ GELDİ (BUTONLAR PATLAMASIN DİYE)
    // ==========================================
    public void AnsiklopediToggle() { ProgramAc(0); }
    public void BasarimlarToggle() { ProgramAc(1); }
    public void MasaMenusuAcKapat() { BilgisayariAcKapat(); }

    /// <summary>SADECE satis onayi yapar — genel etkilesim zincirine hic girmez, sohbet/raf-bilgisi
    /// gibi baska hicbir seyi tetiklemez. Panelin icindeki "Satis Yap" butonuna bunu bagla.</summary>
    public void SatisYapButonunaBasildi()
    {
        if (CounterController.Instance == null || PlayerInteraction.Instance == null) return;

        bool basarili = CounterController.Instance.TryConfirmNearest(PlayerInteraction.Instance.transform.position);

        if (basarili)
        {
            if (kasaSesKaynagi != null && satisSesi != null) kasaSesKaynagi.PlayOneShot(satisSesi);
            if (paraEfekti != null) paraEfekti.Play();

            OnSatisYapildi?.Invoke();
            VezneBildiriminiGuncelle();
        }
    }

    /// <summary>PlayerInteraction'in merkezi zincirinden cagrilir. Sadece acar, zaten aciksa
    /// bir sey yapmaz. Basarili olursa true doner.</summary>
    public bool EtkilesimIleAc()
    {
        if (pcAnaEkran == null || pcAnaEkran.activeSelf) return false;

        pcAnaEkran.SetActive(true);
        VezneBildiriminiGuncelle();
        if (PlayerController.Instance != null) PlayerController.Instance.SetMovementLocked(true);
        return true;
    }

    /// <summary>Yaklasinca beliren AYRI PC butonuna baglanir. Genel etkilesim zincirinden
    /// bagimsiz — cakisma olmasin diye ozel/adanmis bir buton.</summary>
    public void PcButonunaBasildi()
    {
        EtkilesimIleAc();
    }

    /// <summary>PlayerInteraction, butonun "aktif" gorunmesi gerekip gerekmedigini sormak icin kullanir.</summary>
    public bool EkranKapaliMi()
    {
        return pcAnaEkran != null && !pcAnaEkran.activeSelf;
    }

    /// <summary>TEST BUTONU: mesai suresini 1 saniyeye dusurur. Kapali test surumunde
    /// testerlarin da beklemeden test edebilmesi icin BILEREK gercek build'de de calisiyor —
    /// halka acik surume gecmeden once bu metodu ve marketteki butonu kaldirmayi unutma.</summary>
    public void DEBUG_MesaiyiHizlandir()
    {
        if (mesaiKilidiAcik) return;

        mesaiSayaci = Mathf.Max(mesaiSayaci, mesaiIcinGerekenSure - 1f);
        Debug.Log("[TEST] Mesai suresi 1 saniyeye dusuruldu (test butonu).");
    }

    // ── Oturum kaydi koprusu ──
    public float MesaiSayaciniAl() { return mesaiSayaci; }

    public void MesaiSayaciniGeriYukle(float deger)
    {
        mesaiSayaci = deger;

        // Sayac zaten dolmussa kilidi de acik duruma getir (buton "Geceye Gec" olsun)
        if (mesaiSayaci >= mesaiIcinGerekenSure)
        {
            mesaiKilidiAcik = true;
            if (mesaiBitirButonu != null) mesaiBitirButonu.interactable = true;
            if (mesaiBitirButonuYazisi != null) mesaiBitirButonuYazisi.text = "Geceye Geç";
        }
    }

    public void GeceyiBaslatButonu()
    {
        Debug.Log("[KasaEtkilesim] GeceyiBaslatButonu() cagrildi. mesaiKilidiAcik=" + mesaiKilidiAcik +
                   ", GameManager.Instance=" + (GameManager.Instance != null ? "VAR" : "NULL") +
                   ", bu objenin adi=" + gameObject.name +
                   ", CurrentPhase=" + (GameManager.Instance != null ? GameManager.Instance.CurrentPhase.ToString() : "?") +
                   ", CurrentDay=" + (GameManager.Instance != null ? GameManager.Instance.CurrentDay.ToString() : "?"));

        if (mesaiKilidiAcik && GameManager.Instance != null)
        {
            bool basladi = GameManager.Instance.RequestGoToNight();
            Debug.Log("[KasaEtkilesim] RequestGoToNight() sonucu: " + basladi);
            BilgisayariAcKapat();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            oyuncuYakin = true;
            // Artik PlayerInteraction'in genel zincirine kaydolmuyoruz — cakisma olmasin diye
            // sadece kendi adanmis butonumuzu gosteriyoruz.
            if (pcButonu != null) pcButonu.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            oyuncuYakin = false;
            if (pcButonu != null) pcButonu.SetActive(false);
        }
    }
}