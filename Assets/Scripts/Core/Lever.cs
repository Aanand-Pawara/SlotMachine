
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SlotMachine.Core
{
    // This script changes the lever picture when a spin starts.
    // The lever is only an animation. The player does not click it to spin.
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

        // True while the lever is showing its pressed picture.
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

        // Starts the lever animation. Returns false if it is already moving.
        public bool TryActivate()
        {
            if (!isActiveAndEnabled || IsAnimating)
                return false;

            animationRoutine = StartCoroutine(PlayPressAnimation());
            return true;
        }

        // Shows the pressed picture, waits, and then shows the idle picture.
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
}
