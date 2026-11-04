// KorumaliPanel.cs — Herhangi bir panele Add Component ile eklenir, o paneli
// PanelYoneticisi'nde "korumali" olarak kaydeder — baska bir panel acilirken
// ASLA otomatik kapanmaz, sadece kendi Close()'u (varsa) kapatabilir.
//
// KULLANIMI: Hierarchy'de korumali olmasini istedigin panelin KOK objesine
// (orn. AnaMasaPaneli) Add Component > KorumaliPanel ekle. Kod yazmana gerek yok.
//
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;

public class KorumaliPanel : MonoBehaviour
{
    private void Start()
    {
        // Start() kullanilir (Awake degil) — boylece PanelYoneticisi'nin kendi Awake'i
        // ne zaman calisirsa calissin, Instance'i garanti hazir olur (Unity tum Awake'leri
        // TUM Start'lardan once tamamlar).
        if (PanelYoneticisi.Instance != null)
            PanelYoneticisi.Instance.Kaydet(gameObject, korumali: true);
        else
            Debug.LogWarning("[KorumaliPanel] '" + gameObject.name + "' icin PanelYoneticisi bulunamadi — sahnede bir yere PanelYoneticisi eklenmis mi kontrol et.");
    }
}