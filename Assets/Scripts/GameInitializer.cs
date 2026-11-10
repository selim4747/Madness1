// GameInitializer.cs — Sahnedeki tum sistemlerin baslatma noktasi
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using Madness.Mods;
using System.Collections;

public class GameInitializer : MonoBehaviour
{
    [Header("Mobil ayarlar")]
    [SerializeField] private int targetFrameRate = 60;
    [SerializeField] private bool neverSleep = true;

    private IEnumerator Start()
    {
        // Mod servisleri mevcut sahne yöneticilerinden önce hazır edilir.
        ModRuntime.EnsureExists();
        ModWorldService.EnsureExists();
        ApplyMobileSettings();

        // Tum singleton'larin Awake'i bitsin diye bir kare bekle
        yield return null;

        // Total-conversion mod normal oyun akışını tamamen devralır.
        if (ModTotalConversion.TryStart())
            yield break;

        bool loadedExistingSave = SaveSystem.Instance.ConsumePendingIntent();

        if (loadedExistingSave && SaveSystem.Instance.Current.tutorialCompleted)
        {
            // Kayitli oyun — dogrudan gunduze basla
            GameManager.Instance.StartFirstDay();

            // Ana Menu'ye donup geri gelinmisse, o ANIN tam durumunu geri yukle
            // (konum, mesai sayaci, gun sayaci, faz, musteriler). StartFirstDay'den SONRA
            // cagrilmali — yoksa StartFirstDay bu degerleri sifirlayip ezer.
            yield return null; // sistemlerin StartFirstDay sonrasi oturmasi icin bir kare
            GameManager.Instance.OturumDurumunuGeriYukle(SaveSystem.Instance.Current);
        }
        else
        {
            // Yeni oyun (veya tutorial yarim kalmis) — Patron ile basla
            TutorialController.Instance.BeginTutorial();
        }
    }

    private void ApplyMobileSettings()
    {
        Application.targetFrameRate = targetFrameRate;

        if (neverSleep) Screen.sleepTimeout = SleepTimeout.NeverSleep;

        // NOT: Ekran yonu artik SADECE GameManager.Awake()'te ayarlaniyor (LandscapeLeft).
        // Burada ayrica Portrait ayarlamak, GameManager'dan SONRA calisip onu eziyordu —
        // oyun acilista once yatay baslayip aniden dikeye donuyordu. Tek kaynak kalsin diye
        // buradaki satir kaldirildi.
    }
}