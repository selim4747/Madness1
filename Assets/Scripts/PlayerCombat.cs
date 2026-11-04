// PlayerCombat.cs — Otomatik nisan alan ates sistemi
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    public static PlayerCombat Instance { get; private set; }

    [Header("Referanslar")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject projectilePrefab;

    private float cooldownTimer;
    private bool firingHeld;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>Sanal ates butonu tarafindan cagrilir.</summary>
    public void SetFiringInput(bool held)
    {
        firingHeld = held;
    }

    private void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

        if (!firingHeld) return;
        if (GameManager.Instance.CurrentPhase != GamePhase.Night) return;
        if (PlayerStats.Instance.IsDead) return;

        // Olum ekrani acikken de ates edilmemeli
        if (Time.timeScale == 0f) return;

        WeaponData weapon = WeaponInventory.Instance.Equipped;
        if (weapon == null) return;
        if (cooldownTimer > 0f) return;

        Transform target = FindNearestTarget(weapon.range);
        if (target == null) return;

        Fire(weapon, target);
        cooldownTimer = weapon.fireInterval;   // mermi sonsuz — sadece hiz sinirlar
    }

    private Transform FindNearestTarget(float range)
    {
        Transform nearest = null;
        float best = range;

        EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            if (!enemies[i].IsAlive) continue;

            float d = Vector2.Distance(transform.position, enemies[i].transform.position);
            if (d < best) { best = d; nearest = enemies[i].transform; }
        }

        if (BossController.Instance != null && BossController.Instance.IsBossActive)
        {
            float d = Vector2.Distance(transform.position, BossController.Instance.transform.position);
            if (d < best) { best = d; nearest = BossController.Instance.transform; }
        }

        return nearest;
    }

    private void Fire(WeaponData weapon, Transform target)
    {
        Vector2 baseDir = ((Vector2)(target.position - firePoint.position)).normalized;
        float damage = weapon.GetFinalDamage(PlayerStats.Instance.DamageStat);

        int pellets = Mathf.Max(1, weapon.pelletCount);
        for (int i = 0; i < pellets; i++)
        {
            Vector2 dir = baseDir;

            // Pompali: sacmalari yayilma acisina gore dagit
            if (pellets > 1 && weapon.spreadAngle > 0f)
            {
                float t = (pellets == 1) ? 0.5f : (float)i / (pellets - 1);
                float angle = Mathf.Lerp(-weapon.spreadAngle * 0.5f, weapon.spreadAngle * 0.5f, t);
                dir = Quaternion.Euler(0f, 0f, angle) * baseDir;
            }

            GameObject go = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            go.GetComponent<Projectile>().Launch(dir, weapon.projectileSpeed, damage, weapon.range);
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(weapon.fireSound);
        if (CameraFollow.Instance != null) CameraFollow.Instance.Shake(0.05f, 0.03f); // hasar sarsintisindan cok daha kucuk/hizli
    }
}