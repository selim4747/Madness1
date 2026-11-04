// ShelfEntryUI.cs — Raf listesindeki tek satir
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShelfEntryUI : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private Image coverImage;
    [SerializeField] private Image rarityFrame;
    [SerializeField] private TextMeshProUGUI nameLabel;
    [SerializeField] private TextMeshProUGUI rarityLabel;
    [SerializeField] private TextMeshProUGUI stockLabel;
    [SerializeField] private Button unlockButton;
    [SerializeField] private TextMeshProUGUI unlockLabel;

    private ShelfController shelf;
    private ShelfUnlockUI owner;

    public void Bind(ShelfController target, ShelfUnlockUI panel)
    {
        shelf = target;
        owner = panel;

        if (coverImage != null) coverImage.sprite = shelf.book.bookCover;
        if (nameLabel != null) nameLabel.text = shelf.book.displayName;

        if (rarityFrame != null) rarityFrame.color = RarityUtil.GetColor(shelf.book.rarity);

        if (rarityLabel != null)
        {
            rarityLabel.text = RarityUtil.GetDisplayName(shelf.book.rarity) +
                               " — satis " + shelf.book.GetFinalPrice() + " Para";
            rarityLabel.color = RarityUtil.GetColor(shelf.book.rarity);
        }

        RefreshState();

        if (unlockButton != null)
        {
            unlockButton.onClick.RemoveAllListeners();
            unlockButton.onClick.AddListener(OnPressed);
        }
    }

    private void RefreshState()
    {
        if (shelf.isUnlocked)
        {
            if (unlockLabel != null) unlockLabel.text = "Acik";
            if (unlockButton != null) unlockButton.interactable = false;
            if (stockLabel != null)
                stockLabel.text = shelf.CurrentStock + " / " + ShelfController.MaxStock + " kopya";
            return;
        }

        if (stockLabel != null) stockLabel.text = "Kapali";

        if (ShopExpansionManager.Instance == null) return;

        bool canUnlock = ShopExpansionManager.Instance.CanUnlockShelf(shelf);
        int cost = ShopExpansionManager.Instance.GetShelfCost(shelf);

        if (!canUnlock)
        {
            // Dukkan henuz satin alinmamis
            if (unlockLabel != null) unlockLabel.text = "Dukkan kilitli";
            if (unlockButton != null) unlockButton.interactable = false;
            return;
        }

        if (unlockLabel != null) unlockLabel.text = cost + " Para";
        if (unlockButton != null) unlockButton.interactable = EconomyManager.Instance.CanAfford(cost);
    }

    private void OnPressed()
    {
        owner.OnUnlockPressed(shelf);
    }
}