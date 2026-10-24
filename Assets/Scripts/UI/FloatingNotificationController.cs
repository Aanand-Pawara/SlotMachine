using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotMachine.UI
{
    public sealed class FloatingNotificationController : MonoBehaviour
    {
        [SerializeField] private GameObject toastPrefab;
        [SerializeField] private RectTransform container;
        [SerializeField] private float displayDuration = 1.4f;

        private Canvas parentCanvas;

        private void Awake()
        {
            if (container == null)
                container = GetComponent<RectTransform>();

            if (parentCanvas == null)
                parentCanvas = GetComponentInParent<Canvas>();
        }

        public void Show(string message, Color color, Vector2 screenPosition)
        {
            if (toastPrefab == null)
                return;

            if (parentCanvas == null)
                parentCanvas = GetComponentInParent<Canvas>();

            GameObject toast = Instantiate(toastPrefab, container, false);
            var rect = toast.GetComponent<RectTransform>();
            var text = toast.GetComponentInChildren<TMP_Text>();
            var background = toast.GetComponentInChildren<Image>();
            var canvasGroup = toast.GetComponent<CanvasGroup>();

            if (text != null)
                text.text = message;

            if (background != null)
                background.color = new Color(color.r, color.g, color.b, 0.75f);

            if (canvasGroup == null)
                canvasGroup = toast.AddComponent<CanvasGroup>();

            Vector2 anchoredPosition;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    container,
                    screenPosition,
                    parentCanvas != null ? parentCanvas.worldCamera : null,
                    out var localPosition))
            {
                anchoredPosition = localPosition;
            }
            else
            {
                anchoredPosition = Vector2.zero;
            }

            rect.anchoredPosition = anchoredPosition + new Vector2(0f, 50f);
            StartCoroutine(AnimateToast(rect, canvasGroup, text, color));
        }

        private IEnumerator AnimateToast(RectTransform rect, CanvasGroup canvasGroup, TMP_Text text, Color color)
        {
            float elapsed = 0f;
            Vector3 startScale = Vector3.one * 0.7f;
            Vector3 targetScale = Vector3.one;
            Vector3 startPos = rect.anchoredPosition;
            Vector3 endPos = rect.anchoredPosition + new Vector2(0f, 40f);

            while (elapsed < displayDuration)
            {
                float t = Mathf.Clamp01(elapsed / displayDuration);

                rect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                rect.localScale = Vector3.Lerp(startScale, targetScale, Mathf.SmoothStep(0f, 1f, t));

                if (canvasGroup != null)
                    canvasGroup.alpha = 1f - Mathf.Clamp01(t * 1.2f);

                if (text != null)
                    text.color = new Color(color.r, color.g, color.b, 1f - Mathf.Clamp01(t * 1.1f));

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (rect != null)
                Destroy(rect.gameObject);
        }
    }
}
