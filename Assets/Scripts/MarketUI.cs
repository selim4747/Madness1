// MarketUI.cs — Tum satin almalarin tek arayuzu
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MarketUI : MonoBehaviour
{
    public static MarketUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI goldLabel;

    [Header("Stok")]
    [SerializeField] private Button restockButton;
    [SerializeField] private TextMeshProUGUI restockLabel;

    [Header("Silahlar")]
    [SerializeField] private Transform weaponListParent;
    [SerializeField] private GameObject weaponEntryPrefab;

    [Header("Genisletme")]
    [SerializeField] private Button buyShopButton;
    [SerializeField] private TextMeshProUGUI buyShopLabel;
    [SerializeField] private Button shelfPanelButton;       // ShelfUnlockUI'yi acar
    [SerializeField] private Button studentDeskPanelButton; // StudentDeskUnlockUI'yi acar
    [SerializeField] private TextMeshProUGUI studentDeskSummaryLabel; // "2/2 masa acik" gibi ozet

    [Header("Can (basit, sabit fiyatli)")]
    [SerializeField] private Button healButton;       // Cani 1 artir — 30 Para
    [SerializeField] private Button maxHealthButton;  // Maks cani 1 artir — 100 Para
    private const int HEAL_FIYATI = 30;
    private const int MAX_HEALTH_FIYATI = 100;

    [Header("Odullu reklam")]
    [SerializeField] private Button adRestockButton;
    [SerializeField] private Button adCustomerButton;

    private readonly List<GameObject> spawnedWeaponEntries = new List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (root != null) root.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (restockButton != null) restockButton.onClick.AddListener(OnRestockPressed);
        if (buyShopButton != null) buyShopButton.onClick.AddListener(OnBuyShopPressed);
        if (healButton != null) healButton.onClick.AddListener(OnHealPressed);
        if (maxHealthButton != null) maxHealthButton.onClick.AddListener(OnMaxHealthPressed);
        if (studentDeskPanelButton != null) studentDeskPanelButton.onClick.AddListener(OnStudentDeskPanelPressed);
        if (adRestockButton != null) adRestockButton.onClick.AddListener(OnAdRestockPressed);
        if (adCustomerButton != null) adCustomerButton.onClick.AddListener(OnAdCustomerPressed);
        if (shelfPanelButton != null) shelfPanelButton.onClick.AddListener(OnShelfPanelPressed);

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

    public void Toggle()
    {
        if (root == null) return;

        if (root.activeSelf) Close();
        else Open();
    }

    private void Refresh()
    {
        if (goldLabel != null) goldLabel.text = EconomyManager.Instance.Gold + " Para";

        if (restockLabel != null && RestockSystem.Instance != null)
            restockLabel.text = "Stok Paketi (" + RestockSystem.Instance.CopiesPerPackage +
                                " kitap) — " + RestockSystem.Instance.PackagePrice + " Para";

        RefreshShopButton();
        RefreshStudentDeskSummary();
        RefreshWeaponList();
    }

    private void RefreshShopButton()
    {
        if (ShopExpansionManager.Instance == null) return;

        int cost = ShopExpansionManager.Instance.GetNextShopCost();

        if (cost < 0)
        {
            if (buyShopLabel != null) buyShopLabel.text = "Tum dukkanlar acik";
            if (buyShopButton != null) buyShopButton.interactable = false;
            return;
        }

        int next = EconomyManager.Instance.ShopsUnlocked + 1;
        if (buyShopLabel != null) buyShopLabel.text = next + ". Dukkan — " + cost + " Para";
        if (buyShopButton != null) buyShopButton.interactable = EconomyManager.Instance.CanAfford(cost);
    }

    /// <summary>Butonun uzerindeki ozet yaziyi gunceller: "1/2 Masa Acik" gibi.</summary>
    private void RefreshStudentDeskSummary()
    {
        if (studentDeskSummaryLabel == null) return;

        int acik = StudentDeskManager.Instance.UnlockedCount;
        int toplam = StudentDeskManager.Instance.TotalCount;

        studentDeskSummaryLabel.text = "Ogrenci Masalari (" + acik + "/" + toplam + " Acik)";
    }

    private void RefreshWeaponList()
    {
        // weaponEntryPrefab/weaponListParent henuz baglanmadiysa (test surumunde eksik
        // birakilmis olabilir) bu bolumu sessizce atla — cokme yerine sadece silah listesi
        // bos gorunur.
        if (weaponEntryPrefab == null || weaponListParent == null) return;

        for (int i = 0; i < spawnedWeaponEntries.Count; i++)
            if (spawnedWeaponEntries[i] != null) Destroy(spawnedWeaponEntries[i]);
        spawnedWeaponEntries.Clear();

        List<WeaponData> weapons = WeaponInventory.Instance.AllWeapons;

        for (int i = 0; i < weapons.Count; i++)
        {
            WeaponData weapon = weapons[i];
            GameObject entry = Instantiate(weaponEntryPrefab, weaponListParent);
            spawnedWeaponEntries.Add(entry);

            WeaponEntryUI ui = entry.GetComponent<WeaponEntryUI>();
            if (ui != null) ui.Bind(weapon, this);
        }
    }

    /// <summary>WeaponEntryUI tarafindan cagrilir.</summary>
    public void OnWeaponPurchase(WeaponData weapon)
    {
        if (WeaponInventory.Instance.TryPurchase(weapon)) Refresh();
    }

    public void OnWeaponEquip(WeaponType type)
    {
        if (WeaponInventory.Instance.Equipped != null &&
            WeaponInventory.Instance.Equipped.type == type)
            WeaponInventory.Instance.Unequip();
        else
            WeaponInventory.Instance.Equip(type);

        Refresh();
    }

    private void OnRestockPressed()
    {
        if (RestockSystem.Instance != null && RestockSystem.Instance.TryPurchase()) Refresh();
    }

    private void OnBuyShopPressed()
    {
        if (ShopExpansionManager.Instance != null && ShopExpansionManager.Instance.TryUnlockNextShop()) Refresh();
    }

    private void OnHealPressed()
    {
        if (PlayerStats.Instance == null || EconomyManager.Instance == null) return;
        if (!EconomyManager.Instance.TrySpendGold(HEAL_FIYATI)) return;

        PlayerStats.Instance.Heal(1);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPurchase();
        Refresh();
    }

    private void OnMaxHealthPressed()
    {
        if (PlayerStats.Instance == null || EconomyManager.Instance == null) return;
        if (!EconomyManager.Instance.TrySpendGold(MAX_HEALTH_FIYATI)) return;

        PlayerStats.Instance.IncreaseMaxHealth(1);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPurchase();
        Refresh();
    }

    private void OnStudentDeskPanelPressed()
    {
        StudentDeskUnlockUI.Instance.Open();
    }

    private void OnAdRestockPressed()
    {
        RewardedAdManager.Instance.RequestAd(RewardType.ExtraStock);
        Refresh();
    }

    private void OnAdCustomerPressed()
    {
        RewardedAdManager.Instance.RequestAd(RewardType.CustomerLimitBoost);
        Refresh();
    }

    private void OnShelfPanelPressed()
    {
        ShelfUnlockUI.Instance.Open();
    }
}
