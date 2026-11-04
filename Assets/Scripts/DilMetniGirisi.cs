// DilMetniGirisi.cs — Bir metnin, TABAN (Turkce) alanina EK olarak baska bir dildeki
// karsiligi. LISTE TABANLI: bir cumleye/satira birden fazla dil eklenebilir, ileride yeni
// bir dil (GameLanguage'e yeni deger) eklendiginde SADECE bu listeye yeni bir giris
// eklenir — hicbir mevcut alan/veri BOZULMAZ (Turkce metin hep kendi orijinal alaninda
// kalir, bu liste sadece EK diller icindir).
//
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class DilMetniGirisi
{
    public GameLanguage dil = GameLanguage.English;

    [TextArea(1, 4)]
    public string metin;
}

/// <summary>Cok-dilli metin cozumlemesi icin ortak yardimci — DialogueLine, BahceCumlesi,
/// BahceSecenegi, DedikoduData gibi tum yapilar AYNI mantigi kullanir.</summary>
public static class LokalizasyonYardimcisi
{
    /// <summary>Aktif dile (LocalizationManager.CurrentLanguage) gore dogru metni doner.
    /// Aktif dil Turkce'yse (ya da LocalizationManager sahnede yoksa) HER ZAMAN tabanMetin
    /// (Turkce) donulur. Baska bir dilse, ekDiller icinde o dile ait bir giris aranir;
    /// bulunursa o metin, bulunamazsa (henuz cevrilmemisse) YEDEK olarak tabanMetin (Turkce)
    /// donulur — boylece eksik ceviri asla BOS metin gostermez.</summary>
    public static string MetniCoz(string tabanMetin, List<DilMetniGirisi> ekDiller)
    {
        GameLanguage dil = LocalizationManager.Instance != null
            ? LocalizationManager.Instance.CurrentLanguage
            : GameLanguage.Turkish;

        if (dil == GameLanguage.Turkish || ekDiller == null) return tabanMetin;

        for (int i = 0; i < ekDiller.Count; i++)
        {
            if (ekDiller[i] != null && ekDiller[i].dil == dil && !string.IsNullOrEmpty(ekDiller[i].metin))
                return ekDiller[i].metin;
        }

        return tabanMetin; // o dile cevrilmemis — Turkce'ye duser
    }

    /// <summary>KOD ICINDE uretilen (dialog verisi degil, buton/sistem metni gibi) kisa
    /// UI metinleri icin — LocalizationManager.Get(anahtar) CSV'sini kullanir. LocalizationManager.Get
    /// anahtar CSV'de BULUNAMAZSA anahtarin KENDISINI dondurur (arayuzde cirkin gorunur) —
    /// bu yuzden burada sonuc bos VEYA anahtarla AYNIYSA yerine turkceYedek metni dondurulur.
    /// KULLANIM: CSV'ye "anahtar;metin" formatinda hem Turkce hem Ingilizce satiri EKLEMEK
    /// gerekir — eklenmezse (ya da LocalizationManager sahnede yoksa) her zaman turkceYedek
    /// gosterilir, hicbir zaman bos/cirkin metin gosterilmez.</summary>
    public static string SistemMetniCoz(string anahtar, string turkceYedek)
    {
        if (LocalizationManager.Instance == null) return turkceYedek;

        string sonuc = LocalizationManager.Instance.Get(anahtar);
        if (string.IsNullOrEmpty(sonuc) || sonuc == anahtar) return turkceYedek;
        return sonuc;
    }
}
