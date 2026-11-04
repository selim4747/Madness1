using System.Collections.Generic;
using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    public static CustomerSpawner Instance;

    [Header("Müşteri Tipleri Data")]
    public CustomerData normalCustomerData;
    public CustomerData zenginCustomerData;
    public CustomerData vipCustomerData;
    public CustomerData ogrenciCustomerData; // 2. Duzey ogrenci — musteri gibi gelir

    [Header("Prefablar")]
    public GameObject normalPrefab;
    public GameObject zenginPrefab;
    public GameObject vipPrefab;
    public GameObject ogrenciPrefab;

    [Header("2. Duzey Ogrenci Ihtimali")]
    [Tooltip("Musteri uretilirken yuzde kac ihtimalle 2. Duzey ogrenci gelsin (Normal/Zengin/VIP oranlarindan BAGIMSIZ, ayri bir zar atilir).")]
    [Range(0f, 50f)]
    public float ogrenciSpawnSansi = 15f;

    [Header("Noktalar")]
    public Transform kapiNoktasi;
    public Transform kasaNoktasi;
    public List<Transform> raflar = new List<Transform>();

    [Header("Spawn Ayarları")]
    public float spawnAriligi = 5f;
    public int ayniAndaMaxMusteri = 6;

    [Header("Dükkandaki Müşteriler")]
    public List<CustomerController> aktifMusteriler = new List<CustomerController>();

    private float spawnZamanSayaci = 0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnDayChanged += HandleNewDay;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnDayChanged -= HandleNewDay;
    }

    private void HandleNewDay(int day)
    {
        // Her yeni gunde "kimle kac kere konustuk" sayaci sifirlanir.
        MusteriKonusmaSinirlayici.GunuSifirla();
    }

    void Update()
    {
        // Sadece GUNDUZ musteri uretilir — gece kontrolu eksikti, gece de musteri
        // spawn olmaya devam ediyordu.
        if (GameManager.Instance == null || GameManager.Instance.CurrentPhase != GamePhase.Day) return;

        spawnZamanSayaci += Time.deltaTime;
        if (spawnZamanSayaci >= spawnAriligi)
        {
            spawnZamanSayaci = 0f;

            if (aktifMusteriler.Count < ayniAndaMaxMusteri)
            {
                MusteriUret();
            }
        }
    }

    void MusteriUret()
    {
        if (kapiNoktasi == null || kasaNoktasi == null || raflar.Count == 0) return;

        // 2. Duzey ogrenci ayri, bagimsiz bir zarla belirlenir — cikarsa dogrudan
        // ogrenci uretilir, cikmazsa normal Normal/Zengin/VIP secimine devam edilir.
        CustomerData secilenData;
        GameObject uretilecekPrefab;

        if (ogrenciCustomerData != null && ogrenciPrefab != null &&
            UnityEngine.Random.Range(0f, 100f) < ogrenciSpawnSansi)
        {
            secilenData = ogrenciCustomerData;
            uretilecekPrefab = ogrenciPrefab;
        }
        else
        {
            secilenData = SansaGoreTipSec();
            if (secilenData == null) return;

            uretilecekPrefab = normalPrefab;
            if (secilenData == zenginCustomerData) uretilecekPrefab = zenginPrefab;
            else if (secilenData == vipCustomerData) uretilecekPrefab = vipPrefab;
        }

        if (uretilecekPrefab == null) return;

        // Sadece aktif (açık) olan rafları filtrele
        List<Transform> aktifRaflar = new List<Transform>();
        foreach (var raf in raflar)
        {
            if (raf != null && raf.gameObject.activeInHierarchy)
            {
                aktifRaflar.Add(raf);
            }
        }

        if (aktifRaflar.Count == 0) return;

        Transform secilenRaf = aktifRaflar[UnityEngine.Random.Range(0, aktifRaflar.Count)];

        GameObject yeniMusteri = Instantiate(uretilecekPrefab, kapiNoktasi.position, Quaternion.identity);
        CustomerController controller = yeniMusteri.GetComponent<CustomerController>();

        if (controller != null)
        {
            controller.Baslat(kapiNoktasi, kasaNoktasi, secilenRaf, this, secilenData);
            aktifMusteriler.Add(controller);
        }
    }

    CustomerData SansaGoreTipSec()
    {
        float rnd = UnityEngine.Random.Range(0f, 100f);
        if (rnd < 60f) return normalCustomerData;
        else if (rnd < 90f) return zenginCustomerData;
        else return vipCustomerData;
    }

    public void MusteriAyrildi(CustomerController musteri)
    {
        if (aktifMusteriler.Contains(musteri))
        {
            aktifMusteriler.Remove(musteri);
        }
    }

    /// <summary>Gun yeniden baslarken (olum/manuel restart) cagrilir. Sahnede kalmis TUM
    /// musterileri yok eder ve listeyi sifirlar — yoksa "hayalet" musteriler listede takili
    /// kalip yeni musteri spawn olmasini sonsuza kadar engelliyordu.</summary>
    public void ClearAllCustomers()
    {
        for (int i = 0; i < aktifMusteriler.Count; i++)
        {
            if (aktifMusteriler[i] != null) Destroy(aktifMusteriler[i].gameObject);
        }

        aktifMusteriler.Clear();
        spawnZamanSayaci = 0f;
    }

    // ==========================================
    // DİĞER SCRİPTLERİN ÇAĞIRDIĞI KÖPRÜ METOTLAR
    // ==========================================

    // Personel Yükseltme Scripti İçin:
    public void SetSimultaneousLimit(int yeniLimit)
    {
        ayniAndaMaxMusteri = yeniLimit;
    }

    // Reklam / Ödül Yöneticisi İçin:
    public void AddCapacityBonus(int bonusMiktari)
    {
        ayniAndaMaxMusteri += bonusMiktari;
    }

    /// <summary>TEST: musteri kapasitesini VE gelis hizini stres testi icin artirir.</summary>
    public void DEBUG_MusteriSayisiniArtir()
    {
        ayniAndaMaxMusteri += 5;
        spawnAriligi = Mathf.Max(0.5f, spawnAriligi - 1f);
        Debug.Log("[TEST] Musteri kapasitesi: " + ayniAndaMaxMusteri + ", spawn araligi: " + spawnAriligi + "sn");
    }

    // ==========================================
    // OTURUM KAYDI
    // ==========================================

    public List<CustomerSaveEntry> TumMusterileriKaydet()
    {
        List<CustomerSaveEntry> liste = new List<CustomerSaveEntry>();

        for (int i = 0; i < aktifMusteriler.Count; i++)
        {
            CustomerController m = aktifMusteriler[i];
            if (m == null) continue;

            int tip = 0;
            if (m.Profile == zenginCustomerData) tip = 1;
            else if (m.Profile == vipCustomerData) tip = 2;
            else if (m.Profile == ogrenciCustomerData) tip = 3;

            liste.Add(m.DurumuKaydet(tip));
        }

        return liste;
    }

    public void TumMusterileriGeriYukle(List<CustomerSaveEntry> liste)
    {
        ClearAllCustomers();
        if (liste == null) return;

        for (int i = 0; i < liste.Count; i++)
        {
            CustomerSaveEntry e = liste[i];

            GameObject prefab = normalPrefab;
            CustomerData profil = normalCustomerData;
            if (e.prefabTipi == 1) { prefab = zenginPrefab; profil = zenginCustomerData; }
            else if (e.prefabTipi == 2) { prefab = vipPrefab; profil = vipCustomerData; }
            else if (e.prefabTipi == 3) { prefab = ogrenciPrefab; profil = ogrenciCustomerData; }

            if (prefab == null) continue;

            GameObject go = Instantiate(prefab, new Vector3(e.x, e.y, 0f), Quaternion.identity);
            CustomerController m = go.GetComponent<CustomerController>();
            if (m == null) { Destroy(go); continue; }

            aktifMusteriler.Add(m);
            m.DurumuGeriYukle(e, profil, kapiNoktasi);
        }
    }
}