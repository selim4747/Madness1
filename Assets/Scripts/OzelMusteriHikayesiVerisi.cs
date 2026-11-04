// OzelMusteriHikayesiVerisi.cs — Var olan DUZENLI bir musteriye ("hedefMusteriIsmi") GECICI
// olarak "hikaye konugu" kimligi kazandiran, GUN GUN ilerleyen dallanan bir hikaye.
//
// AKIS: Belirlenen bir gunde baslar. O GUNDEN itibaren, hikaye BITENE kadar HER GUN, konuk
// 3 ozel spawn noktasindan birinde belirir ve GUN SONUNA KADAR bekler (gece yok olur, ertesi
// gun hikaye devam ediyorsa YENIDEN spawn olur). Oyuncu o gun icinde istedigi kadar konusabilir
// — o gunun segmentindeki satirlar sirayla gosterilir, EN SONUNDA HER ZAMAN bir SECIM sunulur.
// Secilen secenek, YARIN hangi segmentin oynayacagini belirler — bos birakilirsa (hedefSegmentId
// yok) hikaye O GUN BITER ve konuk ertesi gunden itibaren tekrar SIRADAN bir musteri olur.
//
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class OzelHikayeSecenegi
{
    [Tooltip("SECENEK BUTONUNDA gosterilecek KISA metin.")]
    public string secenekMetni;

    [Tooltip("Turkce disinda baska dillerde bu secenek metninin karsiligi. Bos birakilabilir.")]
    public List<DilMetniGirisi> ekDiller = new List<DilMetniGirisi>();

    [Tooltip("Secilince YARIN oynayacak segmentin id'si. BOS birakilirsa hikaye burada BITER (yarindan itibaren normal musteri).")]
    public string hedefSegmentId;
}

[System.Serializable]
public class HikayeGunSegmenti
{
    [Tooltip("Benzersiz kimlik — secenekler ve ilkSegmentId bunu hedef alir.")]
    public string id;

    [Tooltip("Bu gun konusulunca ust uste (ETK ile ilerleyerek) gosterilecek giris satirlari. Bos birakilabilir.\n\nNOT (YERELLEŞTIRME): bu liste SADECE Turkce'dir — coklu-dil destegi yok (List<string> oldugu icin her satira ayri dil eklemek yapisal degisiklik gerektirir). Cok dilli olmasi gereken tek-satirlik onemli metinler icin sonSatir'i (asagida, coklu-dil destekli) kullan.")]
    public List<string> onSatirlar = new List<string>();

    [Tooltip("Secim butonlariyla BIRLIKTE gosterilecek son satir.")]
    [TextArea(2, 4)]
    public string sonSatir;

    [Tooltip("Turkce disinda baska dillerde sonSatir'in karsiligi. Bos birakilabilir.")]
    public List<DilMetniGirisi> sonSatirEkDiller = new List<DilMetniGirisi>();

    [Tooltip("Oyuncuya sunulacak secenekler — hikaye HER ZAMAN bir secimle biter ya da devam eder, en az 1 tane olmali.")]
    public List<OzelHikayeSecenegi> secenekler = new List<OzelHikayeSecenegi>();
}

[CreateAssetMenu(fileName = "OzelHikaye_", menuName = "Delilik/Ozel Musteri Hikayesi")]
public class OzelMusteriHikayesiVerisi : ScriptableObject
{
    [Header("Hedef Kisi")]
    [Tooltip("Bu hikaye SADECE bu isimdeki musteride tetiklenir — NPCKimligi.musteriIsmi ile BIREBIR ayni yazilmali.")]
    public string hedefMusteriIsmi = "";

    [Tooltip("SESLENDIRME icin: hedef kisinin GERCEK NPCKimligi'sindeki cinsiyet/yas ile AYNI olmali (dogru 'blabla' havuzunun secilmesi icin) — bu asset kendi basina NPCKimligi'ye baglanmadigi icin elle kopyalanmasi gerekir.")]
    public Cinsiyet hedefCinsiyet = Cinsiyet.Erkek;
    public YasGrubu hedefYasGrubu = YasGrubu.Yetiskin;

    [Header("Kimlik (kayit icin)")]
    public string hikayeId = "hikaye_001";

    [Header("Gorunum")]
    [Tooltip("Konuk olarak gorunurken kullanilacak prefab — GeciciHikayeKonuguController icermeli.")]
    public GameObject konukPrefab;

    [Header("Hangi Gun BASLAR (sadece ilk gun icin gecerli — basladiktan sonra hikaye kendi devam eder)")]
    public List<int> baslayacagiGunler = new List<int>();
    [Tooltip("Her KAC gunde bir baslayabilir (0 = kullanma).")]
    public int herKacGundeBirBaslar = 0;
    public int enErkenBaslamaGunu = 0;
    public int enGecBaslamaGunu = 0;

    [Header("Segmentler")]
    [Tooltip("Hikayenin baslayacagi ilk segmentin id'si.")]
    public string ilkSegmentId = "";
    public List<HikayeGunSegmenti> segmentler = new List<HikayeGunSegmenti>();

    /// <summary>Bu hikaye BUGUN baslar mi? (Sadece hikaye HENUZ baslamamissa sorulur —
    /// devam eden bir hikaye icin gecerli degildir.)</summary>
    public bool BaslarMi(int gun)
    {
        if (baslayacagiGunler != null && baslayacagiGunler.Contains(gun)) return true;
        if (herKacGundeBirBaslar > 0 && gun % herKacGundeBirBaslar == 0) return true;
        if (enErkenBaslamaGunu > 0 && enGecBaslamaGunu > 0 && gun >= enErkenBaslamaGunu && gun <= enGecBaslamaGunu) return true;
        return false;
    }
}
