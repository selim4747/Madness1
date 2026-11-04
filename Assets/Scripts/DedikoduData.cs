// DedikoduData.cs — Tek bir "dedikodu" tanimi: KAYNAK kisi bunu SOYLER (oyuncu ogrenir),
// oyuncu bunu HEDEF kisiyle PAYLASIR (ona anlatir). Her ikisiyle de iliski puanina etki
// edebilir, hedefin normal sohbet havuzuna KALICI yeni satirlar acabilir, ve (istersen)
// bahce konu agacinda ozel bir dala BAGLANABILIR.
//
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "YeniDedikodu", menuName = "Delilik/Dedikodu")]
public class DedikoduData : ScriptableObject
{
    [Header("Kimlik")]
    [Tooltip("Benzersiz kimlik — kayit/karsilastirma icin kullanilir. Orn: 'egir_ile_zafki'.")]
    public string dedikoduId;

    [Header("Kaynak (bunu SÖYLEYEN kişi)")]
    [Tooltip("Bu dedikoduyu oyuncuya SOYLEYECEK kisinin musteriIsmi'si.")]
    public string kaynakIsim;

    [TextArea(2, 4)]
    [Tooltip("Kaynak kisi bunu soylerken gosterilecek metin — oyuncu bunu OGRENIR.")]
    public string ogrenmeMetni;

    [Tooltip("Turkce disinda baska dillerde ogrenmeMetni'nin karsiligi. Bos birakilabilir.")]
    public List<DilMetniGirisi> ogrenmeMetniEkDiller = new List<DilMetniGirisi>();

    [Tooltip("SESLENDIRME (opsiyonel — bos birakilirsa varsayilan 'blabla' sesi calar).")]
    public SesKlipSeti ogrenmeSesi;

    [Tooltip("Ogrenilince KAYNAK kisiyle iliski puanina eklenecek miktar (guven karsiligi, genelde pozitif).")]
    public int kaynakIliskiEtkisi = 3;

    [Header("Hedef (dedikodunun HAKKINDA OLDUĞU / paylaşılacağı kişi)")]
    [Tooltip("Bu dedikodunun HAKKINDA OLDUGU, oyuncunun PAYLASACAGI kisinin musteriIsmi'si.")]
    public string hedefIsim;

    [TextArea(2, 4)]
    [Tooltip("Oyuncu bunu HEDEF kisiyle paylasinca, hedefin vereceği tepki metni.")]
    public string paylasimTepkiMetni;

    [Tooltip("Turkce disinda baska dillerde paylasimTepkiMetni'nin karsiligi. Bos birakilabilir.")]
    public List<DilMetniGirisi> paylasimTepkiMetniEkDiller = new List<DilMetniGirisi>();

    [Tooltip("SESLENDIRME (opsiyonel — bos birakilirsa varsayilan 'blabla' sesi calar).")]
    public SesKlipSeti paylasimSesi;

    [Tooltip("Paylasilinca HEDEF kisiyle iliski puanina eklenecek miktar (pozitif ya da negatif olabilir — mahrem bir sey paylasmak gucendirebilir).")]
    public int hedefIliskiEtkisi = 0;

    [Header("Kalıcı Diyalog (opsiyonel)")]
    [Tooltip("Paylasilinca HEDEF kisinin normal sohbet havuzuna KALICI olarak eklenecek ekstra satir(lar). Bos birakilabilir.")]
    public List<string> paylasimSonrasiDiyaloglar = new List<string>();

    [Header("Bahçe Ağacı Dallanması (opsiyonel)")]
    [Tooltip("HEDEF kisinin NPCKimligi.bahceDiyaloglari listesinde tanimli bir BahceCumlesi id'si. Doldurulursa, dedikodu bahcede paylasilinca konusma DUZ bir tepki metniyle bitmez — bu cumleden itibaren normal bahce agacina (secenekleriyle) devam eder. Bos birakilirsa sadece paylasimTepkiMetni gosterilir ve konusma orada biter.")]
    public string baglananBahceCumleId;

    // ── YERELLEŞTIRME cozumleme yardimcilari ──

    /// <summary>Aktif dile gore cozulmus ogrenmeMetni (yoksa Turkce'ye duser).</summary>
    public string OgrenmeMetniCoz()
    {
        return LokalizasyonYardimcisi.MetniCoz(ogrenmeMetni, ogrenmeMetniEkDiller);
    }

    /// <summary>Aktif dile gore cozulmus paylasimTepkiMetni (yoksa Turkce'ye duser).</summary>
    public string PaylasimTepkiMetniCoz()
    {
        return LokalizasyonYardimcisi.MetniCoz(paylasimTepkiMetni, paylasimTepkiMetniEkDiller);
    }
}
