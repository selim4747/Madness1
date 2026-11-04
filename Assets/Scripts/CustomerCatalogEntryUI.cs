// CustomerCatalogEntryUI.cs — Musteri katalogundaki tek sa
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using TMPro;
using System.Text;

public class CustomerCatalogEntryUI : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private TextMeshProUGUI nameLabel;
    [SerializeField] private TextMeshProUGUI loreLabel;
    [SerializeField] private TextMeshProUGUI preferencesLabel;

    public void Bind(NPCKimligi kimlik, bool met)
    {
        if (!met)
        {
            if (nameLabel != null) nameLabel.text = "???";
            if (loreLabel != null) loreLabel.text = "Henuz tanismadin.";
            if (preferencesLabel != null) preferencesLabel.text = "";
            return;
        }

        if (nameLabel != null) nameLabel.text = kimlik.musteriIsmi;

        if (loreLabel != null)
        {
            // Meslek doluysa lore'un basina eklenir
            loreLabel.text = string.IsNullOrEmpty(kimlik.meslek)
                ? kimlik.loreHikayesi
                : "(" + kimlik.meslek + ")\n" + kimlik.loreHikayesi;
        }

        if (preferencesLabel != null)
        {
            StringBuilder sb = new StringBuilder();

            if (kimlik.sevilenTurler != null)
            {
                for (int i = 0; i < kimlik.sevilenTurler.Count; i++)
                {
                    KitapTuru tur = kimlik.sevilenTurler[i];
                    bool revealed = CustomerCatalogUI.IsPreferenceRevealed(kimlik.musteriIsmi, tur);
                    sb.Append(revealed ? tur.ToString() : "???");
                    if (i < kimlik.sevilenTurler.Count - 1) sb.Append(", ");
                }
            }

            preferencesLabel.text = "Sevdigi turler: " + sb.ToString();
        }
    }
}