using UnityEngine;
using UnityEngine.EventSystems;

public class PlatformTouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private bool isHeld;
    private bool wasPressedThisFrame;

    public bool IsHeld => isHeld;

    public void OnPointerDown(PointerEventData eventData)
    {
        isHeld = true;
        wasPressedThisFrame = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isHeld = false;
    }

    public bool ConsumePressedThisFrame()
    {
        bool result = wasPressedThisFrame;
        wasPressedThisFrame = false;
        return result;
    }
}
