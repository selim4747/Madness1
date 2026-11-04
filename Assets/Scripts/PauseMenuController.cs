// PauseMenuController.cs — Duraklat menusu ve alt panel yonlendirmeleri
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    public static PauseMenuController Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;

    [Header("Alt paneller")]
    [SerializeField] private Button catalogButton;
    [SerializeField] private Button achievementsButton;
    [SerializeField] private Button marketButton;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private UpgradePanelUI upgradePanel;

    public bool IsPaused { get { return root != null && root.activeSelf; } }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (root != null) root.SetActive(false);

        if (pauseButton != null) pauseButton.onClick.AddListener(Pause);
        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (catalogButton != null) catalogButton.onClick.AddListener(OpenCatalog);
        if (achievementsButton != null) achievementsButton.onClick.AddListener(OpenAchievements);
        if (marketButton != null) marketButton.onClick.AddListener(OpenMarket);
        if (upgradeButton != null) upgradeButton.onClick.AddListener(OpenUpgrades);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (saveButton != null) saveButton.onClick.AddListener(SaveGame);
    }

    public void Pause()
    {
        if (root != null) root.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        if (root != null) root.SetActive(false);
        Time.timeScale = 1f;
    }

    private void OpenCatalog()
    {
        BookCatalogUI.Instance.Open();
    }

    /// <summary>Bolum 5E'deki AchievementsPanelUI'yi acar.</summary>
    private void OpenAchievements()
    {
        AchievementsPanelUI.Instance.Open();
    }

    private void OpenMarket()
    {
        MarketUI.Instance.Open();
    }

    private void OpenUpgrades()
    {
        if (upgradePanel != null) upgradePanel.Open();
    }

    private void OpenSettings()
    {
        SettingsMenu.Instance.Show();
    }

    private void SaveGame()
    {
        int slot = SaveSystem.Instance.ActiveSlot;
        if (slot < 0) slot = 0;

        // Oyun ici "Kaydet" butonu da artik oturum durumunu (konum, gun sayaci, musteriler)
        // yaziyor -- daha once sadece Ana Menu'ye Don butonu bunu yapiyordu, bu yuzden
        // sadece bu butonla kaydedenlerde konum/musteri bilgisi kayboluyordu.
        if (GameManager.Instance != null)
            GameManager.Instance.OturumDurumunuYaz(SaveSystem.Instance.Current);

        SaveSystem.Instance.SaveToSlot(slot);
        if (ToastNotification.Instance != null)
            ToastNotification.Instance.Show("Oyun kaydedildi (Slot " + (slot + 1) + ").", 3f);
    }
}