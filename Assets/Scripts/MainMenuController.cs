// MainMenuController.cs — Ana menu
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Madness.Mods;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("Sahne")]
    [SerializeField] private string gameSceneName = "Game";

    [Header("Slotlar")]
    [SerializeField] private SaveSlotUI[] slots;

    [Header("Butonlar")]
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    [Header("Paneller")]
    [Tooltip("X paneli: ana menü butonlarının (Devam Et, Yeni Oyun, Ayarlar, Çıkış) çocuğu olduğu panel. Devam Et'e basılınca gizlenir.")]
    [SerializeField] private GameObject mainMenuPanel;

    [Header("Devam seçimi")]
    [Tooltip("Y paneli: Devam Et'e basılınca görünen kayıt seçim paneli (içindeki butonları sen hazırlarsın).")]
    [SerializeField] private GameObject continueSelectionPanel;
    [Tooltip("Y panelindeki 5 kayıt butonu, sırayla (0,1,2,3,4). OnClick'i elle bağlamana gerek yok; boş slotun butonu tıklanamaz olur.")]
    [SerializeField] private Button[] continueSlotButtons;
    [Tooltip("Opsiyonel: kayıt butonlarının yazıları, aynı sırayla. Doldurulursa gün/para/mod özeti otomatik yazılır.")]
    [SerializeField] private TextMeshProUGUI[] continueSlotLabels;
    [Tooltip("Y panelinin içindeki Geri Dön butonu. Basılınca Y gizlenir ve X (ana menü paneli) tekrar görünür olur.")]
    [SerializeField] private Button continueBackButton;

    private void Start()
    {
        try { ModRuntime.EnsureExists(); } // Mod Ekle butonu artık sahnede elle: ModImportUI bileşeni
        catch (System.Exception ex) { Debug.LogException(ex); }
        SaveSystem.EnsureExists(); // ana menü sahnesinde SaveSystem yok — PeekSlot null veriyordu

        if (slots != null)
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] != null) slots[i].Bind(this);

        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (quitButton != null)     quitButton.onClick.AddListener(Quit);
        if (continueBackButton != null)
        {
            continueBackButton.onClick.RemoveAllListeners();
            continueBackButton.onClick.AddListener(CloseContinueSelection);
        }
        BindContinueSlotButtons();
        if (continueSelectionPanel != null) continueSelectionPanel.SetActive(false);

        if (AudioManager.Instance != null) AudioManager.Instance.PlayMenuMusic();
    }

    /// <summary>Kayitli oyunu yukle — sadece NIYETI isaretler, veriyi yuklemez.</summary>
    public void LoadSlot(int slot)
    {
        LoadSlotDirect(slot);
    }

    public void Continue()
    {
        OpenContinueSelection();
    }

    private void LoadSlotDirect(int slot)
    {
        SaveSystem saveSystem = SaveSystem.EnsureExists();
        if (saveSystem == null || !saveSystem.SlotExists(slot)) return;
        SaveSystem.RequestLoadSlot(slot);
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>Devam Et: ana menü paneli (X) gizlenir, kayıt seçim paneli (Y) görünür olur.</summary>
    private void OpenContinueSelection()
    {
        if (continueSelectionPanel == null)
        {
            Debug.LogWarning("[MainMenuController] 'Continue Selection Panel' (Y) Inspector'da atanmadı.");
            return;
        }

        RefreshContinueSlotButtons();
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        continueSelectionPanel.SetActive(true);
    }

    /// <summary>Geri Dön: kayıt seçim paneli (Y) gizlenir, ana menü paneli (X) tekrar görünür olur.</summary>
    public void CloseContinueSelection()
    {
        if (continueSelectionPanel != null) continueSelectionPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    /// <summary>Y panelindeki kendi yaptığın kayıt butonlarını slotlarına bağlar.</summary>
    private void BindContinueSlotButtons()
    {
        if (continueSlotButtons == null) return;

        for (int i = 0; i < continueSlotButtons.Length; i++)
        {
            Button button = continueSlotButtons[i];
            if (button == null) continue;
            int slot = i;
            button.onClick.AddListener(() => LoadSlot(slot));
        }

        RefreshContinueSlotButtons();
    }

    /// <summary>Butonların tıklanabilirliğini ve yazılarını güncel kayda göre yeniler.</summary>
    private void RefreshContinueSlotButtons()
    {
        SaveSystem saveSystem = SaveSystem.EnsureExists();

        if (continueSlotButtons != null)
        {
            for (int i = 0; i < continueSlotButtons.Length; i++)
            {
                Button button = continueSlotButtons[i];
                if (button != null) button.interactable = saveSystem.SlotExists(i);
            }
        }

        if (continueSlotLabels == null) return;

        for (int i = 0; i < continueSlotLabels.Length; i++)
        {
            TextMeshProUGUI label = continueSlotLabels[i];
            if (label != null) label.text = GetSlotSummary(saveSystem, i);
        }
    }

    private static string GetSlotSummary(SaveSystem saveSystem, int slot)
    {
        SaveData data = saveSystem.PeekSlot(slot);
        if (data == null) return "Kayıt " + (slot + 1) + " — Boş";
        return saveSystem.GetSlotName(slot) + "  |  Gün " + data.currentDay + "  |  " + data.gold + " Para\nModlar: " + saveSystem.GetRequiredModsText(slot);
    }

    /// <summary>Yeni oyun — sadece NIYETI isaretler.</summary>
    public void StartNewGame(int slot)
    {
        SaveSystem.RequestNewGame(slot);
        SceneManager.LoadScene(gameSceneName);
    }

    private void OpenSettings()
    {
        SettingsMenu.Instance.Show();
    }

    private void Quit()
    {
        Application.Quit();
    }
}
