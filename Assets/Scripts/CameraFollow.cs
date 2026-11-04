// CameraFollow.cs — Oyuncuyu yumusak takip eden kamera (2D)
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [Header("Hedef")]
    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private Vector2 offset = Vector2.zero;

    [Header("Sinirlar (dukkan alani)")]
    [SerializeField] private bool useBounds = true;
    [SerializeField] private Vector2 minBounds = new Vector2(-10f, -10f);
    [SerializeField] private Vector2 maxBounds = new Vector2(10f, 10f);

    private Vector3 velocity;
    private Vector3 smoothedPosition;
    private Vector2 defaultOffset;
    private bool hasStoredDefaultOffset;
    private float focusSmoothTime;
    private bool isFocused;

    private float shakeTimer;
    private float shakeDuration;
    private float shakeMagnitude;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (target == null && PlayerController.Instance != null)
            target = PlayerController.Instance.transform;

        if (target != null)
        {
            transform.position = BuildTargetPosition(target.position);
            smoothedPosition = transform.position;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            if (isFocused) return;   // odaklanilan NPC yok olduysa oyuncuya donmeyi StoryManager karar versin
            if (PlayerController.Instance == null) return;
            target = PlayerController.Instance.transform;
        }

        float smooth = isFocused ? focusSmoothTime : smoothTime;
        Vector3 desired = BuildTargetPosition(target.position);
        smoothedPosition = Vector3.SmoothDamp(smoothedPosition, desired, ref velocity, smooth);

        Vector3 finalPosition = smoothedPosition;

        // Sarsinti, "gercek" takip edilen konumu BOZMADAN sadece gorsel olarak eklenir —
        // yoksa SmoothDamp'in kendi ic hesaplamasi sarsintiyi da "hedef" sanip garip
        // sekilde geri toparlanmaya calisirdi.
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float guc = shakeMagnitude * (shakeTimer / shakeDuration);
            Vector2 sarsintiOfset = UnityEngine.Random.insideUnitCircle * guc;
            finalPosition += new Vector3(sarsintiOfset.x, sarsintiOfset.y, 0f);
        }

        transform.position = finalPosition;
    }

    /// <summary>Kisa bir ekran sarsintisi baslatir — hasar alinca/vurulunca cagrilir.
    /// Sure boyunca gitgide azalan rastgele bir titresim uygular.</summary>
    public void Shake(float duration, float magnitude)
    {
        shakeDuration = duration;
        shakeTimer = duration;
        shakeMagnitude = magnitude;
    }

    private Vector3 BuildTargetPosition(Vector3 targetPos)
    {
        float x = targetPos.x + offset.x;
        float y = targetPos.y + offset.y;

        if (useBounds && !isFocused)   // odaklanirken sinir disina cikmasi gerekebilir (orn. kapidaki NPC)
        {
            x = Mathf.Clamp(x, minBounds.x, maxBounds.x);
            y = Mathf.Clamp(y, minBounds.y, maxBounds.y);
        }

        return new Vector3(x, y, transform.position.z);
    }

    /// <summary>Dukkan genisleyince sinirlar guncellenir (ShopRoomController cagirir).</summary>
    public void SetBounds(Vector2 min, Vector2 max)
    {
        minBounds = min;
        maxBounds = max;
    }

    /// <summary>StoryManager/StoryNPCController cagirir: kamera bu hedefe odaklanir.</summary>
    public void FocusOn(Transform focusTarget, Vector2 focusOffset, float transitionTime = 0.4f)
    {
        if (!hasStoredDefaultOffset)
        {
            defaultOffset = offset;
            hasStoredDefaultOffset = true;
        }

        target = focusTarget;
        offset = focusOffset;
        focusSmoothTime = transitionTime;
        isFocused = true;
    }

    /// <summary>Odaklanma biter bitmez oyuncuya geri doner.</summary>
    public void ReturnToPlayer(float transitionTime = 0.4f)
    {
        isFocused = false;
        focusSmoothTime = transitionTime;
        offset = hasStoredDefaultOffset ? defaultOffset : Vector2.zero;

        if (PlayerController.Instance != null) target = PlayerController.Instance.transform;
    }
}