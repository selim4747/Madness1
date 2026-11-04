// PanelYoneticisi.cs — TUM PC panellerinin (Katalog, Basarimlar, Market, Yukseltme, Vezne,
// Ana Ekran/Hub vs.) kayit oldugu MERKEZI yonetici. Hangi sistemden acilirsa acilsin
// (KasaKontrol.programlar[], PauseMenuController, ya da baska bir yerden dogrudan Open()),
// bir panel acilinca DIGER TUM kayitli paneller otomatik kapanir.
//
// SORUN NEYDI: BookCatalogUI, AchievementsPanelUI, MarketUI gibi paneller kendi Open()'larinda
// sadece "root.SetActive(true)" yapiyordu — KasaKontrol.programlar[] listesinin TAMAMEN
// DISINDA olduklari icin, KasaKontrol'un kendi ic "digerlerini kapat" mantigi bunlardan
// HABERSIZDI. Sonuc: birden fazla panel ayni anda ekranda ust uste kalabiliyordu.
//
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System.Collections.Generic;

public class PanelYoneticisi : MonoBehaviour
{
    public static PanelYoneticisi Instance { get; private set; }

    private readonly List<GameObject> tumPaneller = new List<GameObject>();

    // KORUMALI paneller: DigerleriniKapat() bunlara ASLA dokunmaz. Sadece kendi Close()
    // metodu (dogrudan cagrilirsa) kapatabilir. DialogueBox (PanelTutorial) gibi, baska
    // bir panel acilirken kaybolmasi ASLA istenmeyen paneller icin.
    private readonly HashSet<GameObject> korumaliPaneller = new HashSet<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>Her panel kendi Awake/Start'inda bunu cagirip kendi kok objesini kaydettirir.
    /// Ayni obje birden fazla kaydedilirse yok sayilir (guvenli).
    /// korumali=true verilirse, bu panel BASKA panellerin Open()'i tarafindan ASLA otomatik
    /// kapatilmaz — sadece kendi Close() 'u (elle, dogrudan) kapatabilir.</summary>
    public void Kaydet(GameObject panel, bool korumali = false)
    {
        if (panel == null) return;
        if (!tumPaneller.Contains(panel)) tumPaneller.Add(panel);
        if (korumali) korumaliPaneller.Add(panel);
    }

    /// <summary>Verilen panel HARIC, kayitli TUM diger (KORUMALI OLMAYAN) panelleri kapatir.
    /// Panelin kendisini acmak (SetActive(true)) cagiranin sorumlulugundadir — bu metot
    /// sadece "digerlerini temizler".</summary>
    public void DigerleriniKapat(GameObject acilacakPanel)
    {
        for (int i = 0; i < tumPaneller.Count; i++)
        {
            if (tumPaneller[i] != null && tumPaneller[i] != acilacakPanel &&
                !korumaliPaneller.Contains(tumPaneller[i]))
            {
                tumPaneller[i].SetActive(false);
            }
        }
    }

    /// <summary>Kayitli TUM panelleri kapatir — KORUMALI olanlar dahil BILE degil, bu
    /// sadece PC'yi TAMAMEN kapatirken (orn. mesai bitince) bilerek kullanilmali.</summary>
    public void HepsiniKapat(bool korumalilariDaKapat = false)
    {
        for (int i = 0; i < tumPaneller.Count; i++)
        {
            if (tumPaneller[i] == null) continue;
            if (!korumalilariDaKapat && korumaliPaneller.Contains(tumPaneller[i])) continue;
            tumPaneller[i].SetActive(false);
        }
    }
}