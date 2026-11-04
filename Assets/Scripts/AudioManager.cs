// AudioManager.cs — Muzik (gunduz/gece ayri, fade'li) ve efekt yonetimi
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Kaynaklar")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Muzikler")]
    [SerializeField] private AudioClip dayMusic;
    [SerializeField] private AudioClip nightMusic;
    [SerializeField] private AudioClip menuMusic;

    [Header("Ortak efektler (tek ses — hicbir varyasyon eklemezsen bu kullanilir)")]
    [SerializeField] private AudioClip sellSound;
    [SerializeField] private AudioClip purchaseSound;
    [SerializeField] private AudioClip alarmSound;
    [SerializeField] private AudioClip achievementSound;

    [Header("Ses Varyasyonlari (opsiyonel — doldurursan bunlardan RASTGELE secilir)")]
    [Tooltip("Birden fazla varyasyon eklersen, ayni sesi surekli tekrar tekrar duymak yerine her seferinde farkli biri calar.")]
    [SerializeField] private AudioClip[] sellSoundVaryasyonlari;
    [SerializeField] private AudioClip[] purchaseSoundVaryasyonlari;
    [SerializeField] private AudioClip[] alarmSoundVaryasyonlari;
    [SerializeField] private AudioClip[] achievementSoundVaryasyonlari;

    [Header("Gecis")]
    [SerializeField] private float fadeDuration = 1f;

    public float MusicVolume { get; private set; } = 0.7f;
    public float SfxVolume { get; private set; } = 1f;

    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnPhaseChanged += HandlePhaseChanged;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        // Gece bitince oyun sirayla NightToDay -> DaySummary -> Day fazlarindan gecer.
        // Eskiden sadece "Day" taninıyordu, o yuzden aradaki gecis fazlarinda GECE muzigi
        // calmaya devam ediyordu. Artik gece DISINDAKI tum gunduz-tarafi fazlar gunduz
        // muzigine gecer; geceye giris (DayToNight) ise hemen gece muzigini baslatir.
        if (phase == GamePhase.Night || phase == GamePhase.DayToNight)
        {
            PlayMusic(nightMusic);
        }
        else if (phase == GamePhase.Day || phase == GamePhase.Tutorial ||
                 phase == GamePhase.NightToDay || phase == GamePhase.DaySummary)
        {
            PlayMusic(dayMusic);
        }
    }

    public void PlayMenuMusic()
    {
        PlayMusic(menuMusic);
    }

    private void PlayMusic(AudioClip clip)
    {
        if (musicSource == null) return;

        // Klip YOKSA (orn. gunduz muzigi henuz hazir degil), eskiden hicbir sey yapmadan
        // donuyorduk — bu yuzden gece muzigi gunduze gecince de calmaya DEVAM ediyordu.
        // Artik klip yoksa mevcut muzik yumusakca kisilip durduruluyor.
        if (clip == null)
        {
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeOutVeDurdur());
            return;
        }

        if (musicSource.clip == clip && musicSource.isPlaying) return;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeToClip(clip));
    }

    /// <summary>Mevcut muzigi yumusakca kisip durdurur (yeni klip verilmediginde).</summary>
    private IEnumerator FadeOutVeDurdur()
    {
        float startVolume = musicSource.volume;
        float t = 0f;

        while (t < fadeDuration && musicSource.isPlaying)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeDuration);
            yield return null;
        }

        musicSource.Stop();
        musicSource.clip = null;
        musicSource.volume = MusicVolume; // ayarlardaki gercek ses seviyesine geri don
    }

    private IEnumerator FadeToClip(AudioClip newClip)
    {
        // Mevcut muzigi kis
        float startVolume = musicSource.volume;
        float t = 0f;
        while (t < fadeDuration && musicSource.isPlaying)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeDuration);
            yield return null;
        }

        musicSource.clip = newClip;
        musicSource.loop = true;
        musicSource.Play();

        // Yeni muzigi ac
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(0f, MusicVolume, t / fadeDuration);
            yield return null;
        }
        musicSource.volume = MusicVolume;
        fadeRoutine = null;
    }

    public void PlaySfx(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, SfxVolume);
    }

    public void PlaySell() { PlaySfx(SecRastgele(sellSoundVaryasyonlari, sellSound)); }
    public void PlayPurchase() { PlaySfx(SecRastgele(purchaseSoundVaryasyonlari, purchaseSound)); }
    public void PlayAlarm() { PlaySfx(SecRastgele(alarmSoundVaryasyonlari, alarmSound)); }
    public void PlayAchievement() { PlaySfx(SecRastgele(achievementSoundVaryasyonlari, achievementSound)); }

    /// <summary>Varyasyon listesi doluysa rastgele birini, bosSa eski/tek klip'i doner.</summary>
    private AudioClip SecRastgele(AudioClip[] varyasyonlar, AudioClip yedek)
    {
        if (varyasyonlar != null && varyasyonlar.Length > 0)
            return varyasyonlar[UnityEngine.Random.Range(0, varyasyonlar.Length)];
        return yedek;
    }

    public void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        if (musicSource != null && fadeRoutine == null) musicSource.volume = MusicVolume;
    }

    public void SetSfxVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
    }

    public void ApplySave(SaveData data)
    {
        SetMusicVolume(data.musicVolume);
        SetSfxVolume(data.sfxVolume);
    }

    public void WriteToSave(SaveData data)
    {
        data.musicVolume = MusicVolume;
        data.sfxVolume = SfxVolume;
    }
}