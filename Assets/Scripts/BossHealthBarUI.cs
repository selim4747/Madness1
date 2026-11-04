// BossHealthBarUI.cs — Boss can bari (faz renkleriyle)
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealthBarUI : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private GameObject root;
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI phaseLabel;

    [Header("Faz renkleri")]
    [SerializeField] private Color phase1Color = new Color(0.85f, 0.25f, 0.25f);
    [SerializeField] private Color phase2Color = new Color(0.95f, 0.55f, 0.15f);
    [SerializeField] private Color phase3Color = new Color(0.70f, 0.10f, 0.75f);

    private void Start()
    {
        if (root != null) root.SetActive(false);

        if (BossController.Instance != null)
        {
            BossController.Instance.OnBossActiveChanged += HandleActiveChanged;
            BossController.Instance.OnHealthRatioChanged += HandleHealthChanged;
        }
    }

    private void OnDestroy()
    {
        if (BossController.Instance == null) return;

        BossController.Instance.OnBossActiveChanged -= HandleActiveChanged;
        BossController.Instance.OnHealthRatioChanged -= HandleHealthChanged;
    }

    private void HandleActiveChanged(bool active)
    {
        if (root != null) root.SetActive(active);
    }

    private void HandleHealthChanged(float ratio)
    {
        if (fillImage != null) fillImage.fillAmount = Mathf.Clamp01(ratio);

        int phase = BossController.Instance.CurrentPhase;

        if (fillImage != null)
        {
            if (phase >= 3)      fillImage.color = phase3Color;
            else if (phase >= 2) fillImage.color = phase2Color;
            else                 fillImage.color = phase1Color;
        }

        if (phaseLabel != null) phaseLabel.text = "Faz " + phase;
    }
}
