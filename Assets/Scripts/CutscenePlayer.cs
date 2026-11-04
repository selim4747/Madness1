// CutscenePlayer.cs — Video + zamanli altyazi oynaticisi (StoryCutsceneData tuketir)
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using System;
using System.Collections;

public class CutscenePlayer : MonoBehaviour
{
    public static CutscenePlayer Instance { get; private set; }

    [Header("Ekran")]
    [SerializeField] private GameObject root;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private AudioSource voiceOverSource;
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Header("Altyazi — iki ayri metin (isim + konusma)")]
    [SerializeField] private TextMeshProUGUI captionSpeakerLabel;
    [SerializeField] private TextMeshProUGUI captionTextLabel;

    [Header("Atlama")]
    [SerializeField] private GameObject skipButtonRoot;
    [SerializeField] private Button skipButton;

    private bool skipRequested;
    private bool isPlaying;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (root != null) root.SetActive(false);
        if (skipButton != null) skipButton.onClick.AddListener(RequestSkip);
    }

    public bool IsPlaying { get { return isPlaying; } }

    /// <summary>StoryManager cagirir. onComplete: video/atlama bitince (fade-out sonrasi) calisir.</summary>
    public void Play(StoryCutsceneData data, Action onComplete)
    {
        if (data == null || data.videoClip == null)
        {
            if (onComplete != null) onComplete();
            return;
        }

        StartCoroutine(PlayRoutine(data, onComplete));
    }

    private IEnumerator PlayRoutine(StoryCutsceneData data, Action onComplete)
    {
        isPlaying = true;
        skipRequested = false;

        // ── Oyunu dondur: "hersey duracak" ──
        float previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        if (root != null) root.SetActive(true);
        if (skipButtonRoot != null) skipButtonRoot.SetActive(data.canSkip);
        if (captionSpeakerLabel != null) captionSpeakerLabel.text = "";
        if (captionTextLabel != null) captionTextLabel.text = "";

        // ── Fade-in ──
        yield return FadeCanvas(0f, 1f, data.fadeInSeconds);

        // ── Video + seslendirme baslat ──
        videoPlayer.clip = data.videoClip;
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared && !skipRequested)
            yield return null;

        videoPlayer.Play();

        if (voiceOverSource != null && data.narratorVoiceOver != null)
        {
            voiceOverSource.clip = data.narratorVoiceOver;
            voiceOverSource.Play();
        }

        // ── Video oynarken altyazilari zamanina gore goster ──
        int nextCaptionIndex = 0;

        while (videoPlayer.isPlaying && !skipRequested)
        {
            double t = videoPlayer.time;

            while (data.captions != null && nextCaptionIndex < data.captions.Length &&
                   t >= data.captions[nextCaptionIndex].showAtSeconds)
            {
                CutsceneCaption cap = data.captions[nextCaptionIndex];
                if (captionSpeakerLabel != null) captionSpeakerLabel.text = cap.speakerName;
                if (captionTextLabel != null) captionTextLabel.text = cap.text;
                nextCaptionIndex++;
            }

            yield return null;
        }

        videoPlayer.Stop();
        if (voiceOverSource != null) voiceOverSource.Stop();

        // ── Fade-out ──
        yield return FadeCanvas(1f, 0f, data.fadeOutSeconds);

        if (root != null) root.SetActive(false);
        if (captionSpeakerLabel != null) captionSpeakerLabel.text = "";
        if (captionTextLabel != null) captionTextLabel.text = "";

        // ── Oyunu geri baslat ──
        Time.timeScale = previousTimeScale;
        isPlaying = false;

        if (onComplete != null) onComplete();
    }

    private void RequestSkip()
    {
        skipRequested = true;
    }

    private IEnumerator FadeCanvas(float from, float to, float duration)
    {
        if (fadeCanvasGroup == null || duration <= 0f)
        {
            if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = to;
            yield break;
        }

        float t = 0f;
        fadeCanvasGroup.alpha = from;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;   // Time.timeScale = 0 iken de calismali
            fadeCanvasGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }

        fadeCanvasGroup.alpha = to;
    }
}
