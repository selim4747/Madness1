// PlayerStats.cs — Oyuncunun cani ve statlari
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.UI;
using System;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Baslangic degerleri")]
    [SerializeField] private int baseMaxHealth = 10;
    [SerializeField] private float baseDamage = 2f;
    [SerializeField] private float baseSpeed = 1.5f;

    [Header("Hasar geri bildirimi")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [Tooltip("Opsiyonel — bos birakilirsa hicbir sey bozulmaz. Doluysa hasar aninda tetiklenir.")]
    [SerializeField] private ProseduralHareketAnimasyonu hareketAnimasyonu;
    [SerializeField] private float invulnerabilitySeconds = 0.4f;
    [SerializeField] private AudioClip hasarAlmaSesi;

    [Header("Dusuk Can Vinyeti (opsiyonel — bos birakilirsa hicbir sey bozulmaz)")]
    [Tooltip("Tam ekrani kaplayan, alpha=0 baslayan kirmizi bir UI Image. Can dusukken nabiz gibi yanip soner.")]
    [SerializeField] private Image dusukCanVinyeti;
    [SerializeField] private int dusukCanEsigi = 3;

    public int MaxHealth { get; private set; }
    public float DamageStat { get; private set; }
    public float SpeedStat { get; private set; }
    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    public event Action<int, int> OnHealthChanged;   // (current, max)

    private float invulnerableUntil;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        MaxHealth = baseMaxHealth;
        DamageStat = baseDamage;
        SpeedStat = baseSpeed;
        CurrentHealth = MaxHealth;
    }

    private bool kritikCandaydi = false; // "dusuk can hayatta kalma" sayacinin tekrar tekrar artmamasi icin

    private void Update()
    {
        // SAYAC TAKIBI: vinyet gorseli atanmamis olsa BILE calisir — gorselden bagimsizdir.
        // Kritik esigin ustunden ALTINA ilk gectigi anda SAYILIR (surekli o bolgede kalirken
        // her karede tekrar tekrar sayilmasin diye).
        bool suAndaKritikMi = !IsDead && CurrentHealth > 0 && CurrentHealth <= dusukCanEsigi;
        if (suAndaKritikMi && !kritikCandaydi)
        {
            PlayerProgressStats.AddLowHealthSurvival();
        }
        kritikCandaydi = suAndaKritikMi;

        if (dusukCanVinyeti == null) return;

        if (suAndaKritikMi)
        {
            // Basit bir nabiz: sinus dalgasiyla 0-0.35 arasi alpha degisir.
            float nabiz = (Mathf.Sin(Time.unscaledTime * 4f) + 1f) * 0.5f; // 0..1
            Color c = dusukCanVinyeti.color;
            c.a = Mathf.Lerp(0.05f, 0.35f, nabiz);
            dusukCanVinyeti.color = c;
        }
        else
        {
            Color c = dusukCanVinyeti.color;
            if (c.a > 0f)
            {
                c.a = Mathf.MoveTowards(c.a, 0f, Time.unscaledDeltaTime * 2f);
                dusukCanVinyeti.color = c;
            }
        }
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;
        if (Time.time < invulnerableUntil) return;   // kisa dokunulmazlik

        invulnerableUntil = Time.time + invulnerabilitySeconds;

        CurrentHealth -= Mathf.Max(1, Mathf.RoundToInt(amount));
        CurrentHealth = Mathf.Max(0, CurrentHealth);

        if (OnHealthChanged != null) OnHealthChanged(CurrentHealth, MaxHealth);
        StartCoroutine(FlashRed());
        if (hareketAnimasyonu != null) hareketAnimasyonu.HasarTepkisiOynat();
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(hasarAlmaSesi);
        if (CameraFollow.Instance != null) CameraFollow.Instance.Shake(0.2f, 0.15f);
        FloatingText.Spawn(transform.position, "-" + Mathf.Max(1, Mathf.RoundToInt(amount)), FloatingText.Renk.Hasar);

        if (CurrentHealth <= 0) Die();
    }

    private System.Collections.IEnumerator FlashRed()
    {
        if (bodyRenderer == null) yield break;

        Color original = bodyRenderer.color;
        bodyRenderer.color = new Color(1f, 0.4f, 0.4f);
        yield return new WaitForSeconds(0.12f);
        bodyRenderer.color = original;
    }

    private void Die()
    {
        IsDead = true;

        // Once olum ekrani acilir: oyuncu reklam izleyip devam edebilir.
        // Vazgecerse DeathPromptUI, GameManager.OnPlayerDied()'i kendisi cagirir.
        if (DeathPromptUI.Instance != null)
            DeathPromptUI.Instance.Show();
        else
            GameManager.Instance.OnPlayerDied();   // guvenlik: ekran yoksa eski davranis
    }

    public void FullHeal()
    {
        IsDead = false;
        CurrentHealth = MaxHealth;
        invulnerableUntil = 0f;
        if (OnHealthChanged != null) OnHealthChanged(CurrentHealth, MaxHealth);
    }

    /// <summary>Mevcut cani belirtilen kadar artirir (max'i asmaz). Market'teki "Cani 1 Artir"
    /// gibi tuketilebilir/tekli iyilesmeler icin — kalici bir yukseltme DEGIL.</summary>
    public void Heal(int amount)
    {
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        if (OnHealthChanged != null) OnHealthChanged(CurrentHealth, MaxHealth);
    }

    /// <summary>Odullu reklam "Yeniden Dogus": tam can + guc bonusu.</summary>
    public void ReviveWithBonus(float damageBonusPercent)
    {
        FullHeal();
        DamageStat += DamageStat * damageBonusPercent;
    }

    // ── CharacterUpgradeManager tarafindan cagrilir ──

    public void IncreaseMaxHealth(int amount)
    {
        MaxHealth += amount;
        CurrentHealth = MaxHealth;
        if (OnHealthChanged != null) OnHealthChanged(CurrentHealth, MaxHealth);
    }

    public void IncreaseDamage(float amount) { DamageStat += amount; }
    public void IncreaseSpeed(float amount) { SpeedStat += amount; }

    // ── Kayit koprusu ──

    public void ApplySave(SaveData data)
    {
        MaxHealth = data.maxHealth;
        DamageStat = data.damageStat;
        SpeedStat = data.speedStat;
        FullHeal();
    }

    public void WriteToSave(SaveData data)
    {
        data.maxHealth = MaxHealth;
        data.damageStat = DamageStat;
        data.speedStat = SpeedStat;
    }
}