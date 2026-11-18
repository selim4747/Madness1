// PlayerSpotlightController.cs — Oyuncuya bagli 2D spot isigi: gun/gece + genislik upgrade
// NJG Games | Delilik | Unity 6 | UTF-8
using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Oyuncunun altindaki Light2D bilesenini GameManager fazina gore ayarlar.
/// Tum degerler Inspector'dan degistirilebilir. Genislik upgrade'i dis UI'dan
/// TryUpgradeWidth() ile cagrilabilir.
/// </summary>
[DisallowMultipleComponent]
public class PlayerSpotlightController : MonoBehaviour
{
    public static PlayerSpotlightController Instance { get; private set; }

    [Header("Referans")]
    [Tooltip("Bos birakilirsa bu obje ve cocuklarinda Light2D aranir.")]
    [SerializeField] private Light2D spotLight;
    [SerializeField] private bool searchInChildren = true;

    [Header("Gun ayarlari")]
    [SerializeField] private bool dayLightEnabled = false;
    [Min(0f)] [SerializeField] private float dayIntensity = 0f;
    [Min(0f)] [SerializeField] private float dayOuterRadius = 3f;
    [Range(0f, 360f)] [SerializeField] private float dayOuterAngle = 45f;
    [Range(0f, 360f)] [SerializeField] private float dayInnerAngle = 25f;
    [SerializeField] private Color dayColor = Color.white;
    [Range(0f, 1f)] [SerializeField] private float dayFalloff = 0.5f;

    [Header("Gece ayarlari")]
    [SerializeField] private bool nightLightEnabled = true;
    [Min(0f)] [SerializeField] private float nightIntensity = 1f;
    [Min(0f)] [SerializeField] private float nightOuterRadius = 5f;
    [Range(0f, 360f)] [SerializeField] private float nightOuterAngle = 55f;
    [Range(0f, 360f)] [SerializeField] private float nightInnerAngle = 30f;
    [SerializeField] private Color nightColor = Color.white;
    [Range(0f, 1f)] [SerializeField] private float nightFalloff = 0.5f;

    [Header("Faz gecisi")]
    [Min(0f)] [SerializeField] private float phaseTransitionSeconds = 1.25f;
    [SerializeField] private bool useUnscaledTimeDuringTransition = false;
    [SerializeField] private bool followGameManagerPhase = true;
    [SerializeField] private bool enableInTutorial = false;

    [Header("Genislik upgrade'i")]
    [Tooltip("Her seviye gece spotunun dis acisini bu kadar derece arttirir.")]
    [Min(0f)] [SerializeField] private float widthPerUpgradeDegrees = 8f;
    [Min(0)] [SerializeField] private int maxWidthUpgradeLevel = 5;
    [Min(0)] [SerializeField] private int widthUpgradeBaseCost = 250;
    [Min(1f)] [SerializeField] private float widthUpgradeCostMultiplier = 1.5f;
    [SerializeField] private int widthUpgradeLevel;

    public event Action<int> OnWidthUpgraded;
    public int WidthUpgradeLevel { get { return widthUpgradeLevel; } }
    public int MaxWidthUpgradeLevel { get { return maxWidthUpgradeLevel; } }
    public float CurrentNightOuterAngle
    {
        get { return Mathf.Clamp(nightOuterAngle + widthUpgradeLevel * widthPerUpgradeDegrees, 0f, 360f); }
    }
    public bool IsMaxWidth { get { return widthUpgradeLevel >= maxWidthUpgradeLevel; } }

    private bool isNight;
    private float transitionElapsed;
    private LightSettings fromSettings;
    private LightSettings toSettings;

    private struct LightSettings
    {
        public bool enabled;
        public float intensity;
        public float radius;
        public float outerAngle;
        public float innerAngle;
        public Color color;
        public float falloff;

        public LightSettings(bool enabled, float intensity, float radius, float outerAngle,
                             float innerAngle, Color color, float falloff)
        {
            this.enabled = enabled;
            this.intensity = intensity;
            this.radius = radius;
            this.outerAngle = outerAngle;
            this.innerAngle = innerAngle;
            this.color = color;
            this.falloff = falloff;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (spotLight == null)
            spotLight = searchInChildren ? GetComponentInChildren<Light2D>(true) : GetComponent<Light2D>();

        widthUpgradeLevel = Mathf.Clamp(widthUpgradeLevel, 0, Mathf.Max(0, maxWidthUpgradeLevel));
        isNight = false;
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPhaseChanged += HandlePhaseChanged;
            ApplyPhaseImmediately(GameManager.Instance.CurrentPhase);
        }
    }

    private void Start()
    {
        // GameManager bazi sahnelerde bu objeden sonra Awake/OnEnable alabilir.
        if (followGameManagerPhase && GameManager.Instance != null)
            ApplyPhaseImmediately(GameManager.Instance.CurrentPhase);
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
    }

