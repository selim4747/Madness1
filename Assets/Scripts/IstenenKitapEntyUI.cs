// IstenenKitapEntryUI.cs — Istenen Kitaplar panelindeki tek satir: "X. Masa: Kitap1, Kitap2".
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Text;

public class IstenenKitapEntryUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI deskLabel;
    [SerializeField] private TextMeshProUGUI booksLabel;

    public void Bind(int deskIndex, List<ShelfController> beklenenRaflar)
    {
        if (deskLabel != null) deskLabel.text = deskIndex + ". Masa";

        if (booksLabel != null)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < beklenenRaflar.Count; i++)
            {
                if (beklenenRaflar[i] == null || beklenenRaflar[i].book == null) continue;
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(beklenenRaflar[i].book.displayName);
            }
            booksLabel.text = sb.Length > 0 ? sb.ToString() : "—";
        }
    }
}