// BossController.cs — 3 fazli boss savasi
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using Madness.Mods;

public class BossController : NavMeshMover
{
    public static BossController Instance { get; private set; }

    [Header("Veri")]
    [SerializeField] private BossData data;

    [Header("Referanslar")]
    [SerializeField] private GameObject areaDamagePrefab;
    [SerializeField] private EnemyType minionType = EnemyType.FastWeak;
    [SerializeField] private SpriteRenderer bodyRenderer;

    public bool IsBossActive { get; private set; }
    public int CurrentPhase { get; private set; } = 1;
    public float HealthRatio { get { return maxHealth > 0f ? currentHealth / maxHealth : 0f; } }

    public event Action<float> OnHealthRatioChanged;
    public event Action<bool> OnBossActiveChanged;

    private float currentHealth;
    private float maxHealth;
    private float currentDamage;
    private float attackTimer;
    private float summonTimer;
    private float areaTimer;
    private Transform player;
    private readonly List<GameObject> activeMinions = new List<GameObject>();
    private int currentDay;

    protected override void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        base.Awake();
        gameObject.SetActive(false);
    }

    public void SpawnBoss(int day)
    {
        currentDay = day;
        maxHealth = data.GetHealthForDay(day);
        currentHealth = maxHealth;
        currentDamage = data.GetDamageForDay(day);

        CurrentPhase = 1;
        IsBossActive = true;
        attackTimer = 0f;
        summonTimer = 0f;
        areaTimer = data.areaDamageInterval;
        activeMinions.Clear();

        SetSpeed(data.baseMoveSpeed);
        gameObject.SetActive(true);

        if (bodyRenderer != null) bodyRenderer.color = Color.white;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        AudioManager.Instance.PlaySfx(data.spawnSound);

        if (OnBossActiveChanged != null) OnBossActiveChanged(true);
        if (OnHealthRatioChanged != null) OnHealthRatioChanged(1f);
    }

    private void Update()
    {
        if (!IsBossActive || player == null) return;
        if (GameManager.Instance.CurrentPhase != GamePhase.Night) return;

        MoveTo(player.position);

        TickMeleeAttack();
        TickAreaDamage();

        if (CurrentPhase >= 2) TickMinionSummon();
    }

    private void TickMeleeAttack()
    {
        if (attackTimer > 0f) { attackTimer -= Time.deltaTime; return; }
        if (DistanceTo(player.position) > data.attackRange) return;

        float cooldown = data.attackCooldown;
        if (CurrentPhase >= 3) cooldown /= data.enrageAttackSpeedMultiplier;

        attackTimer = cooldown;
        PlayerStats.Instance.TakeDamage(currentDamage);
    }

    private void TickAreaDamage()
    {
        areaTimer -= Time.deltaTime;
        if (areaTimer > 0f) return;

        areaTimer = data.areaDamageInterval;
        if (areaDamagePrefab == null) return;

        GameObject go = Instantiate(areaDamagePrefab, player.position, Quaternion.identity);
        go.GetComponent<BossAreaDamage>().Trigger(
            data.areaDamageRadius,
            data.areaDamageAmount,
            data.areaDamageWarningTime);
    }

    private void TickMinionSummon()
    {
        CleanupMinions();

        summonTimer -= Time.deltaTime;
        if (summonTimer > 0f) return;
        summonTimer = data.minionSummonInterval;

        if (activeMinions.Count >= data.maxSimultaneousMinions) return;

        Vector3 spawnPos = transform.position + (Vector3)(Random.insideUnitCircle.normalized * 1.5f);
        GameObject minion = EnemyPool.Instance.Get(minionType, spawnPos);
        if (minion == null) return;

        minion.GetComponent<EnemyController>().Initialize(currentDay);
        activeMinions.Add(minion);
    }

    private void CleanupMinions()
    {
        for (int i = activeMinions.Count - 1; i >= 0; i--)
            if (activeMinions[i] == null || !activeMinions[i].activeSelf)
                activeMinions.RemoveAt(i);
    }

    public void TakeDamage(float amount)
    {
        if (!IsBossActive) return;
        DamageContext damageContext = ModEventBus.Publish(new DamageContext { Target = this, Amount = amount });
        if (damageContext.Cancelled) return;
        amount = Mathf.Max(0f, damageContext.Amount);

        currentHealth -= amount;
        if (OnHealthRatioChanged != null) OnHealthRatioChanged(HealthRatio);

        if (currentHealth <= 0f) { Die(); return; }

        // Faz gecisleri
        if (HealthRatio <= 0.33f && CurrentPhase < 3) EnterPhase(3);
        else if (HealthRatio <= 0.66f && CurrentPhase < 2) EnterPhase(2);
    }

    private void EnterPhase(int phase)
    {
        CurrentPhase = phase;

        if (phase == 3)
        {
            // Cilgin mod: hiz artar, saldiri hizlanir (TickMeleeAttack icinde)
            SetSpeed(data.baseMoveSpeed * data.enrageSpeedMultiplier);
            if (bodyRenderer != null) bodyRenderer.color = new Color(1f, 0.6f, 0.6f);
        }
    }

    private void Die()
    {
        IsBossActive = false;
        AudioManager.Instance.PlaySfx(data.deathSound);

        EconomyManager.Instance.AddGold(data.goldReward);
        PlayerProgressStats.AddBossKilled();

        DespawnMinions();
        gameObject.SetActive(false);

        if (OnBossActiveChanged != null) OnBossActiveChanged(false);
        WaveManager.Instance.OnBossKilled();
    }

    /// <summary>Oyuncu olunce cagrilir.</summary>
    public void DespawnBoss()
    {
        if (!IsBossActive) return;

        IsBossActive = false;
        DespawnMinions();
        gameObject.SetActive(false);

        if (OnBossActiveChanged != null) OnBossActiveChanged(false);
    }

    private void DespawnMinions()
    {
        CleanupMinions();
        for (int i = activeMinions.Count - 1; i >= 0; i--)
        {
            GameObject m = activeMinions[i];
            if (m == null) continue;

            EnemyController ec = m.GetComponent<EnemyController>();
            if (ec != null) ec.ForceDespawn();
            EnemyPool.Instance.Return(minionType, m);
        }
        activeMinions.Clear();
    }
}
