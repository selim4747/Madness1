// BahceSohbetRadari.cs — Musteriye DEGIL, MASAYA baglidir. Oyuncu ETK'ye basinca, o masada
// oturan musteriyle SECIMLI bir konusma baslatir: NPC bir cumle soyler, DialogueBox'ta
// secenek butonlari belirir, oyuncu birini secer, NPC secime gore devam eder. Seceneksiz
// bir cumleye ulasilinca (ya da oyuncu Kapat'a basinca) konusma biter.
//
// DEDIKODU ZINCIRI: her SohbetEt() cagrisinda ONCE "bu kisiden ogrenebilecegim bir dedikodu
// var mi?" diye bakilir (varsa o gosterilir, normal agac o turda beklemeye alinir). Sonra
// normal bahce agaci gosterilirken, "bu kisiYLE paylasabilecegim bir dedikodu var mi?"
// bakilir — varsa secenek listesine EKSTRA buton(lar) olarak eklenir. Paylasilinca, dedikoduya
// bagli bir bahce cumlesi tanimliysa oraya ATLANIR (dallanma); tanimli degilse duz bir tepki
// metni gosterilir.
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System.Collections.Generic;

public class BahceSohbetRadari : MonoBehaviour, ISohbetHedefi
{
    [Tooltip("Bu buyuk masanin ETRAFINDAKI TUM oturma noktalari (orn. 8 nokta). BahceYoneticisi'nde her biri AYRI ayri takip edildigi icin, radar HANGISINDE oturan varsa (oyuncuya en yakin OLANI) onu bulur.")]
    [SerializeField] private Transform[] masaNoktalari;

    private CustomerController aktifMusteri;
    private NPCKimligi aktifKimlik;

    // O turda gosterilen secenek listesinde, DEDIKODU secenekleri normal secenekten SONRA
    // eklenir — bu iki alan, hangi index'in hangi dedikoduya karsilik geldigini takip eder.
    private List<DedikoduData> aktifPaylasilabilecekler;
    private int aktifNormalSecenekSayisi;

    public Transform SohbetTransformuAl() { return transform; }

    /// <summary>masaNoktalari'ndaki TUM noktalari tarar, oturan biri olan noktalar arasindan
    /// RASTGELE birini secer (en yakin DEGIL) — boylece ETK'ye tekrar tekrar basinca hep
    /// ayni (en yakin) kisiyle degil, gruptaki FARKLI kisilerle konusma sansi olur.</summary>
    private CustomerController RastgeleOturaniBul()
    {
        if (masaNoktalari == null || masaNoktalari.Length == 0 || BahceYoneticisi.Instance == null) return null;

        List<CustomerController> oturanlar = new List<CustomerController>();
        foreach (Transform nokta in masaNoktalari)
        {
            if (nokta == null) continue;
            CustomerController oturan = BahceYoneticisi.Instance.GetOturan(nokta);
            if (oturan != null) oturanlar.Add(oturan);
        }

        return oturanlar.Count > 0 ? oturanlar[Random.Range(0, oturanlar.Count)] : null;
    }

