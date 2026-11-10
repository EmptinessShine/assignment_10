using UnityEngine;
using UnityEngine.EventSystems;

public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] RectTransform background;
    [SerializeField] RectTransform handle;

    public Vector2 value;

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, eventData.pressEventCamera, out localPoint);

        float radius = background.rect.width / 2f;
        value = Vector2.ClampMagnitude(localPoint / radius, 1f);
        handle.anchoredPosition = value * radius;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        value = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
    }
}
