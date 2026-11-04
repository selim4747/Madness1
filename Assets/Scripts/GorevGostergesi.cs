// GorevGostergesi.cs — Aktif Kara Kitap Satici gorevini SUREKLI ekranda gosterir
// ("5 Mitik Kitap Sat (2/5)" gibi, canli guncellenir). Normal bir ToastNotification
// gelince ANINDA gizlenir, bildirim bitince BIRKAC SANIYE bekleyip tekrar belirir —
// boylece ikisi ust uste binip okunmaz hale gelmez.
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using TMPro;
using System.Collections;

public class GorevGostergesi : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private GameObject root;
    [SerializeField] private TextMeshProUGUI label;

    [Header("Zamanlama")]
    [Tooltip("Bir bildirim bittikten kac saniye sonra gorev gostergesi tekrar belirsin.")]
    [SerializeField] private float bildirimSonrasiBekleme = 1.5f;

    private bool oncekiToastDurumu = false;
    private Coroutine bekleyipGosterRoutine;

    private void Awake()
    {
        if (root != null) root.SetActive(false);
    }

    private void OnEnable()
    {
        if (GorevYoneticisi.Instance != null) GorevYoneticisi.Instance.OnGorevDegisti += HandleGorevDegisti;
    }

    private void OnDisable()
    {
        if (GorevYoneticisi.Instance != null) GorevYoneticisi.Instance.OnGorevDegisti -= HandleGorevDegisti;
    }

    private void HandleGorevDegisti()
    {
        // Gorev bittiyse (tamamlandi/suresi doldu) hemen gizle. Yeni gorev baslarsa,
        // Update() zaten normal akisla (toast durumuna gore) gosterecek.
        if (GorevYoneticisi.Instance == null || !GorevYoneticisi.Instance.GorevVarMi)
        {
            if (root != null) root.SetActive(false);
            if (bekleyipGosterRoutine != null) { StopCoroutine(bekleyipGosterRoutine); bekleyipGosterRoutine = null; }
        }
    }

    private void Update()
    {
        if (GorevYoneticisi.Instance == null || !GorevYoneticisi.Instance.GorevVarMi) return;

        MetniGuncelle();

        bool toastGosteriliyor = ToastNotification.Instance != null && ToastNotification.Instance.BirBildirimGosteriliyor;

        if (toastGosteriliyor)
        {
            // Bildirim basladigi an ANINDA gizlenir.
            if (root != null && root.activeSelf) root.SetActive(false);
            if (bekleyipGosterRoutine != null) { StopCoroutine(bekleyipGosterRoutine); bekleyipGosterRoutine = null; }
        }
        else if (oncekiToastDurumu)
        {
            // Bildirim AZ ONCE bitti — birkac saniye bekleyip tekrar goster.
            if (bekleyipGosterRoutine == null) bekleyipGosterRoutine = StartCoroutine(BekleyipGoster());
        }
        else if (root != null && !root.activeSelf && bekleyipGosterRoutine == null)
        {
            // Ne bildirim var ne bekleme surecinde — dogrudan goster (orn. ilk acilis).
            root.SetActive(true);
        }

        oncekiToastDurumu = toastGosteriliyor;
    }

    private IEnumerator BekleyipGoster()
    {
        yield return new WaitForSecondsRealtime(bildirimSonrasiBekleme);
        if (GorevYoneticisi.Instance != null && GorevYoneticisi.Instance.GorevVarMi && root != null)
            root.SetActive(true);
        bekleyipGosterRoutine = null;
    }

    private void MetniGuncelle()
    {
        if (label == null) return;
        GorevTanimi gorev = GorevYoneticisi.Instance.AktifGorev;
        if (gorev == null) return;

        int ilerleme = GorevYoneticisi.Instance.GuncelIlerleme();
        label.text = gorev.aciklama + "  (" + ilerleme + "/" + gorev.hedefSayi + ")";
    }
}