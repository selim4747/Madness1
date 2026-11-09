using UnityEngine;
using System.Collections;
using TMPro;
using System;
using UnityEngine.UI;

public class ShelfController : MonoBehaviour
{
    [Header("Rafın İçeriği")]
    public BookData raftakiKitap;

    [Header("Arayüz (UI) Ayarları")]
    public GameObject rafBilgiPaneli;
    public TextMeshProUGUI rafBilgiYazisi;

    [Header("Öğrenci Talebi — Parlama Efekti")]
    [Tooltip("Bir öğrenci bu raftaki kitabı istediğinde aktif/pasif olacak obje (örn. sarı ışık/outline).")]
    [SerializeField] private GameObject glowVisual;

    [Header("Devrilme (Kitap İsyanı — ÇILGIN ÖZELLİK)")]
    [Tooltip("Raf devrilince rengi bu tona döner (soluk/kararmış). Bos birakilirsa gorsel degismez, sadece kullanilamaz olur.")]
    [SerializeField] private SpriteRenderer rafGorseli;
    [SerializeField] private Color devrikRenk = new Color(0.35f, 0.35f, 0.35f, 1f);
    [Tooltip("Gece ICINDE duzeltilirse gereken sure (saniye) — kisa.")]
    [SerializeField] private float kisaTamirSuresi = 4f;
    [Tooltip("Gece GECIP guNE kalirsa gereken sure (saniye) — cok daha uzun.")]
    [SerializeField] private float uzunTamirSuresi = 25f;

    private bool oyuncuYakin = false;
    private Color normalRenk;
    private bool normalRenkKaydedildi = false;

    private float tamirIlerlemesi = 0f;

    public bool DevrilmisMi { get; private set; }
    public bool GeceyiGecirdi { get; private set; }

    public float TamirGerekenSure { get { return GeceyiGecirdi ? uzunTamirSuresi : kisaTamirSuresi; } }
    public float TamirYuzdesi { get { return Mathf.Clamp01(tamirIlerlemesi / Mathf.Max(0.01f, TamirGerekenSure)); } }

    // ==================================================
    // GLOBAL SİSTEM ÖZELLİKLERİ
    // ==================================================

    public Action<BookData> OnFirstDiscovered;

    public BookData book
    {
        get { return raftakiKitap; }
        set { raftakiKitap = value; }
    }

    // Kasa'dan güncel stoğu saniye saniye çeken sistem
    public int CurrentStock
    {
        get
        {
            if (KasaEtkilesim.Instance != null && raftakiKitap != null && KasaEtkilesim.Instance.kitapStoklari.ContainsKey(raftakiKitap))
                return KasaEtkilesim.Instance.kitapStoklari[raftakiKitap];
            return 0;
        }
    }

    public bool HasStock { get { return CurrentStock > 0; } }
    public const int MaxStock = 15;
    public bool isUnlocked { get { return gameObject.activeInHierarchy; } }
    public Vector3 ApproachPosition { get { return this.transform.position; } }

    public void Unlock() { gameObject.SetActive(true); }

    /// <summary>Öğrenci bu raftaki kitabı istediğinde çağrılır (parlama efekti).</summary>
    public void SetGlow(bool isGlowing)
    {
        if (glowVisual != null) glowVisual.SetActive(isGlowing);
    }

    /// <summary>Öğrenci kitabı geri koyduğunda (teslimattan sonra ya da zaman aşımında) çağrılır — stoğu 1 artırır.</summary>
    public void ReturnOne()
    {
        if (KasaEtkilesim.Instance == null || raftakiKitap == null) return;

        if (!KasaEtkilesim.Instance.kitapStoklari.ContainsKey(raftakiKitap))
            KasaEtkilesim.Instance.kitapStoklari[raftakiKitap] = 0;

        KasaEtkilesim.Instance.kitapStoklari[raftakiKitap] += 1;
    }

    /// <summary>Raftan bir kitap alınır — stoğu 1 azaltır. Stok yoksa ya da raf DEVRILMISSE null döner.</summary>
    public BookData TryTakeOne()
    {
        if (DevrilmisMi) return null; // devrilmis raftan kitap alinamaz — gun boyunca kullanilmaz
        if (!HasStock) return null;

        if (KasaEtkilesim.Instance != null && KasaEtkilesim.Instance.kitapStoklari.ContainsKey(raftakiKitap))
            KasaEtkilesim.Instance.kitapStoklari[raftakiKitap] -= 1;

        return raftakiKitap;
    }

    public void KitapAta(BookData yeniKitap)
    {
        raftakiKitap = yeniKitap;
        OnFirstDiscovered?.Invoke(raftakiKitap);
    }

