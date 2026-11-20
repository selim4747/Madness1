// MainMenuController.cs — Ana menu
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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
    [Tooltip("X paneli: ana menü butonlarının çocuğu olduğu panel. Devam Et'e basılınca gizlenir.")]
    [SerializeField] private GameObject mainMenuPanel;

    [Header("Devam seçimi")]
    [Tooltip("Y paneli: Devam Et'e basılınca görünen kayıt seçim paneli.")]
    [SerializeField] private GameObject continueSelectionPanel;
    [Tooltip("Y panelindeki kayıt butonları, sırayla 0,1,2,3,4.")]
    [SerializeField] private Button[] continueSlotButtons;
    [Tooltip("Y panelinin içindeki Geri Dön butonu.")]
    [SerializeField] private Button continueBackButton;

    [Header("Mod paneli geçişi")]
    [Tooltip("GameObject (2): mod listesinin bulunduğu panel.")]
    [SerializeField] private GameObject modSelectionPanel;
    [Tooltip("Ana menüdeki Modlar butonu.")]
    [SerializeField] private Button modOpenButton;
    [Tooltip("GameObject (2) içindeki Geri Dön butonu.")]
    [SerializeField] private Button modBackButton;

    private void Start()
    {
        SaveSystem.EnsureExists();

        if (slots != null)
        {
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] != null) slots[i].Bind(this);
        }

        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (quitButton != null) quitButton.onClick.AddListener(Quit);

        if (continueBackButton != null)
        {
            continueBackButton.onClick.RemoveAllListeners();
            continueBackButton.onClick.AddListener(CloseContinueSelection);
        }

        BindContinueSlotButtons();
        if (continueSelectionPanel != null) continueSelectionPanel.SetActive(false);
        if (modSelectionPanel != null) modSelectionPanel.SetActive(false);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayMenuMusic();
    }

    /// <summary>Kayıtlı oyunu yükle — sadece niyeti işaretler, veriyi yüklemez.</summary>
    public void LoadSlot(int slot)
    {
        if (!SaveSystem.EnsureExists().SlotExists(slot)) return;
        SaveSystem.RequestLoadSlot(slot);
        SceneManager.LoadScene(gameSceneName);
    }

    public void Continue()
    {
        OpenContinueSelection();
    }

    private void OpenContinueSelection()
    {
        if (continueSelectionPanel == null)
        {
            Debug.LogWarning("[MainMenuController] Continue Selection Panel Inspector'da atanmadı.");
            return;
        }

        RefreshContinueSlotButtons();
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        continueSelectionPanel.SetActive(true);
    }

    public void CloseContinueSelection()
    {
        if (continueSelectionPanel != null) continueSelectionPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    public void OpenModSelection()
    {
        if (modSelectionPanel == null)
        {
            Debug.LogWarning("[MainMenuController] Mod Selection Panel Inspector'da atanmadı.");
            return;
        }

        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        modSelectionPanel.SetActive(true);
    }

    public void CloseModSelection()
    {
        if (modSelectionPanel != null) modSelectionPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

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

    private void RefreshContinueSlotButtons()
    {
        if (continueSlotButtons == null) return;

        SaveSystem saveSystem = SaveSystem.EnsureExists();
        for (int i = 0; i < continueSlotButtons.Length; i++)
        {
            Button button = continueSlotButtons[i];
            if (button == null) continue;

            button.interactable = saveSystem.SlotExists(i);
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null) label.text = BuildSlotInfo(saveSystem, i);
        }
    }

    private static string BuildSlotInfo(SaveSystem saveSystem, int slot)
    {
        SaveData data = saveSystem.PeekSlot(slot);
        if (data == null) return "Kayıt " + (slot + 1) + "\n<size=65%>Boş slot</size>";

        return saveSystem.GetSlotName(slot) + "\n" +
               "Gün " + data.currentDay + "  ·  " + data.gold + " Para\n" +
               "Son kayıt: " + data.saveTimestamp + "\n" +
               "Modlar:\n" +
               "<size=60%>" + GetModIdentities(data) + "</size>";
    }

    private static string GetModIdentities(SaveData data)
    {
        if (data.requiredMods == null || data.requiredMods.Count == 0) return "yok";

        System.Text.StringBuilder ids = new System.Text.StringBuilder();
        for (int i = 0; i < data.requiredMods.Count; i++)
        {
            RequiredModSaveEntry mod = data.requiredMods[i];
            if (mod == null) continue;

            string id = string.IsNullOrEmpty(mod.modIdentity) ? mod.modId : mod.modIdentity;
            if (string.IsNullOrEmpty(id)) continue;
            if (ids.Length > 0) ids.Append(", ");
            ids.Append(id);
        }

        return ids.Length > 0 ? ids.ToString() : "yok";
    }

    /// <summary>Yeni oyun — boş slot varsa onu kullanır; dolu slotların üzerine yazmaz.</summary>
    public void StartNewGame(int slot)
    {
        SaveSystem saveSystem = SaveSystem.EnsureExists();
        if (slot < 0 || slot >= SaveSystem.SlotCount) slot = 0;

        if (saveSystem.SlotExists(slot))
        {
            int emptySlot = -1;
            for (int i = 0; i < SaveSystem.SlotCount; i++)
            {
                if (!saveSystem.SlotExists(i))
                {
                    emptySlot = i;
                    break;
                }
            }

            if (emptySlot < 0)
            {
                Debug.LogWarning("[MainMenuController] Tüm kayıt slotları dolu — yeni oyun başlatılamadı.");
                if (ToastNotification.Instance != null)
                    ToastNotification.Instance.Show("Tüm kayıt slotları dolu. Önce bir kaydı silin.", 3f);
                return;
            }

            slot = emptySlot;
        }

        SaveSystem.RequestNewGame(slot);
        SceneManager.LoadScene(gameSceneName);
    }

    private void OpenSettings()
    {
        if (SettingsMenu.Instance != null)
            SettingsMenu.Instance.Show();
    }

    private void Quit()
    {
        Application.Quit();
    }
}
