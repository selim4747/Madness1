// BlackBookDealerNPC.cs — Kara Kitap Satıcısı'nın FİZİKSEL varlığı. Sabit bir yerde durur
// (hareket etmez), SADECE BlackBookDealerManager.BugunGoruldu true olan günlerde görünür ve
// etkileşime girilebilir olur. Diğer günler görsel kapalı, oyuncu yanına gelse bile ETK
// butonuna kaydolmaz.
//
// Oyuncu ETK'ya basınca (MusteriSohbetRadari ile AYNI ISohbetHedefi + tetik alanı deseni)
// BlackBookDealerManager.Konus() çağrılır — TÜM görev değerlendirme + yeni görev verme mantığı
// SADECE bu anda tetiklenir, gün değişince OTOMATİK OLMAZ.
//
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System.Collections.Generic;

public class BlackBookDealerNPC : MonoBehaviour, ISohbetHedefi
{
    [Header("Kimlik")]
    [Tooltip("Diyalog kutusunda konuşan adı olarak görünür. İleride gerçek bir isme değiştirebilirsin.")]
    [SerializeField] private string npcIsmi = "Kara Kitap Satıcısı";

    [Tooltip("SESLENDIRME icin: dogru 'blabla' havuzunun secilmesi icin (varsayilan Erkek+Yetiskin).")]
    [SerializeField] private Cinsiyet npcCinsiyeti = Cinsiyet.Erkek;
    [SerializeField] private YasGrubu npcYasGrubu = YasGrubu.Yetiskin;

    [Header("Görsel")]
    [Tooltip("Sadece geldiği günlerde aktif/pasif yapılacak görsel kök. Boş bırakılırsa bu objenin kendi SpriteRenderer'ı kullanılır.")]
    [SerializeField] private SpriteRenderer gorselRenderer;

    public Transform SohbetTransformuAl() { return transform; }

    private void Start()
    {
        if (gorselRenderer == null) gorselRenderer = GetComponent<SpriteRenderer>();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayChanged += HandleDayChanged;
            GorunurluguGuncelle(BlackBookDealerManager.Instance != null && BlackBookDealerManager.Instance.BugunGoruldu);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnDayChanged -= HandleDayChanged;
    }

    private void HandleDayChanged(int gun)
    {
        bool gelir = BlackBookDealerManager.Instance != null && BlackBookDealerManager.Instance.BugunGelirMi(gun);
        GorunurluguGuncelle(gelir);

        // Gunu kacirdiysa (o gun aktif etkilesim alanindaysa bile) hemen kayittan dus —
        // gorunmez olan biriyle konusulmaz.
        if (!gelir && PlayerInteraction.Instance != null)
            PlayerInteraction.Instance.UnregisterChatTarget(this);
    }

    private void GorunurluguGuncelle(bool gorunur)
    {
        if (gorselRenderer != null) gorselRenderer.enabled = gorunur;
    }

    public void SohbetEt()
    {
        if (BlackBookDealerManager.Instance == null) return;

        List<string> metinler = BlackBookDealerManager.Instance.Konus();
        if (metinler == null || metinler.Count == 0) return;

        List<DialogueLine> satirlar = new List<DialogueLine>();
        for (int i = 0; i < metinler.Count; i++)
        {
            DialogueLine satir = new DialogueLine();
            satir.speakerName = npcIsmi;
            satir.text = metinler[i];
            // SESLENDIRME: bu satirlar kod icinde olusturuluyor, cinsiyet/yas grubu ELLE
            // atanmali — atanmazsa struct'in varsayilani (Kadin+Cocuk) kullanilir.
            satir.konusanCinsiyeti = npcCinsiyeti;
            satir.konusanYasGrubu = npcYasGrubu;
            satirlar.Add(satir);
        }

        if (DialogueBox.Instance != null) DialogueBox.Instance.Show(satirlar);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || PlayerInteraction.Instance == null) return;
        if (BlackBookDealerManager.Instance == null || !BlackBookDealerManager.Instance.BugunGoruldu) return;

        PlayerInteraction.Instance.RegisterChatTarget(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && PlayerInteraction.Instance != null)
            PlayerInteraction.Instance.UnregisterChatTarget(this);
    }

    private void OnDisable()
    {
        if (PlayerInteraction.Instance != null)
            PlayerInteraction.Instance.UnregisterChatTarget(this);
    }
}