    private void Update()
    {
        if (spotLight == null || transitionElapsed < 0f) return;

        float delta = useUnscaledTimeDuringTransition ? Time.unscaledDeltaTime : Time.deltaTime;
        transitionElapsed += delta;
        float t = phaseTransitionSeconds <= 0f ? 1f : Mathf.Clamp01(transitionElapsed / phaseTransitionSeconds);
        ApplySettings(LerpSettings(fromSettings, toSettings, t));

        if (t >= 1f) transitionElapsed = -1f;
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        if (!followGameManagerPhase) return;
        BeginPhaseTransition(phase);
    }

    private void ApplyPhaseImmediately(GamePhase phase)
    {
        if (spotLight == null) return;
        LightSettings settings = SettingsForPhase(phase);
        fromSettings = settings;
        toSettings = settings;
        transitionElapsed = -1f;
        ApplySettings(settings);
    }

    private void BeginPhaseTransition(GamePhase phase)
    {
        LightSettings next = SettingsForPhase(phase);
        fromSettings = ReadCurrentSettings();
        toSettings = next;
        transitionElapsed = 0f;
        isNight = IsNightPhase(phase);

        if (phaseTransitionSeconds <= 0f)
        {
            transitionElapsed = -1f;
            ApplySettings(next);
        }
    }

    private LightSettings SettingsForPhase(GamePhase phase)
    {
        bool nightPhase = IsNightPhase(phase);
        if (nightPhase)
        {
            return new LightSettings(nightLightEnabled, nightIntensity, nightOuterRadius,
                CurrentNightOuterAngle, nightInnerAngle, nightColor, nightFalloff);
        }

        bool tutorial = phase == GamePhase.Tutorial;
        return new LightSettings(tutorial ? enableInTutorial && dayLightEnabled : dayLightEnabled,
            dayIntensity, dayOuterRadius, dayOuterAngle, dayInnerAngle, dayColor, dayFalloff);
    }

    private static bool IsNightPhase(GamePhase phase)
    {
        return phase == GamePhase.Night || phase == GamePhase.DayToNight || phase == GamePhase.DaySummary;
    }

    private LightSettings ReadCurrentSettings()
    {
        return new LightSettings(spotLight.enabled, spotLight.intensity, spotLight.pointLightOuterRadius,
            spotLight.pointLightOuterAngle, spotLight.pointLightInnerAngle, spotLight.color,
            spotLight.falloffIntensity);
    }

    private static LightSettings LerpSettings(LightSettings a, LightSettings b, float t)
    {
        return new LightSettings(t < 1f ? a.enabled || b.enabled : b.enabled,
            Mathf.Lerp(a.intensity, b.intensity, t), Mathf.Lerp(a.radius, b.radius, t),
            Mathf.Lerp(a.outerAngle, b.outerAngle, t), Mathf.Lerp(a.innerAngle, b.innerAngle, t),
            Color.Lerp(a.color, b.color, t), Mathf.Lerp(a.falloff, b.falloff, t));
    }

    private void ApplySettings(LightSettings settings)
    {
        if (spotLight == null) return;
        spotLight.enabled = settings.enabled;
        spotLight.intensity = settings.intensity;
        spotLight.pointLightOuterRadius = Mathf.Max(0f, settings.radius);
        spotLight.pointLightOuterAngle = Mathf.Clamp(settings.outerAngle, 0f, 360f);
        spotLight.pointLightInnerAngle = Mathf.Clamp(settings.innerAngle, 0f, spotLight.pointLightOuterAngle);
        spotLight.color = settings.color;
        spotLight.falloffIntensity = Mathf.Clamp01(settings.falloff);
    }

    public int GetWidthUpgradeCost()
    {
        if (IsMaxWidth) return -1;
        return Mathf.RoundToInt(widthUpgradeBaseCost * Mathf.Pow(widthUpgradeCostMultiplier, widthUpgradeLevel));
    }

    public bool CanUpgradeWidth()
    {
        int cost = GetWidthUpgradeCost();
        return cost >= 0 && EconomyManager.Instance != null && EconomyManager.Instance.CanAfford(cost);
    }

    /// <summary>Upgrade panel veya baska bir UI tarafindan cagrilabilir.</summary>
    public bool TryUpgradeWidth()
    {
        int cost = GetWidthUpgradeCost();
        if (cost < 0) return false;
        if (EconomyManager.Instance == null || !EconomyManager.Instance.TrySpendGold(cost)) return false;

        widthUpgradeLevel++;
        ApplyPhaseImmediately(GameManager.Instance != null ? GameManager.Instance.CurrentPhase : GamePhase.Night);
        PlayerProgressStats.AddUpgradePurchased();
        OnWidthUpgraded?.Invoke(widthUpgradeLevel);
        return true;
    }

    public void SetWidthUpgradeLevel(int level)
    {
        widthUpgradeLevel = Mathf.Clamp(level, 0, Mathf.Max(0, maxWidthUpgradeLevel));
        if (spotLight != null)
            ApplyPhaseImmediately(GameManager.Instance != null ? GameManager.Instance.CurrentPhase : GamePhase.Night);
    }

    public void ApplySave(SaveData data)
    {
        SetWidthUpgradeLevel(data.spotlightWidthLevel);
    }

    public void WriteToSave(SaveData data)
    {
        data.spotlightWidthLevel = widthUpgradeLevel;
    }
}
