// InteractionButton.cs — Etkilesim butonu (sadece gunduz)
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.UI;

public class InteractionButton : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private GameObject buttonVisual;
    [SerializeField] private Button button;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Gorunum")]
    [SerializeField] private float activeAlpha = 1f;
    [SerializeField] private float idleAlpha = 0.4f;

    private void Start()
    {
        if (button != null) button.onClick.AddListener(OnPressed);

        GameManager.Instance.OnPhaseChanged += HandlePhaseChanged;
        HandlePhaseChanged(GameManager.Instance.CurrentPhase);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        bool visible = phase == GamePhase.Day || phase == GamePhase.Tutorial;
        if (buttonVisual != null) buttonVisual.SetActive(visible);
    }

    private void Update()
    {
        if (canvasGroup == null) return;
        if (PlayerInteraction.Instance == null) return;

        bool available = PlayerInteraction.Instance.HasAnyInteractionAvailable();
        canvasGroup.alpha = available ? activeAlpha : idleAlpha;
    }

    private void OnPressed()
    {
        if (PlayerInteraction.Instance != null)
            PlayerInteraction.Instance.Interact();
    }
}
