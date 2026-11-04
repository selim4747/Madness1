// PlayerController.cs — Oyuncu hareketi (2D, sanal joystick)
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Gorsel")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private Animator animator;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool movementLocked = false;

    // SARHOŞ KİTAP (çılgın özellik): aktifken joystick girdisi TERS uygulanır.
    private bool tersKontrolAktif = false;

    public Vector2 MoveInput { get { return moveInput; } }

    /// <summary>Bir panel (orn. kasa/PC ekrani) acikken hareketi kilitler/acar.</summary>
    public void SetMovementLocked(bool locked)
    {
        movementLocked = locked;
        if (locked && rb != null) rb.linearVelocity = Vector2.zero;
    }

    /// <summary>Oyuncuyu belirtilen noktaya aninda isinlar (gun basi/gun yeniden baslama icin).
    /// Rigidbody2D fizigiyle ugrasmadan hem transform hem rb pozisyonunu birlikte gunceller.</summary>
    public void Teleport(Vector3 position)
    {
        transform.position = position;
        if (rb != null) rb.position = position;
        moveInput = Vector2.zero;
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    /// <summary>SarhosKitapEfekti tarafindan cagrilir — acikken tum hareket girdisi TERSINE doner.</summary>
    public void SetTersKontrol(bool aktif)
    {
        tersKontrolAktif = aktif;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;              // top-down: yercekimi yok
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    /// <summary>VirtualJoystick tarafindan cagrilir.</summary>
    public void SetMoveInput(Vector2 input)
    {
        if (tersKontrolAktif) input = -input;
        moveInput = Vector2.ClampMagnitude(input, 1f);
    }

    private void FixedUpdate()
    {
        if (PlayerStats.Instance == null) return;   // yanlis/eksik sahnede test edilirse guvenli dursun

        if (PlayerStats.Instance.IsDead || movementLocked)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Unity 6: linearVelocity (eski "velocity" DEGIL)
        rb.linearVelocity = moveInput * PlayerStats.Instance.SpeedStat;
    }

    private void Update()
    {
        if (bodyRenderer != null && Mathf.Abs(moveInput.x) > 0.05f)
            bodyRenderer.flipX = moveInput.x < 0f;

        if (animator != null && animator.runtimeAnimatorController != null)
            animator.SetFloat("Speed", moveInput.sqrMagnitude);
    }
}