    public void SohbetEt()
    {
        if (BahceYoneticisi.Instance == null || BahceSohbetYoneticisi.Instance == null || DialogueBox.Instance == null)
        {
            DevLog.Warning($"🔴 [BAHÇE SOHBET] Eksik referans — BahceYoneticisi={(BahceYoneticisi.Instance == null ? "YOK" : "var")}, BahceSohbetYoneticisi={(BahceSohbetYoneticisi.Instance == null ? "YOK" : "var")}, DialogueBox={(DialogueBox.Instance == null ? "YOK" : "var")}");
            return;
        }

        aktifMusteri = RastgeleOturaniBul();
        if (aktifMusteri == null)
        {
            DevLog.Warning($"🔴 [BAHÇE SOHBET] {masaNoktalari.Length} noktanın hiçbirinde kimse oturmuyor.");
            return;
        }

        aktifKimlik = aktifMusteri.SecilenKimlik;
        if (aktifKimlik == null)
        {
            DevLog.Warning($"🔴 [BAHÇE SOHBET] {aktifMusteri.name} oturuyor ama SecilenKimlik null.");
            return;
        }

        // COKLU KISILIK aktifse: normal agac + dedikodu tamamen atlanir, bu ziyaret boyunca
        // SADECE digerKisilikDiyaloglari'ndan duz bir laf gosterilir (baska biri gibi konusuyor).
        if (aktifMusteri.DigerKisilikAktifMi)
        {
            DigerKisilikLafiGoster();
            return;
        }

        // DEDIKODU OGRENME: bu kisiden henuz ogrenilmemis bir dedikodu varsa, bu tur ONU
        // goster (normal agac bir sonraki ETK'ye kalir).
        if (DedikoduYoneticisi.Instance != null)
        {
            DedikoduData ogrenilecek = DedikoduYoneticisi.Instance.OgrenilebilecekDedikoduBul(aktifKimlik.musteriIsmi);
            if (ogrenilecek != null)
            {
                DedikoduYoneticisi.Instance.DedikoduOgrenildi(ogrenilecek);
                DedikoduOgrenmeSonucunuGoster(ogrenilecek);
                return;
            }
        }

        // NOT: MusteriKonusmaSinirlayici (gunluk konusma limiti) BILEREK burada kontrol
        // EDILMIYOR — o sistem MAGAZA kucuk sohbeti icin tasarlandi (oyuncu yururken
        // rastgele bir kere konusur). Bahcede oyuncu AYNI kisiyle defalarca ETK'ye basip
        // konu agini kesfediyor — o limit hemen dolup "bugun epey konustuk" mesajina
        // takiliyordu. Bahcenin KENDI dogal siniri zaten var: sikilma sayaci (BahceSohbetiKaydet).
        BahceSohbetSonucu sonuc = BahceSohbetYoneticisi.Instance.SohbetiBaslat(aktifMusteri, aktifKimlik);
        DevLog.Info($"🟢 [BAHÇE SOHBET] {aktifKimlik.musteriIsmi} ile sohbet başlatılıyor, sonuc={(sonuc == null ? "NULL" : "'" + sonuc.npcMetni + "'")}");
        SonucuGoster(sonuc);
    }

    /// <summary>Ogrenme anini gosterir — tek secenek ("Devam et"), secilince normal bahce
    /// agacina gecilir.</summary>
    private void DedikoduOgrenmeSonucunuGoster(DedikoduData ogrenilen)
    {
        aktifMusteri.BahceSohbetiKaydet();
        if (SadikMusteriYoneticisi.Instance != null)
            SadikMusteriYoneticisi.Instance.SohbetEdildi(aktifKimlik.musteriIsmi);

        List<string> devamSecenegi = new List<string> { LokalizasyonYardimcisi.SistemMetniCoz("buton_devam_et", "Devam et") };
        aktifPaylasilabilecekler = null;
        aktifNormalSecenekSayisi = 0;

        DialogueBox.Instance.SecenekliGoster(aktifKimlik.musteriIsmi, ogrenilen.OgrenmeMetniCoz(), devamSecenegi, OgrenmeDevamSecildi, ogrenilen.ogrenmeSesi, aktifKimlik.cinsiyet, aktifKimlik.GetYasGrubu());
    }

    /// <summary>COKLU KISILIK aktifken cagrilir — normal agac/dedikodu yerine sadece
    /// digerKisilikDiyaloglari'ndan duz bir laf gosterir, "Kapat" ile biter.</summary>
    private void DigerKisilikLafiGoster()
    {
        aktifMusteri.BahceSohbetiKaydet();
        if (SadikMusteriYoneticisi.Instance != null)
            SadikMusteriYoneticisi.Instance.SohbetEdildi(aktifKimlik.musteriIsmi);

        List<string> digerHavuz = aktifKimlik.digerKisilikDiyaloglari;
        string laf = (digerHavuz != null && digerHavuz.Count > 0)
            ? digerHavuz[Random.Range(0, digerHavuz.Count)]
            : "...";

        string isim = !string.IsNullOrEmpty(aktifKimlik.digerKisilikIsmi) ? aktifKimlik.digerKisilikIsmi : aktifKimlik.musteriIsmi;

        aktifPaylasilabilecekler = null;
        aktifNormalSecenekSayisi = 0;
        DialogueBox.Instance.SecenekliGoster(isim, laf, new List<string>(), null, default(SesKlipSeti), aktifKimlik.cinsiyet, aktifKimlik.GetYasGrubu());
    }

    private void OgrenmeDevamSecildi(int index)
    {
        if (aktifMusteri == null || aktifKimlik == null || BahceSohbetYoneticisi.Instance == null) return;
        BahceSohbetSonucu sonuc = BahceSohbetYoneticisi.Instance.SohbetiBaslat(aktifMusteri, aktifKimlik);
        SonucuGoster(sonuc);
    }

