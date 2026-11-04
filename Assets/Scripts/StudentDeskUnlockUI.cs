// StudentDeskUnlockUI.cs — Ogrenci masasi acma paneli (liste)
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class StudentDeskUnlockUI : MonoBehaviour
{
    public static StudentDeskUnlockUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI goldLabel;

    [Header("Liste")]
    [SerializeField] private Transform listParent;
    [SerializeField] private GameObject deskEntryPrefab;

    private readonly List<GameObject> spawned = new List<GameObject>();

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

    public void Refresh()
    {
        if (goldLabel != null) goldLabel.text = EconomyManager.Instance.Gold + " Para";

        for (int i = 0; i < spawned.Count; i++)
            if (spawned[i] != null) Destroy(spawned[i]);
        spawned.Clear();

        List<StudentDeskController> all = StudentDeskManager.Instance.AllDesks;

        for (int i = 0; i < all.Count; i++)
        {
            if (all[i] == null) continue;

            GameObject entry = Instantiate(deskEntryPrefab, listParent);
            spawned.Add(entry);

            StudentDeskEntryUI ui = entry.GetComponent<StudentDeskEntryUI>();
            if (ui != null) ui.Bind(all[i], this);
        }
    }

    /// <summary>StudentDeskEntryUI tarafindan cagrilir.</summary>
    public void OnUnlockPressed(StudentDeskController masa)
    {
        if (masa.TryUnlock()) Refresh();
    }
}