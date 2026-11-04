// MusteriKonusmaSinirlayici.cs — Karakter basina gunluk "yeni laf" sinirini takip eder
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using System.Collections.Generic;

public static class MusteriKonusmaSinirlayici
{
    private static readonly Dictionary<string, int> bugunkuSayim = new Dictionary<string, int>();

    /// <summary>GameManager.OnDayChanged'e baglanmali — her yeni gunde sayaclar sifirlanir.</summary>
    public static void GunuSifirla()
    {
        bugunkuSayim.Clear();
    }

    /// <summary>Bu karakterden BUGUN daha yeni bir laf alabilir miyiz? (limit 0 ise sinirsiz.)</summary>
    public static bool YeniLafAlabilirMi(NPCKimligi kimlik)
    {
        if (kimlik == null) return false;
        if (kimlik.gunlukKonusmaLimiti <= 0) return true;

        int sayi;
        bugunkuSayim.TryGetValue(kimlik.musteriIsmi, out sayi);
        return sayi < kimlik.gunlukKonusmaLimiti;
    }

    /// <summary>Yeni bir laf verildikten sonra cagrilir, sayaci bir artirir.</summary>
    public static void LafVerildi(NPCKimligi kimlik)
    {
        if (kimlik == null) return;

        int sayi;
        bugunkuSayim.TryGetValue(kimlik.musteriIsmi, out sayi);
        bugunkuSayim[kimlik.musteriIsmi] = sayi + 1;
    }
}