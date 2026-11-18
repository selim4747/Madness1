// PauseMenuController.cs — Durdur menusu: devam, kaydet, sesler, ana menu
// NJG Games | Delilik | Unity 6 | UTF-8
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

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

    [Header("Pause ek butonlari")]
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Toggle musicToggle;
    [SerializeField] private Toggle sfxToggle;
    [SerializeField] private string mainMenuSceneName = "AnaMenu";

    public bool IsPaused { get { return root != null && root.activeSelf; } }
    private float musicBeforeMute = 0.7f;
    private float sfxBeforeMute = 1f;
    private bool muteCallbacksReady;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (root == null) CreateRuntimePauseUI();
        if (root != null) root.SetActive(false);

        if (pauseButton != null) pauseButton.onClick.AddListener(Pause);
        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (catalogButton != null) catalogButton.onClick.AddListener(OpenCatalog);
        if (achievementsButton != null) achievementsButton.onClick.AddListener(OpenAchievements);
        if (marketButton != null) marketButton.onClick.AddListener(OpenMarket);
        if (upgradeButton != null) upgradeButton.onClick.AddListener(OpenUpgrades);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (saveButton != null) saveButton.onClick.AddListener(SaveGame);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        if (musicToggle != null) musicToggle.onValueChanged.AddListener(SetMusicEnabled);
        if (sfxToggle != null) sfxToggle.onValueChanged.AddListener(SetSfxEnabled);
    }

    private void Start()
    {
        RefreshAudioToggles();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Time.timeScale = 1f;
    }

    public void Pause()
    {
        RefreshAudioToggles();
        if (root != null) root.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        if (root != null) root.SetActive(false);
        Time.timeScale = 1f;
    }

    private void RefreshAudioToggles()
    {
        if (AudioManager.Instance == null) return;
        muteCallbacksReady = false;
        if (musicToggle != null) musicToggle.SetIsOnWithoutNotify(AudioManager.Instance.MusicVolume > 0.001f);
        if (sfxToggle != null) sfxToggle.SetIsOnWithoutNotify(AudioManager.Instance.SfxVolume > 0.001f);
        muteCallbacksReady = true;
    }

    private void SetMusicEnabled(bool enabled)
    {
        if (!muteCallbacksReady || AudioManager.Instance == null) return;
        if (enabled)
        {
            float value = musicBeforeMute > 0.001f ? musicBeforeMute : 0.7f;
            AudioManager.Instance.SetMusicVolume(value);
        }
        else
        {
            musicBeforeMute = AudioManager.Instance.MusicVolume;
            AudioManager.Instance.SetMusicVolume(0f);
        }
    }

    private void SetSfxEnabled(bool enabled)
    {
        if (!muteCallbacksReady || AudioManager.Instance == null) return;
        if (enabled)
        {
            float value = sfxBeforeMute > 0.001f ? sfxBeforeMute : 1f;
            AudioManager.Instance.SetSfxVolume(value);
        }
        else
        {
            sfxBeforeMute = AudioManager.Instance.SfxVolume;
            AudioManager.Instance.SetSfxVolume(0f);
        }
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

    private void CreateRuntimePauseUI()
    {
        Transform canvas = transform;
        pauseButton = CreateButton(canvas, "Oyunu Durdur", new Vector2(1f, 1f), new Vector2(-115f, -55f), new Vector2(190f, 55f));
        root = new GameObject("PauseMenuRoot");
        root.transform.SetParent(canvas, false);
        RectTransform panelRect = root.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f); panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(620f, 650f); panelRect.anchoredPosition = Vector2.zero;
        Image panelImage = root.AddComponent<Image>(); panelImage.color = new Color(0.025f, 0.025f, 0.05f, 0.97f);

        CreateLabel(root.transform, "OYUN DURDURULDU", new Vector2(0, 265), 30);
        resumeButton = CreateButton(root.transform, "Devam Et", new Vector2(0.5f, 1f), new Vector2(0, -85), new Vector2(420, 58));
        saveButton = CreateButton(root.transform, "Oyunu Kaydet", new Vector2(0.5f, 1f), new Vector2(0, -155), new Vector2(420, 58));
        mainMenuButton = CreateButton(root.transform, "Ana Menüye Dön", new Vector2(0.5f, 1f), new Vector2(0, -225), new Vector2(420, 58));
        musicToggle = CreateToggle(root.transform, "Müzik Açık", new Vector2(0, -330));
        sfxToggle = CreateToggle(root.transform, "Ses Efektleri Açık", new Vector2(0, -390));
        CreateLabel(root.transform, "Müzik ve ses düğmelerini kapatmak için işareti kaldırın.", new Vector2(0, -465), 16);
    }

    private Button CreateButton(Transform parent, string label, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(label);
        go.transform.SetParent(parent, false);
        Image image = go.AddComponent<Image>(); image.color = new Color(0.15f, 0.2f, 0.3f, 1f);
        Button button = go.AddComponent<Button>(); button.targetGraphic = image;
        RectTransform rect = go.GetComponent<RectTransform>(); rect.anchorMin = anchor; rect.anchorMax = anchor; rect.sizeDelta = size; rect.anchoredPosition = position;
        TextMeshProUGUI text = CreateLabel(go.transform, label, Vector2.zero, 22);
        text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one; text.rectTransform.sizeDelta = Vector2.zero;
        return button;
    }

    private Toggle CreateToggle(Transform parent, string label, Vector2 position)
    {
        GameObject go = new GameObject(label);
        go.transform.SetParent(parent, false);
        RectTransform rect = go.AddComponent<RectTransform>(); rect.anchorMin = new Vector2(0.5f, 1f); rect.anchorMax = new Vector2(0.5f, 1f); rect.sizeDelta = new Vector2(420, 48); rect.anchoredPosition = position;
        Toggle toggle = go.AddComponent<Toggle>();
        GameObject textGo = new GameObject("Label"); textGo.transform.SetParent(go.transform, false);
        TextMeshProUGUI text = textGo.AddComponent<TextMeshProUGUI>(); text.text = label; text.fontSize = 21; text.alignment = TextAlignmentOptions.Center; text.color = Color.white;
        RectTransform textRect = text.rectTransform; textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one; textRect.sizeDelta = Vector2.zero;
        toggle.graphic = text;
        return toggle;
    }

    private TextMeshProUGUI CreateLabel(Transform parent, string textValue, Vector2 position, float size)
    {
        GameObject go = new GameObject("Label"); go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>(); text.text = textValue; text.fontSize = size; text.alignment = TextAlignmentOptions.Center; text.color = Color.white;
        RectTransform rect = text.rectTransform; rect.anchorMin = new Vector2(0.5f, 0.5f); rect.anchorMax = new Vector2(0.5f, 0.5f); rect.sizeDelta = new Vector2(560, 52); rect.anchoredPosition = position;
        return text;
    }
}
