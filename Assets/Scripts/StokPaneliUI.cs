// StokPaneliUI.cs — PC uygulamasi: dukkandaki tum raflarin stok durumunu listeler.
// Tester/oyuncu icin hizli genel bakis — hangi kitap tukenmek uzere gorulur.
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class StokPaneliUI : MonoBehaviour
{
    public static StokPaneliUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button closeButton;

    [Header("Liste")]
    [SerializeField] private Transform gridParent;
    [SerializeField] private GameObject entryPrefab;

    [Header("Dusuk Stok Esigi")]
    [Tooltip("Bu sayinin altindaki stoklar kirmizi/uyari rengiyle gosterilir.")]
    [SerializeField] private int dusukStokEsigi = 3;

    private readonly List<GameObject> spawnedEntries = new List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (root != null) root.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);

        if (PanelYoneticisi.Instance != null && root != null) PanelYoneticisi.Instance.Kaydet(root);
    }

    public void Open()
    {
        if (PanelYoneticisi.Instance != null) PanelYoneticisi.Instance.DigerleriniKapat(root);
        if (root != null) root.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        if (root != null) root.SetActive(false);
    }

    private void Refresh()
    {
        for (int i = 0; i < spawnedEntries.Count; i++)
            if (spawnedEntries[i] != null) Destroy(spawnedEntries[i]);
        spawnedEntries.Clear();

        if (ShopManager.Instance == null || entryPrefab == null || gridParent == null) return;

        List<ShelfController> all = ShopManager.Instance.UnlockedShelves;

        // Stogu en az olandan en cok olana dogru sirala — tester en cok dikkat gereken
        // rafi hemen en ustte gorsun.
        List<ShelfController> siraliListe = new List<ShelfController>(all);
        siraliListe.Sort((a, b) => a.CurrentStock.CompareTo(b.CurrentStock));

        for (int i = 0; i < siraliListe.Count; i++)
        {
            ShelfController shelf = siraliListe[i];
            if (shelf == null || shelf.book == null) continue;

            GameObject entry = Instantiate(entryPrefab, gridParent);
            spawnedEntries.Add(entry);

            StokEntryUI ui = entry.GetComponent<StokEntryUI>();
            if (ui != null) ui.Bind(shelf.book, shelf.CurrentStock, ShelfController.MaxStock, dusukStokEsigi);
        }
    }
}