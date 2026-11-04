// StokEntryUI.cs — Stok panelindeki tek satir (kitap adi + stok sayisi).
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StokEntryUI : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private TextMeshProUGUI nameLabel;
    [SerializeField] private TextMeshProUGUI stockLabel;
    [SerializeField] private Image stockBarFill; // opsiyonel — 0..1 arasi doldurma gostergesi

    [Header("Renkler")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color lowStockColor = new Color(0.85f, 0.25f, 0.2f);

    public void Bind(BookData book, int currentStock, int maxStock, int lowThreshold)
    {
        bool dusukStok = currentStock <= lowThreshold;
        Color renk = dusukStok ? lowStockColor : normalColor;

        if (nameLabel != null)
        {
            nameLabel.text = book.displayName;
            nameLabel.color = renk;
        }

        if (stockLabel != null)
        {
            stockLabel.text = currentStock + " / " + maxStock;
            stockLabel.color = renk;
        }

        if (stockBarFill != null)
        {
            stockBarFill.fillAmount = maxStock > 0 ? (float)currentStock / maxStock : 0f;
            stockBarFill.color = renk;
        }
    }
}