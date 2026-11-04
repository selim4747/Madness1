// ToastNotification.cs — Kisa sureli ekran bildirimi
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ToastNotification : MonoBehaviour
{
    public static ToastNotification Instance { get; private set; }

    [Header("Referanslar")]
    [SerializeField] private GameObject root;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Animasyon")]
    [SerializeField] private float fadeTime = 0.25f;

    private readonly Queue<string> queue = new Queue<string>();
    private readonly Queue<float> durations = new Queue<float>();
    private Coroutine routine;

    /// <summary>Su an ekranda bir bildirim GORUNUYOR mu — GorevGostergesi gibi disaridan
    /// baska UI'larin "simdi gizlenmeliyim" karari verebilmesi icin.</summary>
    public bool BirBildirimGosteriliyor { get { return root != null && root.activeSelf; } }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (root != null) root.SetActive(false);
    }

    /// <summary>Bildirim gosterir. Art arda gelenler kuyruga alinir.</summary>
    public void Show(string message, float duration)
    {
        queue.Enqueue(message);
        durations.Enqueue(duration);

        if (routine == null) routine = StartCoroutine(ProcessQueue());
    }

    private IEnumerator ProcessQueue()
    {
        while (queue.Count > 0)
        {
            string message = queue.Dequeue();
            float duration = durations.Dequeue();

            if (label != null) label.text = message;
            if (root != null) root.SetActive(true);

            yield return Fade(0f, 1f);
            yield return new WaitForSecondsRealtime(duration);
            yield return Fade(1f, 0f);

            if (root != null) root.SetActive(false);
        }
        routine = null;
    }

    private IEnumerator Fade(float from, float to)
    {
        if (canvasGroup == null) yield break;

        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;   // pause sirasinda da calisir
            canvasGroup.alpha = Mathf.Lerp(from, to, t / fadeTime);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}