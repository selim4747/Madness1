// FesYonetici.cs — Afor'un 24. gunde calacagi fes'in durumunu yonetir.
// IKI KULLANIM SEKLI DESTEKLENIR:
//   (A) SPRITE DEGISTIRME (onerilen, senin yaptigin gibi): fesli/fessiz TAM karakter
//       sprite'larini alanlara surukle — kod dogru olani otomatik uygular.
//   (B) AYRI FES OBJESI: ayri bir fes gorseli objesi acip kapatilir (eski yontem).
// Ikisini de doldurmana gerek yok, hangisi doluysa o calisir.
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;

public class FesYonetici : MonoBehaviour
{
    public static FesYonetici Instance { get; private set; }

    public const int FES_OLAYI_GUNU = 24;

    [Header("(A) SPRITE DEGISTIRME — onerilen yol")]
    [Tooltip("Oyuncunun govde SpriteRenderer'i. Doluysa asagidaki iki sprite arasinda otomatik gecis yapilir.")]
    [SerializeField] private SpriteRenderer oyuncuSpriteRenderer;
    [Tooltip("Oyuncunun FESLI hali.")]
    [SerializeField] private Sprite oyuncuFesliSprite;
    [Tooltip("Oyuncunun FESSIZ hali.")]
    [SerializeField] private Sprite oyuncuFessizSprite;

    [Header("(B) AYRI FES OBJESI — alternatif yol")]
    [Tooltip("Sadece (A)'yi kullanmiyorsan doldur: oyuncunun basindaki ayri fes objesi.")]
    [SerializeField] private GameObject oyuncuFesGorseli;

    [Header("PC Butonu")]
    [Tooltip("'Fes Giy/Cikar' butonunun oldugu obje — 24. gune kadar gizli kalir.")]
    [SerializeField] private GameObject fesButonu;

    public bool FesOlayiOldu { get; private set; } = false;
    public bool FesAforda { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnDayChanged += HandleDayChanged;
        RefreshVisuals();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnDayChanged -= HandleDayChanged;
    }

    private void HandleDayChanged(int day)
    {
        if (!FesOlayiOldu && day >= FES_OLAYI_GUNU)
        {
            FesOlayiOldu = true;
            FesAforda = true; // Afor caldigi gun fes onda olur
        }

        RefreshVisuals();
    }

    /// <summary>PC'deki "Fes Giy/Cikar" butonuna baglanir.</summary>
    public void FesiDegistir()
    {
        if (!FesOlayiOldu) return;

        FesAforda = !FesAforda;
        RefreshVisuals();
        PlayerProgressStats.AddFesSwap();

        if (ToastNotification.Instance != null)
            ToastNotification.Instance.Show(FesAforda ? "Fes Afor'da." : "Fes sende.", 2f);
    }

    /// <summary>StoryNPCController, Afor spawn olurken bunu sorup kendi gorunumunu ayarlar.</summary>
    public bool AforFesTakiyorMu()
    {
        return FesOlayiOldu && FesAforda;
    }

    private void RefreshVisuals()
    {
        if (fesButonu != null) fesButonu.SetActive(FesOlayiOldu);

        bool oyuncudaFesVar = !FesAforda;

        // (A) Sprite degistirme yolu
        if (oyuncuSpriteRenderer != null)
        {
            Sprite hedef = oyuncudaFesVar ? oyuncuFesliSprite : oyuncuFessizSprite;
            if (hedef != null) oyuncuSpriteRenderer.sprite = hedef;
        }

        // (B) Ayri fes objesi yolu
        if (oyuncuFesGorseli != null) oyuncuFesGorseli.SetActive(oyuncudaFesVar);

        // Afor sahnedeyse onun gorunumu de ANINDA guncellensin (sonraki spawn'i beklemesin)
        StoryNPCController[] storyNPCler = FindObjectsByType<StoryNPCController>(FindObjectsSortMode.None);
        for (int i = 0; i < storyNPCler.Length; i++)
        {
            if (storyNPCler[i] != null) storyNPCler[i].FesGorunumunuGuncelle();
        }
    }

    // ── Kayit koprusu ──

    public void ApplySave(SaveData data)
    {
        FesOlayiOldu = data.fesOlayiOldu;
        FesAforda = data.fesAforda;
        RefreshVisuals();
    }

    public void WriteToSave(SaveData data)
    {
        data.fesOlayiOldu = FesOlayiOldu;
        data.fesAforda = FesAforda;
    }
}