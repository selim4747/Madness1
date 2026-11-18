using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Çok oyunculu lobi / oda ekranı.
/// Host başlat, IP ile katıl, oyuncu listesi göster, haritayı yükle.
/// Wi-Fi yerel ağ için basit arayüz iskeleti.
/// </summary>
public class MultiplayerLobbyUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private TMP_InputField hostAddressInput;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI playerListText;
    [SerializeField] private Button startMapButton;

    private void Start()
    {
        if (hostButton != null) hostButton.onClick.AddListener(OnHostClicked);
        if (joinButton != null) joinButton.onClick.AddListener(OnJoinClicked);
        if (startMapButton != null) startMapButton.onClick.AddListener(OnStartMap);

        RefreshUI();
    }

    private void OnHostClicked()
    {
        string name = playerNameInput != null ? playerNameInput.text : "Host";
        MultiplayerSession.Instance.StartHost(name);
        SetStatus("Host olarak başlatıldı. Diğerleri bağlanabilir.");
        RefreshUI();
    }

    private void OnJoinClicked()
    {
        string name = playerNameInput != null ? playerNameInput.text : "Oyuncu";
        string address = hostAddressInput != null ? hostAddressInput.text : "127.0.0.1";
        MultiplayerSession.Instance.JoinAsClient(name, address);
        SetStatus("Bağlanılıyor: " + address);
        RefreshUI();
    }

    private void OnStartMap()
    {
        if (MultiplayerSession.Instance != null && MultiplayerSession.Instance.IsConnected)
            MultiplayerSession.Instance.LoadMultiplayerMap();
        else
            SetStatus("Önce oturum başlat.");
    }

    private void RefreshUI()
    {
        if (MultiplayerSession.Instance == null) return;

        if (playerListText != null)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Oyuncular (" + MultiplayerSession.Instance.PlayerCount + "):");
            foreach (var p in MultiplayerSession.Instance.Players)
                sb.AppendLine("- " + p.Name + (p.IsLocal ? " (Sen)" : ""));
            playerListText.text = sb.ToString();
        }

        if (startMapButton != null)
            startMapButton.interactable = MultiplayerSession.Instance.IsConnected;
    }

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
        Debug.Log("[Lobby] " + msg);
    }
}
