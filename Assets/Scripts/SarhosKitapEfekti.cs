// SarhosKitapEfekti.cs — SARHOŞ KİTAP (çılgın özellik): bir Kara Kitap satıldığında küçük
// bir ihtimalle tetiklenir. Süre boyunca: kamera sallanır (CameraFollow.Shake — mevcut API,
// yeni kod gerekmez), kontroller TERSİNE döner (PlayerController.SetTersKontrol), ve
// DialogueBox'ta gösterilen tüm konuşma metinleri harfleri karışmış gibi gösterilir.
//
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System.Collections;

public class SarhosKitapEfekti : MonoBehaviour
{
    public static SarhosKitapEfekti Instance { get; private set; }

    [Header("Tetiklenme")]
    [Tooltip("Bir Kara Kitap (Joker ya da Özel) satıldığında bu ihtimalle tetiklenir.")]
    [Range(0f, 1f)]
    [SerializeField] private float tetiklenmeSansi = 0.15f;

    [Header("Efekt Ayarları")]
    [SerializeField] private float efektSuresi = 10f;
    [SerializeField] private float kameraSarsintiGucu = 0.35f;

    /// <summary>DialogueBox, gosterilecek metni karistirmasi gerekip gerekmedigini bunun icin sorar.</summary>
    public bool EfektAktifMi { get; private set; }

    private Coroutine aktifEfekt;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>CounterController, bir Kara Kitap satildiginda cagirir — sansli tetikleme.</summary>
    public void SansliTetikle()
    {
        if (Random.value > tetiklenmeSansi) return;
        Tetikle();
    }

    /// <summary>Sansa bakmadan DOGRUDAN tetikler (test/debug icin de kullanilabilir).</summary>
    public void Tetikle()
    {
        if (aktifEfekt != null) StopCoroutine(aktifEfekt);
        aktifEfekt = StartCoroutine(EfektRoutine());
    }

    private IEnumerator EfektRoutine()
    {
        EfektAktifMi = true;
        DevLog.Info("🍷 [SARHOŞ KİTAP] Efekt başladı!");

        if (PlayerController.Instance != null) PlayerController.Instance.SetTersKontrol(true);
        if (CameraFollow.Instance != null) CameraFollow.Instance.Shake(efektSuresi, kameraSarsintiGucu);

        yield return new WaitForSeconds(efektSuresi);

        EfektAktifMi = false;
        if (PlayerController.Instance != null) PlayerController.Instance.SetTersKontrol(false);
        aktifEfekt = null;

        DevLog.Info("🍷 [SARHOŞ KİTAP] Efekt bitti.");
    }
}