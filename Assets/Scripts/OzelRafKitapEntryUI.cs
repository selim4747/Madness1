// OzelRafKitapEntryUI.cs — Ozel raf panelindeki tek satir: kitap adi + adet + etiket
// (Joker / Ozel: [kisi ismi] / normal).
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using TMPro;

public class OzelRafKitapEntryUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameLabel;
    [SerializeField] private TextMeshProUGUI adetLabel;
    [SerializeField] private TextMeshProUGUI etiketLabel;

    public void Bind(BookData kitap, int adet)
    {
        if (nameLabel != null) nameLabel.text = kitap.displayName;
        if (adetLabel != null) adetLabel.text = "x" + adet;

        if (etiketLabel != null)
        {
            if (kitap is KaraKitapJokerData)
            {
                etiketLabel.text = "JOKER — herkese satılabilir";
            }
            else if (kitap is KaraKitapOzelData ozel)
            {
                etiketLabel.text = "ÖZEL: " + ozel.hedefMusteriIsmi;
            }
            else
            {
                etiketLabel.text = "";
            }
        }
    }
}