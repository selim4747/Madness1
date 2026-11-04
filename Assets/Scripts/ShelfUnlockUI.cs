// ShelfUnlockUI.cs — Raf acma paneli (dukkan sekmeli)
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShelfUnlockUI : MonoBehaviour
{
    public static ShelfUnlockUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI goldLabel;
    [SerializeField] private TextMeshProUGUI headerLabel;

    [Header("Liste")]
    [SerializeField] private Transform listParent;
    [SerializeField] private GameObject shelfEntryPrefab;

    [Header("Dukkan sekmeleri (6 adet)")]
    [SerializeField] private Button[] shopTabs;

    private readonly List<GameObject> spawned = new List<GameObject>();
    private int activeShop = 1;

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

        if (PanelYoneticisi.Instance != null && root != null) PanelYoneticisi.Instance.Kaydet(root);
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
        Refresh();
    }

    public void Refresh()
    {
        if (goldLabel != null) goldLabel.text = EconomyManager.Instance.Gold + " Para";

        bool shopOwned = activeShop <= EconomyManager.Instance.ShopsUnlocked;

        if (headerLabel != null)
        {
            headerLabel.text = shopOwned
                ? activeShop + ". Dukkan — Raflar"
                : activeShop + ". Dukkan KILITLI (once dukkani satin al)";
        }

        for (int i = 0; i < spawned.Count; i++)
            if (spawned[i] != null) Destroy(spawned[i]);
        spawned.Clear();

        List<ShelfController> all = ShopManager.Instance.AllShelves;

        for (int i = 0; i < all.Count; i++)
        {
            ShelfController shelf = all[i];
            if (shelf.book == null) continue;
            if (shelf.book.shopNumber != activeShop) continue;

            GameObject entry = Instantiate(shelfEntryPrefab, listParent);
            spawned.Add(entry);

            ShelfEntryUI ui = entry.GetComponent<ShelfEntryUI>();
            if (ui != null) ui.Bind(shelf, this);
        }
    }

    /// <summary>ShelfEntryUI tarafindan cagrilir.</summary>
    public void OnUnlockPressed(ShelfController shelf)
    {
        if (ShopExpansionManager.Instance != null && ShopExpansionManager.Instance.TryUnlockShelf(shelf))
            Refresh();
    }
}