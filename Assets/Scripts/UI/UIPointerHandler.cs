
using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Summary>
/// A component that allows you to handle pointer events on a GameObject.
/// </summary>
[DisallowMultipleComponent]
public sealed class UIPointerHandler : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    public event Action<PointerEventData> Clicked;
    public event Action<PointerEventData> PointerDown;
    public event Action<PointerEventData> PointerUp;
    public event Action<PointerEventData> PointerEntered;
    public event Action<PointerEventData> PointerExited;

    [SerializeField] private bool interactable = true;
    [SerializeField] private bool allowRightClick;
    [SerializeField] private bool allowMiddleClick;

    public bool Interactable
    {
        get => interactable;
        set => interactable = value;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!CanProcess(eventData)) return;
        if (eventData.button != PointerEventData.InputButton.Left &&
            !(allowRightClick && eventData.button == PointerEventData.InputButton.Right) &&
            !(allowMiddleClick && eventData.button == PointerEventData.InputButton.Middle))
            return;

        Clicked?.Invoke(eventData);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (CanProcess(eventData) && eventData.button == PointerEventData.InputButton.Left)
            PointerDown?.Invoke(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (CanProcess(eventData) && eventData.button == PointerEventData.InputButton.Left)
            PointerUp?.Invoke(eventData);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (CanProcess(eventData))
            PointerEntered?.Invoke(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (CanProcess(eventData))
            PointerExited?.Invoke(eventData);
    }

    private bool CanProcess(PointerEventData eventData)
    {
        return isActiveAndEnabled && interactable && eventData != null;
    }

    private void OnDisable()
    {
        PointerUp = null;
        PointerDown = null;
        PointerEntered = null;
        PointerExited = null;
        Clicked = null;
    }
}
