// YSorter.cs — Y konumuna gore otomatik siralama (2D top-down)
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;

/// <summary>
/// Asagida duran obje daha onde gorunur.
/// Oyuncu, musteri, canavar ve TUM raflara eklenmelidir.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class YSorter : MonoBehaviour
{
    [Tooltip("Hareketsiz objelerde (raf, masa) true yap — her karede hesaplanmaz, performans kazanci.")]
    [SerializeField] private bool isStatic = false;

    [Tooltip("Hassasiyet carpani. Yuksek deger = daha ince siralama.")]
    [SerializeField] private int precision = 100;

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        UpdateOrder();
    }

    private void LateUpdate()
    {
        if (isStatic) return;
        UpdateOrder();
    }

    private void UpdateOrder()
    {
        // Y ne kadar kucukse (asagidaysa) order o kadar buyuk = daha onde
        sr.sortingOrder = -Mathf.RoundToInt(transform.position.y * precision);
    }
}
