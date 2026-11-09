// OzelKitapDiyaloglariYoneticisi.cs — Bir kisi kendi KISIYE OZEL kitabini (KaraKitapOzelData)
// satin aldiktan sonra, o kitaba dair ekstra sohbet satirlarini KALICI olarak acar.
//
// AKIS: CounterController, kisiye ozel bir kitap satildiginda KitapSatildi() cagirir — o
// kitabin kitapSonrasiDiyaloglar listesi bu kisi icin KAYDEDILIR (kopyalanir). Bundan sonra
// MusteriSohbetRadari, bu kisiyle normal sohbet havuzunu olustururken EkDiyaloglariAl() ile
// bu satirlari da havuza katar — kalici olarak, oyun boyunca.
//
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System.Collections.Generic;

public class OzelKitapDiyaloglariYoneticisi : MonoBehaviour
{
    public static OzelKitapDiyaloglariYoneticisi Instance { get; private set; }

    // musteriIsmi -> kitaptan acilan ekstra satirlar
    private readonly Dictionary<string, List<string>> acilanDiyaloglar = new Dictionary<string, List<string>>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>CounterController, kisiye ozel bir kitap SATILDIGINDA cagirir.</summary>
    public void KitapSatildi(KaraKitapOzelData kitap, string aliciIsmi)
    {
        if (kitap == null || string.IsNullOrEmpty(aliciIsmi)) return;
        if (kitap.kitapSonrasiDiyaloglar == null || kitap.kitapSonrasiDiyaloglar.Count == 0) return;
        if (acilanDiyaloglar.ContainsKey(aliciIsmi)) return; // zaten acilmis, tekrar eklenmez

        acilanDiyaloglar[aliciIsmi] = new List<string>(kitap.kitapSonrasiDiyaloglar);
        DevLog.Info("📖 [KİTAP SONRASI] " + aliciIsmi + " için " + kitap.kitapSonrasiDiyaloglar.Count + " yeni diyalog açıldı.");
    }

    /// <summary>MusteriSohbetRadari, normal havuzu olustururken bu isimde ek satir var mi diye sorar.
    /// Yoksa (ya da hic acilmamissa) null doner.</summary>
    public List<string> EkDiyaloglariAl(string musteriIsmi)
    {
        List<string> liste;
        if (!string.IsNullOrEmpty(musteriIsmi) && acilanDiyaloglar.TryGetValue(musteriIsmi, out liste)) return liste;
        return null;
    }

    // ── Kayit koprusu ──

    public void ApplySave(SaveData data)
    {
        acilanDiyaloglar.Clear();
        if (data.acilanOzelKitapDiyaloglari == null) return;

        for (int i = 0; i < data.acilanOzelKitapDiyaloglari.Count; i++)
        {
            OzelKitapDiyalogKaydi kayit = data.acilanOzelKitapDiyaloglari[i];
            if (kayit != null && !string.IsNullOrEmpty(kayit.musteriIsmi))
                acilanDiyaloglar[kayit.musteriIsmi] = new List<string>(kayit.acilanDiyaloglar ?? new List<string>());
        }
    }

    public void WriteToSave(SaveData data)
    {
        data.acilanOzelKitapDiyaloglari.Clear();
        foreach (KeyValuePair<string, List<string>> kv in acilanDiyaloglar)
        {
            OzelKitapDiyalogKaydi kayit = new OzelKitapDiyalogKaydi();
            kayit.musteriIsmi = kv.Key;
            kayit.acilanDiyaloglar = new List<string>(kv.Value);
            data.acilanOzelKitapDiyaloglari.Add(kayit);
        }
    }
}
