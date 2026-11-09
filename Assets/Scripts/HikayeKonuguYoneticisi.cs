// HikayeKonuguYoneticisi.cs — Hikaye konuklarini ozel gunlerde bahce kosesinde spawn eder,
// tanisilanlari kaydeder ve (istenmisse) duzenli musteri havuzuna katar.
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System.Collections.Generic;

public class HikayeKonuguYoneticisi : MonoBehaviour
{
    public static HikayeKonuguYoneticisi Instance { get; private set; }

    [Header("Konuk Listesi")]
    [SerializeField] private List<HikayeKonuguData> konuklar = new List<HikayeKonuguData>();

    [Header("Spawn Noktasi")]
    [Tooltip("Bahcenin bir kosesi — konuklar burada belirir ve oyuncu gelene kadar bekler.")]
    [SerializeField] private Transform bahceKosesi;

    [Tooltip("Konuk konustuktan sonra buraya dogru gidip kaybolur (genelde kapi noktasi).")]
    [SerializeField] private Transform cikisNoktasi;

    /// <summary>Tanisilmis konuklarin id'leri — kayittan gelir/kayda gider.</summary>
    private static readonly HashSet<string> tanisilanKonuklar = new HashSet<string>();

    /// <summary>Tanisildiktan sonra duzenli musteri havuzuna katilan kimlikler.
    /// CustomerController spawn olurken bu listeyi de havuza dahil eder.</summary>
    private static readonly Dictionary<MusteriTipi, List<NPCKimligi>> ekHavuz =
        new Dictionary<MusteriTipi, List<NPCKimligi>>();

    private GameObject aktifKonukObjesi;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnDayChanged += HandleDayChanged;
        EkHavuzuYenidenKur();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnDayChanged -= HandleDayChanged;
    }

    private void HandleDayChanged(int gun)
    {
        // Onceki gunden kalan konuk varsa temizle
        if (aktifKonukObjesi != null) Destroy(aktifKonukObjesi);

        HikayeKonuguData bugunkuKonuk = BuGunIcinKonukBul(gun);
        if (bugunkuKonuk != null) KonukSpawnEt(bugunkuKonuk);
    }

    private HikayeKonuguData BuGunIcinKonukBul(int gun)
    {
        if (konuklar == null) return null;

        List<HikayeKonuguData> uygunlar = new List<HikayeKonuguData>();
        for (int i = 0; i < konuklar.Count; i++)
        {
            if (konuklar[i] != null && konuklar[i].BuGunGelirMi(gun)) uygunlar.Add(konuklar[i]);
        }

        if (uygunlar.Count == 0) return null;
        return uygunlar[Random.Range(0, uygunlar.Count)];
    }

    private void KonukSpawnEt(HikayeKonuguData konuk)
    {
        if (konuk.konukPrefab == null || bahceKosesi == null)
        {
            DevLog.Warning("[HikayeKonugu] '" + konuk.isim + "' spawn edilemedi — prefab ya da bahce kosesi atanmamis.");
            return;
        }

        aktifKonukObjesi = Instantiate(konuk.konukPrefab, bahceKosesi.position, Quaternion.identity);

        StoryNPCController npc = aktifKonukObjesi.GetComponent<StoryNPCController>();
        if (npc == null)
        {
            DevLog.Warning("[HikayeKonugu] '" + konuk.isim + "' prefabinda StoryNPCController YOK.");
            return;
        }

        npc.InitializeKonuk(konuk, bahceKosesi, cikisNoktasi != null ? cikisNoktasi : bahceKosesi);
        DevLog.Info("✨ [KONUK] " + konuk.isim + " bahce kosesinde belirdi.");
    }

    /// <summary>StoryNPCController, konukla konusma bitince cagirir — konuk artik "tanisilmis"
    /// sayilir ve (ayarliysa) duzenli musteri havuzuna katilir.</summary>
    public void KonukTanisildi(HikayeKonuguData konuk)
    {
        if (konuk == null || tanisilanKonuklar.Contains(konuk.konukId)) return;

        tanisilanKonuklar.Add(konuk.konukId);
        EkHavuzaKat(konuk);
        PlayerProgressStats.AddStoryGuestMet();

        if (ToastNotification.Instance != null)
            ToastNotification.Instance.Show(konuk.isim + " ile tanistin.", 3f);
    }

    public static bool KonukTanisildiMi(string konukId)
    {
        return tanisilanKonuklar.Contains(konukId);
    }

    private void EkHavuzaKat(HikayeKonuguData konuk)
    {
        if (!konuk.tanisincaDuzenliGelir || konuk.duzenliKimlik == null) return;

        if (!ekHavuz.ContainsKey(konuk.katilacagiTip))
            ekHavuz[konuk.katilacagiTip] = new List<NPCKimligi>();

        // Ayni kimlik iki kez eklenmesin
        List<NPCKimligi> liste = ekHavuz[konuk.katilacagiTip];
        for (int i = 0; i < liste.Count; i++)
            if (liste[i] != null && liste[i].musteriIsmi == konuk.duzenliKimlik.musteriIsmi) return;

        liste.Add(konuk.duzenliKimlik);
    }

    /// <summary>Kayit yuklenince cagrilir — tanisilan konuklarin hepsini havuza geri koyar.</summary>
    private void EkHavuzuYenidenKur()
    {
        ekHavuz.Clear();
        if (konuklar == null) return;

        for (int i = 0; i < konuklar.Count; i++)
        {
            if (konuklar[i] != null && tanisilanKonuklar.Contains(konuklar[i].konukId))
                EkHavuzaKat(konuklar[i]);
        }
    }

    /// <summary>CustomerController, karakter secerken bu ek havuzu da dahil eder.</summary>
    public static List<NPCKimligi> EkHavuzuAl(MusteriTipi tip)
    {
        List<NPCKimligi> liste;
        if (ekHavuz.TryGetValue(tip, out liste)) return liste;
        return null;
    }

    // ── Kayit koprusu ──

    public void ApplySave(SaveData data)
    {
        tanisilanKonuklar.Clear();
        if (data.tanisilanKonukIdleri != null)
            for (int i = 0; i < data.tanisilanKonukIdleri.Count; i++)
                tanisilanKonuklar.Add(data.tanisilanKonukIdleri[i]);

        EkHavuzuYenidenKur();
    }

    public void WriteToSave(SaveData data)
    {
        data.tanisilanKonukIdleri.Clear();
        foreach (string id in tanisilanKonuklar) data.tanisilanKonukIdleri.Add(id);
    }
}