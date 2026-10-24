
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// This object controls the slot machine lever.
// It changes the lever image between idle and pressed states,
// blocks spam clicks, and sends an event when the player pulls it.
[DisallowMultipleComponent]
public sealed class Lever : MonoBehaviour
{
    private const int IdleIndex = 0;
    private const int PressedIndex = 1;

    [Header("References")]
    [SerializeField] private UIPointerHandler pointerHandler;
    [SerializeField] private Image leverImage;
    [SerializeField] private Sprite[] stateSprites;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float pressedDuration = 0.2f;
    [SerializeField, Min(0f)] private float cooldown = 0.15f;

    // Called when the lever is pulled and the action is allowed.
    public event Action<Lever> Activated;

    // Called when the player clicks the lever so the game can open a bet popup.
    public event Action<Lever, Vector2> ActivationRequested;

    // Called after the lever animation finishes.
    public event Action<Lever> AnimationCompleted;

    // True while the lever is moving between pressed and idle state.
    public bool IsAnimating => animationRoutine != null;

    // True when the player can press the lever again.
    public bool IsInteractable { get; private set; } = true;

    private Coroutine animationRoutine;
    private float nextActivationTime;

    private void Awake()
    {
        if (pointerHandler == null)
            pointerHandler = GetComponent<UIPointerHandler>();

        if (leverImage == null)
            leverImage = GetComponent<Image>();

        if (pointerHandler == null || leverImage == null ||
            stateSprites == null || stateSprites.Length < 2 ||
            stateSprites[IdleIndex] == null || stateSprites[PressedIndex] == null)
        {
            Debug.LogError($"[{nameof(Lever)}] Assign a pointer handler, lever Image, and idle/pressed sprites on '{name}'.", this);
            enabled = false;
            return;
        }

        ShowState(IdleIndex);
    }

    private void OnEnable()
    {
        if (pointerHandler != null)
            pointerHandler.Clicked += HandlePointerClick;
    }

    private void OnDisable()
    {
        if (pointerHandler != null)
            pointerHandler.Clicked -= HandlePointerClick;

        StopAnimation();
        ShowState(IdleIndex);
    }

    // Tries to pull the lever.
    // If the lever is not ready, it will not start the animation.
    public bool TryActivate()
    {
        if (!isActiveAndEnabled || !IsInteractable || IsAnimating ||
            Time.unscaledTime < nextActivationTime)
            return false;

        nextActivationTime = Time.unscaledTime + cooldown;
        animationRoutine = StartCoroutine(PlayPressAnimation());
        Activated?.Invoke(this);
        return true;
    }

    // Simple method used by other scripts when they want to trigger the lever.
    public void Press()
    {
        TryActivate();
    }

    // Lets the game disable or enable the lever by user action.
    public void SetInteractable(bool interactable)
    {
        IsInteractable = interactable;

        if (pointerHandler != null)
            pointerHandler.Interactable = interactable;
    }

    private void HandlePointerClick(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (eventData == null)
            return;

        ActivationRequested?.Invoke(this, eventData.position);
    }

    // Plays the press animation and then returns to idle.
    private IEnumerator PlayPressAnimation()
    {
        ShowState(PressedIndex);

        if (pressedDuration > 0f)
            yield return new WaitForSecondsRealtime(pressedDuration);

        ShowState(IdleIndex);
        animationRoutine = null;
        AnimationCompleted?.Invoke(this);
    }

    // Changes the lever image to the selected sprite.
    private void ShowState(int index)
    {
        if (leverImage != null && stateSprites != null &&
            index >= 0 && index < stateSprites.Length &&
            stateSprites[index] != null)
        {
            leverImage.sprite = stateSprites[index];
        }
    }

    // Stops the current animation if the lever is disabled or reset.
    private void StopAnimation()
    {
        if (animationRoutine == null)
            return;

        StopCoroutine(animationRoutine);
        animationRoutine = null;
    }
}
