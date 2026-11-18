// SpriteFrameAnimator.cs — PNG/Sprite dizisini FPS ile oynatir
// NJG Games | Delilik | Unity 6 | UTF-8
using UnityEngine;

/// <summary>
/// Animator Controller gerektirmeden SpriteRenderer sprite'ini kare kare degistirir.
/// Karakter objesine eklenip Inspector'dan PNG'ler Sprite dizisine suruklenebilir.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteFrameAnimator : MonoBehaviour
{
    [Header("Referans")]
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private Sprite[] frames;

    [Header("Oynatma")]
    [Min(0.01f)] [SerializeField] private float framesPerSecond = 8f;
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool loop = true;
    [SerializeField] private bool useUnscaledTime = false;
    [Tooltip("Aciksa PlayerController hareket etmiyorsa ilk kare sabit tutulur.")]
    [SerializeField] private bool animateOnlyWhileMoving = false;
    [Range(0f, 1f)] [SerializeField] private float movementThreshold = 0.05f;

    [Header("Baslangic")]
    [Min(0)] [SerializeField] private int startingFrame;
    [SerializeField] private bool resetToFirstFrameWhenStopped = true;

    public int CurrentFrame { get { return currentFrame; } }
    public int FrameCount { get { return frames == null ? 0 : frames.Length; } }
    public float FramesPerSecond
    {
        get { return framesPerSecond; }
        set { framesPerSecond = Mathf.Max(0.01f, value); }
    }
    public bool IsPlaying { get { return isPlaying; } }

    private int currentFrame;
    private float frameTimer;
    private bool isPlaying;

    private void Reset()
    {
        targetRenderer = GetComponent<SpriteRenderer>();
    }

    private void Awake()
    {
        if (targetRenderer == null) targetRenderer = GetComponent<SpriteRenderer>();
        currentFrame = Mathf.Max(0, startingFrame);
    }

    private void OnEnable()
    {
        if (playOnEnable) Play(true);
        else ApplyCurrentFrame();
    }

    private void Update()
    {
        if (!isPlaying || frames == null || frames.Length == 0) return;

        if (animateOnlyWhileMoving && !IsPlayerMoving())
        {
            if (resetToFirstFrameWhenStopped && currentFrame != 0)
            {
                currentFrame = 0;
                frameTimer = 0f;
                ApplyCurrentFrame();
            }
            return;
        }

        float delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        frameTimer += delta;
        float secondsPerFrame = 1f / Mathf.Max(0.01f, framesPerSecond);

        while (frameTimer >= secondsPerFrame)
        {
            frameTimer -= secondsPerFrame;
            if (!AdvanceFrame()) break;
        }
    }

    private bool IsPlayerMoving()
    {
        return PlayerController.Instance != null &&
               PlayerController.Instance.MoveInput.sqrMagnitude > movementThreshold * movementThreshold;
    }

    private bool AdvanceFrame()
    {
        if (frames == null || frames.Length == 0) return false;

        int next = currentFrame + 1;
        if (next >= frames.Length)
        {
            if (!loop)
            {
                currentFrame = frames.Length - 1;
                ApplyCurrentFrame();
                isPlaying = false;
                return false;
            }
            next = 0;
        }

        currentFrame = next;
        ApplyCurrentFrame();
        return true;
    }

    private void ApplyCurrentFrame()
    {
        if (targetRenderer == null || frames == null || frames.Length == 0) return;
        currentFrame = Mathf.Clamp(currentFrame, 0, frames.Length - 1);
        if (frames[currentFrame] != null) targetRenderer.sprite = frames[currentFrame];
    }

    public void Play(bool restart = false)
    {
        if (frames == null || frames.Length == 0)
        {
            isPlaying = false;
            return;
        }

        if (restart)
        {
            currentFrame = Mathf.Clamp(startingFrame, 0, frames.Length - 1);
            frameTimer = 0f;
            ApplyCurrentFrame();
        }
        isPlaying = true;
    }

    public void Stop()
    {
        isPlaying = false;
        frameTimer = 0f;
        if (resetToFirstFrameWhenStopped && frames != null && frames.Length > 0)
        {
            currentFrame = 0;
            ApplyCurrentFrame();
        }
    }

    public void Pause()
    {
        isPlaying = false;
    }

    public void SetFrames(Sprite[] newFrames, bool restart = true)
    {
        frames = newFrames;
        if (restart) Play(true);
        else ApplyCurrentFrame();
    }

    public void SetFrameRate(float newFramesPerSecond)
    {
        framesPerSecond = Mathf.Max(0.01f, newFramesPerSecond);
    }

    public void SetFrame(int index)
    {
        if (frames == null || frames.Length == 0) return;
        currentFrame = Mathf.Clamp(index, 0, frames.Length - 1);
        frameTimer = 0f;
        ApplyCurrentFrame();
    }
}
