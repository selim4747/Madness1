// WeaponData.cs — Tek bir silahin verisi (ScriptableObject)
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;

[CreateAssetMenu(fileName = "Weapon_", menuName = "Delilik/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Kimlik")]
    public WeaponType type = WeaponType.Pistol;
    public WeaponTier tier = WeaponTier.Mk1;
    public string displayName = "Tabanca Mk1";
    [TextArea(2, 3)] public string description = "Hizli ates eden temel silah.";

    [Header("Istatistikler")]
    public float damageMultiplier = 1f;
    public float fireInterval = 0.15f;
    public float range = 6f;
    public float projectileSpeed = 12f;
    public int price = 150;

    [Header("Pompali icin")]
    public int pelletCount = 1;
    public float spreadAngle = 0f;

    [Header("Gorsel / Ses")]
    public Sprite weaponSprite;
    public AudioClip fireSound;

    public float GetFinalDamage(float characterDamageStat)
    {
        return damageMultiplier * characterDamageStat;
    }
}
