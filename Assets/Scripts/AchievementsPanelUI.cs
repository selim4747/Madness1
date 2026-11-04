// AchievementsPanelUI.cs — Basarim listesi paneli
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class AchievementsPanelUI : MonoBehaviour
{
    public static AchievementsPanelUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI summaryLabel;

    [Header("Liste")]
    [SerializeField] private Transform listParent;
    [SerializeField] private GameObject entryPrefab;

    [Header("Kategori sekmeleri (5 adet + Tumu)")]
    [SerializeField] private Button allTabButton;
    [SerializeField] private Button[] categoryTabs;   // sirasi AchievementCategory ile ayni

    private readonly List<GameObject> spawned = new List<GameObject>();
    private bool showAll = true;
    private AchievementCategory activeCategory = AchievementCategory.Economy;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (root != null) root.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);

        if (allTabButton != null) allTabButton.onClick.AddListener(ShowAll);

        for (int i = 0; i < categoryTabs.Length; i++)
        {
            AchievementCategory cat = (AchievementCategory)i;
            categoryTabs[i].onClick.AddListener(() => ShowCategory(cat));
        }

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

    private void ShowAll()
    {
        showAll = true;
        Refresh();
    }

    private void ShowCategory(AchievementCategory category)
    {
        showAll = false;
        activeCategory = category;
        Refresh();
    }

    private void Refresh()
    {
        for (int i = 0; i < spawned.Count; i++)
            if (spawned[i] != null) Destroy(spawned[i]);
        spawned.Clear();

        List<AchievementData> all = AchievementManager.Instance.AllAchievements;
        int unlockedCount = 0;

        for (int i = 0; i < all.Count; i++)
        {
            AchievementData a = all[i];
            if (a == null) continue;

            if (AchievementManager.Instance.IsUnlocked(a)) unlockedCount++;

            if (!showAll && a.category != activeCategory) continue;

            GameObject entry = Instantiate(entryPrefab, listParent);
            spawned.Add(entry);

            AchievementEntryUI ui = entry.GetComponent<AchievementEntryUI>();
            if (ui != null) ui.Bind(a);
        }

        if (summaryLabel != null)
            summaryLabel.text = unlockedCount + " / " + all.Count + " basarim acildi";
    }
}