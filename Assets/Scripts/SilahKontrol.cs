using UnityEngine;

public class SilahKontrol : MonoBehaviour
{
    [Header("Silah Ayarları")]
    public GameObject mermiPrefab;   // Aseprite'da çizip Prefab yaptığın mermi
    public Transform namluUcu;       // Merminin çıkacağı nokta (Silahın ucuna boş obje koyup buraya sürükle)
    public float mermiHizi = 15f;    // Atari usulü hızlı mermi
    public float atisGecikmesi = 0.2f; // Ne kadar sürede bir ateş edebilir (Taramalı hissi)

    private float sonAtisZamani;

    // MOBİL BUTON İÇİN ÖZEL FONKSİYON
    public void AtesEt()
    {
        if (WeaponInventory.Instance == null || WeaponInventory.Instance.Equipped == null)
            return;

        if (GameManager.Instance != null && GameManager.Instance.CurrentPhase != GamePhase.Night)
            return;

        // Eğer peş peşe basıyorsa bekleme süresini kontrol et
        if (Time.time > sonAtisZamani + atisGecikmesi)
        {
            sonAtisZamani = Time.time;

            // Mermiyi namlu ucunda yarat
            GameObject yeniMermi = Instantiate(mermiPrefab, namluUcu.position, namluUcu.rotation);

            // Mermiye füzeyi ateşle!
            Rigidbody2D rb = yeniMermi.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // Unity 6 kullandığın için yeni fizik komutu olan linearVelocity ile sağa doğru itiyoruz
                rb.linearVelocity = namluUcu.right * mermiHizi;
            }

            // Mermi uzay boşluğunda sonsuza kadar gidip RAM'i şişirmesin diye 3 saniye sonra yok et
            Destroy(yeniMermi, 3f);
        }
    }

    // NOT: PC klavye kısayolu (Space) kaldırıldı — Input System paketine geçince
    // eski UnityEngine.Input çağrıları InvalidOperationException fırlatıyordu.
    // Gerçek ateş etme zaten mobil "Ateş Et" butonundan (AtesEt()) çalışıyor,
    // bu Update() sadece PC test kısayolıydı, oyun mantığına dahil değildi.
}
