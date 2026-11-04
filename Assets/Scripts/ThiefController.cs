// ThiefController.cs — Hirsiz yapay zekasi
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;

public class ThiefController : NavMeshMover
{
    private enum State { GoingToShelf, Fleeing, Caught }

    [Header("Gorsel")]
    [SerializeField] private GameObject alertIcon;
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer stolenBookIcon;

    [Header("Odul")]
    [SerializeField] private int catchReward = 100;

    [Header("Hiz")]
    [Tooltip("Kacarken normal (defaultSpeed) hizin kac kati hizla kosacagi. 1 = fark yok. Rafa giderken normal hizda yurur, kitabi calip kacmaya basladigi an bu carpanla hizlanir.")]
    [SerializeField] private float kacisHizCarpani = 1.6f;

    private State state;
    private ShelfController targetShelf;
    private Transform doorPoint;
    private bool hasStolen;

    // GEÇİT IŞINLAMASI: haritada NavMesh'in aradan geçmediği iki dar bant (Y ekseni).
    // NPC bu bantlardan birinin içine düşerse (yürüyerek geçemediği için), doğrudan
    // bandın diğer tarafındaki güvenli Y konumuna ışınlanır — yol hesaplamaya calisilmaz.
    // CustomerController / StoryNPCController ile AYNI bant degerleri — bahce iki ayri
    // NavMesh adasi gibi davrandigi icin hirsiz da bu bantlari gecerken takilabilirdi.
    private const float GECIT1_MIN_Y = 44.9f;
    private const float GECIT1_MAX_Y = 45.1f;
    private const float GECIT1_HEDEF_Y = 41f;

    private const float GECIT2_MIN_Y = 41.9f;
    private const float GECIT2_MAX_Y = 42.1f;
    private const float GECIT2_HEDEF_Y = 46f;

    public void Initialize(Transform door, ShelfController shelf)
    {
        doorPoint = door;
        targetShelf = shelf;
        state = State.GoingToShelf;
        hasStolen = false;

        if (alertIcon != null) alertIcon.SetActive(true);
        if (stolenBookIcon != null) stolenBookIcon.enabled = false;

        AudioManager.Instance.PlayAlarm();

        // GÜVENLİK AĞI: spawn noktasi (doorPoint) bake edilmis NavMesh'in tam ustunde
        // olmayabilir — CustomerController.MoveTo() ayni riske karsi bu kontrolu yapiyor.
        // Bu kontrol olmadan agent.isOnNavMesh false ise NavMeshMover.MoveTo() sessizce
        // hicbir sey yapmaz ve hirsiz oldugu yerde donup kalirdi.
        if (agent != null && !agent.isOnNavMesh)
        {
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            else
            {
                Debug.LogWarning($"⚠️ [NAVMESH] {gameObject.name} NavMesh uzerinde degil, 5 birim icinde gecerli nokta da bulunamadi — hareket edemiyor. doorPoint'in NavMesh ile kapli oldugundan emin ol.");
            }
        }

        MoveTo(targetShelf.ApproachPosition);
    }

    /// <summary>Haritadaki iki dar geçit bandını (Y ekseni) her frame kontrol eder. NPC bu
    /// bantların icindeyse (NavMesh aradan gecmedigi icin yurumesi imkansiz), bandin
    /// karsi tarafindaki bilinen guvenli Y konumuna dogrudan isinlanir.</summary>
    private void GecitIsinlamasiniKontrolEt()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        float mevcutY = transform.position.y;
        float hedefY;

        if (mevcutY >= GECIT1_MIN_Y && mevcutY <= GECIT1_MAX_Y)
        {
            hedefY = GECIT1_HEDEF_Y;
        }
        else if (mevcutY >= GECIT2_MIN_Y && mevcutY <= GECIT2_MAX_Y)
        {
            hedefY = GECIT2_HEDEF_Y;
        }
        else
        {
            return;
        }

        Vector3 istenenPozisyon = new Vector3(transform.position.x, hedefY, transform.position.z);
        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(istenenPozisyon, out hit, 2f, UnityEngine.AI.NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }
        else
        {
            agent.Warp(istenenPozisyon);
        }

        // KRITIK: Warp() mevcut yolu SILER — hedefi tekrar vererek yola aninda devam ettir.
        // (NavMeshMover.HedefiGeriYukle() zaten SonHedef'i tekrar verip bekleme sayacini
        // 3'e ayarliyor — CustomerController'daki gibi elle sonHedef/hedefVar tutmaya
        // gerek yok, o mekanizma zaten base class'ta hazir.)
        HedefiGeriYukle();

        Debug.Log($"⚡ [IŞINLAMA] {gameObject.name} geçit ({mevcutY:F2}) bandından Y={hedefY} konumuna ışınlandı.");
    }

    private void Update()
    {
        // Her frame ONCE: isinlanma bekleme sayacini azalt, sonra geçit kontrolu yap.
        // (CustomerController/StoryNPCController ile ayni sira — sayaci azaltmadan
        // once HasArrived kontrol edilirse, Warp sonrasi sahte-varis olusabilirdi.)
        IsinlanmaSayaciniAzalt();
        GecitIsinlamasiniKontrolEt();

        if (state == State.Caught) return;

        // Gece basladiysa hirsiz kacar
        if (GameManager.Instance.CurrentPhase != GamePhase.Day)
        {
            Escape();
            return;
        }

        if (!HasArrived) return;

        if (state == State.GoingToShelf)
        {
            StealAndFlee();
        }
        else if (state == State.Fleeing)
        {
            Escape();
        }
    }

    private void StealAndFlee()
    {
        if (targetShelf != null && targetShelf.TryTakeOne())
        {
            hasStolen = true;

            if (stolenBookIcon != null)
            {
                stolenBookIcon.enabled = true;
                stolenBookIcon.sprite = targetShelf.book.bookCover;
            }
        }

        state = State.Fleeing;

        // Yakalanmaktan kacinmak icin kacis hizi normal yurume hizindan daha yuksek.
        // defaultSpeed, NavMeshMover.Awake() icinde zaten agent.speed'e uygulanmisti;
        // burada sadece kacis anindaki carpani uyguluyoruz.
        SetSpeed(defaultSpeed * kacisHizCarpani);

        if (doorPoint != null) MoveTo(doorPoint.position);
    }

    /// <summary>Kapiya ulasti — kitap gitti.</summary>
    private void Escape()
    {
        if (hasStolen)
            ToastNotification.Instance.Show("Hirsiz bir kitap calip kacti!", 3f);

        Destroy(gameObject);
    }

    /// <summary>PlayerInteraction tarafindan cagrilir (menzil kontrolu orada yapilir).</summary>
    public void GetCaught()
    {
        if (state == State.Caught) return;
        state = State.Caught;

        Stop();

        // Caldigi kitabi geri koy
        if (hasStolen && targetShelf != null)
        {
            targetShelf.ReturnOne();
            hasStolen = false;
        }

        EconomyManager.Instance.AddGold(catchReward);
        SatisfactionTracker.Instance.RecordPositiveEvent();   // "itibar" = memnuniyet
        PlayerProgressStats.AddThiefCaught();

        ToastNotification.Instance.Show("Hirsizi yakaladin! +" + catchReward + " Para", 3f);
        Destroy(gameObject);
    }
}