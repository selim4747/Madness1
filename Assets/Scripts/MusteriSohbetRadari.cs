// MusteriSohbetRadari.cs — SADECE MAGAZA (dukkan icinde yururken) sohbeti icin. Bahce
// sohbeti artik BahceSohbetRadari'ye tasindi — bu script bahcedeyken KENDINI KAYIT ETMEZ.
//
// DEDIKODU ZINCIRI: magazada da bahcedeki gibi calisir ama daha sade — dallanma YOK (o
// sadece bahce agacinda var), sadece OGRENME (kaynak kisiyle konusunca) ve PAYLASIM (hedef
// kisiyle konusunca secenek olarak sunulur) var.
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System.Collections.Generic;

public class MusteriSohbetRadari : MonoBehaviour, ISohbetHedefi
{
    private NPCKimligi musteriKimligi;
    private CustomerController musteriScripti;

    // O turda gosterilen secenek listesi = sadece dedikodu paylasim secenekleri (magazada
    // baska secenek yok).
    private List<DedikoduData> aktifPaylasilabilecekler;

    public Transform SohbetTransformuAl() { return transform; }

    public void SohbetEt()
    {
        musteriScripti = GetComponentInParent<CustomerController>();
        if (musteriScripti != null) musteriKimligi = musteriScripti.SecilenKimlik;
        if (musteriKimligi == null) return;

        // MAGAZA disinda (bahcedeyse) bu radar hic konusmaz — o is BahceSohbetRadari'de.
        if (musteriScripti.BahcedeMi) return;

        // 1) DEDIKODU OGRENME: bu kisiden henuz ogrenilmemis bir dedikodu varsa, bu tur ONU
        // goster (normal havuzdan laf secmenin YERINE gecer).
        if (DedikoduYoneticisi.Instance != null)
        {
            DedikoduData ogrenilecek = DedikoduYoneticisi.Instance.OgrenilebilecekDedikoduBul(musteriKimligi.musteriIsmi);
            if (ogrenilecek != null)
            {
                DedikoduYoneticisi.Instance.DedikoduOgrenildi(ogrenilecek);
                SohbetEdildiBildir();
                GosterTekSatir(ogrenilecek.OgrenmeMetniCoz(), ogrenilecek.ogrenmeSesi);
                return;
            }
        }

        // 2) DEDIKODU PAYLASIMI: bu kisiYLE paylasilabilecek dedikodu varsa, normal lafla
        // BIRLIKTE secenek olarak sunulur.
        List<DedikoduData> paylasilabilecekler = DedikoduYoneticisi.Instance != null
            ? DedikoduYoneticisi.Instance.PaylasilabilecekDedikodularBul(musteriKimligi.musteriIsmi)
            : null;

        string normalLaf = NormalLafSecVeIsaretle();

        if (paylasilabilecekler != null && paylasilabilecekler.Count > 0)
        {
            List<string> secenekler = new List<string>();
            foreach (DedikoduData d in paylasilabilecekler)
                secenekler.Add("💬 " + LokalizasyonYardimcisi.SistemMetniCoz("buton_dedikodu_paylas", "{0} hakkında bir şey söyle").Replace("{0}", d.kaynakIsim));

            aktifPaylasilabilecekler = paylasilabilecekler;
            string gosterilecekMetin = !string.IsNullOrEmpty(normalLaf) ? normalLaf : "...";

            SohbetEdildiBildir();
            if (DialogueBox.Instance != null)
                DialogueBox.Instance.SecenekliGoster(GosterilenIsim(), gosterilecekMetin, secenekler, DedikoduSecimYapildi, default(SesKlipSeti), musteriKimligi.cinsiyet, musteriKimligi.GetYasGrubu());
            return;
        }

        if (string.IsNullOrEmpty(normalLaf)) return; // limit dolmus, dedikodu da yok -> eski davranis (hicbir sey gosterme)

        SohbetEdildiBildir();
        GosterTekSatir(normalLaf);
    }

