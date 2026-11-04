// PlayerInteraction.cs — Tek etkilesim merkezi
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System.Collections.Generic;

public class PlayerInteraction : MonoBehaviour
{
    public static PlayerInteraction Instance { get; private set; }

    [Header("Menziller")]
    [SerializeField] private float thiefCatchRange = 1.2f;
    [SerializeField] private float shelfRange = 1.2f;
    [SerializeField] private float deskRange = 1.5f;

    /// <summary>Oyuncunun tasidigi kitap (ogrenci masasi gorevi).</summary>
    public BookData CarriedBook { get; private set; }
    public ShelfController CarriedFromShelf { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (DialogueBox.Instance != null) DialogueBox.Instance.OnClosed += HandleDialogueClosed;
    }

    private void OnDestroy()
    {
        if (DialogueBox.Instance != null) DialogueBox.Instance.OnClosed -= HandleDialogueClosed;
    }

    private void HandleDialogueClosed()
    {
        activeChatTarget = null;
    }

    /// <summary>Etkilesim butonundan cagrilir. Oncelik sirasina gore TEK is yapar.</summary>
    public void Interact()
    {
        Vector3 pos = transform.position;

        // Diyalog ACIKKEN buton BASKA HICBIR SEY yapmaz, SADECE farkli/siradaki diyalogu getirir.
        // Kapatma islemi artik panelin kendi Kapat (X) butonuyla oluyor (DialogueBox.Close()).
        if (DialogueBox.Instance != null && DialogueBox.Instance.IsOpen)
        {
            if (activeChatTarget != null)
                OpenChat(activeChatTarget);       // musteri sohbeti: yeni rastgele laf
            else
                DialogueBox.Instance.ShowNext();  // hikaye diyalogu: siradaki yazili satir
            return;
        }

        if (TryCatchThief(pos)) return;                              // 1) hirsiz kaciyor
        if (TryDeliverToDesk(pos)) return;                           // 2) ogrenciye teslim
        if (TryPickUpFromShelf(pos)) return;                          // 4) raftan al
        if (TryChatWithCustomer(pos)) return;                         // 5) sohbet baslat
        TryBrowseShelfInfo();                                         // 6) raf bilgi paneli (en dusuk oncelik)
    }

    /// <summary>UI butonunun aktif/pasif gorunmesi icin.</summary>
    public bool HasAnyInteractionAvailable()
    {
        // Diyalog acikken buton her zaman "aktif" gorunur — hep yapacak bir isi var (farkli laf getir).
        if (DialogueBox.Instance != null && DialogueBox.Instance.IsOpen) return true;

        Vector3 pos = transform.position;

        if (FindNearestThief(pos) != null) return true;
        if (CarriedBook != null && IsNearDesk(pos)) return true;
        if (CarriedBook == null && FindNearestGlowingShelf(pos) != null) return true;
        if (nearbyChatTargets.Count > 0) return true;
        if (nearbyInfoShelves.Count > 0) return true;

        return false;
    }

    // ── Musteri sohbeti ──
    // MusteriSohbetRadari, tetik alanina girip cikarken burayı kaydettirir/siler.
    // Boylece butona AYRI dinleyici eklemez — tek etkilesim zinciri bozulmaz.

    private readonly HashSet<ISohbetHedefi> nearbyChatTargets = new HashSet<ISohbetHedefi>();

    /// <summary>Su an DialogueBox'ta konusan musteri (varsa). Diyalog kapanana kadar dolu kalir,
    /// buton tekrar basilinca AYNI musteriden yeni bir laf ister.</summary>
    private ISohbetHedefi activeChatTarget;

    public void RegisterChatTarget(ISohbetHedefi target)
    {
        nearbyChatTargets.Add(target);
    }

    public void UnregisterChatTarget(ISohbetHedefi target)
    {
        nearbyChatTargets.Remove(target);
    }

    private bool TryChatWithCustomer(Vector3 pos)
    {
        ISohbetHedefi nearest = null;
        float best = float.MaxValue;

        foreach (ISohbetHedefi t in nearbyChatTargets)
        {
            if (t == null) continue;
            float d = Vector2.Distance(pos, t.SohbetTransformuAl().position);
            if (d < best) { best = d; nearest = t; }
        }

        if (nearest == null) return false;

        OpenChat(nearest);
        return true;
    }

    private void OpenChat(ISohbetHedefi target)
    {
        activeChatTarget = target;
        target.SohbetEt();
    }

