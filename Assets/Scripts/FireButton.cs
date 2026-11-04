// FireButton.cs — Basili tutulan ates butonu (sadece gece)
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.EventSystems;

public class FireButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private GameObject buttonVisual;

    private void Start()
    {
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
        bool visible = phase == GamePhase.Night;
        if (buttonVisual != null) buttonVisual.SetActive(visible);

        // Faz degisince ates birakilir — tus basili kalmasin
        if (!visible && PlayerCombat.Instance != null)
            PlayerCombat.Instance.SetFiringInput(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (PlayerCombat.Instance != null) PlayerCombat.Instance.SetFiringInput(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (PlayerCombat.Instance != null) PlayerCombat.Instance.SetFiringInput(false);
    }
}
