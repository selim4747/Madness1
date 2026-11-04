// EnemyData.cs — Tek bir canavar tipinin verisi (ScriptableObject)
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;

[CreateAssetMenu(fileName = "Enemy_", menuName = "Delilik/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Kimlik")]
    public EnemyType type = EnemyType.FastWeak;
    public string displayName = "Hizli Yaratik";

    [Header("Taban istatistikler (Gun 1)")]
    public float baseHealth = 3f;
    public float baseDamage = 1f;
    public float moveSpeed = 3f;

    [Header("Saldiri")]
    public float attackRange = 1f;
    public float attackCooldown = 1f;
    public bool isRanged = false;
    public float preferredDistance = 4f;
    public float projectileSpeed = 6f;

    [Header("Spawn kurallari")]
    [Range(0.1f, 5f)] public float spawnWeight = 1f;  // yuksek = daha sik cikar
    public int minDayToAppear = 1;                    // bu gunden once hic cikmaz

    [Header("Gunluk guclenme")]
    public float healthGrowthPerDay = 1.05f;
    public float damageGrowthPerDay = 1.03f;

    [Header("Ses")]
    public AudioClip attackSound;
    public AudioClip deathSound;

    public float GetHealthForDay(int day)
    {
        return baseHealth * Mathf.Pow(healthGrowthPerDay, day - 1);
    }

    public float GetDamageForDay(int day)
    {
        return baseDamage * Mathf.Pow(damageGrowthPerDay, day - 1);
    }

    public bool CanAppearOnDay(int day)
    {
        return day >= minDayToAppear;
    }
}
