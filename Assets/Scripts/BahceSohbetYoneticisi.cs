// BahceSohbetYoneticisi.cs — Bahce sohbetlerini YONETIR. ARTIK OYUNCU SECIMLI: NPC bir
// cumle soyler, oyuncuya o cumlenin tanimli SECENEKLERI (kisa buton metinleri) sunulur,
// oyuncu birini secer, NPC o secime gore devam eder — secim FARKLI bir konuya da gecebilir.
//
// KISIYE OZEL CUMLELER: her cagriya, musterinin KENDI NPCKimligi'si de verilir. O kisinin
// bahceDiyaloglari'i, PAYLASIMLI havuzla GECICI birlestirilir (sadece o musteride gecerli).
//
// Bir cumlenin HIC secenegi yoksa, o "yaprak" (konusma sonu) sayilir — konusma orada
// dogal olarak biter, radar kapanis lafini gosterir.
//
// DEDIKODU DALLANMASI: DedikoduYoneticisi, bir dedikodu paylasilinca CumleyeAtla() ile
// dogrudan belirli bir cumleye ATLAYABILIR — normal SohbetiBaslat/SecimYap akisinin disinda,
// tek seferlik bir "giris noktasi" gibi calisir.
//
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System.Collections.Generic;

/// <summary>Bir sohbet aninin sonucu: NPC'nin soyledigi cumle + oyuncuya sunulacak secenekler.</summary>
public class BahceSohbetSonucu
{
    public string npcMetni;
    public List<string> secenekMetinleri = new List<string>(); // bos ise konusma burada biter

    // SESLENDIRME: bu cumleye ozel klip (BahceCumlesi.ses) + konusanin cinsiyeti/yas grubu
    // (dogru "blabla" havuzu icin) — BahceSohbetRadari bunlari DialogueBox'a aktarir.
    public SesKlipSeti ses;
    public Cinsiyet cinsiyet;
    public YasGrubu yasGrubu;
}

public class BahceSohbetYoneticisi : MonoBehaviour
{
    public static BahceSohbetYoneticisi Instance { get; private set; }

    [Header("Icerik")]
    [SerializeField] private BahceKonuVerisi konuVerisi;

