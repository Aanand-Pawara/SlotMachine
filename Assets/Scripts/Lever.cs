
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
    [SerializeField] private Image leverImage;
    [SerializeField] private Sprite[] stateSprites;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float pressedDuration = 0.2f;

    // True while the lever is moving between pressed and idle state.
    public bool IsAnimating => animationRoutine != null;

    private Coroutine animationRoutine;

    private void Awake()
    {
        if (leverImage == null)
            leverImage = GetComponent<Image>();

        if (leverImage == null ||
            stateSprites == null || stateSprites.Length < 2 ||
            stateSprites[IdleIndex] == null || stateSprites[PressedIndex] == null)
        {
            Debug.LogError($"[{nameof(Lever)}] Assign a lever Image and idle/pressed sprites on '{name}'.", this);
            enabled = false;
            return;
        }

        ShowState(IdleIndex);
    }

    private void OnDisable()
    {
        StopAnimation();
        ShowState(IdleIndex);
    }

    // Plays the lever animation when the game starts a spin.
    public bool TryActivate()
    {
        if (!isActiveAndEnabled || IsAnimating)
            return false;

        animationRoutine = StartCoroutine(PlayPressAnimation());
        return true;
    }

    // Plays the press animation and then returns to idle.
    private IEnumerator PlayPressAnimation()
    {
        ShowState(PressedIndex);

        if (pressedDuration > 0f)
            yield return new WaitForSecondsRealtime(pressedDuration);

        ShowState(IdleIndex);
        animationRoutine = null;
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