    private void SonucuGoster(BahceSohbetSonucu sonuc)
    {
        if (sonuc == null || string.IsNullOrEmpty(sonuc.npcMetni))
        {
            DialogueBox.Instance.Close();
            return;
        }

        aktifMusteri.BahceSohbetiKaydet(); // sikilma sayaci — bahcenin kendi dogal siniri

        if (SadikMusteriYoneticisi.Instance != null)
            SadikMusteriYoneticisi.Instance.SohbetEdildi(aktifKimlik.musteriIsmi);

        // DEDIKODU PAYLASIMI: bu kisiYLE paylasilabilecek, ogrenilmis dedikodu(lar) varsa,
        // normal secenek listesinin SONUNA ekstra buton(lar) olarak eklenir.
        List<string> gosterilecekSecenekler = new List<string>(sonuc.secenekMetinleri);
        List<DedikoduData> paylasilabilecekler = DedikoduYoneticisi.Instance != null
            ? DedikoduYoneticisi.Instance.PaylasilabilecekDedikodularBul(aktifKimlik.musteriIsmi)
            : new List<DedikoduData>();

        aktifNormalSecenekSayisi = gosterilecekSecenekler.Count;
        aktifPaylasilabilecekler = paylasilabilecekler;

        foreach (DedikoduData d in paylasilabilecekler)
            gosterilecekSecenekler.Add("💬 " + LokalizasyonYardimcisi.SistemMetniCoz("buton_dedikodu_paylas", "{0} hakkında bir şey söyle").Replace("{0}", d.kaynakIsim));

        // Secenek YOKSA (yaprak cumle) — DialogueBox otomatik olarak sadece "Kapat" gosterir,
        // secenek paneli hic acilmaz (SecenekliGoster bunu zaten boyle yonetir).
        DialogueBox.Instance.SecenekliGoster(aktifKimlik.musteriIsmi, sonuc.npcMetni, gosterilecekSecenekler, SecimYapildi, sonuc.ses, sonuc.cinsiyet, sonuc.yasGrubu);
    }

    /// <summary>Oyuncu bir secenek secince DialogueBox bunu cagirir.</summary>
    private void SecimYapildi(int index)
    {
        if (aktifMusteri == null || aktifKimlik == null || BahceSohbetYoneticisi.Instance == null) return;

        // Normal secenek sayisini asan bir index = dedikodu paylasim secenegi secildi.
        if (index >= aktifNormalSecenekSayisi)
        {
            int dIndex = index - aktifNormalSecenekSayisi;
            if (aktifPaylasilabilecekler != null && dIndex >= 0 && dIndex < aktifPaylasilabilecekler.Count)
                DedikoduPaylasVeGoster(aktifPaylasilabilecekler[dIndex]);
            return;
        }

        BahceSohbetSonucu sonuc = BahceSohbetYoneticisi.Instance.SecimYap(aktifMusteri, index, aktifKimlik);
        SonucuGoster(sonuc);
    }

    private void DedikoduPaylasVeGoster(DedikoduData d)
    {
        if (DedikoduYoneticisi.Instance == null) return;

        DedikoduYoneticisi.Instance.DedikoduPaylasildi(d);

        if (SadikMusteriYoneticisi.Instance != null)
            SadikMusteriYoneticisi.Instance.SohbetEdildi(aktifKimlik.musteriIsmi);

        // Baglanti bahce cumlesi tanimliysa oraya ATLA (dallanma) — normal agaca gecis gibi
        // davranir, oradan itibaren secenekler tekrar normal akista islenir.
        if (!string.IsNullOrEmpty(d.baglananBahceCumleId))
        {
            BahceSohbetSonucu dalSonucu = BahceSohbetYoneticisi.Instance.CumleyeAtla(aktifMusteri, d.baglananBahceCumleId, aktifKimlik);
            if (dalSonucu != null) { SonucuGoster(dalSonucu); return; }
        }

        // Baglanti yok (ya da bulunamadi) — sadece duz tepki metnini goster, konusma orada biter.
        List<string> bos = new List<string>();
        aktifPaylasilabilecekler = null;
        aktifNormalSecenekSayisi = 0;
        DialogueBox.Instance.SecenekliGoster(aktifKimlik.musteriIsmi, d.PaylasimTepkiMetniCoz(), bos, null, d.paylasimSesi, aktifKimlik.cinsiyet, aktifKimlik.GetYasGrubu());
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && PlayerInteraction.Instance != null)
            PlayerInteraction.Instance.RegisterChatTarget(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && PlayerInteraction.Instance != null)
            PlayerInteraction.Instance.UnregisterChatTarget(this);
    }

    private void OnDisable()
    {
        if (PlayerInteraction.Instance != null)
            PlayerInteraction.Instance.UnregisterChatTarget(this);
    }
}
