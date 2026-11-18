using UnityEngine;
using Madness.Mods;

/// <summary>
/// Seçilen mekanikler için context'ler.
/// Orijinal oyunda kullanılmaz. Modlar Subscribe ederek aktif edebilir.
/// </summary>
public class BookEffectContext : ModContext
{
    public string BookId;
    public float Duration;
    public float Power;
}

public class StaffContext : ModContext
{
    public string StaffType;
    public int Cost;
    public float Efficiency;
}

public class PlantContext : ModContext
{
    public string PlantId;
    public float GrowthTime;
    public string BonusType;
    public float BonusValue;
}

public class EventContext : ModContext
{
    public string EventId;
    public float Duration;
    public float CustomerMultiplier;
    public float GoldMultiplier;
}

public class ConsumableContext : ModContext
{
    public string ItemId;
    public float EffectDuration;
    public string EffectType;
    public float EffectPower;
}

public class DecorationContext : ModContext
{
    public string DecorationId;
    public float MoodBonus;
    public float CustomerAttract;
}
