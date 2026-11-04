// BookCatalogUI.cs — 144 kitaplik kutuphane katalogu
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BookCatalogUI : MonoBehaviour
{
    public static BookCatalogUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI completionLabel;

    [Header("Liste")]
    [SerializeField] private Transform gridParent;
    [SerializeField] private GameObject entryPrefab;

    [Header("Dukkan sekmeleri")]
    [SerializeField] private Button[] shopTabs;

    private static readonly HashSet<string> discoveredIds = new HashSet<string>();
    private readonly List<GameObject> spawnedEntries = new List<GameObject>();
    private int activeShop = 1;

    /// <summary>AchievementManager bu sayiyi okur.</summary>
    public static int DiscoveredCount { get { return discoveredIds.Count; } }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (root != null) root.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);

        for (int i = 0; i < shopTabs.Length; i++)
        {
            int shopNumber = i + 1;
            shopTabs[i].onClick.AddListener(() => SelectShop(shopNumber));
        }
    }

    private void Start()
    {
        // Tum raflarin kesif olayina abone ol
        List<ShelfController> all = ShopManager.Instance.AllShelves;
        for (int i = 0; i < all.Count; i++)
            all[i].OnFirstDiscovered += HandleDiscovered;

        if (PanelYoneticisi.Instance != null && root != null) PanelYoneticisi.Instance.Kaydet(root);
    }

    private void OnDestroy()
    {
        if (ShopManager.Instance == null) return;

        List<ShelfController> all = ShopManager.Instance.AllShelves;
        for (int i = 0; i < all.Count; i++)
            if (all[i] != null) all[i].OnFirstDiscovered -= HandleDiscovered;
    }

    private void HandleDiscovered(BookData book)
    {
        if (book == null) return;
        discoveredIds.Add(book.bookId);
    }

    public void Open()
    {
        if (PanelYoneticisi.Instance != null) PanelYoneticisi.Instance.DigerleriniKapat(root);
        if (root != null) root.SetActive(true);
        SelectShop(activeShop);
    }

    public void Close()
    {
        if (root != null) root.SetActive(false);
    }

    private void SelectShop(int shopNumber)
    {
        activeShop = shopNumber;
        RefreshGrid();
    }

    private void RefreshGrid()
    {
        for (int i = 0; i < spawnedEntries.Count; i++)
            if (spawnedEntries[i] != null) Destroy(spawnedEntries[i]);
        spawnedEntries.Clear();

        List<ShelfController> all = ShopManager.Instance.AllShelves;

        // GUVENLIK: Inspector'da elle atanan baslangic kitaplari (orn. oyunun basladigi 3
        // kitap) hic KitapAta() ile calisirilmadigi icin OnFirstDiscovered hic tetiklenmiyordu,
        // bu yuzden katalogda hicbir zaman gorunmuyorlardi. Su an HERHANGI bir rafta duran
        // (fiziksel olarak dukkanda gorulebilir) her kitap burada otomatik kesfedilmis sayilir
        // — hangi sekme acik olursa olsun, panel her acilista TUM dukkanlari tarar.
        for (int i = 0; i < all.Count; i++)
            if (all[i].book != null) discoveredIds.Add(all[i].book.bookId);

        for (int i = 0; i < all.Count; i++)
        {
            ShelfController shelf = all[i];
            if (shelf.book == null) continue;
            if (shelf.book.shopNumber != activeShop) continue;

            GameObject entry = Instantiate(entryPrefab, gridParent);
            spawnedEntries.Add(entry);

            CatalogEntryUI ui = entry.GetComponent<CatalogEntryUI>();
            if (ui != null)
            {
                bool discovered = discoveredIds.Contains(shelf.book.bookId);
                ui.Bind(shelf.book, discovered, shelf.CurrentStock);
            }
        }

        if (completionLabel != null)
            completionLabel.text = discoveredIds.Count + " / 144 kesfedildi";
    }

    // ── Kayit koprusu (SaveSystem bunlari cagirir) ──

    public static void ApplySaveStatic(SaveData data)
    {
        discoveredIds.Clear();
        for (int i = 0; i < data.discoveredBookIds.Count; i++)
            discoveredIds.Add(data.discoveredBookIds[i]);
    }

    public static void WriteToSaveStatic(SaveData data)
    {
        data.discoveredBookIds.Clear();
        foreach (string id in discoveredIds) data.discoveredBookIds.Add(id);
    }
}