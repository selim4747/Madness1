// BahceYoneticisi.cs — Kutuphane bahcesindeki musteri masalarini ve Afor'un kendi masasini
// yonetir. ARTIK kim hangi masada oturuyor da TAKIP EDIYOR (once sadece "dolu/bos" biliniyordu) —
// bu, oturan musteriler arasinda RASTGELE ortam sohbeti (ambient chatter) icin gerekli.
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System.Collections.Generic;

public class BahceYoneticisi : MonoBehaviour
{
    public static BahceYoneticisi Instance { get; private set; }

    [Header("Musteri bahce masalari (oturma noktalari)")]
    [Tooltip("Musterilerin %10 ihtimalle oturabilecegi masalar. Bos birakilirsa bahce sosyallesmesi hic tetiklenmez.")]
    [SerializeField] private Transform[] musteriMasalari;

    [Tooltip("Bir musterinin bahce masasinda ne kadar kalacagi — HER musteri icin bu araliktan RASTGELE secilir (sabit deger degil, oturusta bir kez belirlenir).")]
    [SerializeField] private float bahcedeKalmaSuresiMin = 25f;
    [SerializeField] private float bahcedeKalmaSuresiMax = 70f;

    /// <summary>Yeni oturan bir musteri icin rastgele bir kalma suresi uretir.</summary>
    public float RastgeleKalmaSuresi()
    {
        return Random.Range(bahcedeKalmaSuresiMin, bahcedeKalmaSuresiMax);
    }

    [Header("Bahceye gitme ihtimali")]
    [Range(0f, 1f)]
    [SerializeField] private float bahceIhtimali = 0.10f;
    public float BahceIhtimali { get { return bahceIhtimali; } }

    [Header("Afor'un kendi masasi (musteri masalarindan AYRI)")]
    [SerializeField] private Transform aforMasasi;
    public Transform AforMasasi { get { return aforMasasi; } }
    private bool aforMasasiDolu = false;

    [Header("Ortam Sohbeti (Ambient Chatter)")]
    [Tooltip("Masadaki musteriler arasindan RASTGELE biri, oyuncu yakininda olmasa bile, kendiliginden konussun mu?")]
    [SerializeField] private bool ortamSohbetiAcik = true;
    [Tooltip("Iki ortam-sohbeti arasi minimum/maksimum bekleme suresi (saniye).")]
    [SerializeField] private float ortamSohbetiMinAralik = 12f;
    [SerializeField] private float ortamSohbetiMaxAralik = 28f;

    // Masa -> orada oturan CustomerController. HashSet yerine Dictionary'e gecildi ki
    // "kim oturuyor" bilgisi de tutulabilsin (ambient chatter icin sart).
    private readonly Dictionary<Transform, CustomerController> oturanlar = new Dictionary<Transform, CustomerController>();

    private float ortamSohbetiSayaci;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        ortamSohbetiSayaciSifirla();
    }

    private void Start()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnPhaseChanged += HandlePhaseChanged;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
    }

    /// <summary>Sadece GUNDUZ (Day) fazindayken yeni oturma izni verilir — mesai bitince
    /// (DayToNight ve sonrasi) kimse artik bahce masasina oturamaz.</summary>
    public bool BahceyeOturulabilirMi()
    {
        return GameManager.Instance == null || GameManager.Instance.CurrentPhase == GamePhase.Day;
    }

    private void HandlePhaseChanged(GamePhase yeniFaz)
    {
        // Mesai bitince (gunes batisi basladiginda) ZATEN oturanlar da kaldirilir.
        if (yeniFaz == GamePhase.DayToNight) HerkesiKaldir();
    }

    /// <summary>Su an oturan TUM musterileri zorla kaldirir (mesai sonu). Dictionary
    /// degistirilecegi icin ONCE bir kopyasini alir.</summary>
    private void HerkesiKaldir()
    {
        if (oturanlar.Count == 0) return;

        List<CustomerController> hepsi = new List<CustomerController>(oturanlar.Values);
        foreach (CustomerController musteri in hepsi)
        {
            if (musteri != null) musteri.BahceyiTerkEt();
        }
    }

    private void Update()
    {
        if (!ortamSohbetiAcik) return;

        ortamSohbetiSayaci -= Time.deltaTime;
        if (ortamSohbetiSayaci <= 0f)
        {
            OrtamSohbetiDene();
            ortamSohbetiSayaciSifirla();
        }
    }

    private void ortamSohbetiSayaciSifirla()
    {
        ortamSohbetiSayaci = Random.Range(ortamSohbetiMinAralik, ortamSohbetiMaxAralik);
    }

    /// <summary>Su an oturan musterilerden RASTGELE birini secip, oyuncunun yakininda olup
    /// olmadigina BAKMADAN, kisa sureli bir konusma balonu gosterir.</summary>
    private void OrtamSohbetiDene()
    {
        if (oturanlar.Count == 0 || BahceSohbetYoneticisi.Instance == null) return;

        List<CustomerController> aktifMusteriler = new List<CustomerController>(oturanlar.Values);
        CustomerController secilen = aktifMusteriler[Random.Range(0, aktifMusteriler.Count)];
        if (secilen == null || secilen.SecilenKimlik == null) return;

        string laf = BahceSohbetYoneticisi.Instance.OrtamIcinRastgeleCumle(secilen, secilen.SecilenKimlik);
        if (string.IsNullOrEmpty(laf)) return;

        FloatingText.Spawn(secilen.transform.position + Vector3.up * 0.6f, laf, FloatingText.Renk.Notr);
    }

    /// <summary>Bos bir musteri masasi bulup rezerve eder. sahip: kim oturuyor (ambient
    /// sohbet icin takip edilir). Bulamazsa null doner.</summary>
    public Transform MasaAyir(CustomerController sahip)
    {
        if (musteriMasalari == null) return null;

        for (int i = 0; i < musteriMasalari.Length; i++)
        {
            Transform masa = musteriMasalari[i];
            if (masa != null && !oturanlar.ContainsKey(masa))
            {
                oturanlar[masa] = sahip;
                return masa;
            }
        }

        return null; // hepsi dolu
    }

    public void MasayiBirak(Transform masa)
    {
        if (masa != null) oturanlar.Remove(masa);
    }

    /// <summary>Verilen masada SU AN kim oturuyor? Kimse yoksa null doner. Masa-bazli
    /// BahceSohbetRadari bunu kullanir — hangi musterinin konusacagini boyle bulur.</summary>
    public CustomerController GetOturan(Transform masa)
    {
        if (masa == null) return null;
        CustomerController sonuc;
        return oturanlar.TryGetValue(masa, out sonuc) ? sonuc : null;
    }

    /// <summary>Afor'un masasini rezerve etmeye calisir.</summary>
    public bool AforMasasiniAyir()
    {
        if (aforMasasi == null || aforMasasiDolu) return false;
        aforMasasiDolu = true;
        return true;
    }

    public void AforMasasiniBirak()
    {
        aforMasasiDolu = false;
    }
}