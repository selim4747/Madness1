// WeaponUpgradePanelUI.cs — Silah satin alma, TIPKI CharacterUpgradeManager gibi SABIT
// butonlarla calisir. Prefab/dinamik liste YOK — her silah tipi (Tabanca/Tufek/Pompali)
// icin Inspector'da elle yerlestirilmis TEK bir buton+yazi var, kod sadece onlari gunceller.
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponUpgradePanelUI : MonoBehaviour
{
    [Header("Tabanca (Pistol)")]
    [SerializeField] private Button pistolButton;
    [SerializeField] private TextMeshProUGUI pistolLabel;
    [SerializeField] private TextMeshProUGUI pistolPriceLabel; // Butonun ICINDEKI fiyat yazisi
    [SerializeField] private Button pistolUseButton;

    [Header("Tufek (Rifle)")]
    [SerializeField] private Button rifleButton;
    [SerializeField] private TextMeshProUGUI rifleLabel;
    [SerializeField] private TextMeshProUGUI riflePriceLabel;
    [SerializeField] private Button rifleUseButton;

    [Header("Pompali (Shotgun)")]
    [SerializeField] private Button shotgunButton;
    [SerializeField] private TextMeshProUGUI shotgunLabel;
    [SerializeField] private TextMeshProUGUI shotgunPriceLabel;
    [SerializeField] private Button shotgunUseButton;

    private void Awake()
    {
        if (pistolButton != null) pistolButton.onClick.AddListener(() => TrySatinAl(WeaponType.Pistol));
        if (rifleButton != null) rifleButton.onClick.AddListener(() => TrySatinAl(WeaponType.Rifle));
        if (shotgunButton != null) shotgunButton.onClick.AddListener(() => TrySatinAl(WeaponType.Shotgun));

        if (pistolUseButton != null) pistolUseButton.onClick.AddListener(() => Kullan(WeaponType.Pistol));
        if (rifleUseButton != null) rifleUseButton.onClick.AddListener(() => Kullan(WeaponType.Rifle));
        if (shotgunUseButton != null) shotgunUseButton.onClick.AddListener(() => Kullan(WeaponType.Shotgun));
    }

    private void Kullan(WeaponType type)
    {
        if (WeaponInventory.Instance.Equipped != null &&
            WeaponInventory.Instance.Equipped.type == type)
            WeaponInventory.Instance.Unequip();
        else
            WeaponInventory.Instance.Equip(type);

        Refresh();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void TrySatinAl(WeaponType type)
    {
        WeaponData sonraki = SonrakiKademeyiBul(type);
        if (sonraki == null) return; // zaten Mk3, alinacak bir sey yok

        if (WeaponInventory.Instance.TryPurchase(sonraki)) Refresh();
    }

    public void Refresh()
    {
        if (WeaponInventory.Instance == null) return;

        RefreshSatir(WeaponType.Pistol, "Tabanca", pistolButton, pistolLabel, pistolPriceLabel, pistolUseButton);
        RefreshSatir(WeaponType.Rifle, "Tüfek", rifleButton, rifleLabel, riflePriceLabel, rifleUseButton);
        RefreshSatir(WeaponType.Shotgun, "Pompalı", shotgunButton, shotgunLabel, shotgunPriceLabel, shotgunUseButton);
    }

    private void RefreshSatir(WeaponType type, string adi, Button button, TextMeshProUGUI label,
                               TextMeshProUGUI priceLabel, Button useButton)
    {
        WeaponData mevcut = WeaponInventory.Instance.GetOwned(type);
        WeaponData sonraki = SonrakiKademeyiBul(type);
        bool sahip = mevcut != null;
        bool kusanik = WeaponInventory.Instance.Equipped != null && WeaponInventory.Instance.Equipped.type == type;

        // Ayni buton kusanili silah icin artik "Cikar" olarak kullanilir.
        if (useButton != null)
        {
            useButton.gameObject.SetActive(sahip);
            SetUseButtonText(useButton, kusanik ? "Çıkar" : "Kuşan");
        }

        if (sonraki == null)
        {
            // Mk3'e ulasilmis, alinacak bir sey kalmadi.
            if (label != null) label.text = adi + " (Mk3)" + (kusanik ? " · Kuşanılı" : "");
            if (priceLabel != null) priceLabel.text = "MAKS";
            if (button != null) button.interactable = false;
            return;
        }

        string kademeMetni = !sahip ? "Mk" + (int)sonraki.tier : "Mk" + (int)mevcut.tier + " → Mk" + (int)sonraki.tier;

        // Silah adi/kademe yazisi -- FIYAT ARTIK BURADA YOK, ayri buton yazisina tasindi.
        if (label != null)
            label.text = adi + " (" + kademeMetni + ")" + (kusanik ? " · Kuşanılı" : "");

        // Fiyat, SADECE butonun icindeki yaziya yazilir.
        if (priceLabel != null) priceLabel.text = sonraki.price + " Para";

        if (button != null)
            button.interactable = EconomyManager.Instance.CanAfford(sonraki.price);
    }

    /// <summary>Bu tipin bir sonraki (satin alinabilir) kademesini AllWeapons listesinden bulur.
    /// Hicbir sey sahip degilse Mk1'i, Mk1 sahipse Mk2'yi... dondurur. Mk3 sahipse null doner.</summary>
    private void SetUseButtonText(Button button, string text)
    {
        if (button == null) return;

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null) label.text = text;
    }

    private WeaponData SonrakiKademeyiBul(WeaponType type)
    {
        WeaponData mevcut = WeaponInventory.Instance.GetOwned(type);
        WeaponTier hedefKademe = mevcut == null ? WeaponTier.Mk1 : (WeaponTier)((int)mevcut.tier + 1);

        if ((int)hedefKademe > (int)WeaponTier.Mk3) return null;

        foreach (WeaponData w in WeaponInventory.Instance.AllWeapons)
        {
            if (w != null && w.type == type && w.tier == hedefKademe) return w;
        }
        return null; // asset olusturulmamis olabilir
    }
}
