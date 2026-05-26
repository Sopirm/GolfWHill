using UnityEngine;
using UnityEngine.EventSystems;

public class PlatformVirtualJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [SerializeField] private RectTransform handle;
    [SerializeField] private float movementRange = 60f;

    private RectTransform rectTransform;
    private Canvas canvas;
    private Camera uiCamera;
    private Vector2 input;

    public Vector2 InputVector => input;

    public void SetHandle(RectTransform handleTransform)
    {
        handle = handleTransform;
    }

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = canvas.worldCamera;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rectTransform == null)
        {
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, uiCamera, out Vector2 localPoint))
        {
            return;
        }

        Vector2 normalized = new Vector2(
            localPoint.x / (rectTransform.rect.width * 0.5f),
            localPoint.y / (rectTransform.rect.height * 0.5f));

        input = Vector2.ClampMagnitude(normalized, 1f);

        if (handle != null)
        {
            handle.anchoredPosition = input * movementRange;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        input = Vector2.zero;

        if (handle != null)
        {
            handle.anchoredPosition = Vector2.zero;
        }
    }
}