    // Her aktif konusma (musteri bazinda) hangi cumlede kaldi.
    private readonly Dictionary<object, string> aktifKonusmaCumlesi = new Dictionary<object, string>();
    private Dictionary<string, BahceCumlesi> paylasimliHarita;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        paylasimliHarita = new Dictionary<string, BahceCumlesi>();
        if (konuVerisi != null && konuVerisi.tumCumleler != null)
        {
            foreach (BahceCumlesi c in konuVerisi.tumCumleler)
            {
                if (c != null && !string.IsNullOrEmpty(c.id) && !paylasimliHarita.ContainsKey(c.id))
                    paylasimliHarita[c.id] = c;
            }
        }
    }

    /// <summary>Bu cagriya ozel, paylasimli + kisiye-ozel BIRLESIK harita — kisiye ozel
    /// cumleler ayni id'li paylasimliyi golgeler.</summary>
    private Dictionary<string, BahceCumlesi> BirlesikHaritaOlustur(NPCKimligi kimlik)
    {
        if (kimlik == null || kimlik.bahceDiyaloglari == null || kimlik.bahceDiyaloglari.Count == 0)
            return paylasimliHarita;

        Dictionary<string, BahceCumlesi> harita = new Dictionary<string, BahceCumlesi>(paylasimliHarita);
        foreach (BahceCumlesi kisisel in kimlik.bahceDiyaloglari)
        {
            if (kisisel != null && !string.IsNullOrEmpty(kisisel.id))
                harita[kisisel.id] = kisisel;
        }
        return harita;
    }

    /// <summary>Konusma YENIDEN baslatilir/DEVAM ettirilir — eger bu anahtar icin hafizada
    /// bir cumle varsa (daha once konusulmus, ayrilmamis) ORADAN devam eder; yoksa rastgele
    /// bir acilis cumlesi secer.</summary>
    public BahceSohbetSonucu SohbetiBaslat(object konusmaAnahtari, NPCKimligi kimlik)
    {
        Dictionary<string, BahceCumlesi> harita = BirlesikHaritaOlustur(kimlik);
        if (harita.Count == 0) return null;

        string mevcutId;
        BahceCumlesi cumle = null;

        if (aktifKonusmaCumlesi.TryGetValue(konusmaAnahtari, out mevcutId))
            harita.TryGetValue(mevcutId, out cumle);

        // YAPRAK cumleye (secenegi olmayan) ulasilmissa, ETK'ye TEKRAR basildiginda AYNI
        // cumleyi tekrar tekrar gostermek yerine, KONUYLA ILISKILI rastgele bir cumleye
        // atlanir — sohbet asla "sikismis" gibi durmaz, hep bir yerlere devam eder.
        if (cumle != null && (cumle.secenekler == null || cumle.secenekler.Count == 0))
            cumle = IliskiliRastgeleCumleBul(cumle, harita) ?? cumle;

        if (cumle == null)
        {
            // Ilk kez / hafiza yok — tamamen rastgele acilis.
            List<BahceCumlesi> tumu = new List<BahceCumlesi>(harita.Values);
            cumle = tumu[Random.Range(0, tumu.Count)];
        }

        return CumleyiSonucaCevir(konusmaAnahtari, cumle, harita, kimlik);
    }

    /// <summary>Verilen cumleyle EN AZ BIR ORTAK KONU paylasan, rastgele bir cumle bulur
    /// (kendisi haric). Bulamazsa null doner (cagiran ayni cumleyi tutmaya devam eder).</summary>
    private BahceCumlesi IliskiliRastgeleCumleBul(BahceCumlesi onceki, Dictionary<string, BahceCumlesi> harita)
    {
        if (onceki.konular == null || onceki.konular.Count == 0) return null;

        List<BahceCumlesi> adaylar = new List<BahceCumlesi>();
        foreach (BahceCumlesi aday in harita.Values)
        {
            if (aday == null || aday.id == onceki.id || aday.konular == null) continue;
            foreach (string k in aday.konular)
            {
                if (onceki.konular.Contains(k)) { adaylar.Add(aday); break; }
            }
        }

        return adaylar.Count > 0 ? adaylar[Random.Range(0, adaylar.Count)] : null;
    }

    /// <summary>Oyuncu bir secenek sectiginde cagrilir. secilenIndex, en son donen
    /// BahceSohbetSonucu.secenekMetinleri listesindeki INDEX'tir.</summary>
    public BahceSohbetSonucu SecimYap(object konusmaAnahtari, int secilenIndex, NPCKimligi kimlik)
    {
        Dictionary<string, BahceCumlesi> harita = BirlesikHaritaOlustur(kimlik);

        string mevcutId;
        BahceCumlesi mevcutCumle;
        if (!aktifKonusmaCumlesi.TryGetValue(konusmaAnahtari, out mevcutId) ||
            !harita.TryGetValue(mevcutId, out mevcutCumle) ||
            mevcutCumle.secenekler == null || secilenIndex < 0 || secilenIndex >= mevcutCumle.secenekler.Count)
        {
            return null;
        }

        string hedefId = mevcutCumle.secenekler[secilenIndex].hedefCumleId;
        BahceCumlesi hedefCumle;
        if (!harita.TryGetValue(hedefId, out hedefCumle)) return null;

        return CumleyiSonucaCevir(konusmaAnahtari, hedefCumle, harita, kimlik);
    }

    /// <summary>DEDIKODU (veya benzeri disaridan bir sistem) tarafindan, belirli bir cumle
    /// id'sine DOGRUDAN atlamak icin. Cumle bulunamazsa null doner (cagiran duz metne duser).</summary>
    public BahceSohbetSonucu CumleyeAtla(object konusmaAnahtari, string cumleId, NPCKimligi kimlik)
    {
        if (string.IsNullOrEmpty(cumleId)) return null;

        Dictionary<string, BahceCumlesi> harita = BirlesikHaritaOlustur(kimlik);
        BahceCumlesi cumle;
        if (!harita.TryGetValue(cumleId, out cumle)) return null;

        return CumleyiSonucaCevir(konusmaAnahtari, cumle, harita, kimlik);
    }

    private BahceSohbetSonucu CumleyiSonucaCevir(object konusmaAnahtari, BahceCumlesi cumle, Dictionary<string, BahceCumlesi> harita, NPCKimligi kimlik)
    {
        aktifKonusmaCumlesi[konusmaAnahtari] = cumle.id;

        BahceSohbetSonucu sonuc = new BahceSohbetSonucu();
        // YERELLEŞTIRME: aktif dile gore cozulmus metin (yoksa Turkce'ye duser).
        sonuc.npcMetni = LokalizasyonYardimcisi.MetniCoz(cumle.metin, cumle.ekDiller);
        sonuc.ses = cumle.ses;
        sonuc.cinsiyet = kimlik != null ? kimlik.cinsiyet : Cinsiyet.Erkek;
        sonuc.yasGrubu = kimlik != null ? kimlik.GetYasGrubu() : YasGrubu.Yetiskin;

        if (cumle.secenekler != null)
        {
            foreach (BahceSecenegi secenek in cumle.secenekler)
            {
                // Hedef cumle gercekten haritada var mi kontrol edilir (kirik referans gosterilmesin).
                if (secenek != null && !string.IsNullOrEmpty(secenek.hedefCumleId) && harita.ContainsKey(secenek.hedefCumleId))
                    sonuc.secenekMetinleri.Add(LokalizasyonYardimcisi.MetniCoz(secenek.secenekMetni, secenek.ekDiller));
            }
        }

        return sonuc;
    }

    /// <summary>Musteri bahceden ayrilinca cagrilir — konusma hafizasi temizlenir.</summary>
    public void KonusmayiTemizle(object konusmaAnahtari)
    {
        aktifKonusmaCumlesi.Remove(konusmaAnahtari);
    }

    /// <summary>Ortam sohbeti (ambient chatter) icin — secime GEREK DUYMADAN, sadece bir
    /// sonraki/rastgele cumleyi METIN olarak dondurur (FloatingText balonu icin).</summary>
    public string OrtamIcinRastgeleCumle(object konusmaAnahtari, NPCKimligi kimlik)
    {
        BahceSohbetSonucu sonuc = SohbetiBaslat(konusmaAnahtari, kimlik);
        return sonuc != null ? sonuc.npcMetni : null;
    }
}
