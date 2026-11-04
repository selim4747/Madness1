// NavMeshMover.cs — Tum NPC'lerin ortak NavMesh hareket temeli (2D)
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NavMeshMover : MonoBehaviour
{
    protected NavMeshAgent agent;

    [Header("Hareket")]
    [SerializeField] protected float stoppingDistance = 0.3f;
    [SerializeField] protected float defaultSpeed = 2f;

    /// <summary>Hedefe varildi mi? (yol hesabi bitti + mesafe yeterli + hiz sifira yakin)</summary>
    public bool HasArrived
    {
        get
        {
            if (agent == null || !agent.isOnNavMesh) return false;
            if (agent.pathPending) return false;

            // Isinlanma (Warp) sonrasi birkac kare yol henuz yeniden hesaplanmamis olur.
            // pathStatus'e bakmak YANLISTI (varista da engelliyordu, NPC donup kaliyordu) —
            // onun yerine isinlanmadan sonra sadece birkac kare bekleniyor.
            if (isinlanmaBeklemeKaresi > 0) return false;

            if (agent.remainingDistance > agent.stoppingDistance) return false;
            return !agent.hasPath || agent.velocity.sqrMagnitude < 0.01f;
        }
    }

    public Vector2 CurrentVelocity
    {
        get { return agent != null ? (Vector2)agent.velocity : Vector2.zero; }
    }

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = stoppingDistance;
        agent.speed = defaultSpeed;

        // 2D top-down icin ZORUNLU: agent'in 3D gibi donmesini/yatmasini engeller
        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }

    public void SetSpeed(float speed)
    {
        if (agent != null) agent.speed = speed;
    }

    /// <summary>Son verilen hedef — isinlanma (Warp) yolu sildigi icin, isinlanmadan
    /// sonra bu hedefi yeniden vererek NPC'nin yola aninda devam etmesi saglanir.</summary>
    protected Vector3 SonHedef { get; private set; }
    protected bool HedefVar { get; private set; }

    private int isinlanmaBeklemeKaresi = 0;

    /// <summary>Her karede sayaci azaltir — StoryNPCController.Update() basinda cagrilir.</summary>
    protected void IsinlanmaSayaciniAzalt()
    {
        if (isinlanmaBeklemeKaresi > 0) isinlanmaBeklemeKaresi--;
    }

    public void MoveTo(Vector3 worldPosition)
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(worldPosition);
            SonHedef = worldPosition;
            HedefVar = true;
        }
    }

    /// <summary>Isinlanma sonrasi cagrilir — son hedefi tekrar vererek yolu geri yukler.</summary>
    protected void HedefiGeriYukle()
    {
        if (HedefVar && agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(SonHedef);
        }

        isinlanmaBeklemeKaresi = 3;
    }

    public void Stop()
    {
        if (agent != null && agent.isOnNavMesh)
            agent.ResetPath();
    }

    public float DistanceTo(Vector3 worldPosition)
    {
        return Vector3.Distance(transform.position, worldPosition);
    }
}