    /// <summary>Normal ozel-diyalog havuzundan (+ Kara Kitap/Dedikodu ile acilan kalici
    /// satirlardan) rastgele bir laf secer ve gunluk sayaci isaretler. Secilecek bir sey
    /// yoksa (havuz bos ya da limit dolmus) null doner.
    ///
    /// COKLU KISILIK aktifse: normal havuz + Kara Kitap/Dedikodu eklerinin YERINE, SADECE
    /// digerKisilikDiyaloglari kullanilir (o an "baska biri" gibi konusuyor).</summary>
    private string NormalLafSecVeIsaretle()
    {
        if (musteriScripti != null && musteriScripti.DigerKisilikAktifMi)
        {
            List<string> digerHavuz = musteriKimligi.digerKisilikDiyaloglari;
            if (digerHavuz == null || digerHavuz.Count == 0) return null;
            return digerHavuz[Random.Range(0, digerHavuz.Count)]; // gunluk limit bu moda uygulanmaz — nadir bir an
        }

        List<string> havuz = musteriKimligi.ozelDiyaloglar;
        if (havuz == null || havuz.Count == 0) return null;

        // OZEL KITAP OKUNDUKTAN SONRA + DEDIKODU PAYLASIMINDAN SONRA: bu kisi icin kalici
        // olarak acilmis ekstra satirlar varsa havuza katilir.
        List<string> kitapDiyaloglari = OzelKitapDiyaloglariYoneticisi.Instance != null
            ? OzelKitapDiyaloglariYoneticisi.Instance.EkDiyaloglariAl(musteriKimligi.musteriIsmi)
            : null;
        List<string> dedikoduDiyaloglari = DedikoduYoneticisi.Instance != null
            ? DedikoduYoneticisi.Instance.EkDiyaloglariAl(musteriKimligi.musteriIsmi)
            : null;

        List<string> tamHavuz = havuz;
        if ((kitapDiyaloglari != null && kitapDiyaloglari.Count > 0) || (dedikoduDiyaloglari != null && dedikoduDiyaloglari.Count > 0))
        {
            tamHavuz = new List<string>(havuz);
            if (kitapDiyaloglari != null) tamHavuz.AddRange(kitapDiyaloglari);
            if (dedikoduDiyaloglari != null) tamHavuz.AddRange(dedikoduDiyaloglari);
        }

        if (!MusteriKonusmaSinirlayici.YeniLafAlabilirMi(musteriKimligi)) return null;

        string secilenLaf = tamHavuz[Random.Range(0, tamHavuz.Count)];
        MusteriKonusmaSinirlayici.LafVerildi(musteriKimligi);
        return secilenLaf;
    }

    private void SohbetEdildiBildir()
    {
        if (SadikMusteriYoneticisi.Instance != null)
            SadikMusteriYoneticisi.Instance.SohbetEdildi(musteriKimligi.musteriIsmi);
    }

    /// <summary>COKLU KISILIK aktifse digerKisilikIsmi, degilse gercek musteriIsmi.</summary>
    private string GosterilenIsim()
    {
        if (musteriScripti != null && musteriScripti.DigerKisilikAktifMi && !string.IsNullOrEmpty(musteriKimligi.digerKisilikIsmi))
            return musteriKimligi.digerKisilikIsmi;
        return musteriKimligi.musteriIsmi;
    }

    private void GosterTekSatir(string metin)
    {
        GosterTekSatir(metin, default(SesKlipSeti));
    }

    /// <summary>ses verilmezse (varsayilan/bos) — bu havuz coklu kaynagi (ozelDiyaloglar +
    /// kitap/dedikodu ekleri) birlestirdigi icin tek tek klip takibi yapilmiyor, en azindan
    /// DOGRU cinsiyet+yas grubunda "blabla" sesi calar. Dedikodu ogrenme/paylasim gibi
    /// TEKIL, ozel-klipli satirlar icin cagiran taraf gercek SesKlipSeti verebilir.</summary>
    private void GosterTekSatir(string metin, SesKlipSeti ses)
    {
        DialogueLine satir = new DialogueLine();
        satir.speakerName = GosterilenIsim();
        satir.text = metin;
        satir.ses = ses;
        satir.konusanCinsiyeti = musteriKimligi != null ? musteriKimligi.cinsiyet : Cinsiyet.Erkek;
        satir.konusanYasGrubu = musteriKimligi != null ? musteriKimligi.GetYasGrubu() : YasGrubu.Yetiskin;

        if (DialogueBox.Instance != null) DialogueBox.Instance.Show(new List<DialogueLine> { satir });
    }

    /// <summary>Oyuncu magazadaki dedikodu-paylas secenegini secince cagirir.</summary>
    private void DedikoduSecimYapildi(int index)
    {
        if (aktifPaylasilabilecekler == null || index < 0 || index >= aktifPaylasilabilecekler.Count) return;
        if (DedikoduYoneticisi.Instance == null) return;

        DedikoduData secilen = aktifPaylasilabilecekler[index];
        DedikoduYoneticisi.Instance.DedikoduPaylasildi(secilen);
        SohbetEdildiBildir();
        GosterTekSatir(secilen.PaylasimTepkiMetniCoz(), secilen.paylasimSesi);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || PlayerInteraction.Instance == null) return;

        // Bahcedeyken bu radar KAYDOLMAZ — BahceSohbetRadari devreye girer.
        CustomerController c = GetComponentInParent<CustomerController>();
        if (c != null && c.BahcedeMi) return;

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
