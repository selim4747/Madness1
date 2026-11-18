// PauseMenuController.cs — Durdur menusu: devam, kaydet, ana menu, ses
// NJG Games | Delilik | Unity 6 | UTF-8
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PauseMenuController : MonoBehaviour
{
    public static PauseMenuController Instance { get; private set; }

    [Header("Panel")]
    [Tooltip("Durdur paneli: sahnede hazır olmalı, oyun başında kapatılır.")]
    [SerializeField] private GameObject root;
    [Tooltip("Panel açan buton (eski 'Oyunu Durdur' butonunun yerine koyacağın buton).")]
    [SerializeField] private Button pauseButton;

    [Header("Panel butonları")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button mainMenuButton;
    [Tooltip("Ses aç/kapa. Buton yazısı otomatik 'Ses: Açık/Kapalı' olur.")]
    [SerializeField] private Button sfxButton;
    [Tooltip("Müzik aç/kapa. Buton yazısı otomatik 'Müzik: Açık/Kapalı' olur.")]
    [SerializeField] private Button musicButton;

    [Header("Ek butonlar")]
    [SerializeField] private Button catalogButton;
    [SerializeField] private Button achievementsButton;
    [SerializeField] private Button marketButton;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private UpgradePanelUI upgradePanel;

    [Header("Sahne")]
    [SerializeField] private string mainMenuSceneName = "AnaMenu";

    public bool IsPaused { get { return root != null && root.activeSelf; } }

    private float musicBeforeMute = 0.7f;
    private float sfxBeforeMute = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (root != null) root.SetActive(false);

        if (pauseButton != null) pauseButton.onClick.AddListener(Pause);
        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (saveButton != null) saveButton.onClick.AddListener(SaveGame);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        if (sfxButton != null) sfxButton.onClick.AddListener(ToggleSfx);
        if (musicButton != null) musicButton.onClick.AddListener(ToggleMusic);
        if (catalogButton != null) catalogButton.onClick.AddListener(OpenCatalog);
        if (achievementsButton != null) achievementsButton.onClick.AddListener(OpenAchievements);
        if (marketButton != null) marketButton.onClick.AddListener(OpenMarket);
        if (upgradeButton != null) upgradeButton.onClick.AddListener(OpenUpgrades);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
    }

    private void Start()
    {
        RefreshAudioButtons();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Time.timeScale = 1f;
    }

    public void Pause()
    {
        if (root == null) return;

        RefreshAudioButtons();
        root.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        if (root != null) root.SetActive(false);
        Time.timeScale = 1f;
    }

    // ── Ses / müzik butonları ──

    private void ToggleMusic()
    {
        if (AudioManager.Instance == null) return;

        if (AudioManager.Instance.MusicVolume > 0.001f)
        {
            musicBeforeMute = AudioManager.Instance.MusicVolume;
            AudioManager.Instance.SetMusicVolume(0f);
        }
        else
        {
            AudioManager.Instance.SetMusicVolume(musicBeforeMute > 0.001f ? musicBeforeMute : 0.7f);
        }

        RefreshAudioButtons();
    }

    private void ToggleSfx()
    {
        if (AudioManager.Instance == null) return;

        if (AudioManager.Instance.SfxVolume > 0.001f)
        {
            sfxBeforeMute = AudioManager.Instance.SfxVolume;
            AudioManager.Instance.SetSfxVolume(0f);
        }
        else
        {
            AudioManager.Instance.SetSfxVolume(sfxBeforeMute > 0.001f ? sfxBeforeMute : 1f);
        }

        RefreshAudioButtons();
    }

    private void RefreshAudioButtons()
    {
        bool musicOn = AudioManager.Instance != null && AudioManager.Instance.MusicVolume > 0.001f;
        bool sfxOn = AudioManager.Instance != null && AudioManager.Instance.SfxVolume > 0.001f;

        SetButtonLabel(musicButton, "Müzik: " + (musicOn ? "Açık" : "Kapalı"));
        SetButtonLabel(sfxButton, "Ses: " + (sfxOn ? "Açık" : "Kapalı"));
    }

    /// <summary>Butonun kendi Text (TMP) çocuğuna yazı yazar.</summary>
    private static void SetButtonLabel(Button button, string text)
    {
        if (button == null) return;

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null) label.text = text;
    }

    private void OpenCatalog() { if (BookCatalogUI.Instance != null) BookCatalogUI.Instance.Open(); }
    private void OpenAchievements() { if (AchievementsPanelUI.Instance != null) AchievementsPanelUI.Instance.Open(); }
    private void OpenMarket() { if (MarketUI.Instance != null) MarketUI.Instance.Open(); }
    private void OpenUpgrades() { if (upgradePanel != null) upgradePanel.Open(); }
    private void OpenSettings() { if (SettingsMenu.Instance != null) SettingsMenu.Instance.Show(); }

    public void SaveGame()
    {
        if (SaveSystem.Instance == null) return;
        int slot = SaveSystem.Instance.ActiveSlot;
        if (slot < 0) slot = 0;
        if (GameManager.Instance != null) GameManager.Instance.OturumDurumunuYaz(SaveSystem.Instance.Current);
        SaveSystem.Instance.SaveToSlot(slot);
        if (ToastNotification.Instance != null)
            ToastNotification.Instance.Show("Oyun kaydedildi (Slot " + (slot + 1) + ").", 3f);
    }

    public void ReturnToMainMenu()
    {
        SaveGame();
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
