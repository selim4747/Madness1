// AchievementGenerator.cs — 5 kategoriyi dolduran basarim setini TEK TIKLA olusturur.
// Menu: Delilik > Basarimlari Olustur
// Bu script SADECE Unity Editor'de calisir, oyuna dahil edilmez (Assets/Editor klasoru).
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public static class AchievementGenerator
{
    private struct Tanim
    {
        public string id, title, desc;
        public AchievementCategory kategori;
        public TrackedStat stat;
        public int hedef, odul;

        public Tanim(string id, string title, string desc, AchievementCategory kat, TrackedStat stat, int hedef, int odul)
        {
            this.id = id; this.title = title; this.desc = desc;
            this.kategori = kat; this.stat = stat; this.hedef = hedef; this.odul = odul;
        }
    }

    [MenuItem("Delilik/Basarimlari Olustur (5 Kategori Dolu)")]
    public static void Olustur()
    {
        string klasor = "Assets/Resources/Achievements";
        if (!Directory.Exists(klasor)) Directory.CreateDirectory(klasor);

        Tanim[] tumBasarimlar = new Tanim[]
        {
            // ══════════ EKONOMI ══════════
            new Tanim("ach_econ_sold_10",    "İlk Adımlar",        "10 kitap sat.",                       AchievementCategory.Economy, TrackedStat.BooksSold, 10, 50),
            new Tanim("ach_econ_sold_50",    "Sayfa Kurdu",        "50 kitap sat.",                       AchievementCategory.Economy, TrackedStat.BooksSold, 50, 150),
            new Tanim("ach_econ_sold_200",   "Kitapçının Gururu",  "200 kitap sat.",                      AchievementCategory.Economy, TrackedStat.BooksSold, 200, 400),
            new Tanim("ach_econ_sold_1000",  "Efsane Kitapçı",     "1000 kitap sat.",                     AchievementCategory.Economy, TrackedStat.BooksSold, 1000, 1500),
            new Tanim("ach_econ_mythic_1",   "Şanslı El",          "İlk mitik kitabını sat.",              AchievementCategory.Economy, TrackedStat.MythicBooksSold, 1, 300),
            new Tanim("ach_econ_mythic_10",  "Mitik Avcısı",       "10 mitik kitap sat.",                  AchievementCategory.Economy, TrackedStat.MythicBooksSold, 10, 800),
            new Tanim("ach_econ_gold_1000",  "Cebi Dolu",          "Toplamda 1000 altın kazan.",           AchievementCategory.Economy, TrackedStat.TotalGoldEarned, 1000, 100),
            new Tanim("ach_econ_gold_10000", "Zengin Esnaf",       "Toplamda 10.000 altın kazan.",         AchievementCategory.Economy, TrackedStat.TotalGoldEarned, 10000, 500),
            new Tanim("ach_econ_gold_100000","Servet Sahibi",      "Toplamda 100.000 altın kazan.",        AchievementCategory.Economy, TrackedStat.TotalGoldEarned, 100000, 2000),
            new Tanim("ach_econ_day_500",    "Bereketli Gün",      "Tek günde 500 altın kazan.",           AchievementCategory.Economy, TrackedStat.GoldEarnedToday, 500, 250),
            new Tanim("ach_econ_daysold_20", "Yoğun Mesai",        "Tek günde 20 kitap sat.",              AchievementCategory.Economy, TrackedStat.BooksSoldToday, 20, 250),
            new Tanim("ach_econ_vip_5",      "VIP Muamele",        "5 VIP müşteriye satış yap.",           AchievementCategory.Economy, TrackedStat.VIPCustomersServed, 5, 300),
            new Tanim("ach_econ_vip_25",     "Seçkin Müşteriler",  "25 VIP müşteriye satış yap.",          AchievementCategory.Economy, TrackedStat.VIPCustomersServed, 25, 900),
            new Tanim("ach_econ_upgrade_5",  "Gelişim Yolunda",    "5 yükseltme satın al.",                AchievementCategory.Economy, TrackedStat.UpgradesPurchased, 5, 200),
            new Tanim("ach_econ_upgrade_15", "Tam Donanım",        "15 yükseltme satın al.",               AchievementCategory.Economy, TrackedStat.UpgradesPurchased, 15, 600),

            // ══════════ HAYATTA KALMA ══════════
            new Tanim("ach_surv_kill_50",    "Gece Bekçisi",       "50 düşman öldür.",                     AchievementCategory.Survival, TrackedStat.EnemiesKilled, 50, 150),
            new Tanim("ach_surv_kill_500",   "Karanlığın Kabusu",  "500 düşman öldür.",                    AchievementCategory.Survival, TrackedStat.EnemiesKilled, 500, 700),
            new Tanim("ach_surv_kill_2000",  "Tentiforverse Efsanesi","2000 düşman öldür.",                AchievementCategory.Survival, TrackedStat.EnemiesKilled, 2000, 2000),
            new Tanim("ach_surv_boss_1",     "İlk Boss",           "İlk boss'u yen.",                      AchievementCategory.Survival, TrackedStat.BossesKilled, 1, 400),
            new Tanim("ach_surv_boss_5",     "Boss Avcısı",        "5 boss yen.",                          AchievementCategory.Survival, TrackedStat.BossesKilled, 5, 1200),
            new Tanim("ach_surv_day_10",     "On Gün Ayakta",      "10. güne ulaş.",                       AchievementCategory.Survival, TrackedStat.HighestDayReached, 10, 200),
            new Tanim("ach_surv_day_25",     "Çeyrek Yol",         "25. güne ulaş.",                       AchievementCategory.Survival, TrackedStat.HighestDayReached, 25, 500),
            new Tanim("ach_surv_day_50",     "Yarı Yolda",         "50. güne ulaş.",                       AchievementCategory.Survival, TrackedStat.HighestDayReached, 50, 1000),
            new Tanim("ach_surv_day_100",    "Delilik'in Sonu",    "100. güne ulaş.",                      AchievementCategory.Survival, TrackedStat.HighestDayReached, 100, 3000),

            // ══════════ SOSYAL ══════════
            new Tanim("ach_soc_garden_5",    "Çay Bahçesi",        "Bahçede 5 müşteri ağırla.",            AchievementCategory.Social, TrackedStat.GardenVisitsServed, 5, 150),
            new Tanim("ach_soc_garden_25",   "Sıcak Yuva",         "Bahçede 25 müşteri ağırla.",           AchievementCategory.Social, TrackedStat.GardenVisitsServed, 25, 500),
            new Tanim("ach_soc_guest_1",     "Beklenmedik Konuk",  "Bir hikaye konuğuyla tanış.",          AchievementCategory.Social, TrackedStat.StoryGuestsMet, 1, 250),
            new Tanim("ach_soc_guest_3",     "Tanıdık Yüzler",     "3 farklı hikaye konuğuyla tanış.",     AchievementCategory.Social, TrackedStat.StoryGuestsMet, 3, 600),
            new Tanim("ach_soc_student_10",  "Öğrenci Dostu",      "10 öğrenciye hizmet ver.",             AchievementCategory.Social, TrackedStat.StudentsServed, 10, 150),
            new Tanim("ach_soc_student_50",  "Kampüsün Gözdesi",   "50 öğrenciye hizmet ver.",             AchievementCategory.Social, TrackedStat.StudentsServed, 50, 450),
            new Tanim("ach_soc_thief_3",     "Tetikte",            "3 hırsız yakala.",                     AchievementCategory.Social, TrackedStat.ThievesCaught, 3, 200),
            new Tanim("ach_soc_thief_10",    "Dükkanın Bekçisi",   "10 hırsız yakala.",                    AchievementCategory.Social, TrackedStat.ThievesCaught, 10, 500),

            // ══════════ RISK ══════════
            new Tanim("ach_risk_low_5",      "Ucuz Atlattım",      "5 kez kritik canla hayatta kal.",      AchievementCategory.Risk, TrackedStat.LowHealthSurvivals, 5, 200),
            new Tanim("ach_risk_low_20",     "Ölümle Dans",        "20 kez kritik canla hayatta kal.",     AchievementCategory.Risk, TrackedStat.LowHealthSurvivals, 20, 600),

            // ══════════ KOLEKSIYON ══════════
            new Tanim("ach_coll_books_20",   "Meraklı Okur",       "20 farklı kitap türü/başlık keşfet.",  AchievementCategory.Collection, TrackedStat.BooksDiscovered, 20, 150),
            new Tanim("ach_coll_books_72",   "Kütüphane Ustası",   "72 farklı kitap keşfet (yarısı).",     AchievementCategory.Collection, TrackedStat.BooksDiscovered, 72, 600),
            new Tanim("ach_coll_books_144",  "Tam Katalog",        "Tüm kitapları (144) keşfet.",          AchievementCategory.Collection, TrackedStat.BooksDiscovered, 144, 1500),
            new Tanim("ach_coll_cust_5",     "Yeni Yüzler",        "5 farklı müşteriyle tanış.",           AchievementCategory.Collection, TrackedStat.UniqueCustomersMet, 5, 100),
            new Tanim("ach_coll_cust_15",    "Mahalle Sakinleri",  "15 farklı müşteriyle tanış.",          AchievementCategory.Collection, TrackedStat.UniqueCustomersMet, 15, 350),
            new Tanim("ach_coll_cust_30",    "Herkesi Tanıyorum",  "30 farklı müşteriyle tanış.",          AchievementCategory.Collection, TrackedStat.UniqueCustomersMet, 30, 800),

            // ══════════ GENISLEME 2: Ozel Raf, Silah, 2.Duzey Ogrenci, Fes, Sadik Musteri ══════════
            new Tanim("ach_coll_joker_10",   "Kimse Eli Boş Gitmesin", "10 joker kitap sat.",              AchievementCategory.Collection, TrackedStat.JokerBooksSold, 10, 250),
            new Tanim("ach_coll_joker_50",   "Herkese Bir Şey Var",    "50 joker kitap sat.",               AchievementCategory.Collection, TrackedStat.JokerBooksSold, 50, 700),
            new Tanim("ach_coll_unique_5",   "Eşsiz Anlar",            "5 kişiye özel eşsiz kitap sat.",    AchievementCategory.Collection, TrackedStat.UniqueBooksSold, 5, 400),
            new Tanim("ach_coll_unique_20",  "Kara Kitapların Efendisi","20 kişiye özel eşsiz kitap sat.",  AchievementCategory.Collection, TrackedStat.UniqueBooksSold, 20, 1200),

            new Tanim("ach_econ_weapon_1",   "İlk Silah",           "İlk silahını satın al.",              AchievementCategory.Economy, TrackedStat.WeaponsPurchased, 1, 100),
            new Tanim("ach_econ_weapon_6",   "Cephanelik",          "Toplam 6 silah satın al.",            AchievementCategory.Economy, TrackedStat.WeaponsPurchased, 6, 500),
            new Tanim("ach_econ_weapon_max1","Ustalaşma Yolunda",   "Bir silah tipini Mk3'e yükselt.",     AchievementCategory.Economy, TrackedStat.WeaponTypesMaxed, 1, 400),
            new Tanim("ach_econ_weapon_max3","Silah Ustası",        "Üç silah tipini de Mk3'e yükselt.",   AchievementCategory.Economy, TrackedStat.WeaponTypesMaxed, 3, 1500),

            new Tanim("ach_soc_study_10",    "Kütüphane Sessizliği","10 öğrenciyi çalışma masasında ağırla.",AchievementCategory.Social, TrackedStat.StudyDeskSessionsServed, 10, 200),
            new Tanim("ach_soc_study_40",    "Sınav Haftası",       "40 öğrenciyi çalışma masasında ağırla.",AchievementCategory.Social, TrackedStat.StudyDeskSessionsServed, 40, 650),

            new Tanim("ach_soc_fes_1",       "Fes Kimde?",          "Fes ilk kez el değiştirsin.",         AchievementCategory.Social, TrackedStat.FesSwapCount, 1, 100),
            new Tanim("ach_soc_fes_10",      "Fes Diplomasisi",     "Fes 10 kez el değiştirsin.",          AchievementCategory.Social, TrackedStat.FesSwapCount, 10, 350),

            new Tanim("ach_soc_regular_1",   "İlk Müdavimin",       "İlk müdavim müşterini kazan.",        AchievementCategory.Social, TrackedStat.RegularCustomersCount, 1, 200),
            new Tanim("ach_soc_regular_5",   "Mahalle Dostların",   "5 müdavim müşterin olsun.",           AchievementCategory.Social, TrackedStat.RegularCustomersCount, 5, 600),
            new Tanim("ach_soc_regular_15",  "Herkesin Sevgilisi",  "15 müdavim müşterin olsun.",          AchievementCategory.Social, TrackedStat.RegularCustomersCount, 15, 1500),
        };

        int olusturulan = 0, atlanildi = 0;

        foreach (Tanim t in tumBasarimlar)
        {
            string yol = klasor + "/" + t.id + ".asset";

            if (File.Exists(yol))
            {
                atlanildi++; // zaten var — uzerine yazmiyoruz, elle duzenlemis olabilirsin
                continue;
            }

            AchievementData a = ScriptableObject.CreateInstance<AchievementData>();
            a.achievementId = t.id;
            a.title = t.title;
            a.description = t.desc;
            a.category = t.kategori;
            a.trackedStat = t.stat;
            a.targetValue = t.hedef;
            a.goldReward = t.odul;

            AssetDatabase.CreateAsset(a, yol);
            olusturulan++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[AchievementGenerator] {olusturulan} yeni başarım oluşturuldu, {atlanildi} zaten vardı (atlandı). Konum: {klasor}");
        EditorUtility.DisplayDialog("Başarımlar Oluşturuldu",
            olusturulan + " yeni başarım oluşturuldu.\n" + atlanildi + " tanesi zaten vardı, dokunulmadı.\n\n" +
            "Konum: " + klasor + "\n\n" +
            "AchievementManager'in listesini elle doldurmana GEREK YOK — Resources/Achievements " +
            "klasöründeki her şeyi artık otomatik yüklüyor.",
            "Tamam");
    }
}
#endif