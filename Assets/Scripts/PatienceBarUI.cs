// PatienceBarUI.cs — Musteri Sabir Bari
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.UI;

public class PatienceBarUI : MonoBehaviour
{
    [SerializeField] private Image fillImage;

    public void UpdateBar(float fillAmount)
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = Mathf.Clamp01(fillAmount);
        }
    }
}