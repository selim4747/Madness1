using UnityEngine;

/// <summary>
/// Rafın üstünde stok durumuna göre renk değiştiren basit gösterge.
/// ShelfController ile aynı objeye ekle.
/// </summary>
public class RafDolulukGostergesi : MonoBehaviour
{
    [SerializeField] private ShelfController shelf;
    [SerializeField] private SpriteRenderer gosterge;
    [SerializeField] private Color dolu = Color.green;
    [SerializeField] private Color orta = Color.yellow;
    [SerializeField] private Color az = Color.red;
    [SerializeField] private int azEsik = 3;
    [SerializeField] private int ortaEsik = 8;

    private void Reset()
    {
        shelf = GetComponent<ShelfController>();
        gosterge = GetComponentInChildren<SpriteRenderer>();
    }

    private void Update()
    {
        if (shelf == null || gosterge == null) return;
        int stok = shelf.CurrentStock;
        if (stok <= 0) gosterge.color = az;
        else if (stok <= azEsik) gosterge.color = az;
        else if (stok <= ortaEsik) gosterge.color = orta;
        else gosterge.color = dolu;
    }
}
