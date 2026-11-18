// MainMenuController.cs — Ana menu
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using System.Collections.Generic;
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
    [Tooltip("Y panelinin içindeki Geri Dön butonu. Basılınca Y gizlenir ve X (ana menü paneli) tekrar görünür olur.")]
    [SerializeField] private Button continueBackButton;

    [Header("Mod seçimi")]
    [Tooltip("Z paneli: 'Modlar' butonuna basılınca görünen mod listesi paneli.")]
    [SerializeField] private GameObject modSelectionPanel;
    [Tooltip("Z panelindeki mod butonları. Mod sayısı kadar görünür, geri kalanı gizlenir.")]
    [SerializeField] private Button[] modSlotButtons;
    [Tooltip("Z panelinin içindeki Geri Dön butonu. Basılınca Z gizlenir ve X (ana menü paneli) tekrar görünür olur.")]
    [SerializeField] private Button modBackButton;

    private ModRuntime modRuntime;
    private string pendingDeleteModId;
    private float pendingDeleteUntil;

    private void Start()
    {
        try // Mod Ekle butonu artık sahnede elle: ModImportUI bileşeni
        {
            modRuntime = ModRuntime.EnsureExists();
            modRuntime.OnModsChanged += RefreshModButtons; // mod aç/kapa, içe aktarma → butonlar tazelenir
        }
        catch (System.Exception ex) { Debug.LogException(ex); }
        SaveSystem.EnsureExists(); // ana menü sahnesinde SaveSystem yok — PeekSlot null veriyordu

        if (slots != null)
        {
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] != null) slots[i].Bind(this);
        }

        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (quitButton != null)     quitButton.onClick.AddListener(Quit);
        if (continueBackButton != null)
        {
            continueBackButton.onClick.RemoveAllListeners();
            continueBackButton.onClick.AddListener(CloseContinueSelection);
        }
        BindContinueSlotButtons();
        if (continueSelectionPanel != null) continueSelectionPanel.SetActive(false);

        if (modBackButton != null)
        {
            modBackButton.onClick.RemoveAllListeners();
            modBackButton.onClick.AddListener(CloseModSelection);
        }
        if (modSelectionPanel != null) modSelectionPanel.SetActive(false);
        RefreshModButtons();

        if (AudioManager.Instance != null) AudioManager.Instance.PlayMenuMusic();
    }

    /// <summary>Kayitli oyunu yukle — sadece NIYETI isaretler, veriyi yuklemez.</summary>
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

    /// <summary>Modlar: ana menü paneli (X) gizlenir, mod listesi paneli (Z) görünür olur.</summary>
    public void OpenMods()
    {
        OpenModSelection();
    }

    private void OpenModSelection()
    {
        if (modSelectionPanel == null)
        {
            Debug.LogWarning("[MainMenuController] 'Mod Selection Panel' (Z) Inspector'da atanmadı.");
            return;
        }

        RefreshModButtons();
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        modSelectionPanel.SetActive(true);
    }

    /// <summary>Geri Dön: mod paneli (Z) gizlenir, ana menü paneli (X) tekrar görünür olur.</summary>
    public void CloseModSelection()
    {
        if (modSelectionPanel != null) modSelectionPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    /// <summary>Z panelindeki butonları modlarla doldurur: her moda bir buton, artan butonlar gizlenir.</summary>
    private void RefreshModButtons()
    {
        if (modSlotButtons == null) return;

        List<ModManifest> mods = CollectMods();
        for (int i = 0; i < modSlotButtons.Length; i++)
        {
            Button button = modSlotButtons[i];
            if (button == null) continue;

            bool used = i < mods.Count;
            button.gameObject.SetActive(used);
            button.onClick.RemoveAllListeners();
            if (!used) continue;

            ModManifest mod = mods[i];
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null) label.text = BuildModDetails(mod);
            bool builtIn = modRuntime != null && modRuntime.IsBuiltIn(mod.id);
            button.onClick.AddListener(() => SetModEnabled(mod.id, !mod.enabled, builtIn));
            ConfigureDeleteButton(button, mod, builtIn);
        }

        if (mods.Count > modSlotButtons.Length)
            Debug.LogWarning($"[MainMenuController] {mods.Count} mod var ama sadece {modSlotButtons.Length} mod butonu atanmış; fazlası gösterilmiyor.");
    }

    private void ConfigureDeleteButton(Button modButton, ModManifest mod, bool builtIn)
    {
        Transform existing = modButton.transform.Find("ModDeleteButton");
        Button delete = existing == null ? CreateDeleteButton(modButton.transform) : existing.GetComponent<Button>();
        delete.onClick.RemoveAllListeners();
        delete.interactable = !builtIn;
        TextMeshProUGUI label = delete.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null) label.text = builtIn ? "Silinemez" : "Sil";
        if (!builtIn) delete.onClick.AddListener(() => DeleteMod(mod.id));
    }

    private static Button CreateDeleteButton(Transform parent)
    {
        GameObject buttonObject = new GameObject("ModDeleteButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-12f, 10f);
        rect.sizeDelta = new Vector2(120f, 36f);
        buttonObject.GetComponent<Image>().color = new Color(0.45f, 0.14f, 0.14f, 1f);
        TextMeshProUGUI label = buttonObject.AddComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 18;
        label.color = Color.white;
        label.raycastTarget = false;
        return buttonObject.GetComponent<Button>();
    }

    /// <summary>Harici modlar + yerleşik modlar (varsa) tek listede.</summary>
    private List<ModManifest> CollectMods()
    {
        List<ModManifest> mods = new List<ModManifest>();
        if (modRuntime == null)
        {
            try { modRuntime = ModRuntime.EnsureExists(); }
            catch (System.Exception ex) { Debug.LogException(ex); }
        }

        if (modRuntime != null)
        {
            IReadOnlyList<ModManifest> loaded = modRuntime.LoadedManifests;
            for (int i = 0; i < loaded.Count; i++)
                if (loaded[i] != null) mods.Add(loaded[i]);
        }

        IReadOnlyList<string> builtInIds = BuiltInModCatalog.GetAvailableIds();
        for (int i = 0; i < builtInIds.Count; i++)
        {
            string id = builtInIds[i];
            bool listed = false;
            for (int j = 0; j < mods.Count; j++)
                if (string.Equals(mods[j].id, id, System.StringComparison.OrdinalIgnoreCase)) { listed = true; break; }
            if (listed) continue;

            mods.Add(new ModManifest
            {
                id = id,
                name = BuiltInModCatalog.GetDisplayName(id),
                identity = BuiltInModCatalog.GetIdentity(id),
                version = "yerleşik",
                author = "NJG Games",
                enabled = BuiltInModCatalog.IsEnabled(id)
            });
        }

        return mods;
    }

    /// <summary>Mod butonu yazısı: açıklama, sürüm, yapımcı, 6 karakterlik kimlik ve durum.</summary>
    private string BuildModDetails(ModManifest mod)
    {
        string name = string.IsNullOrWhiteSpace(mod.name) ? mod.id : mod.name;
        string identity = string.IsNullOrWhiteSpace(mod.identity) ? "-" : mod.identity;
        string version = string.IsNullOrWhiteSpace(mod.version) ? "-" : mod.version;
        string author = string.IsNullOrWhiteSpace(mod.author) ? "Bilinmiyor" : mod.author;
        string description = string.IsNullOrWhiteSpace(mod.description) ? "Açıklama yok." : mod.description;
        string status = modRuntime != null && !modRuntime.IsBuiltIn(mod.id)
            ? modRuntime.GetModStatus(mod)
            : (mod.enabled ? "Açık" : "Kapalı");

        return name + "  v" + version + "\n" +
               "<size=65%>Kimlik: " + identity + "  ·  Yapımcı: " + author + "  ·  Durum: " + status + "</size>\n" +
               "<size=65%>" + description + "</size>";
    }

    private void SetModEnabled(string modId, bool enabled, bool builtIn)
    {
        bool changed = builtIn
            ? BuiltInModCatalog.SetEnabled(modId, enabled)
            : modRuntime != null && modRuntime.SetExternalModEnabled(modId, enabled);
        if (!changed) RefreshModButtons();
    }

    private void DeleteMod(string modId)
    {
        if (pendingDeleteModId != modId || Time.unscaledTime > pendingDeleteUntil)
        {
            pendingDeleteModId = modId;
            pendingDeleteUntil = Time.unscaledTime + 5f;
            if (ToastNotification.Instance != null)
                ToastNotification.Instance.Show("Silmek için aynı butona tekrar basın.", 3f);
            return;
        }

        string message;
        if (modRuntime != null) modRuntime.DeleteExternalMod(modId, out message);
        else message = "Mod sistemi hazır değil.";
        pendingDeleteModId = null;
        pendingDeleteUntil = 0f;
        if (ToastNotification.Instance != null) ToastNotification.Instance.Show(message, 3f);
        RefreshModButtons();
    }

    private void OnDestroy()
    {
        if (modRuntime != null) modRuntime.OnModsChanged -= RefreshModButtons;
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

    /// <summary>Buton yazılarını ve boş slotların tıklanamazlığını günceller.</summary>
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

    /// <summary>Slot butonunun yazısı: kayıt adı, gün, para, son kayıt zamanı ve
    /// kaydın gerektirdiği 6 karakterlik mod kimlikleri (küçük yazı).</summary>
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

    /// <summary>Yeni oyun — sadece NIYETI isaretler. Verilen slot bossa orada, doluysa ilk bos
    /// slota yazilir; hic bos slot yoksa mevcut kaydin uzerine yazmaz.</summary>
    public void StartNewGame(int slot)
    {
        SaveSystem saveSystem = SaveSystem.EnsureExists();
        if (slot < 0 || slot >= SaveSystem.SlotCount) slot = 0;

        if (saveSystem.SlotExists(slot))
        {
            int emptySlot = -1;
            for (int i = 0; i < SaveSystem.SlotCount; i++)
                if (!saveSystem.SlotExists(i)) { emptySlot = i; break; }

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
