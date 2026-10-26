using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotMachine.UI
{
    // Shows a short message near the place where the player clicked.
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

        // Sets up the message and starts its movement and fade animation.
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

            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.localScale = Vector3.one;
            rectTransform.anchoredPosition = Vector2.zero;
            Canvas.ForceUpdateCanvases();

            Rect safeBounds = GetSafeBounds(canvasRect, uiCamera);
            float safeMargin = Mathf.Min(8f, Mathf.Min(safeBounds.width, safeBounds.height) * 0.25f);
            safeBounds = Rect.MinMaxRect(
                safeBounds.xMin + safeMargin,
                safeBounds.yMin + safeMargin,
                safeBounds.xMax - safeMargin,
                safeBounds.yMax - safeMargin);

            Rect notificationBounds = GetVisibleBounds(canvasRect);
            if (notificationBounds.width <= 0f || notificationBounds.height <= 0f)
            {
                Debug.LogError("Notification prefab must have a non-zero visible size.", this);
                Destroy(gameObject);
                return;
            }

            float maxScale = Mathf.Min(
                1f,
                Mathf.Min(safeBounds.width / notificationBounds.width, safeBounds.height / notificationBounds.height));
            Vector2 rootPositionAtZero = canvasRect.InverseTransformPoint(
                rectTransform.TransformPoint(Vector3.zero));
            Vector2 minFromRoot = new(
                notificationBounds.xMin - rootPositionAtZero.x,
                notificationBounds.yMin - rootPositionAtZero.y);
            Vector2 maxFromRoot = new(
                notificationBounds.xMax - rootPositionAtZero.x,
                notificationBounds.yMax - rootPositionAtZero.y);

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, screenPosition, uiCamera, out Vector2 localPosition))
            {
                localPosition = Vector2.zero;
            }

            Vector2 startPosition = ClampPosition(
                localPosition + new Vector2(0f, 40f),
                rootPositionAtZero,
                minFromRoot,
                maxFromRoot,
                maxScale,
                safeBounds);
            Vector2 endPosition = ClampPosition(
                startPosition + new Vector2(0f, 45f),
                rootPositionAtZero,
                minFromRoot,
                maxFromRoot,
                maxScale,
                safeBounds);
            rectTransform.anchoredPosition = startPosition;
            rectTransform.localScale = Vector3.one * (maxScale * 0.75f);
            canvasGroup.alpha = 0f;
            StartCoroutine(Animate(duration, color, startPosition, endPosition, maxScale));
        }

        // Moves the message up, makes it larger, and fades it out.
        private IEnumerator Animate(
            float duration,
            Color color,
            Vector2 startPosition,
            Vector2 endPosition,
            float maxScale)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                rectTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, eased);
                rectTransform.localScale = Vector3.one * (maxScale * Mathf.Lerp(0.75f, 1f, eased));
                canvasGroup.alpha = t < 0.75f ? 1f : 1f - Mathf.InverseLerp(0.75f, 1f, t);
                messageText.color = new Color(color.r, color.g, color.b, canvasGroup.alpha);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }

        private Rect GetSafeBounds(RectTransform canvasRect, Camera uiCamera)
        {
            Rect safeArea = Screen.safeArea;
            if (safeArea.width <= 0f || safeArea.height <= 0f)
                return canvasRect.rect;

            Vector2[] screenCorners =
            {
                new(safeArea.xMin, safeArea.yMin),
                new(safeArea.xMin, safeArea.yMax),
                new(safeArea.xMax, safeArea.yMin),
                new(safeArea.xMax, safeArea.yMax)
            };
            Vector2 min = new(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new(float.NegativeInfinity, float.NegativeInfinity);

            foreach (Vector2 screenCorner in screenCorners)
            {
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRect, screenCorner, uiCamera, out Vector2 localCorner))
                {
                    return canvasRect.rect;
                }

                min = Vector2.Min(min, localCorner);
                max = Vector2.Max(max, localCorner);
            }

            Rect safeBounds = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            float xMin = Mathf.Max(safeBounds.xMin, canvasRect.rect.xMin);
            float yMin = Mathf.Max(safeBounds.yMin, canvasRect.rect.yMin);
            float xMax = Mathf.Min(safeBounds.xMax, canvasRect.rect.xMax);
            float yMax = Mathf.Min(safeBounds.yMax, canvasRect.rect.yMax);
            if (xMax <= xMin || yMax <= yMin)
                return canvasRect.rect;

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private Rect GetVisibleBounds(RectTransform canvasRect)
        {
            RectTransform[] rects = GetComponentsInChildren<RectTransform>(true);
            Vector2 min = new(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new(float.NegativeInfinity, float.NegativeInfinity);
            Vector3[] corners = new Vector3[4];

            foreach (RectTransform rect in rects)
            {
                if (!rect.gameObject.activeInHierarchy)
                    continue;

                rect.GetWorldCorners(corners);
                foreach (Vector3 corner in corners)
                {
                    Vector3 localCorner = canvasRect.InverseTransformPoint(corner);
                    min = Vector2.Min(min, localCorner);
                    max = Vector2.Max(max, localCorner);
                }
            }

            if (float.IsInfinity(min.x) || float.IsInfinity(min.y))
                return new Rect();

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static Vector2 ClampPosition(
            Vector2 desiredPosition,
            Vector2 rootPositionAtZero,
            Vector2 minFromRoot,
            Vector2 maxFromRoot,
            float scale,
            Rect bounds)
        {
            float minX = bounds.xMin - rootPositionAtZero.x - minFromRoot.x * scale;
            float maxX = bounds.xMax - rootPositionAtZero.x - maxFromRoot.x * scale;
            float minY = bounds.yMin - rootPositionAtZero.y - minFromRoot.y * scale;
            float maxY = bounds.yMax - rootPositionAtZero.y - maxFromRoot.y * scale;

            return new Vector2(
                Mathf.Clamp(desiredPosition.x, minX, Mathf.Max(minX, maxX)),
                Mathf.Clamp(desiredPosition.y, minY, Mathf.Max(minY, maxY)));
        }
    }
}
