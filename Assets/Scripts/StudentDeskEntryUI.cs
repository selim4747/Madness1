// StudentDeskEntryUI.cs — Ogrenci masasi listesindeki tek satir
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StudentDeskEntryUI : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private TextMeshProUGUI nameLabel;
    [SerializeField] private TextMeshProUGUI statusLabel;
    [SerializeField] private Button unlockButton;
    [SerializeField] private TextMeshProUGUI unlockLabel;

    private StudentDeskController masa;
    private StudentDeskUnlockUI owner;

    public void Bind(StudentDeskController target, StudentDeskUnlockUI panel)
    {
        masa = target;
        owner = panel;

        if (nameLabel != null) nameLabel.text = masa.DeskIndex + ". Ogrenci Masasi";

        RefreshState();

        if (unlockButton != null)
        {
            unlockButton.onClick.RemoveAllListeners();
            unlockButton.onClick.AddListener(OnPressed);
        }
    }

    public void RefreshState()
    {
        if (masa.IsUnlocked)
        {
            if (statusLabel != null) statusLabel.text = masa.HasActiveRequest ? "Ogrenci bekliyor..." : "Bos";
            if (unlockLabel != null) unlockLabel.text = "Acik";
            if (unlockButton != null) unlockButton.interactable = false;
            return;
        }

        if (statusLabel != null) statusLabel.text = "Kapali";
        if (unlockLabel != null) unlockLabel.text = masa.UnlockCost + " Para";
        if (unlockButton != null) unlockButton.interactable = EconomyManager.Instance.CanAfford(masa.UnlockCost);
    }

    private void OnPressed()
    {
        owner.OnUnlockPressed(masa);
    }
}
