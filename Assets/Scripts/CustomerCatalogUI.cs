// CustomerCatalogUI.cs — Musteri katalogu: tanistigimiz musteriler + kitap alinca ortaya cikan sevdikleri turler
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CustomerCatalogUI : MonoBehaviour
{
    public static CustomerCatalogUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI completionLabel;

    [Header("Liste")]
    [SerializeField] private Transform listParent;
    [SerializeField] private GameObject entryPrefab;

    [Header("Tum musteri veri havuzlari (Normal/VIP/Zengin vs. — hepsini surukle)")]
    [SerializeField] private CustomerData[] tumHavuzlar;

    private static readonly HashSet<string> metNames = new HashSet<string>();
    // "musteriIsmi|KitapTuru" formatinda kayitli — hangi musterinin hangi turu "acildi" bilgisini tutar
    private static readonly HashSet<string> revealedPreferences = new HashSet<string>();

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

    /// <summary>CustomerController, kimlik secilir secilmez cagirir — ismini/lore'unu acar.</summary>
    public static void MarkMet(string musteriIsmi)
    {
        if (string.IsNullOrEmpty(musteriIsmi)) return;

        // HashSet.Add() GERCEKTEN yeni eklendiyse true doner — bu sekilde her musteriyi
        // sadece ILK tanistiginda basarim sayacina eklemis oluruz, tekrar tekrar degil.
        bool yeniMi = metNames.Add(musteriIsmi);
        if (yeniMi) PlayerProgressStats.AddUniqueCustomerMet();
    }

    /// <summary>CustomerController, satis tamamlaninca (elinde kitap varsa) cagirir — o turu bu musteri icin acar.</summary>
    public static void RevealPreference(string musteriIsmi, KitapTuru tur)
    {
        if (string.IsNullOrEmpty(musteriIsmi)) return;
        revealedPreferences.Add(musteriIsmi + "|" + tur);
    }

    public static bool IsMet(string musteriIsmi)
    {
        return !string.IsNullOrEmpty(musteriIsmi) && metNames.Contains(musteriIsmi);
    }

    public static bool IsPreferenceRevealed(string musteriIsmi, KitapTuru tur)
    {
        return revealedPreferences.Contains(musteriIsmi + "|" + tur);
    }

    private void Refresh()
    {
        for (int i = 0; i < spawned.Count; i++)
            if (spawned[i] != null) Destroy(spawned[i]);
        spawned.Clear();

        int metCount = 0;
        int totalCount = 0;

        if (tumHavuzlar != null)
        {
            for (int h = 0; h < tumHavuzlar.Length; h++)
            {
                if (tumHavuzlar[h] == null || tumHavuzlar[h].karakterHavuzu == null) continue;

                for (int i = 0; i < tumHavuzlar[h].karakterHavuzu.Count; i++)
                {
                    NPCKimligi kimlik = tumHavuzlar[h].karakterHavuzu[i];
                    if (kimlik == null) continue;

                    totalCount++;
                    bool met = IsMet(kimlik.musteriIsmi);
                    if (met) metCount++;

                    if (entryPrefab != null && listParent != null)
                    {
                        GameObject entry = Instantiate(entryPrefab, listParent);
                        spawned.Add(entry);

                        CustomerCatalogEntryUI ui = entry.GetComponent<CustomerCatalogEntryUI>();
                        if (ui != null) ui.Bind(kimlik, met);
                    }
                }
            }
        }

        if (completionLabel != null)
            completionLabel.text = metCount + " / " + totalCount + " musteri taniniyor";
    }

    // ── Kayit koprusu (SaveSystem bunlari dogrudan cagirir) ──

    public static void ApplySaveStatic(SaveData data)
    {
        metNames.Clear();
        for (int i = 0; i < data.metCustomerNames.Count; i++) metNames.Add(data.metCustomerNames[i]);

        revealedPreferences.Clear();
        for (int i = 0; i < data.revealedCustomerPreferences.Count; i++) revealedPreferences.Add(data.revealedCustomerPreferences[i]);
    }

    public static void WriteToSaveStatic(SaveData data)
    {
        data.metCustomerNames.Clear();
        foreach (string n in metNames) data.metCustomerNames.Add(n);

        data.revealedCustomerPreferences.Clear();
        foreach (string p in revealedPreferences) data.revealedCustomerPreferences.Add(p);
    }
}