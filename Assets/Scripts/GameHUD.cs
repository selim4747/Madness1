using UnityEngine;
using TMPro;

public class GameHUD : MonoBehaviour
{
    [Header("Köşe Arayüz Elemanları")]
    public TextMeshProUGUI paraYazisi; // Ekranın köşesindeki altın yazısı
    public GameObject marketPaneli;    // Açılıp kapanacak market paneli (Shop)

    [Header("Can Göstergesi")]
    public TextMeshProUGUI canYazisi;  // "8 / 10" gibi gösterir

    private void Start()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnHealthChanged += HandleHealthChanged;
            HandleHealthChanged(PlayerStats.Instance.CurrentHealth, PlayerStats.Instance.MaxHealth);
        }
    }

    private void OnDestroy()
    {
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnHealthChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(int current, int max)
    {
        if (canYazisi != null) canYazisi.text = current + " / " + max + " Can";
    }

    private float gosterilenAltin = -1f; // -1: henuz baslatilmadi, ilk karede aninda esitlenir

    void Update()
    {
        // Kasadaki parayı köşede gösterir — aninda atlamak yerine SAYARAK artar/azalir.
        if (KasaEtkilesim.Instance != null && paraYazisi != null && EconomyManager.Instance != null)
        {
            float hedef = EconomyManager.Instance.Gold;

            if (gosterilenAltin < 0f) gosterilenAltin = hedef; // ilk acilista aninda esitle, sayma

            gosterilenAltin = Mathf.Lerp(gosterilenAltin, hedef, Time.deltaTime * 6f);
            if (Mathf.Abs(gosterilenAltin - hedef) < 0.5f) gosterilenAltin = hedef; // sonsuza kadar yaklasip durmasin

            // F0 sayesinde küsuratlı paraları göstermez, tam sayı yazar (örn: Altın: 150)
            paraYazisi.text = $"Altın: {gosterilenAltin:F0} 🪙";
        }
    }

    // Köşedeki market butonuna bu metodu bağlayacaksın!
    public void MarketPaneliniAcKapat()
    {
        if (marketPaneli != null)
        {
            marketPaneli.SetActive(!marketPaneli.activeSelf);
        }
    }
}