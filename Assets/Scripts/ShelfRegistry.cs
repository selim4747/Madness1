using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rafları merkezi olarak tutar. FindObjectsOfType yerine kullanılır.
/// Raflar OnEnable/OnDisable'da kendini kaydeder.
/// </summary>
public class ShelfRegistry : MonoBehaviour
{
    public static ShelfRegistry Instance { get; private set; }

    private readonly List<ShelfController> activeShelves = new List<ShelfController>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>Sahneye manuel eklemeyi unutursan otomatik oluşturur.</summary>
    public static ShelfRegistry EnsureExists()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("ShelfRegistry");
        return go.AddComponent<ShelfRegistry>();
    }

    public void Register(ShelfController shelf)
    {
        if (shelf != null && !activeShelves.Contains(shelf))
            activeShelves.Add(shelf);
    }

    public void Unregister(ShelfController shelf)
    {
        if (shelf != null)
            activeShelves.Remove(shelf);
    }

    /// <summary>Şu an aktif olan tüm raflar (kopya döner, güvenli iterasyon için).</summary>
    public IReadOnlyList<ShelfController> GetActiveShelves()
    {
        // Null ve deaktif olanları temizle
        for (int i = activeShelves.Count - 1; i >= 0; i--)
        {
            if (activeShelves[i] == null || !activeShelves[i].gameObject.activeInHierarchy)
                activeShelves.RemoveAt(i);
        }
        return activeShelves;
    }
}
