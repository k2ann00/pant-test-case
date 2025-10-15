using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class JoystickController : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler

{
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;
    private RectTransform canvasRect;
    public Vector2 InputDir { get; private set; }

    private Canvas canvas;
    private Camera uiCamera;

    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas.transform as RectTransform;

        uiCamera = canvas.renderMode == RenderMode.ScreenSpaceCamera ? canvas.worldCamera : null;
        Debug.Log("UiCamrea: " + uiCamera);
        // Baþlangýçta joystick gizli
        HideJoystick();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        ShowJoystick();
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, eventData.position, uiCamera, out Vector2 localPoint))
        {
            background.anchoredPosition = localPoint;
        }

        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background, eventData.position, eventData.pressEventCamera, out Vector2 pos))
        {
            pos /= background.sizeDelta / 2f;
            InputDir = (pos.magnitude > 1f) ? pos.normalized : pos;

            handle.anchoredPosition = InputDir * (background.sizeDelta / 2.5f);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        InputDir = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
        HideJoystick();
    }

    private void HideJoystick()
    {
        background.gameObject.SetActive(false);
        handle.gameObject.SetActive(false);
        Debug.Log("Joystick Hidden");
    }

    private void ShowJoystick()
    {
        background.gameObject.SetActive(true);
        handle.gameObject.SetActive(true);
        Debug.Log("Joystick Shown");
    }
}