    // ── Raf bilgi paneli (kitap detayi goruntuleme) ──
    // ShelfController da ayni sekilde kaydolur — butona kendi dinleyicisini eklemez.

    private readonly HashSet<ShelfController> nearbyInfoShelves = new HashSet<ShelfController>();

    public void RegisterInfoShelf(ShelfController shelf)
    {
        nearbyInfoShelves.Add(shelf);
    }

    public void UnregisterInfoShelf(ShelfController shelf)
    {
        nearbyInfoShelves.Remove(shelf);
    }

    private bool TryBrowseShelfInfo()
    {
        Vector3 pos = transform.position;

        ShelfController nearest = null;
        float best = float.MaxValue;

        foreach (ShelfController s in nearbyInfoShelves)
        {
            if (s == null) continue;
            float d = Vector2.Distance(pos, s.transform.position);
            if (d < best) { best = d; nearest = s; }
        }

        if (nearest == null) return false;

        nearest.MenuyuAcKapat();
        return true;
    }

    /// <summary>KİTAP İSYANI (çılgın özellik): RafTamirButonu, oyuncunun basılı tuttuğu
    /// sürece hangi devrik rafı onardığını bulmak için bu metodu çağırır. Aynı
    /// nearbyInfoShelves havuzunu kullanır — ayrı bir kayıt sistemi gerekmez.</summary>
    public ShelfController FindNearestDevrikRaf()
    {
        Vector3 pos = transform.position;

        ShelfController nearest = null;
        float best = float.MaxValue;

        foreach (ShelfController s in nearbyInfoShelves)
        {
            if (s == null || !s.DevrilmisMi) continue;
            float d = Vector2.Distance(pos, s.transform.position);
            if (d < best) { best = d; nearest = s; }
        }
        return nearest;
    }

    // ── Hirsiz ──

    private bool TryCatchThief(Vector3 pos)
    {
        ThiefController thief = FindNearestThief(pos);
        if (thief == null) return false;

        thief.GetCaught();
        return true;
    }

    private ThiefController FindNearestThief(Vector3 pos)
    {
        ThiefController[] thieves = FindObjectsByType<ThiefController>(FindObjectsSortMode.None);

        ThiefController nearest = null;
        float best = thiefCatchRange;

        for (int i = 0; i < thieves.Length; i++)
        {
            float d = Vector2.Distance(pos, thieves[i].transform.position);
            if (d <= best) { best = d; nearest = thieves[i]; }
        }
        return nearest;
    }

    // ── Ogrenci masasi ──

    private bool TryPickUpFromShelf(Vector3 pos)
    {
        if (CarriedBook != null) return false;

        ShelfController shelf = FindNearestGlowingShelf(pos);
        if (shelf == null) return false;
        if (!shelf.TryTakeOne()) return false;

        CarriedBook = shelf.book;
        CarriedFromShelf = shelf;
        shelf.SetGlow(false);
        return true;
    }

    private bool TryDeliverToDesk(Vector3 pos)
    {
        if (CarriedBook == null) return false;

        StudentDeskController masa = StudentDeskManager.Instance.FindNearestDeskWithRequest(pos, deskRange);
        if (masa == null) return false;

        bool accepted = masa.DeliverBook(CarriedBook, CarriedFromShelf);
        if (!accepted) return false;

        CarriedBook = null;
        CarriedFromShelf = null;
        return true;
    }

    /// <summary>Menzilde, aktif talebi olan herhangi bir masa var mi?</summary>
    private bool IsNearDesk(Vector3 pos)
    {
        if (StudentDeskManager.Instance == null) return false;
        return StudentDeskManager.Instance.FindNearestDeskWithRequest(pos, deskRange) != null;
    }

    /// <summary>Herhangi bir masanin istedigi (parlayan) raflardan menzildeki en yakini.</summary>
    private ShelfController FindNearestGlowingShelf(Vector3 pos)
    {
        if (StudentDeskManager.Instance == null) return null;

        List<ShelfController> requested = StudentDeskManager.Instance.GetAllActiveRequestedShelves();

        ShelfController nearest = null;
        float best = shelfRange;

        for (int i = 0; i < requested.Count; i++)
        {
            ShelfController s = requested[i];
            if (s == null || !s.HasStock) continue;

            float d = Vector2.Distance(pos, s.transform.position);
            if (d <= best) { best = d; nearest = s; }
        }
        return nearest;
    }
}