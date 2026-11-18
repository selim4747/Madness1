using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Çok oyunculu oturum yönetimi (Wi-Fi yerel ağ hazırlığı).
/// Host/Client modeli. Gerçek network katmanı (Mirror, Netcode for GameObjects, Photon vs.)
/// sonradan buraya bağlanır. Şimdilik iskelet + oyuncu listesi + çarpanlar.
/// </summary>
public class MultiplayerSession : MonoBehaviour
{
    public static MultiplayerSession Instance { get; private set; }

    [Header("Ayarlar")]
    [SerializeField] private string multiplayerSceneName = "MultiplayerMap";
    [SerializeField] private int maxPlayers = 4;

    public bool IsHost { get; private set; }
    public bool IsConnected { get; private set; }
    public int PlayerCount => connectedPlayers.Count;
    public IReadOnlyList<MultiplayerPlayerInfo> Players => connectedPlayers;

    public event Action OnSessionStarted;
    public event Action<int> OnPlayerCountChanged;

    private readonly List<MultiplayerPlayerInfo> connectedPlayers = new List<MultiplayerPlayerInfo>();

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

    /// <summary>Host olarak oturum başlat (Wi-Fi üzerinden).</summary>
    public void StartHost(string playerName = "Host")
    {
        IsHost = true;
        IsConnected = true;
        connectedPlayers.Clear();
        AddPlayer(playerName, isLocal: true);
        OnSessionStarted?.Invoke();
        Debug.Log("[Multiplayer] Host başlatıldı. Oyuncu sayısı: " + PlayerCount);
    }

    /// <summary>Client olarak bağlan.</summary>
    public void JoinAsClient(string playerName, string hostAddress)
    {
        IsHost = false;
        IsConnected = true;
        // Gerçek network bağlanma kodu buraya gelecek (IP/port veya discovery).
        AddPlayer(playerName, isLocal: true);
        OnSessionStarted?.Invoke();
        Debug.Log("[Multiplayer] Client bağlandı: " + hostAddress);
    }

    public void AddPlayer(string name, bool isLocal = false)
    {
        if (connectedPlayers.Count >= maxPlayers) return;

        var info = new MultiplayerPlayerInfo
        {
            Id = connectedPlayers.Count,
            Name = string.IsNullOrEmpty(name) ? "Oyuncu " + (connectedPlayers.Count + 1) : name,
            IsLocal = isLocal
        };
        connectedPlayers.Add(info);
        OnPlayerCountChanged?.Invoke(PlayerCount);
    }

    public void RemovePlayer(int id)
    {
        connectedPlayers.RemoveAll(p => p.Id == id);
        OnPlayerCountChanged?.Invoke(PlayerCount);
    }

    /// <summary>Özel çok oyunculu harita sahnesini yükle.</summary>
    public void LoadMultiplayerMap()
    {
        if (!IsConnected)
        {
            Debug.LogWarning("[Multiplayer] Oturum yok, sahne yüklenemez.");
            return;
        }
        SceneManager.LoadScene(multiplayerSceneName);
    }

    /// <summary>Canavar doğma çarpanı (kişi sayısı kadar).</summary>
    public float GetMonsterSpawnMultiplier()
    {
        return Mathf.Max(1f, PlayerCount);
    }

    public void Disconnect()
    {
        connectedPlayers.Clear();
        IsConnected = false;
        IsHost = false;
        OnPlayerCountChanged?.Invoke(0);
    }
}

[Serializable]
public class MultiplayerPlayerInfo
{
    public int Id;
    public string Name;
    public bool IsLocal;
}
