// SatinAlmaEnflasyonYoneticisi.cs — EKONOMİ SIKILAŞTIRMA: altın üretimine hiç dokunmaz,
// sadece harcama tarafını sıkılaştırır. Aynı gün içinde art arda "büyük" satın alma
// yapıldıkça (raf açma, dükkan açma, karakter/personel yükseltmesi) fiyatlara geçici bir
// "enflasyon çarpanı" biner — parayı tek günde boşaltmak pahalı, günlere yaymak ucuz olur.
// Çarpan her yeni günde kendiliğinden bir miktar düşer. Stok yenileme gibi küçük/operasyonel
// harcamalar bu vergiden MUAF tutulur (sadece yatırım niteliğindeki büyük harcamalar etkilenir).
//
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;

public class SatinAlmaEnflasyonYoneticisi : MonoBehaviour
{
    public static SatinAlmaEnflasyonYoneticisi Instance { get; private set; }

    [Header("Enflasyon Ayarları")]
    [Tooltip("Her buyuk satin almada carpana eklenen miktar (0.15 = fiyat bir sonraki satin almada %15 daha pahali olur).")]
    [SerializeField] private float satinAlmaBasinaArtis = 0.15f;

    [Tooltip("Carpanin cikabilecegi en yuksek deger (2.5 = fiyatlar en fazla %150 artar).")]
    [SerializeField] private float maksimumCarpan = 2.5f;

    [Tooltip("Her yeni gunde carpandan dusen miktar (1.0'a kadar duser, altina inmez).")]
    [SerializeField] private float gunlukAzalma = 0.4f;

    private float carpan = 1f;

    /// <summary>Su anki enflasyon carpani — UI'da "fiyatlar suan %X pahali" gibi bir gosterge icin de kullanilabilir.</summary>
    public float GuncelCarpan { get { return carpan; } }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnDayChanged += HandleDayChanged;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnDayChanged -= HandleDayChanged;
    }

    private void HandleDayChanged(int gun)
    {
        carpan = Mathf.Max(1f, carpan - gunlukAzalma);
    }

    /// <summary>Ham fiyata guncel enflasyon carpanini uygular. Fiyati GOSTEREN her yer (UI) ile
    /// fiyati HARCAYAN yer (satin alma metodu) MUTLAKA bu metottan gecmeli — ikisi arasinda
    /// asla tutarsizlik olmasin (oyuncu ekranda gordugunden farkli para odemesin).
    /// hamFiyat negatifse (-1 = "maksimum seviye" isareti gibi) dokunulmadan geri doner.</summary>
    public int UygulaVergi(int hamFiyat)
    {
        if (hamFiyat < 0) return hamFiyat;
        return Mathf.RoundToInt(hamFiyat * carpan);
    }

    /// <summary>Buyuk bir satin alma BASARIYLA tamamlandiktan SONRA cagrilir — bir sonraki
    /// satin almanin fiyatini yukseltir. Kendi fiyatini etkilemez, sadece bundan sonrakini.
    /// Stok yenileme gibi kucuk/operasyonel harcamalar icin CAGRILMAMALI.</summary>
    public void SatinAlmaKaydet()
    {
        carpan = Mathf.Min(maksimumCarpan, carpan + satinAlmaBasinaArtis);
    }

    // ── Kayit koprusu ──

    public void ApplySave(SaveData data)
    {
        carpan = Mathf.Max(1f, data.ekonomiEnflasyonCarpani);
    }

    public void WriteToSave(SaveData data)
    {
        data.ekonomiEnflasyonCarpani = carpan;
    }
}
