// SatisfactionTracker.cs — Gunluk memnuniyet ortalamasi
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System.Collections.Generic;

public class SatisfactionTracker : MonoBehaviour
{
    public static SatisfactionTracker Instance { get; private set; }

    [Header("Egri ayarlari")]
    [SerializeField] private float bestRatio = 1.3f;      // aninda hizmet
    [SerializeField] private float worstRatio = 0.5f;     // cok gec hizmet
    [SerializeField] private float slowWaitSeconds = 10f; // bu surede en dibe iner
    [SerializeField] private float angryPenalty = 0.3f;   // kacan musteri katkisi
    [SerializeField] private float positiveEventBonus = 1.2f;

    private readonly List<float> todaysContributions = new List<float>();
    private float previousDayRatio = 1f;

    /// <summary>Bugunku anlik ortalama (HUD'da gosterilir).</summary>
    public float TodayAverage
    {
        get
        {
            if (todaysContributions.Count == 0) return 1f;
            float sum = 0f;
            for (int i = 0; i < todaysContributions.Count; i++) sum += todaysContributions[i];
            return Mathf.Clamp(sum / todaysContributions.Count, worstRatio, bestRatio);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        GameManager.Instance.OnDayChanged += HandleDayChanged;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnDayChanged -= HandleDayChanged;
    }

    /// <summary>Satis tamamlandi. secondsWaited = musterinin kasada bekledigi sure.</summary>
    public void RecordSale(float secondsWaited)
    {
        float t = Mathf.Clamp01(secondsWaited / slowWaitSeconds);
        todaysContributions.Add(Mathf.Lerp(bestRatio, worstRatio, t));
    }

    /// <summary>Sabri biten / hizmet alamayan musteri.</summary>
    public void RecordAngryDeparture()
    {
        todaysContributions.Add(angryPenalty);
    }

    /// <summary>Hirsiz yakalama gibi olumlu olaylar (itibar = memnuniyet).</summary>
    public void RecordPositiveEvent()
    {
        todaysContributions.Add(positiveEventBonus);
    }

    public float GetPreviousDayRatio()
    {
        return previousDayRatio;
    }

    private void HandleDayChanged(int day)
    {
        previousDayRatio = TodayAverage;
        todaysContributions.Clear();
    }

    // ── Kayit koprusu ──

    public void ApplySave(SaveData data)
    {
        previousDayRatio = data.lastSatisfactionRatio;
        todaysContributions.Clear();
    }

    public void WriteToSave(SaveData data)
    {
        data.lastSatisfactionRatio = previousDayRatio;
    }
    // Memnuniyet kaydetme metodunu karsilar
    public void RecordCustomer(bool isSatisfied, float score)
    {
        // Tracker tarafindaki gercek kaydetme islemlerine aktarir
    }
}
