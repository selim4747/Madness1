// MainMenuController.cs — Ana menu
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Madness.Mods;

public class MainMenuController : MonoBehaviour
{
    [Header("Sahne")]
    [SerializeField] private string gameSceneName = "Game";

    [Header("Slotlar")]
    [SerializeField] private SaveSlotUI[] slots;

    [Header("Butonlar")]
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    private void Start()
    {
        try { ModRuntime.EnsureExists(); ModImportUI.CreateIfPossible(); }
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
        SaveSystem.RequestLoadSlot(slot);
        SceneManager.LoadScene(gameSceneName);
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
