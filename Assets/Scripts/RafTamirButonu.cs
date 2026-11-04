// RafTamirButonu.cs — KİTAP İSYANI (çılgın özellik): oyuncu bu butonu BASILI TUTARKEN,
// yakınındaki devrik rafı onarır. FireButton.cs ile AYNI teknik (IPointerDown/UpHandler).
// Sadece devrik bir raf yakınken görünür/aktif olsun diye Update()'te kontrol eder.
//
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.EventSystems;

public class RafTamirButonu : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private GameObject buttonVisual;

    [Tooltip("Tamir ilerlerken kac saniyede bir 'Onarılıyor... %X' bildirimi gosterilsin.")]
    [SerializeField] private float bildirimAraligi = 1f;

    private bool basiliTutuluyor = false;
    private ShelfController aktifRaf;
    private float bildirimSayaci;

    private void Update()
    {
        ShelfController enYakinDevrik = PlayerInteraction.Instance != null
            ? PlayerInteraction.Instance.FindNearestDevrikRaf()
            : null;

        if (buttonVisual != null) buttonVisual.SetActive(enYakinDevrik != null);

        if (!basiliTutuluyor || enYakinDevrik == null)
        {
            aktifRaf = null;
            return;
        }

        // Basili tutulurken raf degisebilir (baska bir devrik rafa yaklasilirsa) — en yakini takip eder.
        aktifRaf = enYakinDevrik;
        aktifRaf.TamirIlerlet(Time.deltaTime);

        bildirimSayaci -= Time.deltaTime;
        if (bildirimSayaci <= 0f)
        {
            bildirimSayaci = bildirimAraligi;
            int yuzde = Mathf.RoundToInt(aktifRaf.TamirYuzdesi * 100f);
            FloatingText.Spawn(aktifRaf.transform.position + Vector3.up * 0.5f, "Onarılıyor... %" + yuzde, FloatingText.Renk.Notr);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        basiliTutuluyor = true;
        bildirimSayaci = 0f; // ilk bildirim aninda gorunsun
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        basiliTutuluyor = false;
        aktifRaf = null;
    }
}