using UnityEngine;

/// <summary>
/// Bazı günlerde spawn oranını geçici değiştirir. CustomerSpawner varsa çarpan uygular.
/// </summary>
public class GunlukOlayYoneticisi : MonoBehaviour
{
    public static GunlukOlayYoneticisi Instance { get; private set; }

    [Header("Olasılık")]
    [Range(0f, 1f)] public float olaySansi = 0.25f;

    [Header("Etkiler")]
    public float musteriCarpani = 1.3f;
    public float vipCarpani = 1.5f;

    public bool BugunOlayVar { get; private set; }
    public string BugunOlayAdi { get; private set; } = "";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void YeniGun(int gun)
    {
        BugunOlayVar = Random.value < olaySansi;
        BugunOlayAdi = BugunOlayVar ? (Random.value < 0.5f ? "Kalabalık Gün" : "VIP Günü") : "";
        if (BugunOlayVar && ToastNotification.Instance != null)
            ToastNotification.Instance.Show("Bugün: " + BugunOlayAdi, 3f);
    }

    public float MusteriCarpaniAl()
    {
        return BugunOlayVar ? musteriCarpani : 1f;
    }
}
