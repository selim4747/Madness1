using UnityEngine;

/// <summary>
/// Gündüz bitince kısa özet gösterir. GameManager.OnPhaseChanged dinler.
/// </summary>
public class GeceOncesiOzet : MonoBehaviour
{
    [SerializeField] private bool sadeceGunduzdenGeceye = true;

    private void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnPhaseChanged += FazDegisti;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnPhaseChanged -= FazDegisti;
    }

    private void FazDegisti(GamePhase yeni)
    {
        // Event sadece yeni fazı veriyor. Gündüz->Gece kontrolü için sadece Night'a bakıyoruz.
        if (sadeceGunduzdenGeceye && yeni != GamePhase.Night) return;
        string metin = "Gün bitti.";
        if (GunlukHedefYoneticisi.Instance != null)
        {
            var g = GunlukHedefYoneticisi.Instance;
            metin = $"Bugün: {g.satilanKitap} kitap, {g.sohbetSayisi} sohbet, {g.yakalananHirsiz} hırsız";
        }
        if (ToastNotification.Instance != null)
            ToastNotification.Instance.Show(metin, 4f);
        else
            DevLog.Info("[ÖZET] " + metin);
    }
}
