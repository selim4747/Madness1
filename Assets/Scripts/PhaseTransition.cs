// PhaseTransition.cs — Gunduz/gece gecis animasyonu
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public class PhaseTransition : MonoBehaviour
{
    public static PhaseTransition Instance { get; private set; }

    [Header("Referanslar")]
    [SerializeField] private GameObject root;
    [SerializeField] private Image overlay;
    [SerializeField] private TextMeshProUGUI label;

    [Header("Renkler")]
    [SerializeField] private Color sunsetColor = new Color(0.25f, 0.10f, 0.20f);
    [SerializeField] private Color sunriseColor = new Color(0.95f, 0.75f, 0.45f);

    [Header("Sure")]
    [SerializeField] private float fadeInTime = 0.8f;
    [SerializeField] private float holdTime = 1.0f;
    [SerializeField] private float fadeOutTime = 0.8f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (root != null) root.SetActive(false);
    }

    public void PlayDayToNight(Action onComplete)
    {
        StartCoroutine(PlayTransition(sunsetColor, "Gunes batiyor...", onComplete));
    }

    public void PlayNightToDay(Action onComplete)
    {
        StartCoroutine(PlayTransition(sunriseColor, "Yeni bir gun...", onComplete));
    }

    private IEnumerator PlayTransition(Color color, string message, Action onComplete)
    {
        if (root != null) root.SetActive(true);
        if (label != null) label.text = message;

        yield return FadeOverlay(color, 0f, 1f, fadeInTime);
        yield return new WaitForSecondsRealtime(holdTime);
        yield return FadeOverlay(color, 1f, 0f, fadeOutTime);

        if (root != null) root.SetActive(false);
        if (onComplete != null) onComplete();
    }

    private IEnumerator FadeOverlay(Color color, float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(from, to, t / duration);

            if (overlay != null) overlay.color = new Color(color.r, color.g, color.b, a);
            if (label != null) label.alpha = a;

            yield return null;
        }

        if (overlay != null) overlay.color = new Color(color.r, color.g, color.b, to);
        if (label != null) label.alpha = to;
    }
}
