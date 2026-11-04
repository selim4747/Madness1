// KatalogTabController.cs — Katalog panelindeki Kitaplar/Musteriler sekmeleri arasi gecis
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.UI;

public class KatalogTabController : MonoBehaviour
{
    [SerializeField] private Button kitaplarTabButonu;
    [SerializeField] private Button musterilerTabButonu;

    private void Awake()
    {
        if (kitaplarTabButonu != null) kitaplarTabButonu.onClick.AddListener(ShowBooks);
        if (musterilerTabButonu != null) musterilerTabButonu.onClick.AddListener(ShowCustomers);
    }

    private void OnEnable()
    {
        // Katalog paneli her acildiginda varsayilan olarak Kitaplar sekmesiyle baslasin.
        ShowBooks();
    }

    public void ShowBooks()
    {
        if (BookCatalogUI.Instance != null) BookCatalogUI.Instance.Open();
        if (CustomerCatalogUI.Instance != null) CustomerCatalogUI.Instance.Close();
    }

    public void ShowCustomers()
    {
        if (CustomerCatalogUI.Instance != null) CustomerCatalogUI.Instance.Open();
        if (BookCatalogUI.Instance != null) BookCatalogUI.Instance.Close();
    }
}