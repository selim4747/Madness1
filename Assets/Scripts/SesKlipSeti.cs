// SesKlipSeti.cs — Bir diyalog satirinin SESLENDIRME klipleri. YERELLEŞTIRME ile uyumlu:
// oyun su an sadece Turkce ama LocalizationManager/GameLanguage altyapisi Ingilizce'ye
// hazir — bu yapi da ayni sekilde calisir. Turkce klip ZORUNLU degil ama varsa oncelikli;
// Ingilizce klip ileride eklenince (Inspector'dan surukleyince) otomatik devreye girer,
// hicbir kod degisikligi gerekmez.
//
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System;

[Serializable]
public struct SesKlipSeti
{
    [Tooltip("Turkce seslendirme klibi. Bos birakilabilir (o zaman varsayilan 'blabla' sesi calar).")]
    public AudioClip turkce;

    [Tooltip("Ingilizce seslendirme klibi. Bos birakilabilir — ileride eklenir, oyuncu dili Ingilizce'ye alinca otomatik kullanilir.")]
    public AudioClip ingilizce;

    /// <summary>Su anki aktif dile (LocalizationManager.CurrentLanguage) gore oynatilacak klip.
    /// O dilde klip ATANMAMISSA Turkce'ye (varsa) duser — boylece Ingilizce seslendirme
    /// eksikken bile TAMAMEN sessiz kalinmaz. Hicbiri atanmamissa null doner; bu durumda
    /// SeslendirmeYoneticisi varsayilan "blabla" mirildanma sesini calar.</summary>
    public AudioClip AktifKlip
    {
        get
        {
            GameLanguage dil = LocalizationManager.Instance != null
                ? LocalizationManager.Instance.CurrentLanguage
                : GameLanguage.Turkish;

            if (dil == GameLanguage.English && ingilizce != null) return ingilizce;
            return turkce;
        }
    }

    /// <summary>Hic klip atanmamis mi? (ne Turkce ne Ingilizce) — UI/Editor'da "seslendirilmemis
    /// satir" gostergesi icin kullanilabilir.</summary>
    public bool BosMu { get { return turkce == null && ingilizce == null; } }
}
