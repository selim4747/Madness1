// EnemyController.cs — Canavar yapay zekasi (NavMesh, havuz uyumlu)
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using Madness.Mods;

public class EnemyController : NavMeshMover
{
    [Header("Veri")]
    [SerializeField] private EnemyData data;

    [Header("Gorsel")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [Tooltip("Opsiyonel — bos birakilirsa hicbir sey bozulmaz. Doluysa hasar/olum aninda tetiklenir.")]
    [SerializeField] private ProseduralHareketAnimasyonu hareketAnimasyonu;
    [SerializeField] private GameObject enemyProjectilePrefab;
    [SerializeField] private Transform firePoint;
    [Tooltip("Animator Controller HENUZ yoksa bos birak — kod otomatik atlar, hata vermez.")]
    [SerializeField] private Animator animator;

    public EnemyType Type { get { return data != null ? data.type : EnemyType.FastWeak; } }
    public bool IsAlive { get; private set; }

    private float currentHealth;
    private float maxHealth;
    private float currentDamage;
    private float attackTimer;
    private Transform player;

    /// <summary>Havuzdan alinip sahneye konunca cagrilir.</summary>
    public void Initialize(int day, float healthMultiplier = 1f, float damageMultiplier = 1f)
    {
        maxHealth = data.GetHealthForDay(day) * Mathf.Max(0f, healthMultiplier);
        currentHealth = maxHealth;
        currentDamage = data.GetDamageForDay(day) * Mathf.Max(0f, damageMultiplier);

        SetSpeed(data.moveSpeed);
        attackTimer = 0f;
        IsAlive = true;

        if (bodyRenderer != null) bodyRenderer.color = Color.white;

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
    }

    private void Update()
    {
        if (!IsAlive || player == null) return;
        if (GameManager.Instance.CurrentPhase != GamePhase.Night) return;

        if (attackTimer > 0f) attackTimer -= Time.deltaTime;

        if (data.isRanged) UpdateRanged();
        else UpdateMelee();

        UpdateFacing();
    }

    private void UpdateMelee()
    {
        MoveTo(player.position);

        if (DistanceTo(player.position) > data.attackRange) return;
        if (attackTimer > 0f) return;

        attackTimer = data.attackCooldown;
        PlayerStats.Instance.TakeDamage(currentDamage);
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(data.attackSound);
    }

    private void UpdateRanged()
    {
        float distance = DistanceTo(player.position);

        // Tercih ettigi mesafeyi korumaya calisir
        if (distance > data.preferredDistance * 1.2f)
        {
            MoveTo(player.position);
        }
        else if (distance < data.preferredDistance * 0.7f)
        {
            Vector3 away = transform.position + (transform.position - player.position).normalized * 2f;
            MoveTo(away);
        }
        else
        {
            Stop();
        }

        if (distance > data.preferredDistance * 1.5f) return;
        if (attackTimer > 0f) return;

        attackTimer = data.attackCooldown;
        FireProjectile();
    }

    private void FireProjectile()
    {
        if (enemyProjectilePrefab == null || firePoint == null) return;

        Vector2 dir = ((Vector2)(player.position - firePoint.position)).normalized;
        GameObject go = Instantiate(enemyProjectilePrefab, firePoint.position, Quaternion.identity);
        go.GetComponent<EnemyProjectile>().Launch(dir, data.projectileSpeed, currentDamage);

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(data.attackSound);
    }

    private void UpdateFacing()
    {
        if (bodyRenderer != null)
        {
            Vector2 v = CurrentVelocity;
            if (Mathf.Abs(v.x) > 0.05f) bodyRenderer.flipX = v.x < 0f;
        }

        if (animator != null && animator.runtimeAnimatorController != null)
            animator.SetFloat("Speed", CurrentVelocity.sqrMagnitude);
    }

    public void TakeDamage(float amount)
    {
        if (!IsAlive) return;
        DamageContext damageContext = ModEventBus.Publish(new DamageContext { Target = this, Amount = amount });
        if (damageContext.Cancelled) return;
        amount = Mathf.Max(0f, damageContext.Amount);

        currentHealth -= amount;
        StartCoroutine(FlashHit());
        if (hareketAnimasyonu != null) hareketAnimasyonu.HasarTepkisiOynat();
        FloatingText.Spawn(transform.position, "-" + Mathf.RoundToInt(amount), FloatingText.Renk.Notr);

        if (currentHealth <= 0f) Die();
    }

    private System.Collections.IEnumerator FlashHit()
    {
        if (bodyRenderer == null) yield break;

        bodyRenderer.color = new Color(1f, 0.5f, 0.5f);
        yield return new WaitForSeconds(0.08f);
        if (IsAlive && bodyRenderer != null) bodyRenderer.color = Color.white;
    }

    private void Die()
    {
        IsAlive = false;
        Stop();
        if (AudioManager.Instance != null && data != null) AudioManager.Instance.PlaySfx(data.deathSound);
        DeathPuffEffect.Spawn(transform.position, new Color(0.6f, 0.15f, 0.15f));

        PlayerProgressStats.AddEnemyKilled();

        if (WaveManager.Instance != null) WaveManager.Instance.OnEnemyKilled(this);
    }

    /// <summary>Oyuncu olunce / gece bitince zorla temizlenir.</summary>
    public void ForceDespawn()
    {
        IsAlive = false;
        Stop();
    }
}
