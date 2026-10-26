using UnityEngine;

namespace SlotMachine.UI
{
    // Keeps UI elements and all their child parts inside the visible screen area.
    public static class ScreenSpaceUIUtility
    {
        public static bool FitAndClamp(
            RectTransform item,
            RectTransform canvasRect,
            Camera uiCamera,
            Vector2 desiredPosition,
            out float fittedScale,
            float safeMargin = 8f)
        {
            fittedScale = 1f;
            if (!TryGetLayout(item, canvasRect, uiCamera, safeMargin, out Rect safeBounds,
                    out Rect itemBounds, out Vector2 rootPositionAtZero))
            {
                return false;
            }

            fittedScale = Mathf.Min(
                1f,
                Mathf.Min(safeBounds.width / itemBounds.width, safeBounds.height / itemBounds.height));
            if (fittedScale <= 0f)
                return false;

            ApplyClampedPosition(
                item,
                desiredPosition,
                fittedScale,
                safeBounds,
                itemBounds,
                rootPositionAtZero);
            return true;
        }

        public static bool ClampToSafeArea(
            RectTransform item,
            RectTransform canvasRect,
            Camera uiCamera,
            Vector2 desiredPosition,
            float scale,
            float safeMargin = 8f)
        {
            if (!TryGetLayout(item, canvasRect, uiCamera, safeMargin, out Rect safeBounds,
                    out Rect itemBounds, out Vector2 rootPositionAtZero))
            {
                return false;
            }

            float fitScale = Mathf.Min(
                1f,
                Mathf.Min(safeBounds.width / itemBounds.width, safeBounds.height / itemBounds.height));
            float clampedScale = Mathf.Min(Mathf.Max(0f, scale), fitScale);
            if (clampedScale <= 0f)
                return false;

            ApplyClampedPosition(
                item,
                desiredPosition,
                clampedScale,
                safeBounds,
                itemBounds,
                rootPositionAtZero);
            return true;
        }

        private static bool TryGetLayout(
            RectTransform item,
            RectTransform canvasRect,
            Camera uiCamera,
            float safeMargin,
            out Rect safeBounds,
            out Rect itemBounds,
            out Vector2 rootPositionAtZero)
        {
            safeBounds = new Rect();
            itemBounds = new Rect();
            rootPositionAtZero = Vector2.zero;

            if (item == null || canvasRect == null || item.parent != canvasRect)
                return false;

            item.anchorMin = new Vector2(0.5f, 0.5f);
            item.anchorMax = new Vector2(0.5f, 0.5f);
            item.pivot = new Vector2(0.5f, 0.5f);
            item.localScale = Vector3.one;
            item.anchoredPosition = Vector2.zero;
            Canvas.ForceUpdateCanvases();

            safeBounds = GetSafeBounds(canvasRect, uiCamera);
            float margin = Mathf.Clamp(
                safeMargin,
                0f,
                Mathf.Min(safeBounds.width, safeBounds.height) * 0.25f);
            safeBounds = Rect.MinMaxRect(
                safeBounds.xMin + margin,
                safeBounds.yMin + margin,
                safeBounds.xMax - margin,
                safeBounds.yMax - margin);

            itemBounds = GetVisibleBounds(item, canvasRect);
            if (itemBounds.width <= 0f || itemBounds.height <= 0f ||
                safeBounds.width <= 0f || safeBounds.height <= 0f)
            {
                return false;
            }

            rootPositionAtZero = canvasRect.InverseTransformPoint(item.TransformPoint(Vector3.zero));
            return true;
        }

        private static Rect GetSafeBounds(RectTransform canvasRect, Camera uiCamera)
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

            Rect bounds = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            float xMin = Mathf.Max(canvasRect.rect.xMin, bounds.xMin);
            float yMin = Mathf.Max(canvasRect.rect.yMin, bounds.yMin);
            float xMax = Mathf.Min(canvasRect.rect.xMax, bounds.xMax);
            float yMax = Mathf.Min(canvasRect.rect.yMax, bounds.yMax);

            if (xMax <= xMin || yMax <= yMin)
                return canvasRect.rect;

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private static Rect GetVisibleBounds(RectTransform item, RectTransform canvasRect)
        {
            RectTransform[] rects = item.GetComponentsInChildren<RectTransform>(true);
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

        private static void ApplyClampedPosition(
            RectTransform item,
            Vector2 desiredPosition,
            float scale,
            Rect safeBounds,
            Rect itemBounds,
            Vector2 rootPositionAtZero)
        {
            Vector2 minFromRoot = new(
                itemBounds.xMin - rootPositionAtZero.x,
                itemBounds.yMin - rootPositionAtZero.y);
            Vector2 maxFromRoot = new(
                itemBounds.xMax - rootPositionAtZero.x,
                itemBounds.yMax - rootPositionAtZero.y);
            float minX = safeBounds.xMin - rootPositionAtZero.x - minFromRoot.x * scale;
            float maxX = safeBounds.xMax - rootPositionAtZero.x - maxFromRoot.x * scale;
            float minY = safeBounds.yMin - rootPositionAtZero.y - minFromRoot.y * scale;
            float maxY = safeBounds.yMax - rootPositionAtZero.y - maxFromRoot.y * scale;

            item.localScale = Vector3.one * scale;
            item.anchoredPosition = new Vector2(
                Mathf.Clamp(desiredPosition.x - rootPositionAtZero.x, minX, Mathf.Max(minX, maxX)),
                Mathf.Clamp(desiredPosition.y - rootPositionAtZero.y, minY, Mathf.Max(minY, maxY)));
        }
    }
}
