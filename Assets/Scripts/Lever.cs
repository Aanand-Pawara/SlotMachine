
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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

    public event Action<Lever> Activated;
    public event Action<Lever> AnimationCompleted;

    public bool IsAnimating => animationRoutine != null;
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

    public void Press()
    {
        TryActivate();
    }

    public void SetInteractable(bool interactable)
    {
        IsInteractable = interactable;

        if (pointerHandler != null)
            pointerHandler.Interactable = interactable;
    }

    private void HandlePointerClick(UnityEngine.EventSystems.PointerEventData eventData)
    {
        TryActivate();
    }

    private IEnumerator PlayPressAnimation()
    {
        ShowState(PressedIndex);

        if (pressedDuration > 0f)
            yield return new WaitForSecondsRealtime(pressedDuration);

        ShowState(IdleIndex);
        animationRoutine = null;
        AnimationCompleted?.Invoke(this);
    }

    private void ShowState(int index)
    {
        if (leverImage != null && stateSprites != null &&
            index >= 0 && index < stateSprites.Length &&
            stateSprites[index] != null)
        {
            leverImage.sprite = stateSprites[index];
        }
    }

    private void StopAnimation()
    {
        if (animationRoutine == null)
            return;

        StopCoroutine(animationRoutine);
        animationRoutine = null;
    }
}
