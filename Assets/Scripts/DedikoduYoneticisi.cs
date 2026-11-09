// DedikoduYoneticisi.cs — Dedikodu ZINCIRI'ni yonetir: hangi dedikodular OGRENILDI, hangileri
// HEDEF kisiye PAYLASILDI. MusteriSohbetRadari (magaza) VE BahceSohbetRadari (bahce) — HER
// IKISI DE — kaynak kisiyle konusulunca ogrenme, hedef kisiyle konusulunca paylasim secenegi
// sunmak icin bu yoneticiyi kullanir.
//
// PAYLASIM 3 SEKILDE ETKI EDER (hepsi birlikte calisir):
//   1) Iliski puani  — kaynakIliskiEtkisi (ogrenince) + hedefIliskiEtkisi (paylasinca)
//   2) Kalici diyalog — paylasimSonrasiDiyaloglar, hedefin normal sohbet havuzuna KALICI eklenir
//   3) Bahce dallanmasi — baglananBahceCumleId doluysa, BahceSohbetRadari o dala ATLAR
//      (bu kisim sadece bahcede calisir, magaza sohbetinde sadece 1+2 gecerlidir).
//
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System.Collections.Generic;

public class DedikoduYoneticisi : MonoBehaviour
{
    public static DedikoduYoneticisi Instance { get; private set; }

    [Header("Tüm Dedikodu Verileri")]
    [SerializeField] private List<DedikoduData> tumDedikodular = new List<DedikoduData>();

    private readonly HashSet<string> ogrenilenler = new HashSet<string>();
    private readonly HashSet<string> paylasilanlar = new HashSet<string>();

    // hedefIsmi -> paylasimSonrasiDiyaloglar'dan kalici olarak acilan satirlar
    private readonly Dictionary<string, List<string>> acilanDiyaloglar = new Dictionary<string, List<string>>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private bool GecerliMi(DedikoduData d)
    {
        return d != null && !string.IsNullOrEmpty(d.dedikoduId);
    }

    /// <summary>Bu KAYNAK kisiden henuz ogrenilmemis bir dedikodu var mi? Varsa ilkini doner.</summary>
    public DedikoduData OgrenilebilecekDedikoduBul(string kaynakIsim)
    {
        if (string.IsNullOrEmpty(kaynakIsim)) return null;

        for (int i = 0; i < tumDedikodular.Count; i++)
        {
            DedikoduData d = tumDedikodular[i];
            if (GecerliMi(d) && d.kaynakIsim == kaynakIsim && !ogrenilenler.Contains(d.dedikoduId))
                return d;
        }
        return null;
    }

    /// <summary>Radar, kaynak kisiden dedikoduyu GOSTERDIKTEN sonra cagirir — ogrenilmis
    /// isaretlenir ve kaynakla iliski puanina etki eder.</summary>
    public void DedikoduOgrenildi(DedikoduData d)
    {
        if (!GecerliMi(d) || ogrenilenler.Contains(d.dedikoduId)) return;

        ogrenilenler.Add(d.dedikoduId);
        DevLog.Info($"🗣️ [DEDİKODU] Öğrenildi: '{d.dedikoduId}' ({d.kaynakIsim} → {d.hedefIsim} hakkında)");

        if (d.kaynakIliskiEtkisi != 0 && SadikMusteriYoneticisi.Instance != null)
            SadikMusteriYoneticisi.Instance.DisaridanPuanEkle(d.kaynakIsim, d.kaynakIliskiEtkisi);
    }

    /// <summary>Su an HEDEF kisiyle paylasilabilecek (ogrenilmis ama henuz paylasilmamis)
    /// dedikodulari doner. Bos liste = paylasilacak bir sey yok.</summary>
    public List<DedikoduData> PaylasilabilecekDedikodularBul(string hedefIsim)
    {
        List<DedikoduData> sonuc = new List<DedikoduData>();
        if (string.IsNullOrEmpty(hedefIsim)) return sonuc;

        for (int i = 0; i < tumDedikodular.Count; i++)
        {
            DedikoduData d = tumDedikodular[i];
            if (GecerliMi(d) && d.hedefIsim == hedefIsim &&
                ogrenilenler.Contains(d.dedikoduId) && !paylasilanlar.Contains(d.dedikoduId))
                sonuc.Add(d);
        }
        return sonuc;
    }

