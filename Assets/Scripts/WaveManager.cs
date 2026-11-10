// WaveManager.cs — Gece dalga sistemi (havuz + es zamanli sinir)
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using Madness.Mods;
using System.Collections;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Formul")]
    [SerializeField] private float basePoolSize = 8f;
    [SerializeField] private float growthPerDay = 1.07f;

    [Header("Mobil performans")]
    [SerializeField] private int maxConcurrentAlive = 25;
    [SerializeField] private float spawnInterval = 1.2f;

    [Header("Spawn noktalari")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Canavar veri asset'leri (4 tip)")]
    [SerializeField] private List<EnemyData> enemyTypes = new List<EnemyData>();

    public int TotalPoolTonight { get; private set; }
    public int RemainingToKill { get; private set; }
    public int AliveCount { get { return aliveEnemies.Count; } }

    private readonly List<EnemyController> aliveEnemies = new List<EnemyController>();
    private int spawnedSoFar;
    private bool nightActive;
    private Coroutine spawnRoutine;
    private float currentSpawnIntervalMultiplier = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>GameManager gece basladiginda cagirir.</summary>
    public void StartNight(int day, bool isBossNight)
    {
        NightContext nightContext = ModEventBus.Publish(new NightContext
        {
            Day = day,
            IsBossNight = isBossNight
        });
        if (nightContext.Cancelled) return;
        day = Mathf.Max(1, nightContext.Day);
        isBossNight = nightContext.IsBossNight;
        currentSpawnIntervalMultiplier = Mathf.Max(0.05f, nightContext.SpawnIntervalMultiplier);
        // Dusman Sayisi = 8 x 1.07^(gun-1)
        TotalPoolTonight = Mathf.RoundToInt(basePoolSize * Mathf.Pow(growthPerDay, day - 1) * Mathf.Max(0f, nightContext.PoolMultiplier));
        RemainingToKill = TotalPoolTonight;

        spawnedSoFar = 0;
        nightActive = true;
        aliveEnemies.Clear();

        if (isBossNight) if (BossController.Instance != null) BossController.Instance.SpawnBoss(day);

        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        spawnRoutine = StartCoroutine(SpawnLoop(day));
    }

    private IEnumerator SpawnLoop(int day)
    {
        while (nightActive && spawnedSoFar < TotalPoolTonight)
        {
            CleanupAliveList();

            if (aliveEnemies.Count < maxConcurrentAlive)
            {
                SpawnOne(day);
                spawnedSoFar++;
            }

            yield return new WaitForSeconds(spawnInterval * currentSpawnIntervalMultiplier);
        }
        spawnRoutine = null;
    }

    private void SpawnOne(int day)
    {
        EnemyData chosen = PickWeightedEnemy(day);
        if (chosen == null) return;

        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
        EnemySpawnContext spawnContext = ModEventBus.Publish(new EnemySpawnContext { EnemyType = chosen.type, Day = day, SpawnPosition = point.position });
        if (spawnContext.Cancelled || spawnContext.CancelSpawn) return;
        GameObject go = EnemyPool.Instance != null ? EnemyPool.Instance.Get(spawnContext.EnemyType, spawnContext.SpawnPosition) : null;
        if (go == null) return;
        EnemyController enemy = go.GetComponent<EnemyController>();
        enemy.Initialize(day, spawnContext.HealthMultiplier, spawnContext.DamageMultiplier);
        aliveEnemies.Add(enemy);
    }

    /// <summary>spawnWeight'e gore agirlikli secim; minDayToAppear filtresi uygulanir.</summary>
    private EnemyData PickWeightedEnemy(int day)
    {
        float totalWeight = 0f;
        for (int i = 0; i < enemyTypes.Count; i++)
            if (enemyTypes[i].CanAppearOnDay(day)) totalWeight += enemyTypes[i].spawnWeight;

        if (totalWeight <= 0f) return null;

        float roll = Random.value * totalWeight;
        float acc = 0f;

        for (int i = 0; i < enemyTypes.Count; i++)
        {
            if (!enemyTypes[i].CanAppearOnDay(day)) continue;

            acc += enemyTypes[i].spawnWeight;
            if (roll <= acc) return enemyTypes[i];
        }
        return null;
    }

    /// <summary>EnemyController olunce cagirir.</summary>
    public void OnEnemyKilled(EnemyController enemy)
    {
        aliveEnemies.Remove(enemy);
        if (EnemyPool.Instance != null) EnemyPool.Instance.Return(enemy.Type, enemy.gameObject);

        RemainingToKill = Mathf.Max(0, RemainingToKill - 1);
        CheckNightEnd();
    }

    /// <summary>Boss oldugunde cagirir.</summary>
    public void OnBossKilled()
    {
        CheckNightEnd();
    }

    private void CheckNightEnd()
    {
        if (!nightActive) return;

        bool allSpawned = spawnedSoFar >= TotalPoolTonight;
        bool allDead = aliveEnemies.Count == 0;
        bool bossDone = BossController.Instance == null || !BossController.Instance.IsBossActive;

        if (allSpawned && allDead && bossDone)
        {
            nightActive = false;
            if (spawnRoutine != null) { StopCoroutine(spawnRoutine); spawnRoutine = null; }

            if (GameManager.Instance != null) GameManager.Instance.OnNightCleared();
        }
    }

    /// <summary>Oyuncu olunce tum dusmanlar temizlenir.</summary>
    public void ClearAllEnemies()
    {
        nightActive = false;
        if (spawnRoutine != null) { StopCoroutine(spawnRoutine); spawnRoutine = null; }

        CleanupAliveList();
        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            EnemyController e = aliveEnemies[i];
            if (e == null) continue;

            e.ForceDespawn();
            if (EnemyPool.Instance != null) EnemyPool.Instance.Return(e.Type, e.gameObject);
        }
        aliveEnemies.Clear();

        if (BossController.Instance != null) BossController.Instance.DespawnBoss();
    }

    private void CleanupAliveList()
    {
        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
            if (aliveEnemies[i] == null) aliveEnemies.RemoveAt(i);
    }

    /// <summary>TEST: hem bu geceki hem gelecekteki dusman sayisini stres testi icin artirir.</summary>
    public void DEBUG_CanavarSayisiniArtir()
    {
        maxConcurrentAlive += 10;
        basePoolSize += 10f;

        if (nightActive)
        {
            TotalPoolTonight += 15;
            RemainingToKill += 15;
        }

        DevLog.Info("[TEST] Ayni anda max: " + maxConcurrentAlive + ", bu geceki toplam: " + TotalPoolTonight);
    }
}
