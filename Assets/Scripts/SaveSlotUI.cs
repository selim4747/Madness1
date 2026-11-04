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

        Refresh();
    }

    public void Refresh()
    {
        if (titleLabel != null) titleLabel.text = "Slot " + (slotIndex + 1);

        SaveData data = SaveSystem.Instance.PeekSlot(slotIndex);
        bool exists = data != null;

        if (infoLabel != null)
        {
            if (exists)
            {
                int hours = Mathf.FloorToInt(data.totalPlayTimeSeconds / 3600f);
                int minutes = Mathf.FloorToInt((data.totalPlayTimeSeconds % 3600f) / 60f);

                infoLabel.text = "Gun " + data.currentDay + "  ·  " + data.gold + " Para\n" +
                                 data.saveTimestamp + "  ·  " + hours + "s " + minutes + "dk";
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
        menu.LoadSlot(slotIndex);
    }

    private void OnNewGame()
    {
        menu.StartNewGame(slotIndex);
    }

    private void OnDelete()
    {
        SaveSystem.Instance.DeleteSlot(slotIndex);
        Refresh();
        if (ToastNotification.Instance != null)
            ToastNotification.Instance.Show("Slot " + (slotIndex + 1) + " silindi.", 2.5f);
    }
}