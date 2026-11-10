// GameManager.cs — Merkezi oyun akisi kontrolcusu
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using Madness.Mods;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Zamanlama")]
    [SerializeField] private float dayDurationSeconds = 180f;  // sabit 3 dakika

    [Header("Boss")]
    [SerializeField] private int bossEveryNDays = 10;

    [Header("Gun basi konumu")]
    [Tooltip("Her gun basinda (ve gun yeniden baslayinca) oyuncu buraya isinlanir.")]
    [SerializeField] private Transform respawnPoint;

    public GamePhase CurrentPhase { get; private set; } = GamePhase.Tutorial;
    public int CurrentDay { get; private set; } = 1;
    public float DayTimeRemaining { get; private set; }
    public bool IsDayTimeUp { get; private set; }

    public event Action<GamePhase> OnPhaseChanged;
    public event Action<int> OnDayChanged;
    public event Action OnDayTimeUp;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Ekran yonu: HER IKI YATAY yone de donebilsin, dikeye ASLA donmesin.
        // Once izin verilen yonler belirlenir, SONRA AutoRotation'a gecilir — sira onemli,
        // ters sirada Unity bir an icin izin verilmeyen bir yone donebiliyor.
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.orientation = ScreenOrientation.AutoRotation;
    }

    private void Update()
    {
        if (CurrentPhase != GamePhase.Day) return;
        TickDayTimer();
    }

    private void TickDayTimer()
    {
        if (IsDayTimeUp) return;

        DayTimeRemaining -= Time.deltaTime;
        if (DayTimeRemaining <= 0f)
        {
            DayTimeRemaining = 0f;
            MarkDayTimeUp();
        }
    }

    /// <summary>Sure doldu VEYA tum stok bitti — yeni spawn durur, "Geceye Gec" butonu cikar.</summary>
    public void MarkDayTimeUp()
    {
        if (IsDayTimeUp) return;
        IsDayTimeUp = true;
        OnDayTimeUp?.Invoke();
    }

    public void OnAllStockDepleted()
    {
        MarkDayTimeUp();
    }

    /// <summary>"Geceye Gec" butonundan cagrilir. Silahsizsa gece BASLAMAZ.</summary>
    public bool RequestGoToNight()
    {
        if (CurrentPhase != GamePhase.Day) return false;

        if (!WeaponInventory.Instance.HasAnyWeapon)
        {
            if (ToastNotification.Instance != null)
                ToastNotification.Instance.Show("Gece basliyor — once bir silah almalisin!", 4f);
            else
                DevLog.Warning("[GameManager] Silah yok, gece baslamiyor (ToastNotification sahnede yok, mesaj gosterilemedi).");
            return false;
        }

        // GUVENLIK: PhaseTransition sahnede yoksa faza HIC dokunma. Eskiden SetPhase(DayToNight)
        // burada erken calisiyordu, sonra asagidaki satirda cokup CurrentPhase'i DayToNight'ta
        // SONSUZA KADAR takili birakiyordu (Play modundan cikmadan asla Day'e donmuyordu).
        if (PhaseTransition.Instance == null)
        {
            DevLog.Error("[GameManager] PhaseTransition sahnede yok — gece gecisi baslatilamiyor.");
            return false;
        }

        SetPhase(GamePhase.DayToNight);

        // Bugun icin oynatilmamis bir cutscene varsa, geceye gecmeden ONCE oynatilir.
        StoryCutsceneData cutscene = StoryManager.Instance != null ? StoryManager.Instance.FindCutsceneForDay(CurrentDay) : null;

        if (cutscene != null)
        {
            StoryManager.Instance.PlayCutscene(cutscene, () =>
            {
                if (cutscene.isFinale)
                    TriggerStoryEnding();
                else
                    PhaseTransition.Instance.PlayDayToNight(BeginNight);
            });
        }
        else
        {
            PhaseTransition.Instance.PlayDayToNight(BeginNight);
        }

        return true;
    }

    private void BeginNight()
    {
        SetPhase(GamePhase.Night);
        WaveManager.Instance.StartNight(CurrentDay, IsBossNight(CurrentDay));
    }

    public bool IsBossNight(int day)
    {
        return day % bossEveryNDays == 0;
    }

    private void ShowDaySummary()
    {
        SetPhase(GamePhase.DaySummary);
        DaySummaryUI.Instance.Show(CurrentDay, StartNewDay);
    }

    /// <summary>Ozet ekrani kapaninca yeni gun baslar.</summary>
    public void StartNewDay()
    {
        CurrentDay++;
        PlayerProgressStats.SetHighestDay(CurrentDay);   // tek gercek kaynak
        PlayerProgressStats.ResetDailyCounters();
        DayContext dayContext = ModEventBus.Publish(new DayContext { Day = CurrentDay, DurationSeconds = dayDurationSeconds });
        if (dayContext.Cancelled) return;
        DayTimeRemaining = Mathf.Max(0f, dayContext.DurationSeconds);
        IsDayTimeUp = false;

        PhaseTransition.Instance.PlayNightToDay(() =>
        {
            SetPhase(GamePhase.Day);

            // KRITIK: gece baslarken dukkanda kalan musteriler CustomerSpawner.aktifMusteriler
            // listesinden hic cikmiyordu (sadece MusteriAyrildi() ile normal ayrilista
            // cikiyorlar) — birikince "hayalet" musteriler ayniAndaMaxMusteri limitini
            // dolduruyor, yeni musteri spawn'i sessizce sonsuza kadar duruyordu. Her yeni
            // gun, onceki gunden kalan HERKESI temizleyerek baslar.
            if (CustomerSpawner.Instance != null) CustomerSpawner.Instance.ClearAllCustomers();

            if (respawnPoint != null && PlayerController.Instance != null)
                PlayerController.Instance.Teleport(respawnPoint.position);
            OnDayChanged?.Invoke(CurrentDay);
            SaveSystem.Instance.SaveSnapshot();
        });
    }

    /// <summary>Tutorial bitince veya kayit yuklenince ilk gunu baslatir.</summary>
    public void StartFirstDay()
    {
        DayContext dayContext = ModEventBus.Publish(new DayContext { Day = CurrentDay, DurationSeconds = dayDurationSeconds });
        if (dayContext.Cancelled) return;
        DayTimeRemaining = Mathf.Max(0f, dayContext.DurationSeconds);
        IsDayTimeUp = false;
        PlayerProgressStats.ResetDailyCounters();

        SetPhase(GamePhase.Day);
        OnDayChanged?.Invoke(CurrentDay);
        SaveSystem.Instance.SaveSnapshot();
    }

    /// <summary>PlayerStats can sifirlaninca (DeathPromptUI vazgecince) cagirir — gunun basina donulur.</summary>
    public void OnPlayerDied()
    {
        WaveManager.Instance.ClearAllEnemies();
        if (CustomerSpawner.Instance != null) CustomerSpawner.Instance.ClearAllCustomers();

        SaveSystem.Instance.LoadSnapshot();

        DayTimeRemaining = dayDurationSeconds;
        IsDayTimeUp = false;
        PlayerStats.Instance.FullHeal();
        PlayerProgressStats.ResetDailyCounters();

        if (respawnPoint != null && PlayerController.Instance != null)
            PlayerController.Instance.Teleport(respawnPoint.position);

        if (ToastNotification.Instance != null)
            ToastNotification.Instance.Show("Oldun — gunun basina donuldu.", 4f);

        SetPhase(GamePhase.Day);
        OnDayChanged?.Invoke(CurrentDay);   // KRITIK: yoksa musteri spawn dongusu baslamaz
    }

    /// <summary>Hikaye finali tetiklendiginde StoryManager cagirir.</summary>
    public void TriggerStoryEnding()
    {
        SetPhase(GamePhase.StoryEnding);
    }

    public void SetPhase(GamePhase phase)
    {
        GamePhase previous = CurrentPhase;
        PhaseChangedContext context = ModEventBus.Publish(new PhaseChangedContext { PreviousPhase = previous, NextPhase = phase });
        if (context.Cancelled) return;
        CurrentPhase = context.NextPhase;
        OnPhaseChanged?.Invoke(CurrentPhase);
    }

    // ── Kayit koprusu ──

    public void ApplySave(SaveData data)
    {
        CurrentDay = data.currentDay;

        // NOT: Gun sayaci ve faz BURADA geri yuklenmez — cunku bu metottan SONRA calisan
        // StartFirstDay() onlari sifirlayip ezerdi. Onun yerine OturumDurumunuGeriYukle()
        // icinde, StartFirstDay'den SONRA geri yukleniyorlar.
    }

    public void OnNightCleared()
    {
        ShowDaySummary();
    }

    public void WriteToSave(SaveData data)
    {
        data.currentDay = CurrentDay;
    }

    /// <summary>Ana Menu'ye donerken cagrilir — o ANIN tam durumunu kayda yazar.</summary>
    public void OturumDurumunuYaz(SaveData data)
    {
        data.oturumDurumuVar = true;
        data.dayTimeRemaining = DayTimeRemaining;
        data.currentPhase = (int)CurrentPhase;

        if (PlayerController.Instance != null)
        {
            data.playerX = PlayerController.Instance.transform.position.x;
            data.playerY = PlayerController.Instance.transform.position.y;
        }

        if (KasaEtkilesim.Instance != null)
            data.mesaiSayaci = KasaEtkilesim.Instance.MesaiSayaciniAl();

        if (CustomerSpawner.Instance != null)
            data.aktifMusteriler = CustomerSpawner.Instance.TumMusterileriKaydet();
    }

    /// <summary>Sahne acilirken, kayitli bir oturum varsa o ani geri yukler.</summary>
    public void OturumDurumunuGeriYukle(SaveData data)
    {
        if (!data.oturumDurumuVar) return;

        // Gun sayaci ve faz — StartFirstDay() bunlari sifirladigi icin BURADA (ondan sonra)
        // geri yukleniyorlar.
        DayTimeRemaining = data.dayTimeRemaining;
        IsDayTimeUp = DayTimeRemaining <= 0f;
        SetPhase((GamePhase)data.currentPhase);

        if (PlayerController.Instance != null)
            PlayerController.Instance.Teleport(new Vector3(data.playerX, data.playerY, 0f));

        if (KasaEtkilesim.Instance != null)
            KasaEtkilesim.Instance.MesaiSayaciniGeriYukle(data.mesaiSayaci);

        if (CustomerSpawner.Instance != null)
            CustomerSpawner.Instance.TumMusterileriGeriYukle(data.aktifMusteriler);

        // Oturum bir kez geri yuklendikten sonra tuketilir — ayni durum tekrar tekrar
        // yuklenmesin (orn. olup gunun basina donunce eski oturum geri gelmesin).
        data.oturumDurumuVar = false;
    }
}