// GeciciHikayeKonuguYoneticisi.cs — Var olan DUZENLI musterilerden BELIRLI birinin, GECICI
// olarak "hikaye konugu"na donustugu gunleri yonetir. AYNI ANDA EN FAZLA spawnNoktalari kadar
// (onerilen: 3) konuk bulunabilir.
//
// HER GUN BASINDA (HandleDayChanged):
//   1) Onceki gunden kalan TUM konuklar yok edilir ("gun sonu yokolur").
//   2) Her hikaye icin: HENUZ BASLAMAMISSA bugun baslayip baslamadigina bakilir; DEVAM
//      EDIYORSA (daha once bir secimle bir sonraki segment belirlenmisse) otomatik devam eder.
//   3) Uygun olan hikayeler, bos spawn noktalarina (en fazla nokta sayisi kadar) yerlestirilir.
//
// Bir konukla o GUN konusulmazsa, ertesi gun AYNI segment tekrar sunulur — hicbir sey
// ilerlemeden kaybolmaz, sadece oyuncu firsati kacirmis olur.
//
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System.Collections.Generic;

public class GeciciHikayeKonuguYoneticisi : MonoBehaviour
{
    public static GeciciHikayeKonuguYoneticisi Instance { get; private set; }

    [Header("Hikayeler")]
    [SerializeField] private List<OzelMusteriHikayesiVerisi> hikayeler = new List<OzelMusteriHikayesiVerisi>();

    [Header("Spawn Noktalari (ayni anda EN FAZLA bu kadar konuk olabilir)")]
    [SerializeField] private List<Transform> spawnNoktalari = new List<Transform>();

    // hikayeId -> o an/su an oynanmasi gereken segment id (hikaye HENUZ baslamamissa haritada YOK)
    private readonly Dictionary<string, string> aktifSegment = new Dictionary<string, string>();

    // Tamamlanmis hikayelerin id'leri (kayittan gelir/kayda gider)
    private readonly HashSet<string> tamamlananHikayeler = new HashSet<string>();

    // Bugun spawn edilmis obje referanslari — gece hepsi yok edilir.
    private readonly List<GameObject> buGunkuKonuklar = new List<GameObject>();

    // Su an fiziksel olarak konuk olan musteri isimleri — CustomerController normal
    // musteri secerken bu isimleri havuzdan cikarir.
    private readonly HashSet<string> suAnKonukIsimleri = new HashSet<string>();

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

    /// <summary>CustomerController, normal musteri secerken bu isimdeki kisinin su an
    /// baska bir yerde (spawn noktasinda) konuk olarak bekleyip beklemedigini sorar.</summary>
    public bool SuAnKonukMu(string musteriIsmi)
    {
        return !string.IsNullOrEmpty(musteriIsmi) && suAnKonukIsimleri.Contains(musteriIsmi);
    }

    private void HandleDayChanged(int gun)
    {
        // ONCEKI GUNDEN KALANLARI TEMIZLE — "gun sonu yokolur".
        for (int i = 0; i < buGunkuKonuklar.Count; i++)
            if (buGunkuKonuklar[i] != null) Destroy(buGunkuKonuklar[i]);
        buGunkuKonuklar.Clear();
        suAnKonukIsimleri.Clear();

        if (hikayeler == null || spawnNoktalari == null || spawnNoktalari.Count == 0) return;

        int kullanilanNokta = 0;

        for (int i = 0; i < hikayeler.Count && kullanilanNokta < spawnNoktalari.Count; i++)
        {
            OzelMusteriHikayesiVerisi hikaye = hikayeler[i];
            if (hikaye == null || string.IsNullOrEmpty(hikaye.hikayeId)) continue;
            if (tamamlananHikayeler.Contains(hikaye.hikayeId)) continue;

            string segmentId;
            bool devamEdiyor = aktifSegment.TryGetValue(hikaye.hikayeId, out segmentId);

            if (!devamEdiyor)
            {
                // HENUZ BASLAMAMIS — bugun baslayip baslamadigina bak.
                if (!hikaye.BaslarMi(gun)) continue;
                segmentId = hikaye.ilkSegmentId;
            }

            HikayeGunSegmenti segment = SegmentiBul(hikaye, segmentId);
            if (segment == null) continue;

            Transform nokta = spawnNoktalari[kullanilanNokta];
            kullanilanNokta++;

            KonukSpawnEt(hikaye, segment, nokta);
        }
    }

