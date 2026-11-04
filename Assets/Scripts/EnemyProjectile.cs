// EnemyProjectile.cs — Uzaktan saldiran canavarin mermisi
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 4f;

    private Vector2 direction;
    private float speed;
    private float damage;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    public void Launch(Vector2 dir, float projectileSpeed, float dmg)
    {
        direction = dir.normalized;
        speed = projectileSpeed;
        damage = dmg;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerStats.Instance.TakeDamage(damage);
        Destroy(gameObject);
    }
}
