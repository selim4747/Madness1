// GorevYoneticisi.cs — Genel amacli, tek-seferlik "gorev" (quest) takip sistemi.
// Su an SADECE BlackBookDealerManager kullaniyor ama JENERIK yazildi — ileride baska
// bir gorev vereni (orn. Afor'un ozel istekleri) de ayni sistemle calisabilir.
//
// MANTIK: Bir gorev, TrackedStat enum'undan (Achievement sisteminde de kullanilan) bir
// sayaci hedef alir. Gorev BASLARKEN o sayacin o anki degeri "baslangic" olarak kaydedilir;
// ilerleme = (su anki deger - baslangic). Boylece "5 kitap sat" gorevi, oyuncunun HAYAT
// BOYU sattigi kitap sayisina degil, GOREV BASLADIKTAN SONRA sattigina bakar.
//
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System;

[Serializable]
public class GorevTanimi
{
    public TrackedStat stat;
    [Tooltip("Gorevin hedef sayisi (orn. 5 kitap sat icin 5).")]
    public int hedefSayi;
    [Tooltip("Oyuncuya gosterilecek Turkce aciklama, orn. '5 Efsanevi Kitap Sat'.")]
    public string aciklama;
}

public class GorevYoneticisi : MonoBehaviour
{
    public static GorevYoneticisi Instance { get; private set; }

    /// <summary>Aktif gorev degisince (yeni baslar/tamamlanir/suresi dolar) tetiklenir —
    /// UI'lar buna abone olup kendini guncelleyebilir.</summary>
    public event Action OnGorevDegisti;

    public GorevTanimi AktifGorev { get; private set; }
    public int BaslangicDegeri { get; private set; }
    public int SonGun { get; private set; } // gorevin tamamlanmasi GEREKEN son gun
    public bool GorevVarMi { get { return AktifGorev != null; } }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>Yeni bir gorev baslatir. sonGun: bu gunun SONUNA kadar tamamlanmali.</summary>
    public void GorevBaslat(GorevTanimi tanim, int sonGun)
    {
        AktifGorev = tanim;
        BaslangicDegeri = tanim != null ? PlayerProgressStats.GetStatValue(tanim.stat) : 0;
        SonGun = sonGun;
        OnGorevDegisti?.Invoke();
    }

    /// <summary>Gorevi tamamen temizler (basarili/basarisiz fark etmeksizin, cagiran karar verir).</summary>
    public void GoreviTemizle()
    {
        AktifGorev = null;
        BaslangicDegeri = 0;
        SonGun = 0;
        OnGorevDegisti?.Invoke();
    }

    /// <summary>Gorevin o anki ilerlemesi (baslangictan bu yana artan miktar).</summary>
    public int GuncelIlerleme()
    {
        if (AktifGorev == null) return 0;
        return Mathf.Max(0, PlayerProgressStats.GetStatValue(AktifGorev.stat) - BaslangicDegeri);
    }

    /// <summary>Hedefe ulasildi mi?</summary>
    public bool TamamlandiMi()
    {
        if (AktifGorev == null) return false;
        return GuncelIlerleme() >= AktifGorev.hedefSayi;
    }

    /// <summary>Verilen gun, gorevin son gununu GECTI mi (suresi doldu mu)?</summary>
    public bool SuresiDoldu(int suankiGun)
    {
        return AktifGorev != null && suankiGun > SonGun;
    }
}