// WeaponInventory.cs — Oyuncunun silah envanteri
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System.Collections.Generic;

public class WeaponInventory : MonoBehaviour
{
    public static WeaponInventory Instance { get; private set; }

    [Header("Tum silahlar (9 adet WeaponData asset'i)")]
    [SerializeField] private List<WeaponData> allWeapons = new List<WeaponData>();

    [Header("Baslangic")]
    [SerializeField] private WeaponData starterWeapon;   // Tabanca Mk1 — Gun 1'de ucretsiz

    /// <summary>WeaponType basina sahip olunan TEK silah (kademe otomatik yukselir).</summary>
    private readonly Dictionary<WeaponType, WeaponData> owned = new Dictionary<WeaponType, WeaponData>();

    public WeaponData Equipped { get; private set; }
    public bool HasAnyWeapon { get { return owned.Count > 0; } }
    public List<WeaponData> AllWeapons { get { return allWeapons; } }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (!HasAnyWeapon && starterWeapon != null)
            GrantWeapon(starterWeapon);
    }

    /// <summary>Bu silaha sahip miyiz (ayni tip, ayni veya ust kademe)?</summary>
    public bool IsOwned(WeaponData weapon)
    {
        WeaponData current;
        if (!owned.TryGetValue(weapon.type, out current)) return false;
        return (int)current.tier >= (int)weapon.tier;
    }

    /// <summary>Satin alinabilir mi? (bir onceki kademe gerekir)</summary>
    public bool CanPurchase(WeaponData weapon)
    {
        if (IsOwned(weapon)) return false;
        if (weapon.tier == WeaponTier.Mk1) return true;

        WeaponData current;
        if (!owned.TryGetValue(weapon.type, out current)) return false;
        return (int)current.tier == (int)weapon.tier - 1;
    }

    /// <summary>MarketUI'daki "Satin Al" butonundan cagrilir.</summary>
    public bool TryPurchase(WeaponData weapon)
    {
        if (!CanPurchase(weapon))
        {
            if (ToastNotification.Instance != null)
                ToastNotification.Instance.Show("Once bir onceki kademeyi almalisin.", 2.5f);
            return false;
        }

        if (!EconomyManager.Instance.TrySpendGold(weapon.price))
        {
            if (ToastNotification.Instance != null)
                ToastNotification.Instance.Show("Yeterli paran yok.", 2.5f);
            return false;
        }

        GrantWeapon(weapon);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPurchase();

        PlayerProgressStats.AddWeaponPurchased();

        // Kac FARKLI tip Mk3'e ulasti — bu kademe SETLENIR (increment degil).
        int mk3Sayisi = 0;
        foreach (WeaponType tip in System.Enum.GetValues(typeof(WeaponType)))
        {
            WeaponData sahip = GetOwned(tip);
            if (sahip != null && sahip.tier == WeaponTier.Mk3) mk3Sayisi++;
        }
        PlayerProgressStats.SetWeaponTypesMaxed(mk3Sayisi);

        return true;
    }

    /// <summary>Ayni tipin eski kademesinin yerini alir (upgrade mantigi).</summary>
    private void GrantWeapon(WeaponData weapon)
    {
        owned[weapon.type] = weapon;
        Equip(weapon.type);
    }

    /// <summary>MarketUI'daki "Kusan" butonundan cagrilir.</summary>
    public void Equip(WeaponType type)
    {
        WeaponData data;
        if (owned.TryGetValue(type, out data)) Equipped = data;
    }

    public WeaponData GetOwned(WeaponType type)
    {
        WeaponData data;
        return owned.TryGetValue(type, out data) ? data : null;
    }

    // ── Kayit koprusu ──

    public void ApplySave(SaveData data)
    {
        owned.Clear();

        for (int i = 0; i < data.ownedWeapons.Count; i++)
        {
            WeaponSaveEntry entry = data.ownedWeapons[i];
            WeaponData found = FindWeapon((WeaponType)entry.weaponType, (WeaponTier)entry.weaponTier);
            if (found != null) owned[found.type] = found;
        }

        Equip((WeaponType)data.equippedWeaponType);

        // Bozuk kayit korumasi
        if (!HasAnyWeapon && starterWeapon != null) GrantWeapon(starterWeapon);
    }

    public void WriteToSave(SaveData data)
    {
        data.ownedWeapons.Clear();

        foreach (KeyValuePair<WeaponType, WeaponData> pair in owned)
        {
            WeaponSaveEntry entry = new WeaponSaveEntry();
            entry.weaponType = (int)pair.Key;
            entry.weaponTier = (int)pair.Value.tier;
            data.ownedWeapons.Add(entry);
        }

        if (Equipped != null) data.equippedWeaponType = (int)Equipped.type;
    }

    private WeaponData FindWeapon(WeaponType type, WeaponTier tier)
    {
        for (int i = 0; i < allWeapons.Count; i++)
            if (allWeapons[i].type == type && allWeapons[i].tier == tier)
                return allWeapons[i];
        return null;
    }
}