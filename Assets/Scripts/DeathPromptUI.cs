// DeathPromptUI.cs — Olum ekrani: Gune Bastan Basla / Oyuna Bastan Basla
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class DeathPromptUI : MonoBehaviour
{
    public static DeathPromptUI Instance { get; private set; }

    [Header("Referanslar")]
    [SerializeField] private GameObject root;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button restartDayButton;
    [SerializeField] private Button restartGameButton;
    [SerializeField] private Button reklamlaCanlanButonu;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (root != null) root.SetActive(false);
        if (restartDayButton != null) restartDayButton.onClick.AddListener(OnRestartDayPressed);
        if (restartGameButton != null) restartGameButton.onClick.AddListener(OnRestartGamePressed);
        if (reklamlaCanlanButonu != null) reklamlaCanlanButonu.onClick.AddListener(OnReklamlaCanlanPressed);
    }

    /// <summary>PlayerStats.Die() tarafindan cagrilir.</summary>
    public void Show()
    {
        if (messageText != null)
            messageText.text = "Oldun.";

        // Reklamla canlanma bekleme suresindeyse butonu gizle/pasif yap — cikmaza goturmesin.
        if (reklamlaCanlanButonu != null)
        {
            bool musait = RewardedAdManager.Instance != null && RewardedAdManager.Instance.IsAvailable(RewardType.Revive);
            reklamlaCanlanButonu.gameObject.SetActive(musait);
        }

        Time.timeScale = 0f;
        if (root != null) root.SetActive(true);
    }

    private void OnEnable()
    {
        if (RewardedAdManager.Instance != null) RewardedAdManager.Instance.OnRewardGranted += HandleRewardGranted;
    }

    private void OnDisable()
    {
        if (RewardedAdManager.Instance != null) RewardedAdManager.Instance.OnRewardGranted -= HandleRewardGranted;
    }

    private void HandleRewardGranted(RewardType type)
    {
        if (type != RewardType.Revive) return;

        // Reklam odulu (tam can + hasar bonusu) verildi — olum ekranini kapat, oyuna donulsun.
        Time.timeScale = 1f;
        if (root != null) root.SetActive(false);
    }

    private void OnReklamlaCanlanPressed()
    {
        if (RewardedAdManager.Instance != null) RewardedAdManager.Instance.RequestAd(RewardType.Revive);
    }

    /// <summary>Gunun basina doner — GameManager.OnPlayerDied() zaten gun basi snapshot'ini
    /// (SaveSystem.LoadSnapshot()) yukleyip bugunku ilerlemeyi geri aliyor.</summary>
    private void OnRestartDayPressed()
    {
        Time.timeScale = 1f;
        if (root != null) root.SetActive(false);

        if (GameManager.Instance != null) GameManager.Instance.OnPlayerDied();
    }

    /// <summary>Kaydi tamamen siler, sahneyi sifirdan yukler — oyun 1. gunden baslar.</summary>
    private void OnRestartGamePressed()
    {
        Time.timeScale = 1f;

        if (SaveSystem.Instance != null)
        {
            int slot = SaveSystem.Instance.ActiveSlot;
            if (slot >= 0) SaveSystem.Instance.DeleteSlot(slot);
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}