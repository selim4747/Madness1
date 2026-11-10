// SaveData.cs — Tum kayit verisi. JsonUtility uyumlu (sadece List kullanir).
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using System;
using System.Collections.Generic;

[Serializable]
public class WeaponSaveEntry
{
    public int weaponType;
    public int weaponTier;
}

[Serializable]
public class ShelfSaveEntry
{
    public string bookId;
    public int stock;
    public bool isUnlocked;
}

[Serializable]
public class HikayeIlerlemeKaydi
{
    public string hikayeId;
    public string aktifSegmentId;
}

[Serializable]
public class OzelKitapDiyalogKaydi
{
    public string musteriIsmi;
    public List<string> acilanDiyaloglar = new List<string>();
}

[Serializable]
public class DedikoduKaydi
{
    public string dedikoduId;
    public bool ogrenildi;
    public bool paylasildi;
}

[Serializable]
public class CustomerSaveEntry
{
    public int prefabTipi;        // 0=normal, 1=zengin, 2=vip
    public float x, y;
    public int durum;             // CustomerController.State enum degeri
    public string kimlikIsmi;     // hangi NPCKimligi secilmisti
    public string tasinanKitapId; // elinde kitap varsa
    public float bekleyisSayaci;
    public float sabirSayaci;
}

[Serializable]
public class SaveData
{
    // Modlar ana kayıt şemasını değiştirmeden kendi durumlarını isim alanında saklar.
    public List<Madness.Mods.ModSaveEntry> modStates = new List<Madness.Mods.ModSaveEntry>();
    // --- OTURUM DURUMU (Ana Menu'ye donunce ayni saniyeden devam icin) ---
    public bool oturumDurumuVar = false;
    public float playerX = 0f;
    public float playerY = 0f;
    public float dayTimeRemaining = 0f;
    public int currentPhase = (int)GamePhase.Day;
    public float mesaiSayaci = 0f;
    public List<CustomerSaveEntry> aktifMusteriler = new List<CustomerSaveEntry>();

    // --- Genel ilerleme ---
    public int currentDay = 1;
    public int gold = 0;
    public bool tutorialCompleted = false;

    // --- Slot bilgisi (ana menude gosterilir) ---
    public string saveTimestamp = "";
    public float totalPlayTimeSeconds = 0f;

    // --- Karakter statlari ---
    public int maxHealth = 10;
    public float damageStat = 2f;
    public float speedStat = 1.5f;

    // --- Yukseltme seviyeleri ---
    public int healthLevel = 0;
    public int damageLevel = 0;
    public int speedLevel = 0;
    public int personnelCapacityLevel = 0;

    // --- Ekonomi Enflasyonu (EKONOMİ SIKILAŞTIRMA — çılgın olmayan ama gerekli özellik) ---
    public float ekonomiEnflasyonCarpani = 1f;

    // --- Silahlar ---
    public List<WeaponSaveEntry> ownedWeapons = new List<WeaponSaveEntry>();
    public int equippedWeaponType = (int)WeaponType.Pistol;

    // --- Dukkan / Raf ---
    public int shopsUnlocked = 1;
    public List<ShelfSaveEntry> shelves = new List<ShelfSaveEntry>();
    public List<int> unlockedDeskIndices = new List<int>();

    // --- Musteri / Memnuniyet ---
    public float dailyCapacity = 10f;
    public float lastSatisfactionRatio = 1f;

    // --- Katalog & Basarimlar ---
    public List<string> discoveredBookIds = new List<string>();
    public List<string> metCustomerNames = new List<string>();
    public List<string> revealedCustomerPreferences = new List<string>(); // "musteriIsmi|KitapTuru" formatinda
    public List<string> unlockedAchievementIds = new List<string>();
    public bool metaBasarimTamamlandi = false;
    public bool ozelRafAcikMi = false;
    public List<IliskiKaydi> musteriIliskileri = new List<IliskiKaydi>();
    public List<DedikoduKaydi> dedikoduKayitlari = new List<DedikoduKaydi>();

    public int totalJokerBooksSold = 0;
    public int totalUniqueBooksSold = 0;
    public int totalWeaponsPurchased = 0;
    public int totalWeaponTypesMaxed = 0;
    public int totalStudyDeskSessionsServed = 0;
    public int totalFesSwapCount = 0;
    public int totalRegularCustomersCount = 0;

    // --- Kumulatif sayaclar ---
    public int totalBooksSold = 0;
    public int totalMythicBooksSold = 0;
    public int totalThievesCaught = 0;
    public int totalStudentsServed = 0;
    public int totalBossesKilled = 0;
    public int totalDealerOffersAccepted = 0;
    public int highestDayReached = 1;

    // ── Genisleme: yeni sistemlere bagli sayaclar ──
    public int totalEnemiesKilled = 0;
    public int totalGoldEarnedLifetime = 0;
    public int totalUpgradesPurchased = 0;
    public int totalGardenVisitsServed = 0;
    public int totalStoryGuestsMet = 0;
    public int totalUniqueCustomersMet = 0;
    public int totalLowHealthSurvivals = 0;
    public int totalVIPCustomersServed = 0;

    // --- Hikaye ---
    public List<string> triggeredStoryEventIds = new List<string>();
    public List<string> tanisilanKonukIdleri = new List<string>();
    public List<string> tamamlananOzelHikayeler = new List<string>();
    public List<HikayeIlerlemeKaydi> aktifOzelHikayeIlerlemeleri = new List<HikayeIlerlemeKaydi>();
    public List<OzelKitapDiyalogKaydi> acilanOzelKitapDiyaloglari = new List<OzelKitapDiyalogKaydi>();
    public bool fesOlayiOldu = false;
    public bool fesAforda = false;

    // --- Ayarlar ---
    public float musicVolume = 0.7f;
    public float sfxVolume = 1f;
    public int language = (int)GameLanguage.Turkish;
}