    private HikayeGunSegmenti SegmentiBul(OzelMusteriHikayesiVerisi hikaye, string id)
    {
        if (hikaye.segmentler == null) return null;
        for (int i = 0; i < hikaye.segmentler.Count; i++)
            if (hikaye.segmentler[i] != null && hikaye.segmentler[i].id == id) return hikaye.segmentler[i];
        return null;
    }

    private void KonukSpawnEt(OzelMusteriHikayesiVerisi hikaye, HikayeGunSegmenti segment, Transform nokta)
    {
        if (hikaye.konukPrefab == null)
        {
            DevLog.Warning("[GeciciHikayeKonugu] '" + hikaye.hikayeId + "' spawn edilemedi — konukPrefab atanmamis.");
            return;
        }

        GameObject obj = Instantiate(hikaye.konukPrefab, nokta.position, Quaternion.identity);
        buGunkuKonuklar.Add(obj);
        suAnKonukIsimleri.Add(hikaye.hedefMusteriIsmi);

        GeciciHikayeKonuguController controller = obj.GetComponent<GeciciHikayeKonuguController>();
        if (controller == null)
        {
            DevLog.Warning("[GeciciHikayeKonugu] '" + hikaye.hikayeId + "' prefabinda GeciciHikayeKonuguController YOK.");
            return;
        }

        controller.Initialize(hikaye, segment);
        DevLog.Info("✨ [GECICI KONUK] " + hikaye.hedefMusteriIsmi + " bugunluk konuk oldu.");
    }

    /// <summary>GeciciHikayeKonuguController, oyuncu bir secenek sectiginde cagirir.</summary>
    public void SecimYapildi(OzelMusteriHikayesiVerisi hikaye, OzelHikayeSecenegi secilen)
    {
        if (string.IsNullOrEmpty(secilen.hedefSegmentId))
        {
            // HIKAYE BITTI — yarindan itibaren normal musteri.
            tamamlananHikayeler.Add(hikaye.hikayeId);
            aktifSegment.Remove(hikaye.hikayeId);
        }
        else
        {
            aktifSegment[hikaye.hikayeId] = secilen.hedefSegmentId;
        }
    }

    // ── Kayit koprusu ──

    public void ApplySave(SaveData data)
    {
        tamamlananHikayeler.Clear();
        if (data.tamamlananOzelHikayeler != null)
            for (int i = 0; i < data.tamamlananOzelHikayeler.Count; i++)
                tamamlananHikayeler.Add(data.tamamlananOzelHikayeler[i]);

        aktifSegment.Clear();
        if (data.aktifOzelHikayeIlerlemeleri != null)
        {
            for (int i = 0; i < data.aktifOzelHikayeIlerlemeleri.Count; i++)
            {
                HikayeIlerlemeKaydi kayit = data.aktifOzelHikayeIlerlemeleri[i];
                if (kayit != null && !string.IsNullOrEmpty(kayit.hikayeId))
                    aktifSegment[kayit.hikayeId] = kayit.aktifSegmentId;
            }
        }
    }

    public void WriteToSave(SaveData data)
    {
        data.tamamlananOzelHikayeler.Clear();
        foreach (string id in tamamlananHikayeler) data.tamamlananOzelHikayeler.Add(id);

        data.aktifOzelHikayeIlerlemeleri.Clear();
        foreach (KeyValuePair<string, string> kv in aktifSegment)
        {
            HikayeIlerlemeKaydi kayit = new HikayeIlerlemeKaydi();
            kayit.hikayeId = kv.Key;
            kayit.aktifSegmentId = kv.Value;
            data.aktifOzelHikayeIlerlemeleri.Add(kayit);
        }
    }
}
