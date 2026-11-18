// SaveSlotUI.cs — Ana menudeki tek save slot karti
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveSlotUI : MonoBehaviour
{
    [Header("Slot")]
    [SerializeField] private int slotIndex = 0;

    [Header("Referanslar")]
    [SerializeField] private TextMeshProUGUI titleLabel;
    [SerializeField] private TextMeshProUGUI infoLabel;
    [Tooltip("Opsiyonel. Doldurulursa kayıt adını buradan değiştirebilirsiniz.")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button deleteButton;

    private MainMenuController menu;

    public void Bind(MainMenuController owner)
    {
        menu = owner;

        if (loadButton != null) { loadButton.onClick.RemoveAllListeners(); loadButton.onClick.AddListener(OnLoad); }
        if (newGameButton != null) { newGameButton.onClick.RemoveAllListeners(); newGameButton.onClick.AddListener(OnNewGame); }
        if (deleteButton != null) { deleteButton.onClick.RemoveAllListeners(); deleteButton.onClick.AddListener(OnDelete); }
        if (nameInput != null)
        {
            nameInput.onEndEdit.RemoveAllListeners();
            nameInput.onEndEdit.AddListener(OnRename);
        }

        Refresh();
    }

    public void Refresh()
    {
        SaveSystem saveSystem = SaveSystem.EnsureExists();
        SaveData data = saveSystem.PeekSlot(slotIndex);
        bool exists = data != null;
        if (titleLabel != null) titleLabel.text = data == null ? "Kayıt " + (slotIndex + 1) : saveSystem.GetSlotName(slotIndex);
        if (nameInput != null) nameInput.text = data == null ? "" : saveSystem.GetSlotName(slotIndex);

        if (infoLabel != null)
        {
            if (exists)
            {
                int hours = Mathf.FloorToInt(data.totalPlayTimeSeconds / 3600f);
                int minutes = Mathf.FloorToInt((data.totalPlayTimeSeconds % 3600f) / 60f);

                infoLabel.text = "Gun " + data.currentDay + "  ·  " + data.gold + " Para\n" +
                                 data.saveTimestamp + "  ·  " + hours + "s " + minutes + "dk\n" +
                                 "Gereken modlar: " + saveSystem.GetRequiredModsText(slotIndex);
            }
            else
            {
                infoLabel.text = "Bos slot";
            }
        }

        if (loadButton != null) loadButton.gameObject.SetActive(exists);
        if (deleteButton != null) deleteButton.gameObject.SetActive(exists);
        if (newGameButton != null) newGameButton.gameObject.SetActive(!exists);
    }

    private void OnLoad()
    {
        if (menu != null) menu.LoadSlot(slotIndex);
    }

    private void OnNewGame()
    {
        menu.StartNewGame(slotIndex);
    }

    private void OnDelete()
    {
        SaveSystem.EnsureExists().DeleteSlot(slotIndex);
        Refresh();
        if (ToastNotification.Instance != null)
            ToastNotification.Instance.Show("Slot " + (slotIndex + 1) + " silindi.", 2.5f);
    }

    private void OnRename(string value)
    {
        if (SaveSystem.EnsureExists().RenameSlot(slotIndex, value)) Refresh();
    }
}
