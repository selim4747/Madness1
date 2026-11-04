// AchievementCelebrationUI.cs — Basarim acilinca ekranin ortasinda beliren, buyuk ve
// ozel bir kutlama paneli. Sade Toast bildiriminden cok daha carpici — AchievementManager
// bunu her acilista cagirir. PanelAnimator zaten paneli SetActive(true) olunca otomatik
// "buyuyerek acilma" animasyonuyla gosterir (ayni script'i kullanir, tekrar yazmaya gerek yok).
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class AchievementCelebrationUI : MonoBehaviour
{
    public static AchievementCelebrationUI Instance { get; private set; }

    [Header("Panel (kok objede PanelAnimator OLMALI)")]
    [SerializeField] private GameObject root;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI kategoriEtiketi; // "BAŞARIM AÇILDI" / "TÜM BAŞARIMLAR TAMAMLANDI"
    [SerializeField] private TextMeshProUGUI titleLabel;
    [SerializeField] private TextMeshProUGUI rewardLabel;

    [Header("Zamanlama")]
    [SerializeField] private float gosterimSuresi = 3.5f;

    [Header("Meta (tum basarimlar tamamlaninca) — farkli renk/metin")]
    [SerializeField] private Color normalRenk = Color.white;
    [SerializeField] private Color metaRenk = new Color(1f, 0.84f, 0f); // altin sarisi

    private Coroutine aktifRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (root != null) root.SetActive(false);
    }

    /// <summary>Normal bir basarim acilinca cagrilir.</summary>
    public void Goster(AchievementData data)
    {
        GosterOrtak(
            data.icon,
            "BAŞARIM AÇILDI",
            data.title,
            "+" + data.goldReward + " Para",
            normalRenk
        );
    }

    /// <summary>TUM basarimlar tamamlaninca cagrilir — daha ozel, farkli renkli.</summary>
    public void GosterMeta(int metaOdul)
    {
        GosterOrtak(
            null,
            "★ TÜM BAŞARIMLAR TAMAMLANDI ★",
            "Delilik'in Efsanesi",
            "+" + metaOdul + " Para — Tebrikler!",
            metaRenk
        );
    }

    private void GosterOrtak(Sprite icon, string kategori, string baslik, string odul, Color renk)
    {
        if (root == null) return;

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }
        if (kategoriEtiketi != null) { kategoriEtiketi.text = kategori; kategoriEtiketi.color = renk; }
        if (titleLabel != null) titleLabel.text = baslik;
        if (rewardLabel != null) { rewardLabel.text = odul; rewardLabel.color = renk; }

        // Zaten acikken yeni bir basarim gelirse, suresini sifirdan baslat (kuyruklama yerine
        // en son geleni gostermeye devam et — art arda acilislarda ekran tikanmasin).
        if (aktifRoutine != null) StopCoroutine(aktifRoutine);

        root.SetActive(true); // PanelAnimator varsa OnEnable ile otomatik "buyuyerek acilir"
        aktifRoutine = StartCoroutine(OtomatikKapat());
    }

    private IEnumerator OtomatikKapat()
    {
        yield return new WaitForSecondsRealtime(gosterimSuresi);
        if (root != null) root.SetActive(false);
        aktifRoutine = null;
    }
}