// PlayerProgressStats.cs — Basarimlar icin kumulatif sayaclar
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM

using UnityEngine;

/// <summary>
/// Oyun boyunca biriken sayaclar. AchievementManager bunlari okur.
/// Statik cunku her sistemden tek satirla artirilabilmeli.
/// DIKKAT: statik veri sahne degisiminde bellekte kalir — yeni oyunda ResetAll() sart.
/// </summary>
public static class PlayerProgressStats
{
    public static int BooksSold { get; private set; }
    public static int MythicBooksSold { get; private set; }
    public static int ThievesCaught { get; private set; }
    public static int StudentsServed { get; private set; }
    public static int BossesKilled { get; private set; }
    public static int DealerOffersAccepted { get; private set; }
    public static int HighestDayReached { get; private set; } = 1;
    public static int VIPCustomersServed { get; private set; }
    public static int UniqueCustomersMet { get; private set; }
    public static int GardenVisitsServed { get; private set; }
    public static int StoryGuestsMet { get; private set; }
    public static int LowHealthSurvivals { get; private set; }

    // Bu ucu baska scriptler (EnemyController, CharacterUpgradeManager, PersonnelCapacityUpgrade)
    // ZATEN cagiriyor — kaldirilirsa proje derlenmez, o yuzden burada duruyorlar.
    public static int EnemiesKilled { get; private set; }
    public static int TotalGoldEarned { get; private set; }
    public static int UpgradesPurchased { get; private set; }

    // ── Genisleme 2 ──
    public static int JokerBooksSold { get; private set; }
    public static int UniqueBooksSold { get; private set; }
    public static int WeaponsPurchased { get; private set; }
    public static int WeaponTypesMaxed { get; private set; }
    public static int StudyDeskSessionsServed { get; private set; }
    public static int FesSwapCount { get; private set; }
    public static int RegularCustomersCount { get; private set; }

    // Gunluk sayaclar — her gun basinda sifirlanir
    public static int GoldEarnedToday { get; private set; }
    public static int BooksSoldToday { get; private set; }

    public static void AddBookSold(Rarity rarity)
    {
        BooksSold++;
        BooksSoldToday++;
        if (rarity == Rarity.Mythic) MythicBooksSold++;
    }

    public static void AddThiefCaught() { ThievesCaught++; }
    public static void AddStudentServed() { StudentsServed++; }
    public static void AddBossKilled() { BossesKilled++; }
    public static void AddDealerOfferAccepted() { DealerOffersAccepted++; }
    public static void AddGoldToday(int amount)
    {
        GoldEarnedToday += amount;
        TotalGoldEarned += amount; // omur boyu toplam da birlikte artar
    }
    public static void AddVIPCustomerServed() { VIPCustomersServed++; }
    public static void AddUniqueCustomerMet() { UniqueCustomersMet++; }
    public static void AddGardenVisitServed() { GardenVisitsServed++; }
    public static void AddStoryGuestMet() { StoryGuestsMet++; }
    public static void AddLowHealthSurvival() { LowHealthSurvivals++; }
    public static void AddEnemyKilled() { EnemiesKilled++; }
    public static void AddUpgradePurchased() { UpgradesPurchased++; }

    // ── Genisleme 2 ──
    public static void AddJokerBookSold() { JokerBooksSold++; }
    public static void AddUniqueBookSold() { UniqueBooksSold++; }
    public static void AddWeaponPurchased() { WeaponsPurchased++; }
    /// <summary>Increment DEGIL — dogrudan SET edilir (o an Mk3'te olan FARKLI tip sayisi).</summary>
    public static void SetWeaponTypesMaxed(int deger) { WeaponTypesMaxed = Mathf.Max(WeaponTypesMaxed, deger); }
    public static void AddStudyDeskSession() { StudyDeskSessionsServed++; }
    public static void AddFesSwap() { FesSwapCount++; }
    public static void AddRegularCustomer() { RegularCustomersCount++; }

    public static void SetHighestDay(int day)
    {
        if (day > HighestDayReached) HighestDayReached = day;
    }

    public static void ResetDailyCounters()
    {
        GoldEarnedToday = 0;
        BooksSoldToday = 0;
    }

    /// <summary>Yeni oyun baslarken cagrilir — statik veri sizintisini onler.</summary>
    public static void ResetAll()
    {
        BooksSold = 0;
        MythicBooksSold = 0;
        ThievesCaught = 0;
        StudentsServed = 0;
        BossesKilled = 0;
        DealerOffersAccepted = 0;
        HighestDayReached = 1;
        VIPCustomersServed = 0;
        UniqueCustomersMet = 0;
        GardenVisitsServed = 0;
        StoryGuestsMet = 0;
        LowHealthSurvivals = 0;
        EnemiesKilled = 0;
        TotalGoldEarned = 0;
        UpgradesPurchased = 0;
        JokerBooksSold = 0;
        UniqueBooksSold = 0;
        WeaponsPurchased = 0;
        WeaponTypesMaxed = 0;
        StudyDeskSessionsServed = 0;
        FesSwapCount = 0;
        RegularCustomersCount = 0;
        ResetDailyCounters();
    }

