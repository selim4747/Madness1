// OzelRafPaneliUI.cs — Ozel rafin PANELi: kilitliyse satin alma butonu, aciksa icindeki
// TUM kitaplari (joker/essiz etiketleriyle) SADECE DIKEY kayan bir listede gosterir.
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class OzelRafPaneliUI : MonoBehaviour
{
    public static OzelRafPaneliUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button closeButton;

    [Header("Kilit Durumu")]
    [SerializeField] private GameObject kilitliGorunum;   // satin alma butonu vs. burada
    [SerializeField] private Button satinAlButonu;
    [SerializeField] private TextMeshProUGUI satinAlYazisi;

    [Header("Acik Durumu — SADECE DIKEY kayan liste")]
    [SerializeField] private GameObject acikGorunum;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform listParent;
    [SerializeField] private GameObject entryPrefab;
    [SerializeField] private TextMeshProUGUI kapasiteYazisi; // "12 / 30" gibi

    private readonly List<GameObject> spawnedEntries = new List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (root != null) root.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (satinAlButonu != null) satinAlButonu.onClick.AddListener(SatinAlBasildi);

        // Yatay kaymayi kod tarafinda da garanti kapat.
        if (scrollRect != null)
        {
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
        }

        if (PanelYoneticisi.Instance != null && root != null) PanelYoneticisi.Instance.Kaydet(root);
    }

    private void OnEnable()
    {
        if (OzelRafController.Instance != null) OzelRafController.Instance.OnStokDegisti += Refresh;
    }

    private void OnDisable()
    {
        if (OzelRafController.Instance != null) OzelRafController.Instance.OnStokDegisti -= Refresh;
    }

    public void Open()
    {
        if (PanelYoneticisi.Instance != null) PanelYoneticisi.Instance.DigerleriniKapat(root);
        if (root != null) root.SetActive(true);
        Refresh();
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
    }

    public void Close()
    {
        if (root != null) root.SetActive(false);
    }

    private void SatinAlBasildi()
    {
        if (OzelRafController.Instance != null && OzelRafController.Instance.TrySatinAl())
            Refresh();
    }

    private void Refresh()
    {
        bool acik = OzelRafController.Instance != null && OzelRafController.Instance.IsUnlocked;

        if (kilitliGorunum != null) kilitliGorunum.SetActive(!acik);
        if (acikGorunum != null) acikGorunum.SetActive(acik);

        if (!acik)
        {
            if (satinAlYazisi != null && OzelRafController.Instance != null)
                satinAlYazisi.text = "Özel Raf Satın Al — " + OzelRafController.Instance.GetAcilisMaliyeti() + " Para";
            return;
        }

        for (int i = 0; i < spawnedEntries.Count; i++)
            if (spawnedEntries[i] != null) Destroy(spawnedEntries[i]);
        spawnedEntries.Clear();

        if (entryPrefab == null || listParent == null) return;

        foreach (KeyValuePair<BookData, int> kv in OzelRafController.Instance.Stok)
        {
            if (kv.Key == null || kv.Value <= 0) continue;

            GameObject entry = Instantiate(entryPrefab, listParent);
            spawnedEntries.Add(entry);

            OzelRafKitapEntryUI ui = entry.GetComponent<OzelRafKitapEntryUI>();
            if (ui != null) ui.Bind(kv.Key, kv.Value);
        }

        if (kapasiteYazisi != null)
            kapasiteYazisi.text = OzelRafController.Instance.ToplamKitapSayisi() + " / " + OzelRafController.MaxKapasite;
    }
}