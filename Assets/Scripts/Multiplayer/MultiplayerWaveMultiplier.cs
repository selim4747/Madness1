using UnityEngine;

/// <summary>
/// Gece dalgasında canavar doğma sayısını oyuncu sayısına göre çarpar.
/// WaveManager veya EnemySpawner'a bağlanır.
/// Ortak savaş: herkes aynı canavarlarla savaşır, spawn sayısı artar.
/// </summary>
public class MultiplayerWaveMultiplier : MonoBehaviour
{
    [Header("Bağlantı")]
    [Tooltip("Mevcut WaveManager veya spawn kontrolcün")]
    [SerializeField] private MonoBehaviour waveController;

    private void Start()
    {
        ApplyMultiplier();
    }

    public void ApplyMultiplier()
    {
        if (MultiplayerSession.Instance == null) return;

        float multiplier = MultiplayerSession.Instance.GetMonsterSpawnMultiplier();
        Debug.Log("[Wave] Canavar doğma çarpanı: x" + multiplier + " (Oyuncu: " + MultiplayerSession.Instance.PlayerCount + ")");

        // Mevcut sistemine göre burada spawn havuzunu veya interval'ı çarp.
        // Örnek: NightContext veya doğrudan WaveManager metodu çağır.
        // ModEventBus üzerinden de yayınlanabilir.
    }

    /// <summary>Dışarıdan çağrılabilir (gece başlarken).</summary>
    public float GetCurrentMultiplier()
    {
        return MultiplayerSession.Instance != null
            ? MultiplayerSession.Instance.GetMonsterSpawnMultiplier()
            : 1f;
    }
}
