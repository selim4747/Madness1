// SaveSystem.cs — 3 slotlu kayit + gun basi otomatik snapshot
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System;
using System.IO;
using System.Collections;

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }
    public SaveData Current { get; private set; } = new SaveData();

    public const int SlotCount = 3;
    private const string SnapshotFileName = "day_snapshot.json";

    /// <summary>Su an hangi slota otomatik kayit yapiliyor (-1 = hicbiri).</summary>
    public int ActiveSlot { get; private set; } = -1;

    /// <summary>
    /// MainMenu'den Game sahnesine tasinan niyet.
    /// -1 = yok, >=0 = o slotu yukle, -2 = yeni oyun baslat.
    /// Statik oldugu icin sahne degisiminde silinmez.
    /// </summary>
    public static int PendingLoadSlot = -1;
    public const int PendingNewGame = -2;
    private static int pendingNewGameSlot = 0;

    private float sessionStartTime;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        sessionStartTime = Time.unscaledTime;
    }

    private void Start()
    {
        StartCoroutine(OtomatikKayitDongusu());
    }

    /// <summary>Aktif slota (varsa) her 10 saniyede bir sessizce kaydeder — oyuncunun
    /// hicbir sey yapmasina gerek kalmadan surekli guncel ilerleme diskte durur.</summary>
    private IEnumerator OtomatikKayitDongusu()
    {
        WaitForSeconds bekleme = new WaitForSeconds(10f);

        while (true)
        {
            yield return bekleme;

            if (ActiveSlot >= 0) SaveToSlot(ActiveSlot);
        }
    }

    private string SlotPath(int slot)
    {
        return Path.Combine(Application.persistentDataPath, "save_slot_" + slot + ".json");
    }

    private string SnapshotPath
    {
        get { return Path.Combine(Application.persistentDataPath, SnapshotFileName); }
    }

    // ── Mobil: uygulama arka plana alininca otomatik kaydet ──

    private void OnApplicationPause(bool paused)
    {
        if (paused && ActiveSlot >= 0) SaveToSlot(ActiveSlot);
    }

    private void OnApplicationQuit()
    {
        if (ActiveSlot >= 0) SaveToSlot(ActiveSlot);
    }

    // ── MainMenu tarafi: sadece niyeti isaretler, veriyi YUKLEMEZ ──

    /// <summary>MainMenu'deki "Devam Et" butonundan cagrilir.</summary>
    public static void RequestLoadSlot(int slot)
    {
        PendingLoadSlot = slot;
    }

    /// <summary>MainMenu'deki "Yeni Oyun" butonundan cagrilir.</summary>
    public static void RequestNewGame(int slot)
    {
        PendingLoadSlot = PendingNewGame;
        pendingNewGameSlot = slot;
    }

    /// <summary>Game sahnesinde GameInitializer tarafindan cagrilir.
    /// Bekleyen niyeti uygular ve tuketir.</summary>
    public bool ConsumePendingIntent()
    {
        if (PendingLoadSlot == PendingNewGame)
        {
            StartFreshGame(pendingNewGameSlot);
            PendingLoadSlot = -1;
            return false;   // yeni oyun -> tutorial baslasin
        }

        if (PendingLoadSlot >= 0)
        {
            LoadFromSlot(PendingLoadSlot);
            PendingLoadSlot = -1;
            return true;    // kayit yuklendi -> tutorial atlansin
        }

        return false;
    }

    // ── Gun basi snapshot (olum cezasi icin) ──

    public void SaveSnapshot()
    {
        GatherFromScene();
        WriteJson(SnapshotPath, Current, false);
    }

    public void LoadSnapshot()
    {
        SaveData loaded = ReadJson(SnapshotPath);
        if (loaded == null) return;
        Current = loaded;
        ApplyToScene();
    }

    // ── 3 slot ──

    public void SaveToSlot(int slot)
    {
        GatherFromScene();
        Current.saveTimestamp = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
        Current.totalPlayTimeSeconds += Time.unscaledTime - sessionStartTime;
        sessionStartTime = Time.unscaledTime;

        WriteJson(SlotPath(slot), Current, true);
        ActiveSlot = slot;
    }

    public bool SlotExists(int slot)
    {
        return File.Exists(SlotPath(slot));
    }

    public bool HasAnySave()
    {
        for (int i = 0; i < SlotCount; i++)
            if (SlotExists(i)) return true;
        return false;
    }

    public void LoadFromSlot(int slot)
    {
        SaveData loaded = ReadJson(SlotPath(slot));
        if (loaded == null) return;

        Current = loaded;
        ActiveSlot = slot;
        sessionStartTime = Time.unscaledTime;
        ApplyToScene();
    }

    public void DeleteSlot(int slot)
    {
        if (SlotExists(slot)) File.Delete(SlotPath(slot));
        if (ActiveSlot == slot) ActiveSlot = -1;
    }

    /// <summary>Yeni oyun. Statik sayaclar da sifirlanir — yoksa eski oyunun verisi sizar.</summary>
    public void StartFreshGame(int slot)
    {
        Current = new SaveData();
        ActiveSlot = slot;
        sessionStartTime = Time.unscaledTime;

        PlayerProgressStats.ResetAll();   // KRITIK: statik sinif bellekte kaliyordu
        if (File.Exists(SnapshotPath)) File.Delete(SnapshotPath);
    }

    /// <summary>Slot ozeti (ana menude "Gun 14 — 21.07.2026 15:30"). Bossa null doner.</summary>
    public SaveData PeekSlot(int slot)
    {
        return ReadJson(SlotPath(slot));
    }

    // ── Guvenli JSON okuma/yazma ──

    private void WriteJson(string path, SaveData data, bool prettyPrint)
    {
        try
        {
            File.WriteAllText(path, JsonUtility.ToJson(data, prettyPrint));
        }
        catch (Exception e)
        {
            DevLog.Error("[SaveSystem] Kayit yazilamadi: " + e.Message);
        }
    }

    /// <summary>Bozuk/eksik dosyada oyunu cokertmez, null doner.</summary>
    private SaveData ReadJson(string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
            SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            if (data == null) DevLog.Warning("[SaveSystem] Kayit bos: " + path);
            return data;
        }
        catch (Exception e)
        {
            DevLog.Error("[SaveSystem] Kayit bozuk, atlaniyor: " + e.Message);
            return null;
        }
    }

    // ── Sahne <-> SaveData kopruleri ──
    // NOT: BookCatalogUI ve LocalizationManager satirlari Bolum 5D yamasindan gelir.

    private void ApplyToScene()
    {
        PlayerProgressStats.ApplySave(Current);
        BookCatalogUI.ApplySaveStatic(Current);

        if (EconomyManager.Instance != null) EconomyManager.Instance.ApplySave(Current);
        if (PlayerStats.Instance != null) PlayerStats.Instance.ApplySave(Current);
        if (WeaponInventory.Instance != null) WeaponInventory.Instance.ApplySave(Current);
        if (ShopManager.Instance != null) ShopManager.Instance.ApplySave(Current);
        if (CharacterUpgradeManager.Instance != null) CharacterUpgradeManager.Instance.ApplySave(Current);
        if (PersonnelCapacityUpgrade.Instance != null) PersonnelCapacityUpgrade.Instance.ApplySave(Current);
        if (StudentDeskManager.Instance != null) StudentDeskManager.Instance.ApplySave(Current);
        if (SatisfactionTracker.Instance != null) SatisfactionTracker.Instance.ApplySave(Current);
        if (AchievementManager.Instance != null) AchievementManager.Instance.ApplySave(Current);
        if (StoryManager.Instance != null) StoryManager.Instance.ApplySave(Current);
        CustomerCatalogUI.ApplySaveStatic(Current);
        if (FesYonetici.Instance != null) FesYonetici.Instance.ApplySave(Current);
        if (HikayeKonuguYoneticisi.Instance != null) HikayeKonuguYoneticisi.Instance.ApplySave(Current);
        if (GeciciHikayeKonuguYoneticisi.Instance != null) GeciciHikayeKonuguYoneticisi.Instance.ApplySave(Current);
        if (OzelKitapDiyaloglariYoneticisi.Instance != null) OzelKitapDiyaloglariYoneticisi.Instance.ApplySave(Current);
        if (OzelRafController.Instance != null) OzelRafController.Instance.ApplySave(Current);
        if (SadikMusteriYoneticisi.Instance != null) SadikMusteriYoneticisi.Instance.ApplySave(Current);
        if (DedikoduYoneticisi.Instance != null) DedikoduYoneticisi.Instance.ApplySave(Current);
        if (SatinAlmaEnflasyonYoneticisi.Instance != null) SatinAlmaEnflasyonYoneticisi.Instance.ApplySave(Current);
        if (GameManager.Instance != null) GameManager.Instance.ApplySave(Current);
        if (AudioManager.Instance != null) AudioManager.Instance.ApplySave(Current);
        if (LocalizationManager.Instance != null) LocalizationManager.Instance.ApplySave(Current);
    }

    private void GatherFromScene()
    {
        PlayerProgressStats.WriteToSave(Current);
        BookCatalogUI.WriteToSaveStatic(Current);

        if (EconomyManager.Instance != null) EconomyManager.Instance.WriteToSave(Current);
        if (PlayerStats.Instance != null) PlayerStats.Instance.WriteToSave(Current);
        if (WeaponInventory.Instance != null) WeaponInventory.Instance.WriteToSave(Current);
        if (ShopManager.Instance != null) ShopManager.Instance.WriteToSave(Current);
        if (CharacterUpgradeManager.Instance != null) CharacterUpgradeManager.Instance.WriteToSave(Current);
        if (PersonnelCapacityUpgrade.Instance != null) PersonnelCapacityUpgrade.Instance.WriteToSave(Current);
        if (StudentDeskManager.Instance != null) StudentDeskManager.Instance.WriteToSave(Current);
        if (SatisfactionTracker.Instance != null) SatisfactionTracker.Instance.WriteToSave(Current);
        if (AchievementManager.Instance != null) AchievementManager.Instance.WriteToSave(Current);
        if (StoryManager.Instance != null) StoryManager.Instance.WriteToSave(Current);
        CustomerCatalogUI.WriteToSaveStatic(Current);
        if (FesYonetici.Instance != null) FesYonetici.Instance.WriteToSave(Current);
        if (HikayeKonuguYoneticisi.Instance != null) HikayeKonuguYoneticisi.Instance.WriteToSave(Current);
        if (GeciciHikayeKonuguYoneticisi.Instance != null) GeciciHikayeKonuguYoneticisi.Instance.WriteToSave(Current);
        if (OzelKitapDiyaloglariYoneticisi.Instance != null) OzelKitapDiyaloglariYoneticisi.Instance.WriteToSave(Current);
        if (OzelRafController.Instance != null) OzelRafController.Instance.WriteToSave(Current);
        if (SadikMusteriYoneticisi.Instance != null) SadikMusteriYoneticisi.Instance.WriteToSave(Current);
        if (DedikoduYoneticisi.Instance != null) DedikoduYoneticisi.Instance.WriteToSave(Current);
        if (SatinAlmaEnflasyonYoneticisi.Instance != null) SatinAlmaEnflasyonYoneticisi.Instance.WriteToSave(Current);
        if (GameManager.Instance != null) GameManager.Instance.WriteToSave(Current);
        if (AudioManager.Instance != null) AudioManager.Instance.WriteToSave(Current);
        if (LocalizationManager.Instance != null) LocalizationManager.Instance.WriteToSave(Current);
    }
}