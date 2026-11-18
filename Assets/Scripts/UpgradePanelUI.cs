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

    [Header("Oyuncu Spot Isigi")]
    [Tooltip("Bos birakilabilir. Atanirsa PlayerSpotlightController genislik upgrade'i burada gorunur.")]
    [SerializeField] private Button spotlightWidthButton;
    [SerializeField] private TextMeshProUGUI spotlightWidthLabel;

    private void Awake()
    {
        if (root != null) root.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);

        if (healthButton != null) healthButton.onClick.AddListener(() => Upgrade(StatType.Health));
        if (damageButton != null) damageButton.onClick.AddListener(() => Upgrade(StatType.Damage));
        if (speedButton != null) speedButton.onClick.AddListener(() => Upgrade(StatType.Speed));
        if (personnelButton != null) personnelButton.onClick.AddListener(UpgradePersonnel);
        if (spotlightWidthButton != null) spotlightWidthButton.onClick.AddListener(UpgradeSpotlightWidth);

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

    private void UpgradeSpotlightWidth()
    {
        if (PlayerSpotlightController.Instance != null &&
            PlayerSpotlightController.Instance.TryUpgradeWidth()) Refresh();
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
        RefreshSpotlightWidth();
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

    private void RefreshSpotlightWidth()
    {
        PlayerSpotlightController mgr = PlayerSpotlightController.Instance;
        if (mgr == null)
        {
            if (spotlightWidthButton != null) spotlightWidthButton.interactable = false;
            if (spotlightWidthLabel != null) spotlightWidthLabel.text = "Spot isigi — kullanilamaz";
            return;
        }

        int level = mgr.WidthUpgradeLevel;
        if (mgr.IsMaxWidth)
        {
            if (spotlightWidthLabel != null)
                spotlightWidthLabel.text = "Spot isigi genisligi (Sv." + level + ") — MAKS";
            if (spotlightWidthButton != null) spotlightWidthButton.interactable = false;
            return;
        }

        int cost = mgr.GetWidthUpgradeCost();
        if (spotlightWidthLabel != null)
            spotlightWidthLabel.text = "Spot isigi genisligi (Sv." + level + ") — " + cost + " Para";
        if (spotlightWidthButton != null)
            spotlightWidthButton.interactable = EconomyManager.Instance != null && EconomyManager.Instance.CanAfford(cost);
    }
}
