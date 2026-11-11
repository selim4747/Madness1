// ThiefController.cs — Hirsiz yapay zekasi
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;

public class ThiefController : NavMeshMover
{
    private enum State { GoingToShelf, Fleeing, Caught, GoingToGate }

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
    private Transform gecitNoktasi;
    private bool hasStolen;

    public void Initialize(Transform door, ShelfController shelf, Transform gate = null)
    {
        doorPoint = door;
        gecitNoktasi = gate;
        targetShelf = shelf;
        state = gecitNoktasi != null ? State.GoingToGate : State.GoingToShelf;
        hasStolen = false;

        if (alertIcon != null) alertIcon.SetActive(true);
        if (stolenBookIcon != null) stolenBookIcon.enabled = false;

        if (AudioManager.Instance != null) AudioManager.Instance.PlayAlarm();

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
                DevLog.Warning($"⚠️ [NAVMESH] {gameObject.name} NavMesh uzerinde degil, 5 birim icinde gecerli nokta da bulunamadi — hareket edemiyor. doorPoint'in NavMesh ile kapli oldugundan emin ol.");
            }
        }

        MoveTo(gecitNoktasi != null ? gecitNoktasi.position : targetShelf.ApproachPosition);
    }

    private void Update()
    {
        // NavMesh hedef yolu hesaplanirken varis kontrolunu bekleme sayaci korur.
        IsinlanmaSayaciniAzalt();

        if (state == State.Caught) return;

        // Gece basladiysa hirsiz kacar
        if (GameManager.Instance.CurrentPhase != GamePhase.Day)
        {
            Escape();
            return;
        }

        if (!HasArrived) return;

        if (state == State.GoingToGate)
        {
            state = State.GoingToShelf;
            MoveTo(targetShelf != null ? targetShelf.ApproachPosition : transform.position);
        }
        else if (state == State.GoingToShelf)
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

        if (gecitNoktasi != null) MoveTo(gecitNoktasi.position);
        else if (doorPoint != null) MoveTo(doorPoint.position);
    }

    /// <summary>Kapiya ulasti — kitap gitti.</summary>
    private void Escape()
    {
        if (hasStolen)
            if (ToastNotification.Instance != null) ToastNotification.Instance.Show("Hirsiz bir kitap calip kacti!", 3f);

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

        if (EconomyManager.Instance != null) EconomyManager.Instance.AddGold(catchReward);
        if (SatisfactionTracker.Instance != null) SatisfactionTracker.Instance.RecordPositiveEvent();   // "itibar" = memnuniyet
        PlayerProgressStats.AddThiefCaught();

        if (ToastNotification.Instance != null) ToastNotification.Instance.Show("Hirsizi yakaladin! +" + catchReward + " Para", 3f);
        Destroy(gameObject);
    }
}