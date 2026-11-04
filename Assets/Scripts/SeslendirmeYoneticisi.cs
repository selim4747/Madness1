// SeslendirmeYoneticisi.cs — Oyundaki TUM diyalog seslendirmesinin merkezi. DialogueBox her
// satir gosterdiginde burasi cagrilir: eger o satira ozel bir SesKlipSeti atanmissa
// (Inspector'dan surukleme, dile gore turkce/ingilizce) onu, atanmamissa CINSIYET + YAS
// GRUBUNA uygun rastgele bir "blabla" (mirildanma) sesi calar — 17 yasindaki bir kadin ile
// 40 yasindaki bir erkek ayni sesle mirildanmaz. Boylece OYUNDAKI HER KONUSMA bir sekilde
// seslendirilmis hissi verir — binlerce satiri tek tek elle kaydetmek ZORUNDA KALMADAN, ama
// istersen (Inspector'dan) herhangi bir satira ozel gercek seslendirme klibi de atayabilirsin.
//
// YERELLEŞTIRME UYUMLULUGU: gercek klipler SesKlipSeti.AktifKlip uzerinden aktif dile gore
// otomatik secilir. "Blabla" havuzlari ise kasitli olarak DIL-BAGIMSIZ tutulur (gercek kelime
// icermeyen mirildanma sesleri) — hangi dilde olursa olsun aynen calisir, sadece cinsiyet/yas
// gruplarina ayrilir.
//
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>Belirli bir (yas grubu, cinsiyet) kombinasyonu icin bir "blabla" ses havuzu.
/// Inspector'da bu tur birden fazla giris eklenir (orn. 6 yas grubu x 2 cinsiyet = en fazla
/// 12 giris) — hepsini doldurmak ZORUNLU degil, doldurulmayan kombinasyonlar icin en yakin
/// yedek (asagida VarsayilanBlablaSec'te aciklanan sirayla) kullanilir.</summary>
[Serializable]
public class BlablaHavuzu
{
    public YasGrubu yasGrubu;
    public Cinsiyet cinsiyet;

    [Tooltip("Bu yas grubu + cinsiyet kombinasyonu icin mirildanma sesleri. Birden fazla eklersen her seferinde rastgele biri calar.")]
    public AudioClip[] sesler;
}

public class SeslendirmeYoneticisi : MonoBehaviour
{
    public static SeslendirmeYoneticisi Instance { get; private set; }

    [Header("Varsayilan 'Blabla' Sesleri (ozel klip atanmamis satirlar icin — DIL BAGIMSIZ)")]
    [Tooltip("Yas grubu + cinsiyete gore ayri ayri ses havuzlari. Doldurulmayan kombinasyonlar icin cinsiyeti ayni olan EN YAKIN yas grubuna, o da yoksa herhangi bir gruba duser.")]
    [SerializeField] private List<BlablaHavuzu> blablaHavuzlari = new List<BlablaHavuzu>();

    [Header("Varyasyon")]
    [Tooltip("Ayni blabla sesi surekli tekrarlaninca monoton gelmesin diye kucuk rastgele perde (pitch) degisimi uygulanir.")]
    [SerializeField] private float pitchMinimum = 0.92f;
    [SerializeField] private float pitchMaksimum = 1.08f;

    [Header("Kisitlama")]
    [Tooltip("Ayni anda ust uste binen sesleri onlemek icin: bir onceki seslendirme uzerinden bu kadar sure gecmeden yenisi calinmaz (saniye).")]
    [SerializeField] private float minimumAralik = 0.05f;

    private AudioSource kaynak;
    private float sonCalmaZamani = -999f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        kaynak = GetComponent<AudioSource>();
        if (kaynak == null) kaynak = gameObject.AddComponent<AudioSource>();
        kaynak.playOnAwake = false;
    }

    /// <summary>DialogueBox tarafindan HER satir gosterildiginde cagrilir. sesSeti icinde
    /// (aktif dile gore) gercek bir klip varsa onu calar; yoksa cinsiyet+yas grubuna uygun
    /// rastgele bir "blabla" sesi calar. Ikisi de yoksa (havuzlar da bossa) sessiz kalinir,
    /// cokme olmaz.</summary>
    public void SatirSesiniCal(SesKlipSeti sesSeti, Cinsiyet cinsiyet, YasGrubu yasGrubu)
    {
        if (kaynak == null) return;
        if (Time.unscaledTime - sonCalmaZamani < minimumAralik) return;

        AudioClip klip = sesSeti.AktifKlip;
        if (klip == null) klip = VarsayilanBlablaSec(cinsiyet, yasGrubu);
        if (klip == null) return;

        kaynak.pitch = UnityEngine.Random.Range(pitchMinimum, pitchMaksimum);
        float sfxVolume = AudioManager.Instance != null ? AudioManager.Instance.SfxVolume : 1f;
        kaynak.PlayOneShot(klip, sfxVolume);
        sonCalmaZamani = Time.unscaledTime;
    }

    /// <summary>Once TAM eslesen (yas grubu + cinsiyet) havuzu arar. Bulamazsa AYNI cinsiyette
    /// EN YAKIN yas grubunu dener (YasGrubu enum sirasina gore mesafe). O da bossa, cinsiyete
    /// bakmadan HERHANGI bir dolu havuza duser. Hicbiri yoksa null (sessiz).</summary>
    private AudioClip VarsayilanBlablaSec(Cinsiyet cinsiyet, YasGrubu yasGrubu)
    {
        if (blablaHavuzlari == null || blablaHavuzlari.Count == 0) return null;

        // 1) Tam eslesme
        BlablaHavuzu tamEslesme = null;
        foreach (var h in blablaHavuzlari)
            if (h != null && h.cinsiyet == cinsiyet && h.yasGrubu == yasGrubu && h.sesler != null && h.sesler.Length > 0)
            { tamEslesme = h; break; }

        if (tamEslesme != null) return tamEslesme.sesler[UnityEngine.Random.Range(0, tamEslesme.sesler.Length)];

        // 2) Ayni cinsiyet, EN YAKIN yas grubu (enum sira farkina gore)
        BlablaHavuzu enYakin = null;
        int enYakinFark = int.MaxValue;
        foreach (var h in blablaHavuzlari)
        {
            if (h == null || h.cinsiyet != cinsiyet || h.sesler == null || h.sesler.Length == 0) continue;
            int fark = Mathf.Abs((int)h.yasGrubu - (int)yasGrubu);
            if (fark < enYakinFark) { enYakinFark = fark; enYakin = h; }
        }
        if (enYakin != null) return enYakin.sesler[UnityEngine.Random.Range(0, enYakin.sesler.Length)];

        // 3) Herhangi bir dolu havuz (son çare)
        foreach (var h in blablaHavuzlari)
            if (h != null && h.sesler != null && h.sesler.Length > 0)
                return h.sesler[UnityEngine.Random.Range(0, h.sesler.Length)];

        return null;
    }
}
