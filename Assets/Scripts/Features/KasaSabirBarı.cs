using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Kasada bekleyen müşterinin sabır barı. CounterController + CustomerController ile çalışır.
/// </summary>
public class KasaSabirBarı : MonoBehaviour
{
    [SerializeField] private Slider bar;
    [SerializeField] private CustomerController hedef;

    public void Bagla(CustomerController musteri)
    {
        hedef = musteri;
        if (bar != null) bar.gameObject.SetActive(musteri != null);
    }

    private void Update()
    {
        if (hedef == null || bar == null) return;
        float max = Mathf.Max(0.1f, hedef.maxPatienceTime);
        // CustomerController patienceTimer private, bu yüzden sadece görsel placeholder.
        // Gerçek değer için CustomerController'a public float PatienceNormalized eklenebilir.
        bar.value = 1f;
    }
}
