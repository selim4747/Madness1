using UnityEngine;

/// <summary>
/// Oyuncu sayısına göre masa oluşturur.
/// Her oyuncu kendi masasında müşteri konuşması / satış yapar.
/// Haritayı sen özel hazırlayacaksın; bu script masa noktalarını kullanır.
/// </summary>
public class MultiplayerTableManager : MonoBehaviour
{
    [Header("Masa Noktaları (Haritada elle yerleştir)")]
    [SerializeField] private Transform[] tableSpawnPoints;

    [Header("Masa Prefab")]
    [SerializeField] private GameObject tablePrefab;

    private void Start()
    {
        if (MultiplayerSession.Instance == null)
        {
            Debug.LogWarning("[Tables] MultiplayerSession yok.");
            return;
        }

        SpawnTablesForPlayers();
        MultiplayerSession.Instance.OnPlayerCountChanged += OnPlayerCountChanged;
    }

    private void OnDestroy()
    {
        if (MultiplayerSession.Instance != null)
            MultiplayerSession.Instance.OnPlayerCountChanged -= OnPlayerCountChanged;
    }

    private void OnPlayerCountChanged(int count)
    {
        // İstersen dinamik yeniden oluştur. Şimdilik başlangıçta bir kez.
        // SpawnTablesForPlayers();
    }

    public void SpawnTablesForPlayers()
    {
        int count = MultiplayerSession.Instance != null ? MultiplayerSession.Instance.PlayerCount : 1;
        count = Mathf.Clamp(count, 1, tableSpawnPoints != null ? tableSpawnPoints.Length : 1);

        for (int i = 0; i < count; i++)
        {
            if (tableSpawnPoints == null || i >= tableSpawnPoints.Length || tableSpawnPoints[i] == null)
                continue;

            if (tablePrefab != null)
            {
                GameObject table = Instantiate(tablePrefab, tableSpawnPoints[i].position, tableSpawnPoints[i].rotation);
                table.name = "Masa_Oyuncu_" + (i + 1);

                // Masa üzerinde oyuncu kimliği tutulabilir
                var marker = table.GetComponent<MultiplayerTableMarker>();
                if (marker == null) marker = table.AddComponent<MultiplayerTableMarker>();
                marker.OwnerPlayerId = i;
            }
        }

        Debug.Log("[Tables] " + count + " masa oluşturuldu.");
    }
}

/// <summary>Masayı hangi oyuncuya ait olduğunu işaretler.</summary>
public class MultiplayerTableMarker : MonoBehaviour
{
    public int OwnerPlayerId;
}
