// RestockSystem.cs — Toplu restock paketi (150 Para / 10 kopya)
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;

public class RestockSystem : MonoBehaviour
{
    public static RestockSystem Instance { get; private set; }

    [Header("Paket")]
    [SerializeField] private int packagePrice = 150;
    [SerializeField] private int copiesPerPackage = 10;

    public int PackagePrice { get { return packagePrice; } }
    public int CopiesPerPackage { get { return copiesPerPackage; } }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>Market UI'daki "Stok Al" butonundan cagrilir.</summary>
    public bool TryPurchase()
    {
        if (!EconomyManager.Instance.TrySpendGold(packagePrice))
        {
            ToastNotification.Instance.Show("Yeterli paran yok.", 2.5f);
            return false;
        }

        GrantPackage();
        AudioManager.Instance.PlayPurchase();
        return true;
    }

    /// <summary>Odullu reklam: para dusmeden bedava paket.</summary>
    public void ForceRestockPackage()
    {
        GrantPackage();
        ToastNotification.Instance.Show("Bedava stok paketi eklendi!", 3f);
    }

    private void GrantPackage()
    {
        ShopManager.Instance.DistributeRandomCopies(copiesPerPackage);
    }
}
