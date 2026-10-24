using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotMachine.UI
{
    public sealed class FloatingNotificationController : MonoBehaviour
    {
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Image background;
        [SerializeField] private CanvasGroup canvasGroup;

        private RectTransform rectTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();

            if (messageText == null)
                messageText = GetComponentInChildren<TMP_Text>();

            if (background == null)
                background = GetComponentInChildren<Image>();

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void Show(
            string message,
            Color color,
            RectTransform canvasRect,
            Camera uiCamera,
            Vector2 screenPosition,
            float duration)
        {
            if (messageText == null || rectTransform == null || canvasRect == null)
            {
                Debug.LogError("Notification prefab needs a RectTransform, TMP text, and canvas.", this);
                Destroy(gameObject);
                return;
            }

            messageText.text = message;
            messageText.raycastTarget = false;
            if (background != null)
            {
                background.color = new Color(color.r, color.g, color.b, 0.82f);
                background.raycastTarget = false;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, screenPosition, uiCamera, out Vector2 localPosition))
            {
                localPosition = Vector2.zero;
            }

            rectTransform.anchoredPosition = localPosition + new Vector2(0f, 40f);
            rectTransform.localScale = Vector3.one * 0.75f;
            canvasGroup.alpha = 0f;
            StartCoroutine(Animate(duration, color));
        }

        private IEnumerator Animate(float duration, Color color)
        {
            Vector2 startPosition = rectTransform.anchoredPosition;
            Vector2 endPosition = startPosition + new Vector2(0f, 45f);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                rectTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, eased);
                rectTransform.localScale = Vector3.Lerp(Vector3.one * 0.75f, Vector3.one, eased);
                canvasGroup.alpha = t < 0.75f ? 1f : 1f - Mathf.InverseLerp(0.75f, 1f, t);
                messageText.color = new Color(color.r, color.g, color.b, canvasGroup.alpha);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
