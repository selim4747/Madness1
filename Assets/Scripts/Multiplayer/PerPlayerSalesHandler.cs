using UnityEngine;

/// <summary>
/// Her oyuncunun kendi müşteri konuşması ve satışını yönetmesi için işaretçi.
/// Ortak savaş dışında herkes kendi masasında bağımsız çalışır.
/// Mevcut CustomerController / satış sistemine bağlanacak.
/// </summary>
public class PerPlayerSalesHandler : MonoBehaviour
{
    [SerializeField] private int ownerPlayerId;

    public int OwnerPlayerId
    {
        get => ownerPlayerId;
        set => ownerPlayerId = value;
    }

    /// <summary>
    /// Bu oyuncuya ait satış gerçekleştiğinde çağrılır.
    /// Altın kendi oyuncusuna veya paylaşımlı havuza gidebilir (sen karar ver).
    /// </summary>
    public void OnLocalSale(int goldAmount, string bookId)
    {
        Debug.Log("[Sales] Oyuncu " + ownerPlayerId + " satış yaptı: " + goldAmount + " altın (" + bookId + ")");

        // Örnek: sadece bu oyuncunun ekonomisine ekle
        // veya MultiplayerSession üzerinden senkronize et.
        // Şimdilik boş — mevcut EconomyManager'a bağlanacak.
    }

    /// <summary>Müşteri ile konuşma bu oyuncuya özel.</summary>
    public bool CanInteractWithCustomer()
    {
        // Yerel oyuncu kontrolü burada yapılır.
        return true;
    }
}
