// RafIsyaniYoneticisi.cs — KİTAP İSYANI (çılgın özellik): her gece BAŞLARKEN, aktif her raf
// için küçük bir ihtimalle raf DEVRİLİR. Gece bitmeden (RafTamirButonu ile) düzeltilirse
// tamiri KISA sürer. Düzeltilmezse gece biter, raf GÜNE KALIR — o zaman tamiri ÇOK DAHA UZUN
// sürer VE raf o gün boyunca hiç kullanılamaz (ShelfController.TryTakeOne bunu zaten engeller).
//
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;

public class RafIsyaniYoneticisi : MonoBehaviour
{
    public static RafIsyaniYoneticisi Instance { get; private set; }

    [Header("Devrilme İhtimali")]
    [Tooltip("Gece BAŞLARKEN, aktif (kilidi açık) HER raf için ayrı ayrı denenen devrilme ihtimali.")]
    [Range(0f, 1f)]
    [SerializeField] private float geceDevrilmeIhtimali = 0.15f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnPhaseChanged += HandlePhaseChanged;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
    }

    private void HandlePhaseChanged(GamePhase yeniFaz)
    {
        if (yeniFaz == GamePhase.Night) GeceBasladi();
        else if (yeniFaz == GamePhase.Day) GunBasladi();
    }

    /// <summary>Gece başlarken her aktif rafta bağımsız bir zar atılır.</summary>
    private void GeceBasladi()
    {
        if (KasaEtkilesim.Instance == null || KasaEtkilesim.Instance.tumRaflar == null) return;

        foreach (GameObject rafObj in KasaEtkilesim.Instance.tumRaflar)
        {
            if (rafObj == null || !rafObj.activeInHierarchy) continue;

            ShelfController raf = rafObj.GetComponentInChildren<ShelfController>();
            if (raf == null || raf.DevrilmisMi) continue;

            if (Random.value < geceDevrilmeIhtimali)
                raf.Devril();
        }
    }

    /// <summary>Gündüz başlarken — HÂLÂ devrik olan raflar "geceyi geçti" sayılır, tamirleri
    /// artık çok daha uzun sürer (ShelfController.TamirGerekenSure buna göre hesaplar).</summary>
    private void GunBasladi()
    {
        if (KasaEtkilesim.Instance == null || KasaEtkilesim.Instance.tumRaflar == null) return;

        foreach (GameObject rafObj in KasaEtkilesim.Instance.tumRaflar)
        {
            if (rafObj == null) continue;

            ShelfController raf = rafObj.GetComponentInChildren<ShelfController>();
            if (raf != null) raf.GeceyiGecirdiIsaretle();
        }
    }
}