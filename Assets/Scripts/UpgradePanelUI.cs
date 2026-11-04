// UpgradePanelUI.cs — Karakter ve personel yukseltme paneli
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradePanelUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI goldLabel;

    [Header("Can")]
    [SerializeField] private Button healthButton;
    [SerializeField] private TextMeshProUGUI healthLabel;

    [Header("Hasar")]
    [SerializeField] private Button damageButton;
    [SerializeField] private TextMeshProUGUI damageLabel;

    [Header("Hiz")]
    [SerializeField] private Button speedButton;
    [SerializeField] private TextMeshProUGUI speedLabel;

    [Header("Personel Kapasitesi")]
    [SerializeField] private Button personnelButton;
    [SerializeField] private TextMeshProUGUI personnelLabel;

    private void Awake()
    {
        if (root != null) root.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);

        if (healthButton != null) healthButton.onClick.AddListener(() => Upgrade(StatType.Health));
        if (damageButton != null) damageButton.onClick.AddListener(() => Upgrade(StatType.Damage));
        if (speedButton != null) speedButton.onClick.AddListener(() => Upgrade(StatType.Speed));
        if (personnelButton != null) personnelButton.onClick.AddListener(UpgradePersonnel);

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

    private void Upgrade(StatType stat)
    {
        if (CharacterUpgradeManager.Instance.TryUpgrade(stat)) Refresh();
    }

    private void UpgradePersonnel()
    {
        if (PersonnelCapacityUpgrade.Instance.TryUpgrade()) Refresh();
    }

    private void Refresh()
    {
        if (goldLabel != null) goldLabel.text = EconomyManager.Instance.Gold + " Para";

        RefreshStat(StatType.Health, healthLabel, healthButton, "Can",
                    PlayerStats.Instance.MaxHealth.ToString());

        RefreshStat(StatType.Damage, damageLabel, damageButton, "Hasar",
                    PlayerStats.Instance.DamageStat.ToString("0.0"));

        RefreshStat(StatType.Speed, speedLabel, speedButton, "Hiz",
                    PlayerStats.Instance.SpeedStat.ToString("0.0"));

        RefreshPersonnel();
    }

    private void RefreshStat(StatType stat, TextMeshProUGUI label, Button button,
                             string title, string currentValue)
    {
        CharacterUpgradeManager mgr = CharacterUpgradeManager.Instance;
        int level = mgr.GetLevel(stat);

        if (mgr.IsMaxed(stat))
        {
            if (label != null) label.text = title + " (Sv." + level + ") — MAKS";
            if (button != null) button.interactable = false;
            return;
        }

        int cost = mgr.GetCost(stat);
        if (label != null)
            label.text = title + " " + currentValue + " (Sv." + level + ") — " + cost + " Para";

        if (button != null) button.interactable = EconomyManager.Instance.CanAfford(cost);
    }

    private void RefreshPersonnel()
    {
        PersonnelCapacityUpgrade mgr = PersonnelCapacityUpgrade.Instance;

        if (mgr.IsMaxed)
        {
            if (personnelLabel != null)
                personnelLabel.text = "Personel (Sv." + mgr.Level + ") — MAKS";
            if (personnelButton != null) personnelButton.interactable = false;
            return;
        }

        int cost = mgr.NextCost;
        if (personnelLabel != null)
        {
            personnelLabel.text = "Personel — es zamanli " + mgr.CurrentSimultaneousLimit +
                                  " musteri (Sv." + mgr.Level + ") — " + cost + " Para";
        }

        if (personnelButton != null) personnelButton.interactable = EconomyManager.Instance.CanAfford(cost);
    }
}