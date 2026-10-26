
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SlotMachine.UI
{
    // Add this script to a UI object to listen for pointer actions.
    // The Canvas also needs a Graphic Raycaster, and the scene needs an EventSystem.
    [DisallowMultipleComponent]
    public sealed class UIPointerHandler : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        // Other scripts can listen to these events to react to pointer actions.
        public event Action<PointerEventData> Clicked;
        public event Action<PointerEventData> PointerDown;
        public event Action<PointerEventData> PointerUp;
        public event Action<PointerEventData> PointerEntered;
        public event Action<PointerEventData> PointerExited;

        // These settings decide which clicks the object accepts.
        [SerializeField] private bool interactable = true;
        [SerializeField] private bool allowRightClick;
        [SerializeField] private bool allowMiddleClick;

        // Turn this off to ignore pointer actions without removing the component.
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

        // Sends a click event when an allowed mouse button is clicked.
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!CanProcess(eventData)) return;
            if (eventData.button != PointerEventData.InputButton.Left &&
                !(allowRightClick && eventData.button == PointerEventData.InputButton.Right) &&
                !(allowMiddleClick && eventData.button == PointerEventData.InputButton.Middle))
                return;

            Clicked?.Invoke(eventData);
        }

        // Sends an event when the left mouse button is pressed down.
        public void OnPointerDown(PointerEventData eventData)
        {
            if (CanProcess(eventData) && eventData.button == PointerEventData.InputButton.Left)
                PointerDown?.Invoke(eventData);
        }

        // Sends an event when the left mouse button is released.
        public void OnPointerUp(PointerEventData eventData)
        {
            if (CanProcess(eventData) && eventData.button == PointerEventData.InputButton.Left)
                PointerUp?.Invoke(eventData);
        }

        // Sends events when the pointer enters or leaves this object.
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

        // Checks that this object can receive the current pointer event.
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
}