    public static void ApplySave(SaveData data)
    {
        BooksSold = data.totalBooksSold;
        MythicBooksSold = data.totalMythicBooksSold;
        ThievesCaught = data.totalThievesCaught;
        StudentsServed = data.totalStudentsServed;
        BossesKilled = data.totalBossesKilled;
        DealerOffersAccepted = data.totalDealerOffersAccepted;
        HighestDayReached = data.highestDayReached;
        VIPCustomersServed = data.totalVIPCustomersServed;
        UniqueCustomersMet = data.totalUniqueCustomersMet;
        GardenVisitsServed = data.totalGardenVisitsServed;
        StoryGuestsMet = data.totalStoryGuestsMet;
        LowHealthSurvivals = data.totalLowHealthSurvivals;
        EnemiesKilled = data.totalEnemiesKilled;
        TotalGoldEarned = data.totalGoldEarnedLifetime;
        UpgradesPurchased = data.totalUpgradesPurchased;
        JokerBooksSold = data.totalJokerBooksSold;
        UniqueBooksSold = data.totalUniqueBooksSold;
        WeaponsPurchased = data.totalWeaponsPurchased;
        WeaponTypesMaxed = data.totalWeaponTypesMaxed;
        StudyDeskSessionsServed = data.totalStudyDeskSessionsServed;
        FesSwapCount = data.totalFesSwapCount;
        RegularCustomersCount = data.totalRegularCustomersCount;
        ResetDailyCounters();
    }

    public static void WriteToSave(SaveData data)
    {
        data.totalBooksSold = BooksSold;
        data.totalMythicBooksSold = MythicBooksSold;
        data.totalThievesCaught = ThievesCaught;
        data.totalStudentsServed = StudentsServed;
        data.totalBossesKilled = BossesKilled;
        data.totalDealerOffersAccepted = DealerOffersAccepted;
        data.highestDayReached = HighestDayReached;
        data.totalVIPCustomersServed = VIPCustomersServed;
        data.totalUniqueCustomersMet = UniqueCustomersMet;
        data.totalGardenVisitsServed = GardenVisitsServed;
        data.totalStoryGuestsMet = StoryGuestsMet;
        data.totalLowHealthSurvivals = LowHealthSurvivals;
        data.totalEnemiesKilled = EnemiesKilled;
        data.totalGoldEarnedLifetime = TotalGoldEarned;
        data.totalUpgradesPurchased = UpgradesPurchased;
        data.totalJokerBooksSold = JokerBooksSold;
        data.totalUniqueBooksSold = UniqueBooksSold;
        data.totalWeaponsPurchased = WeaponsPurchased;
        data.totalWeaponTypesMaxed = WeaponTypesMaxed;
        data.totalStudyDeskSessionsServed = StudyDeskSessionsServed;
        data.totalFesSwapCount = FesSwapCount;
        data.totalRegularCustomersCount = RegularCustomersCount;
    }

    /// <summary>TrackedStat enum'ini gercek sayaca cevirir. Hem AchievementManager hem
    /// GorevYoneticisi (ve ileride baska sistemler) BURADAN okur — tek kaynak, tekrar yok.</summary>
    public static int GetStatValue(TrackedStat stat)
    {
        switch (stat)
        {
            case TrackedStat.BooksSold: return BooksSold;
            case TrackedStat.MythicBooksSold: return MythicBooksSold;
            case TrackedStat.ThievesCaught: return ThievesCaught;
            case TrackedStat.StudentsServed: return StudentsServed;
            case TrackedStat.BossesKilled: return BossesKilled;
            case TrackedStat.DealerOffersAccepted: return DealerOffersAccepted;
            case TrackedStat.HighestDayReached: return HighestDayReached;
            case TrackedStat.GoldEarnedToday: return GoldEarnedToday;
            case TrackedStat.BooksSoldToday: return BooksSoldToday;
            case TrackedStat.BooksDiscovered: return BookCatalogUI.DiscoveredCount;
            case TrackedStat.ShopsUnlocked: return EconomyManager.Instance != null ? EconomyManager.Instance.ShopsUnlocked : 0;

            case TrackedStat.EnemiesKilled: return EnemiesKilled;
            case TrackedStat.TotalGoldEarned: return TotalGoldEarned;
            case TrackedStat.UpgradesPurchased: return UpgradesPurchased;
            case TrackedStat.GardenVisitsServed: return GardenVisitsServed;
            case TrackedStat.StoryGuestsMet: return StoryGuestsMet;
            case TrackedStat.UniqueCustomersMet: return UniqueCustomersMet;
            case TrackedStat.LowHealthSurvivals: return LowHealthSurvivals;
            case TrackedStat.VIPCustomersServed: return VIPCustomersServed;

            case TrackedStat.JokerBooksSold: return JokerBooksSold;
            case TrackedStat.UniqueBooksSold: return UniqueBooksSold;
            case TrackedStat.WeaponsPurchased: return WeaponsPurchased;
            case TrackedStat.WeaponTypesMaxed: return WeaponTypesMaxed;
            case TrackedStat.StudyDeskSessionsServed: return StudyDeskSessionsServed;
            case TrackedStat.FesSwapCount: return FesSwapCount;
            case TrackedStat.RegularCustomersCount: return RegularCustomersCount;

            default: return 0;
        }
    }
}