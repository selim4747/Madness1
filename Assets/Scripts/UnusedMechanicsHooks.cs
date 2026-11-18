using UnityEngine;
using Madness.Mods;

/// <summary>
/// Seçilen mekanikler için boş hook'lar.
/// Orijinal oyunda hiçbir etki yapmaz.
/// Modlar bu metodları çağırabilir veya EventBus'u dinleyebilir.
/// </summary>
public class UnusedMechanicsHooks : MonoBehaviour
{
    public static UnusedMechanicsHooks Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void TryTriggerBookEffect(string bookId)
    {
        ModEventBus.Publish(new BookEffectContext
        {
            BookId = bookId ?? "",
            Duration = 0f,
            Power = 0f
        });
    }

    public void TryHireStaff(string type)
    {
        ModEventBus.Publish(new StaffContext
        {
            StaffType = type ?? "",
            Cost = 0,
            Efficiency = 0f
        });
    }

    public void TryPlant(string plantId)
    {
        ModEventBus.Publish(new PlantContext
        {
            PlantId = plantId ?? "",
            GrowthTime = 0f,
            BonusType = "",
            BonusValue = 0f
        });
    }

    public void TryStartEvent(string eventId)
    {
        ModEventBus.Publish(new EventContext
        {
            EventId = eventId ?? "",
            Duration = 0f,
            CustomerMultiplier = 1f,
            GoldMultiplier = 1f
        });
    }

    public void TryUseConsumable(string itemId)
    {
        ModEventBus.Publish(new ConsumableContext
        {
            ItemId = itemId ?? "",
            EffectDuration = 0f,
            EffectType = "",
            EffectPower = 0f
        });
    }

    public void TryPlaceDecoration(string decoId)
    {
        ModEventBus.Publish(new DecorationContext
        {
            DecorationId = decoId ?? "",
            MoodBonus = 0f,
            CustomerAttract = 0f
        });
    }
}
