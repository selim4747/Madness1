// WeaponEntryUI.cs — Market listesindeki tek silah satiri
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponEntryUI : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameLabel;
    [SerializeField] private TextMeshProUGUI statsLabel;
    [SerializeField] private Button actionButton;
    [SerializeField] private TextMeshProUGUI actionLabel;

    private WeaponData weapon;
    private MarketUI market;

    public void Bind(WeaponData data, MarketUI owner)
    {
        weapon = data;
        market = owner;

        if (iconImage != null) iconImage.sprite = weapon.weaponSprite;
        if (nameLabel != null) nameLabel.text = weapon.displayName;

        if (statsLabel != null)
        {
            statsLabel.text = "Hasar x" + weapon.damageMultiplier.ToString("0.0") +
                              "  |  Atis " + weapon.fireInterval.ToString("0.00") + "s";
        }

        RefreshButton();

        if (actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnPressed);
        }
    }

    private void RefreshButton()
    {
        bool owned = WeaponInventory.Instance.IsOwned(weapon);
        bool canBuy = WeaponInventory.Instance.CanPurchase(weapon);
        bool equipped = WeaponInventory.Instance.Equipped == weapon;

        if (actionLabel != null)
        {
            if (equipped)      actionLabel.text = "Çıkar";
            else if (owned)    actionLabel.text = "Kusan";
            else if (canBuy)   actionLabel.text = weapon.price + " Para";
            else               actionLabel.text = "Kilitli";
        }

        if (actionButton != null)
            actionButton.interactable = owned || canBuy;
    }

    private void OnPressed()
    {
        if (WeaponInventory.Instance.IsOwned(weapon))
            market.OnWeaponEquip(weapon.type);
        else
            market.OnWeaponPurchase(weapon);
    }
}
