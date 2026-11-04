using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class NPCKimligi
{
    [Header("Kimlik Bilgileri")]
    public string musteriIsmi = "İsimsiz Karakter";
    public Cinsiyet cinsiyet = Cinsiyet.Erkek;

    [Tooltip("SESLENDIRME icin: dogru yas grubunda 'blabla' sesi calinmasi icin kullanilir (bkz. GetYasGrubu). 17 yasindaki biri ile 40 yasindaki biri ayni mirildanma sesiyle konusmasin diye.")]
    [Range(10, 90)]
    public int yas = 30;

    [Header("Lore & Arka Plan")]
    [TextArea(2, 4)]
    public string loreHikayesi = "Evrende sıradan bir vatandaş."; // Lore buraya eklendi!

    [Header("Kişisel Zevkler")]
    public List<KitapTuru> sevilenTurler = new List<KitapTuru>(); // TÜR DİREKT KİŞİYE TAŞINDI!

    [Tooltip("Bu karakterin HIC sevmedigi turler. Bu turdeki bir rafa gitmez — sevdigi tur yoksa direkt kasaya gider.")]
    public List<KitapTuru> sevmedigiTurler = new List<KitapTuru>();

    [Header("Kişilik (oyunu gercekten etkiler)")]
    [Tooltip("Sabir carpani. 1 = normal (30sn). 0.5 = cabuk sinirlenir (15sn). 2 = cok sabirli (60sn).")]
    [Range(0.2f, 3f)]
    public float sabirCarpani = 1f;

    [Tooltip("Odeme carpani. 1 = normal fiyat. 1.5 = comert, %50 fazla oder. 0.8 = pazarlikci, az oder.")]
    [Range(0.5f, 3f)]
    public float odemeCarpani = 1f;

    [Tooltip("Raftaki kitabi inceleme suresi carpani. 1 = normal (5sn). Buyuk = uzun uzun inceler.")]
    [Range(0.2f, 3f)]
    public float incelemeSuresiCarpani = 1f;

    [Header("Meslek / Detay (katalogda gorunur)")]
    public string meslek = "";

    [Header("Diyaloglar")]
    [TextArea(2, 5)]
    public List<string> ozelDiyaloglar = new List<string>();

    [Header("Gorunum")]
    [Tooltip("Bu karaktere ozel tisort/kiyafet gorseli. Bos birakilirsa prefabin varsayilan tisortu kullanilir.")]
    public Sprite tisortSprite;

    [Header("Bahce Sosyallesmesi")]
    [Tooltip("Isaretliyse bu karakter %10 ihtimalle bahce masasina oturup sohbet edebilir. Isaretsizse hic bahceye gitmez.")]
    public bool sosyal = true;

    [Tooltip("SADECE bahce masasindayken kullanilir — normal magaza diyalogundan (Ozel Diyaloglar) AYRI bir havuz. Artik KONU ETIKETLI: her satir BahceKonuVerisi'ndeki paylasimli konu agina baglanabilir (ayni konudayken bu KISIYE OZEL satir da secilebilir olur).")]
    public List<BahceCumlesi> bahceDiyaloglari = new List<BahceCumlesi>();

    [Header("Gunluk Konusma Siniri")]
    [Tooltip("Bu karakterle gunde en fazla kac kere YENI bir laf duyabilirsin (0 = sinirsiz). Limit dolunca sadece kapanis lafi gelir.")]
    public int gunlukKonusmaLimiti = 3;

    [Header("OZEL GUN KARAKTERI (turist, gonderme karakterleri vs.)")]
    [Tooltip("ISARETSIZ: normal karakter, her gun gelebilir. ISARETLI: sadece asagida belirtilen gunlerde gelir.")]
    public bool ozelGunKarakteri = false;

    [Tooltip("Tam olarak bu gunlerde gelir (orn. 7, 24, 50). Bos birakabilirsin.")]
    public List<int> gorunecegiGunler = new List<int>();

    [Tooltip("Her KAC gunde bir gelsin (orn. 10 = her 10. gunde gelir). 0 = kullanma.")]
    public int herKacGundeBir = 0;

    [Tooltip("Bu gun araliginda gelebilir (orn. 30-40 arasi bir turist sezonu). Ikisi de 0 ise kullanilmaz.")]
    public int enErkenGun = 0;
    public int enGecGun = 0;

    [Header("Çoklu Kişilik (opsiyonel — ÇILGIN ÖZELLİK)")]
    [Tooltip("İşaretliyse, bu karakter bazı gelişlerinde TAMAMEN FARKLI bir isim/kişilikle gelebilir — aynı görsel, farklı isim ve farklı diyalog. Kime uygulayacağını SEN seçersin (herkese açmak zorunda değilsin).")]
    public bool cokluKisilikliMi = false;

    [Tooltip("Her gelişinde, diğer kişiliğin ortaya çıkma ihtimali (0-1). Örn: 0.25 = 4 gelişten 1'inde diğer kişilikle gelir.")]
    [Range(0f, 1f)]
    public float digerKisilikIhtimali = 0.25f;

    [Tooltip("Diğer kişiliğin adı — bu aktifken müşteri bu isimle görünür/hitap edilir.")]
    public string digerKisilikIsmi = "";

    [TextArea(2, 5)]
    [Tooltip("Diğer kişilik aktifken kullanılacak diyaloglar — normal 'Özel Diyaloglar' YERİNE geçer.")]
    public List<string> digerKisilikDiyaloglari = new List<string>();

    /// <summary>SESLENDIRME icin: yas degerinden yas grubunu hesaplar (10-15 Cocuk, 16-20
    /// Genc, 21-30 GencYetiskin, 30-45 Yetiskin, 45-60 OrtaYas, 60+ Yasli). Sinirlar SIRAYLA
    /// kontrol edilir, yani tam 30 -> GencYetiskin, tam 45 -> Yetiskin, tam 60 -> OrtaYas sayilir.</summary>
    public YasGrubu GetYasGrubu()
    {
        if (yas <= 15) return YasGrubu.Cocuk;
        if (yas <= 20) return YasGrubu.Genc;
        if (yas <= 30) return YasGrubu.GencYetiskin;
        if (yas <= 45) return YasGrubu.Yetiskin;
        if (yas <= 60) return YasGrubu.OrtaYas;
        return YasGrubu.Yasli;
    }

    /// <summary>Bu karakter belirtilen gunde dukkana gelebilir mi?</summary>
    public bool BuGunGelebilirMi(int gun)
    {
        // Normal karakterler her gun gelebilir.
        if (!ozelGunKarakteri) return true;

        // Ozel gun karakteri: asagidaki kosullardan EN AZ BIRI tutmali.
        if (gorunecegiGunler != null && gorunecegiGunler.Contains(gun)) return true;

        if (herKacGundeBir > 0 && gun % herKacGundeBir == 0) return true;

        if (enErkenGun > 0 && enGecGun > 0 && gun >= enErkenGun && gun <= enGecGun) return true;

        return false;
    }
}

[CreateAssetMenu(fileName = "YeniMusteriTuru", menuName = "Delilik/Customer Data")]
public class CustomerData : ScriptableObject
{
    [Header("Bu Müşteri Türüne Ait Karakter Havuzu")]
    public List<NPCKimligi> karakterHavuzu = new List<NPCKimligi>();

    [Header("Genel Alışveriş Limitleri (Bütçe/Nadirlik)")]
    public Rarity preferredRarity = Rarity.Common;
    public int budget = 100;
}
