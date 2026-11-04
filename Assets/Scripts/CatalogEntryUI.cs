// CatalogEntryUI.cs — Katalogdaki tek kitap karti
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CatalogEntryUI : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private Image coverImage;
    [SerializeField] private Image rarityFrame;
    [SerializeField] private TextMeshProUGUI nameLabel;
    [SerializeField] private TextMeshProUGUI genreLabel;
    [SerializeField] private TextMeshProUGUI rarityLabel;
    [SerializeField] private TextMeshProUGUI stockLabel;
    [SerializeField] private TextMeshProUGUI loreLabel;

    [Header("Kesfedilmemis gorunum")]
    [SerializeField] private Sprite unknownSprite;
    [SerializeField] private Color unknownColor = new Color(0.25f, 0.25f, 0.25f);

    public void Bind(BookData book, bool discovered, int currentStock)
    {
        if (!discovered)
        {
            if (coverImage != null)  { coverImage.sprite = unknownSprite; coverImage.color = unknownColor; }
            if (rarityFrame != null) rarityFrame.color = unknownColor;
            if (nameLabel != null)   nameLabel.text = "???";
            if (genreLabel != null)  genreLabel.text = "";
            if (rarityLabel != null) rarityLabel.text = "";
            if (stockLabel != null)  stockLabel.text = "";
            if (loreLabel != null)   loreLabel.text = "";
            return;
        }

        if (coverImage != null)
        {
            coverImage.sprite = book.bookCover;
            coverImage.color = Color.white;
        }

        if (rarityFrame != null) rarityFrame.color = RarityUtil.GetColor(book.rarity);
        if (nameLabel != null) nameLabel.text = book.displayName;
        if (genreLabel != null)  genreLabel.text = book.genre;

        if (rarityLabel != null)
        {
            rarityLabel.text = RarityUtil.GetDisplayName(book.rarity) + " — " + book.GetFinalPrice() + " Para";
            rarityLabel.color = RarityUtil.GetColor(book.rarity);
        }

        if (stockLabel != null) stockLabel.text = currentStock + " / " + ShelfController.MaxStock + " kopya";
        if (loreLabel != null)  loreLabel.text = book.loreSentence;
    }
}
