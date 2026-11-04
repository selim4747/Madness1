// BossData.cs — Boss'un verisi (ScriptableObject)
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;

[CreateAssetMenu(fileName = "BossData", menuName = "Delilik/Boss Data")]
public class BossData : ScriptableObject
{
    [Header("Taban istatistikler (Gun 10)")]
    public float baseHealth = 200f;
    public float baseDamage = 3f;
    public float baseMoveSpeed = 1.8f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1.2f;

    [Header("Gunluk guclenme")]
    public float healthGrowthPerDay = 1.07f;
    public float damageGrowthPerDay = 1.04f;

    [Header("Faz 2 — destek canavar cagirma")]
    public float minionSummonInterval = 4f;
    public int maxSimultaneousMinions = 3;

    [Header("Faz 3 — cilgin mod")]
    public float enrageSpeedMultiplier = 1.6f;
    public float enrageAttackSpeedMultiplier = 1.5f;

    [Header("Alan hasari")]
    public float areaDamageInterval = 6f;
    public float areaDamageRadius = 2.5f;
    public float areaDamageAmount = 4f;
    public float areaDamageWarningTime = 1.2f;

    [Header("Odul")]
    public int goldReward = 1000;

    [Header("Ses")]
    public AudioClip spawnSound;
    public AudioClip deathSound;

    public float GetHealthForDay(int day)
    {
        return baseHealth * Mathf.Pow(healthGrowthPerDay, day - 10);
    }

    public float GetDamageForDay(int day)
    {
        return baseDamage * Mathf.Pow(damageGrowthPerDay, day - 10);
    }
}
