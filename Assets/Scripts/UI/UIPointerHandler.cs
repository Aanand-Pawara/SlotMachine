
using System;
using UnityEngine;
using UnityEngine.EventSystems;

// This script listens to mouse and pointer events on a Unity UI object.
// It lets other scripts know when the player clicked, pressed, or moved over this object.
[DisallowMultipleComponent]
public sealed class UIPointerHandler : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    // Called when the pointer is clicked on this object.
    public event Action<PointerEventData> Clicked;

    // Called when the mouse button goes down.
    public event Action<PointerEventData> PointerDown;

    // Called when the mouse button is released.
    public event Action<PointerEventData> PointerUp;

    // Called when the pointer enters this object.
    public event Action<PointerEventData> PointerEntered;

    // Called when the pointer leaves this object.
    public event Action<PointerEventData> PointerExited;

    [SerializeField] private bool interactable = true;
    [SerializeField] private bool allowRightClick;
    [SerializeField] private bool allowMiddleClick;

    // Can this object receive pointer input right now?
    public bool Interactable
    {
        get => interactable;
        set => interactable = value;
    }

    public bool AllowRightClick
    {
        get => allowRightClick;
        set => allowRightClick = value;
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

    // Checks whether this object is active and can process input.
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