    // ── Devrilme / Tamir (Kitap İsyanı) ──

    /// <summary>RafIsyaniYoneticisi, gece basinda sansli olarak cagirir.</summary>
    public void Devril()
    {
        if (DevrilmisMi) return;

        DevrilmisMi = true;
        GeceyiGecirdi = false;
        tamirIlerlemesi = 0f;
        GorseliGuncelle();

        DevLog.Info($"📚💥 [KİTAP İSYANI] {gameObject.name} devrildi!");
    }

    /// <summary>RafIsyaniYoneticisi, gece bitip gunduz baslarken HALA devrik olan raflari
    /// isaretlemek icin cagirir — bundan sonra tamir suresi COK UZAR.</summary>
    public void GeceyiGecirdiIsaretle()
    {
        if (DevrilmisMi) GeceyiGecirdi = true;
    }

    /// <summary>RafTamirButonu, oyuncu basili tutarken her frame cagirir.</summary>
    public void TamirIlerlet(float deltaTime)
    {
        if (!DevrilmisMi) return;

        tamirIlerlemesi += deltaTime;
        if (tamirIlerlemesi >= TamirGerekenSure) TamirTamamlandi();
    }

    private void TamirTamamlandi()
    {
        DevrilmisMi = false;
        GeceyiGecirdi = false;
        tamirIlerlemesi = 0f;
        GorseliGuncelle();

        DevLog.Info($"🛠️ [KİTAP İSYANI] {gameObject.name} tamir edildi.");
    }

    private void GorseliGuncelle()
    {
        if (rafGorseli == null) return;

        if (!normalRenkKaydedildi)
        {
            normalRenk = rafGorseli.color;
            normalRenkKaydedildi = true;
        }

        rafGorseli.color = DevrilmisMi ? devrikRenk : normalRenk;
    }

    public void MenuyuAcKapat()
    {
        if (oyuncuYakin && rafBilgiPaneli != null)
        {
            rafBilgiPaneli.SetActive(!rafBilgiPaneli.activeSelf);
            if (rafBilgiPaneli.activeSelf && raftakiKitap != null)
            {
                // PATRONUN İSTEĞİ: Stok miktarı artık ekranda yeşil yeşil parlıyor!
                string devrikNotu = DevrilmisMi ? "\n<color=red><b>DEVRİLDİ — şu an kullanılamıyor!</b></color>" : "";
                rafBilgiYazisi.text = $"<color=yellow>{raftakiKitap.kitapAdi}</color>\n" +
                                      $"<color=#00ff00><b>Stok Miktarı: {CurrentStock} Adet</b></color>\n" +
                                      $"Tür: {raftakiKitap.kitapTuru}\n" +
                                      $"Nadirlik: {raftakiKitap.nadirlik}\n" +
                                      $"Fiyat: {raftakiKitap.fiyat} Altın\n\n" +
                                      $"<i>{raftakiKitap.loreBilgisi}</i>" + devrikNotu;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            oyuncuYakin = true;
            if (PlayerInteraction.Instance != null) PlayerInteraction.Instance.RegisterInfoShelf(this);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            oyuncuYakin = false;
            if (rafBilgiPaneli != null) rafBilgiPaneli.SetActive(false);

            if (PlayerInteraction.Instance != null) PlayerInteraction.Instance.UnregisterInfoShelf(this);
        }
    }

    
    private void OnEnable()
    {
        // Registry henüz yoksa bir sonraki frame'de tekrar dene (Awake sırası güvenliği)
        if (ShelfRegistry.Instance != null)
            ShelfRegistry.Instance.Register(this);
        else
            StartCoroutine(RegisterLater());
    }

    private System.Collections.IEnumerator RegisterLater()
    {
        yield return null;
        if (ShelfRegistry.Instance != null && isActiveAndEnabled)
            ShelfRegistry.Instance.Register(this);
    }

    private void OnDisable()
    {
        // Raf havuza donerken/deaktif olurken kayitli kalmasin
        if (PlayerInteraction.Instance != null) PlayerInteraction.Instance.UnregisterInfoShelf(this);
        if (ShelfRegistry.Instance != null)
            ShelfRegistry.Instance.Unregister(this);
    }

    // NOT: PC klavye kısayolu (E tuşu) kaldırıldı — Input System paketine geçince
    // eski UnityEngine.Input çağrıları InvalidOperationException fırlatıyordu.
    // Gerçek açma/kapama zaten PlayerInteraction'ın tek etkileşim zincirinden çalışıyor.
}