// VirtualJoystick.cs — Mobil sanal joystick
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.EventSystems;

public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Referanslar")]
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;

    [Header("Ayarlar")]
    [SerializeField] private float handleRange = 60f;
    [SerializeField] private float deadZone = 0.12f;

    private Vector2 input;

    private void Start()
    {
        ResetHandle();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPoint;
        bool ok = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background, eventData.position, eventData.pressEventCamera, out localPoint);

        if (!ok) return;

        Vector2 clamped = Vector2.ClampMagnitude(localPoint, handleRange);
        if (handle != null) handle.anchoredPosition = clamped;

        input = clamped / handleRange;
        if (input.magnitude < deadZone) input = Vector2.zero;

        SendToPlayer();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        input = Vector2.zero;
        ResetHandle();
        SendToPlayer();
    }

    private void ResetHandle()
    {
        if (handle != null) handle.anchoredPosition = Vector2.zero;
    }

    private void SendToPlayer()
    {
        if (PlayerController.Instance != null)
            PlayerController.Instance.SetMoveInput(input);
    }
}
