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
    [Header("Devam seçimi")]
    [Tooltip("Boş bırakılırsa Devam Et basılınca panel ve 5 kayıt butonu otomatik oluşturulur.")]
    [SerializeField] private Transform continuePanelParent;

    private GameObject generatedContinuePanel;

    private void Start()
    {
        try { ModRuntime.EnsureExists(); } // Mod Ekle butonu artık sahnede elle: ModImportUI bileşeni
        catch (System.Exception ex) { Debug.LogException(ex); }
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] != null) slots[i].Bind(this);

        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (quitButton != null)     quitButton.onClick.AddListener(Quit);

        if (AudioManager.Instance != null) AudioManager.Instance.PlayMenuMusic();
    }

    /// <summary>Kayitli oyunu yukle — sadece NIYETI isaretler, veriyi yuklemez.</summary>
    public void LoadSlot(int slot)
    {
        if (slots == null || slots.Length == 0)
        {
            OpenContinueSelection();
            return;
        }
        LoadSlotDirect(slot);
    }

    public void Continue()
    {
        OpenContinueSelection();
    }

    private void LoadSlotDirect(int slot)
    {
        if (SaveSystem.Instance == null || !SaveSystem.Instance.SlotExists(slot)) return;
        SaveSystem.RequestLoadSlot(slot);
        SceneManager.LoadScene(gameSceneName);
    }

    private void OpenContinueSelection()
    {
        if (generatedContinuePanel != null)
        {
            generatedContinuePanel.SetActive(true);
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;
        Transform parent = continuePanelParent != null ? continuePanelParent : canvas.transform;
        generatedContinuePanel = new GameObject("ContinueSaveSelection");
        generatedContinuePanel.transform.SetParent(parent, false);
        RectTransform root = generatedContinuePanel.AddComponent<RectTransform>();
        root.anchorMin = new Vector2(0.5f, 0.5f); root.anchorMax = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(720f, 620f);
        root.anchoredPosition = Vector2.zero;
        UnityEngine.UI.Image background = generatedContinuePanel.AddComponent<UnityEngine.UI.Image>();
        background.color = new Color(0.04f, 0.04f, 0.07f, 0.96f);

        GameObject titleObject = new GameObject("Title");
        titleObject.transform.SetParent(generatedContinuePanel.transform, false);
        TextMeshProUGUI title = titleObject.AddComponent<TextMeshProUGUI>();
        title.text = "Kayıtlı Oyun Seç"; title.fontSize = 32; title.alignment = TextAlignmentOptions.Center;
        RectTransform titleRect = title.rectTransform; titleRect.anchorMin = new Vector2(0, 1); titleRect.anchorMax = new Vector2(1, 1); titleRect.sizeDelta = new Vector2(-40, 60); titleRect.anchoredPosition = new Vector2(0, -45);

        for (int i = 0; i < SaveSystem.SlotCount; i++) CreateContinueButton(generatedContinuePanel.transform, i);
        GameObject closeObject = new GameObject("Close");
        closeObject.transform.SetParent(generatedContinuePanel.transform, false);
        UnityEngine.UI.Button close = closeObject.AddComponent<UnityEngine.UI.Button>();
        TextMeshProUGUI closeText = closeObject.AddComponent<TextMeshProUGUI>();
        closeText.text = "Kapat"; closeText.fontSize = 22; closeText.alignment = TextAlignmentOptions.Center;
        RectTransform closeRect = closeText.rectTransform; closeRect.anchorMin = new Vector2(0.5f, 0); closeRect.anchorMax = new Vector2(0.5f, 0); closeRect.sizeDelta = new Vector2(220, 50); closeRect.anchoredPosition = new Vector2(0, 35);
        close.onClick.AddListener(() => generatedContinuePanel.SetActive(false));
    }

    private void CreateContinueButton(Transform parent, int slot)
    {
        GameObject objectRoot = new GameObject("SaveSlot_" + slot);
        objectRoot.transform.SetParent(parent, false);
        UnityEngine.UI.Button button = objectRoot.AddComponent<UnityEngine.UI.Button>();
        TextMeshProUGUI text = objectRoot.AddComponent<TextMeshProUGUI>();
        SaveData data = SaveSystem.Instance.PeekSlot(slot);
        bool exists = data != null;
        text.text = exists
            ? SaveSystem.Instance.GetSlotName(slot) + "  |  Gün " + data.currentDay + "  |  " + data.gold + " Para\nModlar: " + SaveSystem.Instance.GetRequiredModsText(slot)
            : "Kayıt " + (slot + 1) + " — Boş";
        text.fontSize = 20; text.alignment = TextAlignmentOptions.Center; text.color = Color.white;
        RectTransform rect = text.rectTransform; rect.anchorMin = new Vector2(0.5f, 1); rect.anchorMax = new Vector2(0.5f, 1); rect.sizeDelta = new Vector2(650, 75); rect.anchoredPosition = new Vector2(0, -115 - slot * 85);
        button.interactable = exists;
        int selectedSlot = slot;
        button.onClick.AddListener(() => LoadSlotDirect(selectedSlot));
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
