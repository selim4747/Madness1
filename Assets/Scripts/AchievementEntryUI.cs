// AchievementEntryUI.cs — Basarim listesindeki tek satir
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AchievementEntryUI : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI titleLabel;
    [SerializeField] private TextMeshProUGUI descriptionLabel;
    [SerializeField] private TextMeshProUGUI progressLabel;
    [SerializeField] private Slider progressBar;
    [SerializeField] private GameObject unlockedBadge;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Gorunum")]
    [SerializeField] private float lockedAlpha = 0.55f;

    public void Bind(AchievementData data)
    {
        bool unlocked = AchievementManager.Instance.IsUnlocked(data);
        int progress = AchievementManager.Instance.GetProgress(data);

        // GIZLI BASARIM: acilana kadar baslik/aciklama/ikon yerine "???" gosterilir,
        // ilerleme sayisi da gizlenir (surpriz bozulmasin).
        bool gizliVeKilitli = data.gizli && !unlocked;

        if (iconImage != null) iconImage.sprite = gizliVeKilitli ? null : data.icon;
        if (titleLabel != null) titleLabel.text = gizliVeKilitli ? "??? (Gizli Başarım)" : data.title;
        if (descriptionLabel != null)
            descriptionLabel.text = gizliVeKilitli ? "Bunu nasıl açacağın bir sır..." : data.description;

        int clamped = Mathf.Min(progress, data.targetValue);

        if (progressLabel != null)
        {
            if (unlocked)
                progressLabel.text = "Tamamlandi  ·  +" + data.goldReward + " Para";
            else if (gizliVeKilitli)
                progressLabel.text = "???"; // ilerleme sayisi da surprizin parcasi
            else
                progressLabel.text = clamped + " / " + data.targetValue;
        }

        if (progressBar != null)
        {
            progressBar.minValue = 0f;
            progressBar.maxValue = data.targetValue;
            progressBar.value = gizliVeKilitli ? 0f : clamped; // gizliyken ilerleme cubugu da bos
        }

        if (unlockedBadge != null) unlockedBadge.SetActive(unlocked);
        if (canvasGroup != null) canvasGroup.alpha = unlocked ? 1f : lockedAlpha;
    }
}