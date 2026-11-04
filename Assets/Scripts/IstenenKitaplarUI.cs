// IstenenKitaplarUI.cs — PC uygulamasi: tum ogrenci masalarinin (3. Duzey/StudentDesk)
// aktif kitap taleplerini listeler. Oyuncu "hangi masaya ne goturmem lazim" diye
// hizlica gorebilsin diye.
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class IstenenKitaplarUI : MonoBehaviour
{
    public static IstenenKitaplarUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI bosDurumYazisi; // "Su an aktif talep yok" gibi

    [Header("Liste")]
    [SerializeField] private Transform listParent;
    [SerializeField] private GameObject entryPrefab;

    private readonly List<GameObject> spawnedEntries = new List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (root != null) root.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);

        if (PanelYoneticisi.Instance != null && root != null) PanelYoneticisi.Instance.Kaydet(root);
    }

    private void OnEnable()
    {
        AbonelikleriBagla();
    }

    private void OnDisable()
    {
        AbonelikleriKaldir();
    }

    /// <summary>Herhangi bir masanin talebi degisince (yeni talep/teslimat/zaman asimi)
    /// panel aciksa kendini otomatik gunceller.</summary>
    private void AbonelikleriBagla()
    {
        if (StudentDeskManager.Instance == null) return;
        List<StudentDeskController> tumMasalar = StudentDeskManager.Instance.AllDesks;
        for (int i = 0; i < tumMasalar.Count; i++)
            if (tumMasalar[i] != null) tumMasalar[i].OnRequestChanged += HandleRequestChanged;
    }

    private void AbonelikleriKaldir()
    {
        if (StudentDeskManager.Instance == null) return;
        List<StudentDeskController> tumMasalar = StudentDeskManager.Instance.AllDesks;
        for (int i = 0; i < tumMasalar.Count; i++)
            if (tumMasalar[i] != null) tumMasalar[i].OnRequestChanged -= HandleRequestChanged;
    }

    private void HandleRequestChanged()
    {
        if (root != null && root.activeSelf) Refresh();
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

        if (StudentDeskManager.Instance == null) return;

        List<StudentDeskController> tumMasalar = StudentDeskManager.Instance.AllDesks;
        int aktifTalepSayisi = 0;

        for (int i = 0; i < tumMasalar.Count; i++)
        {
            StudentDeskController masa = tumMasalar[i];
            if (masa == null || !masa.HasActiveRequest) continue;

            List<ShelfController> beklenen = masa.BeklenenRaflar;
            if (beklenen.Count == 0) continue; // hepsi zaten teslim edilmis, gosterecek bir sey yok

            aktifTalepSayisi++;

            if (entryPrefab == null || listParent == null) continue;

            GameObject entry = Instantiate(entryPrefab, listParent);
            spawnedEntries.Add(entry);

            IstenenKitapEntryUI ui = entry.GetComponent<IstenenKitapEntryUI>();
            if (ui != null) ui.Bind(masa.DeskIndex, beklenen);
        }

        if (bosDurumYazisi != null)
            bosDurumYazisi.gameObject.SetActive(aktifTalepSayisi == 0);
    }
}