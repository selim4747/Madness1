// Projectile.cs — Oyuncunun mermisi (2D)
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private float damage;
    private float maxDistance;
    private float travelled;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    public void Launch(Vector2 dir, float projectileSpeed, float dmg, float range)
    {
        direction = dir.normalized;
        speed = projectileSpeed;
        damage = dmg;
        maxDistance = range;
        travelled = 0f;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Update()
    {
        float step = speed * Time.deltaTime;
        transform.position += (Vector3)(direction * step);

        travelled += step;
        if (travelled >= maxDistance) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        EnemyController enemy = other.GetComponent<EnemyController>();
        if (enemy != null && enemy.IsAlive)
        {
            enemy.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        BossController boss = other.GetComponent<BossController>();
        if (boss != null && boss.IsBossActive)
        {
            boss.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