    /// <summary>Radar, oyuncu "paylas" secenegini secince cagirir — paylasilmis isaretlenir,
    /// hedefle iliski puanina etki eder, varsa kalici diyaloglari acar. Bahce dallanmasi
    /// (baglananBahceCumleId) RADAR seviyesinde ayrica islenir (bu metot ona dokunmaz).</summary>
    public void DedikoduPaylasildi(DedikoduData d)
    {
        if (!GecerliMi(d) || paylasilanlar.Contains(d.dedikoduId)) return;

        paylasilanlar.Add(d.dedikoduId);
        DevLog.Info($"💬 [DEDİKODU] Paylaşıldı: '{d.dedikoduId}' → {d.hedefIsim}");

        if (d.hedefIliskiEtkisi != 0 && SadikMusteriYoneticisi.Instance != null)
            SadikMusteriYoneticisi.Instance.DisaridanPuanEkle(d.hedefIsim, d.hedefIliskiEtkisi);

        if (d.paylasimSonrasiDiyaloglar != null && d.paylasimSonrasiDiyaloglar.Count > 0)
        {
            if (!acilanDiyaloglar.ContainsKey(d.hedefIsim))
                acilanDiyaloglar[d.hedefIsim] = new List<string>();
            acilanDiyaloglar[d.hedefIsim].AddRange(d.paylasimSonrasiDiyaloglar);
        }
    }

    /// <summary>MusteriSohbetRadari, normal havuzu olustururken bu isimde ek satir var mi diye
    /// sorar. Yoksa (ya da hic acilmamissa) null doner.</summary>
    public List<string> EkDiyaloglariAl(string musteriIsmi)
    {
        List<string> liste;
        if (!string.IsNullOrEmpty(musteriIsmi) && acilanDiyaloglar.TryGetValue(musteriIsmi, out liste)) return liste;
        return null;
    }

    // ── Kayit köprüsü ──

    public void ApplySave(SaveData data)
    {
        ogrenilenler.Clear();
        paylasilanlar.Clear();
        acilanDiyaloglar.Clear();

        if (data.dedikoduKayitlari == null) return;

        for (int i = 0; i < data.dedikoduKayitlari.Count; i++)
        {
            DedikoduKaydi kayit = data.dedikoduKayitlari[i];
            if (kayit == null || string.IsNullOrEmpty(kayit.dedikoduId)) continue;

            if (kayit.ogrenildi) ogrenilenler.Add(kayit.dedikoduId);
            if (kayit.paylasildi)
            {
                paylasilanlar.Add(kayit.dedikoduId);

                // Kalici diyaloglari yeniden kurmak icin, ilgili DedikoduData'yi tekrar bul.
                DedikoduData d = BulById(kayit.dedikoduId);
                if (d != null && d.paylasimSonrasiDiyaloglar != null && d.paylasimSonrasiDiyaloglar.Count > 0)
                {
                    if (!acilanDiyaloglar.ContainsKey(d.hedefIsim))
                        acilanDiyaloglar[d.hedefIsim] = new List<string>();
                    acilanDiyaloglar[d.hedefIsim].AddRange(d.paylasimSonrasiDiyaloglar);
                }
            }
        }
    }

    private DedikoduData BulById(string id)
    {
        for (int i = 0; i < tumDedikodular.Count; i++)
            if (GecerliMi(tumDedikodular[i]) && tumDedikodular[i].dedikoduId == id) return tumDedikodular[i];
        return null;
    }

    public void WriteToSave(SaveData data)
    {
        data.dedikoduKayitlari.Clear();

        // Ogrenilen VEYA paylasilan HERSEYI tek bir kayit listesinde topla.
        HashSet<string> hepsi = new HashSet<string>(ogrenilenler);
        hepsi.UnionWith(paylasilanlar);

        foreach (string id in hepsi)
        {
            DedikoduKaydi kayit = new DedikoduKaydi();
            kayit.dedikoduId = id;
            kayit.ogrenildi = ogrenilenler.Contains(id);
            kayit.paylasildi = paylasilanlar.Contains(id);
            data.dedikoduKayitlari.Add(kayit);
        }
    }
}