// GameEnums.cs — Tum oyunun ortak enum tanimlari
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM

/// <summary>Oyunun o anki ana fazi.</summary>
public enum GamePhase
{
    Tutorial,
    Day,
    Night,
    DayToNight,     // gunes batisi animasyonu
    NightToDay,     // gunes dogusu animasyonu
    DaySummary,     // gun sonu ozet ekrani
    Paused,
    StoryEnding     // hikaye finali
}

/// <summary>5 nadirlik seviyesi. Kitabin SABIT kimligidir, restock'ta degismez.</summary>
public enum Rarity
{
    Common,     // Siradan  — 1x  -> 40 Para
    Rare,       // Nadir    — 2x  -> 80 Para
    Epic,       // Epik     — 3x  -> 120 Para
    Legendary,  // Efsanevi — 5x  -> 200 Para
    Mythic      // Mitik    — 8x  -> 320 Para
}

/// <summary>3 musteri tipi. Dagilim: %60 / %30 / %10.</summary>
public enum CustomerType { Normal, Rich, VIP }

/// <summary>3 silah tipi. Her tipin 3 kademesi vardir.</summary>
public enum WeaponType { Pistol, Rifle, Shotgun }

/// <summary>Silah kademesi. Ust kademe alininca alt kademe otomatik degisir.</summary>
public enum WeaponTier { Mk1 = 1, Mk2 = 2, Mk3 = 3 }

/// <summary>4 canavar tipi.</summary>
public enum EnemyType { FastWeak, SlowStrong, Ranged, Tank }

/// <summary>Karakter yukseltme dallari.</summary>
public enum StatType { Health, Damage, Speed }

/// <summary>Odullu reklam turleri.</summary>
public enum RewardType { ExtraStock, Revive, CustomerLimitBoost }

/// <summary>Kara Kitapci teklif turleri.</summary>
public enum DealerOffer { BloodTrade, DoubleOrNothing, MysteryBag }

/// <summary>Basarim kategorileri.</summary>
public enum AchievementCategory
{
    Economy,      // Ekonomi
    Survival,     // Hayatta kalma
    Social,       // Sosyal
    Risk,         // Risk
    Collection    // Koleksiyon
}

/// <summary>
/// Bir basarimin hangi sayaci izledigi.
/// AchievementData bir ScriptableObject oldugu icin Func/delegate serilesemez —
/// bu yuzden "neyi sayiyor" bilgisi enum ile tutulur.
/// </summary>
public enum TrackedStat
{
    BooksSold,
    MythicBooksSold,
    ThievesCaught,
    StudentsServed,
    BossesKilled,
    DealerOffersAccepted,
    HighestDayReached,
    GoldEarnedToday,
    BooksSoldToday,
    BooksDiscovered,
    ShopsUnlocked,

    // ── Genisleme: yeni sistemlere bagli istatistikler ──
    EnemiesKilled,          // TUM dusman olumleri (Survival) — sadece boss degil
    TotalGoldEarned,        // Omur boyu kazanilan altin (Economy) — gunluk degil, TOPLAM
    UpgradesPurchased,      // Can/Hasar/Hiz/Personel yukseltmelerinin TOPLAMI (Economy)
    GardenVisitsServed,     // Bahce masasinda agirlanan musteri sayisi (Social)
    StoryGuestsMet,         // Tanisilan hikaye konugu sayisi (Social)
    UniqueCustomersMet,     // Katalogda tanisilan FARKLI musteri sayisi (Collection)
    LowHealthSurvivals,     // Kritik canla (%20 alti) hayatta kalinan an sayisi (Risk)
    VIPCustomersServed,     // Basariyla satis yapilan VIP musteri sayisi (Economy)

    // ── Genisleme 2: Ozel Raf, Silah, 2.Duzey Ogrenci, Fes, Sadik Musteri ──
    JokerBooksSold,         // Ozel raftan satilan TURSUZ joker kitap sayisi (Collection)
    UniqueBooksSold,        // Ozel raftan satilan, kisiye ozel essiz kitap sayisi (Collection)
    WeaponsPurchased,       // Satin alinan silah (herhangi tip/kademe) TOPLAMI (Economy)
    WeaponTypesMaxed,       // Mk3'e ulasan FARKLI silah tipi sayisi, 0-3 arasi (Economy)
    StudyDeskSessionsServed,// 2. Duzey ogrencinin calisma masasinda agirlandigi sayi (Social)
    FesSwapCount,           // Fes'in oyuncu/Afor arasinda el degistirdigi sayi (Social)
    RegularCustomersCount   // "Mudavim" olan FARKLI musteri sayisi (Social)
}

/// <summary>Desteklenen diller. Su an TR/EN aktif ama LISTE TABANLI kullanildigi icin
/// (bkz. DilMetniGirisi, SesKlipSeti) ileride yeni bir dil eklemek SADECE buraya bir
/// deger eklemek + Inspector'da ilgili listelere o dilin girisini eklemek demek —
/// hicbir mevcut yapi degismez.</summary>
public enum GameLanguage { Turkish = 0, English = 1 }

/// <summary>SESLENDIRME icin yas gruplari (varsayilan "blabla" mirildanma seslerinin
/// dogru sese sahip olmasi icin — bir cocuk, bir yasli ile ayni sesle mirildanmasin).
/// NPCKimligi.yas alanindan otomatik hesaplanir (bkz. NPCKimligi.GetYasGrubu).</summary>
public enum YasGrubu
{
    Cocuk,          // 10-15
    Genc,           // 16-20
    GencYetiskin,   // 21-30
    Yetiskin,       // 30-45 (sinirdaki 30 GencYetiskin'e sayilir, siralamada once o kontrol edilir)
    OrtaYas,        // 45-60
    Yasli           // 60+
}
