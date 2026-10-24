using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotMachine.UI
{
    public sealed class BettingPopupController : MonoBehaviour
    {
        private sealed class OptionBinding
        {
            public UIPointerHandler Handler;
            public Action<UnityEngine.EventSystems.PointerEventData> Clicked;
            public Action<UnityEngine.EventSystems.PointerEventData> PointerDown;
            public Action<UnityEngine.EventSystems.PointerEventData> PointerUp;
            public Action<UnityEngine.EventSystems.PointerEventData> PointerEntered;
            public Action<UnityEngine.EventSystems.PointerEventData> PointerExited;
            public RectTransform RectTransform;
            public Graphic Graphic;
            public Vector3 OriginalScale;
            public Color OriginalColor;
            public int Bet;
            public bool IsHovered;
        }

        [SerializeField] private RectTransform popupRoot;
        [SerializeField] private CanvasGroup popupGroup;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Transform optionContainer;

        private readonly List<OptionBinding> optionBindings = new();
        private Func<int, bool> selectionCallback;
        private string currencySymbol;
        private Camera uiCamera;
        private bool selectionInProgress;
        private Action<AudioClip> playSound;
        private AudioClip hoverSound;
        private AudioClip clickSound;

        private void Awake()
        {
            if (popupRoot == null)
                popupRoot = (RectTransform)transform;

            if (popupGroup == null)
                popupGroup = GetComponent<CanvasGroup>();

            if (popupGroup == null)
                popupGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void Open(
            Vector2 screenPosition,
            RectTransform parentCanvas,
            Camera cameraForCanvas,
            IReadOnlyList<int> bets,
            GameObject betOptionPrefab,
            string currency,
            Func<int, bool> onSelection,
            Action<AudioClip> soundPlayer,
            AudioClip hoverClip,
            AudioClip clickClip)
        {
            if (parentCanvas == null || bets == null || betOptionPrefab == null || optionContainer == null)
            {
                Debug.LogError("Bet popup needs a canvas, bet options, option prefab, and option container.", this);
                return;
            }

            uiCamera = cameraForCanvas;
            currencySymbol = currency;
            selectionCallback = onSelection;
            playSound = soundPlayer;
            hoverSound = hoverClip;
            clickSound = clickClip;
            selectionInProgress = false;
            gameObject.SetActive(true);
            if (titleText != null)
                titleText.text = "Choose your bet";
            BuildOptions(bets, betOptionPrefab);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)optionContainer);
            LayoutRebuilder.ForceRebuildLayoutImmediate(popupRoot);
            Canvas.ForceUpdateCanvases();
            popupRoot.SetAsLastSibling();
            popupRoot.gameObject.SetActive(true);
            float finalScale = PositionAt(screenPosition, parentCanvas);
            PlayOpenAnimation(finalScale);
        }

        public void Close()
        {
            selectionCallback = null;
            selectionInProgress = false;
            UnsubscribeOptions();

            gameObject.SetActive(false);
        }

        private void BuildOptions(IReadOnlyList<int> bets, GameObject betOptionPrefab)
        {
            UnsubscribeOptions();

            for (int i = optionContainer.childCount - 1; i >= 0; i--)
                Destroy(optionContainer.GetChild(i).gameObject);

            for (int i = 0; i < bets.Count; i++)
            {
                int bet = bets[i];
                GameObject option = Instantiate(betOptionPrefab, optionContainer, false);
                TMP_Text label = option.GetComponentInChildren<TMP_Text>();
                UIPointerHandler pointer = option.GetComponentInChildren<UIPointerHandler>();

                if (label == null || pointer == null)
                {
                    Debug.LogError("Each bet option prefab needs a TMP text and UIPointerHandler.", option);
                    Destroy(option);
                    continue;
                }

                label.text = $"{currencySymbol}{bet}";
                OptionBinding binding = new()
                {
                    Handler = pointer,
                    RectTransform = option.GetComponent<RectTransform>(),
                    Graphic = option.GetComponent<Graphic>(),
                    Bet = bet
                };
                binding.OriginalScale = binding.RectTransform.localScale;
                binding.OriginalColor = binding.Graphic != null ? binding.Graphic.color : Color.white;
                binding.Clicked = _ => SelectBet(binding);
                binding.PointerDown = _ => SetOptionPressed(binding, true);
                binding.PointerUp = _ => SetOptionPressed(binding, false);
                binding.PointerEntered = _ =>
                {
                    SetOptionHovered(binding, true);
                    playSound?.Invoke(hoverSound);
                };
                binding.PointerExited = _ => SetOptionHovered(binding, false);
                pointer.Clicked += binding.Clicked;
                pointer.PointerDown += binding.PointerDown;
                pointer.PointerUp += binding.PointerUp;
                pointer.PointerEntered += binding.PointerEntered;
                pointer.PointerExited += binding.PointerExited;
                optionBindings.Add(binding);
            }
        }

        private float PositionAt(Vector2 screenPosition, RectTransform parent)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parent, screenPosition, uiCamera, out Vector2 localPosition))
            {
                localPosition = Vector2.zero;
            }

            Rect bounds = GetSafeBounds(parent);
            if (bounds.width <= 0f || bounds.height <= 0f)
                bounds = parent.rect;

            Vector2 popupSize = popupRoot.rect.size;
            if (popupSize.x <= 0f || popupSize.y <= 0f)
            {
                Debug.LogError("Bet popup must have a non-zero size.", this);
                return 1f;
            }

            float safeMargin = Mathf.Min(8f, Mathf.Min(bounds.width, bounds.height) * 0.25f);
            bounds = Rect.MinMaxRect(
                bounds.xMin + safeMargin,
                bounds.yMin + safeMargin,
                bounds.xMax - safeMargin,
                bounds.yMax - safeMargin);
            if (bounds.width <= 0f || bounds.height <= 0f)
                bounds = parent.rect;

            float scale = Mathf.Min(
                1f,
                Mathf.Min(bounds.width / popupSize.x, bounds.height / popupSize.y));
            scale = Mathf.Max(0.001f, scale);
            popupRoot.anchorMin = new Vector2(0.5f, 0.5f);
            popupRoot.anchorMax = new Vector2(0.5f, 0.5f);
            popupRoot.pivot = new Vector2(0.5f, 0.5f);
            popupRoot.sizeDelta = popupSize;
            popupRoot.localScale = Vector3.one * scale;

            Vector2 halfSize = popupSize * (scale * 0.5f);
            float minX = bounds.xMin + halfSize.x;
            float maxX = bounds.xMax - halfSize.x;
            float minY = bounds.yMin + halfSize.y;
            float maxY = bounds.yMax - halfSize.y;
            localPosition.x = Mathf.Clamp(localPosition.x, minX, Mathf.Max(minX, maxX));
            localPosition.y = Mathf.Clamp(localPosition.y, minY, Mathf.Max(minY, maxY));
            popupRoot.anchoredPosition = localPosition;
            return scale;
        }

        private Rect GetSafeBounds(RectTransform parent)
        {
            Rect safeArea = Screen.safeArea;
            if (safeArea.width <= 0f || safeArea.height <= 0f)
                return parent.rect;

            Vector2 bottomLeftScreen = new(safeArea.xMin, safeArea.yMin);
            Vector2 topRightScreen = new(safeArea.xMax, safeArea.yMax);

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parent, bottomLeftScreen, uiCamera, out Vector2 bottomLeft) ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parent, topRightScreen, uiCamera, out Vector2 topRight))
            {
                return parent.rect;
            }

            return Rect.MinMaxRect(
                Mathf.Max(parent.rect.xMin, bottomLeft.x),
                Mathf.Max(parent.rect.yMin, bottomLeft.y),
                Mathf.Min(parent.rect.xMax, topRight.x),
                Mathf.Min(parent.rect.yMax, topRight.y));
        }

        private void PlayOpenAnimation(float finalScale)
        {
            popupGroup.alpha = 0f;
            popupRoot.localScale = Vector3.one * finalScale * 0.82f;
            StopAllCoroutines();
            StartCoroutine(AnimateOpen(finalScale));
        }

        private System.Collections.IEnumerator AnimateOpen(float finalScale)
        {
            const float duration = 0.18f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                popupGroup.alpha = t;
                popupRoot.localScale = Vector3.Lerp(
                    Vector3.one * finalScale * 0.82f,
                    Vector3.one * finalScale,
                    t);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            popupGroup.alpha = 1f;
            popupRoot.localScale = Vector3.one * finalScale;
        }

        private void SelectBet(OptionBinding binding)
        {
            if (selectionCallback == null || selectionInProgress)
                return;

            SetOptionPressed(binding, true);
            playSound?.Invoke(clickSound);
            bool accepted = selectionCallback.Invoke(binding.Bet);
            if (accepted)
            {
                selectionInProgress = true;
                if (titleText != null)
                    titleText.text = $"BET {currencySymbol}{binding.Bet} - GOOD LUCK!";
                StartCoroutine(CloseAfterSelection(binding));
            }
            else
                StartCoroutine(ShowRejectedSelection(binding));
        }

        private IEnumerator CloseAfterSelection(OptionBinding binding)
        {
            yield return new WaitForSecondsRealtime(0.24f);
            if (binding.RectTransform != null)
                Close();
        }

        private IEnumerator ShowRejectedSelection(OptionBinding binding)
        {
            if (binding.Graphic != null)
                binding.Graphic.color = new Color(1f, 0.35f, 0.35f, binding.OriginalColor.a);

            yield return new WaitForSecondsRealtime(0.22f);

            if (binding.RectTransform != null)
                SetOptionHovered(binding, binding.IsHovered);
        }

        private void SetOptionHovered(OptionBinding binding, bool hovered)
        {
            binding.IsHovered = hovered;
            if (binding.RectTransform == null)
                return;

            binding.RectTransform.localScale = binding.OriginalScale * (hovered ? 1.06f : 1f);
            if (binding.Graphic != null)
                binding.Graphic.color = hovered
                    ? Color.Lerp(binding.OriginalColor, Color.white, 0.18f)
                    : binding.OriginalColor;
        }

        private void SetOptionPressed(OptionBinding binding, bool pressed)
        {
            if (binding.RectTransform == null)
                return;

            float scale = pressed ? 0.94f : binding.IsHovered ? 1.06f : 1f;
            binding.RectTransform.localScale = binding.OriginalScale * scale;
            if (binding.Graphic != null)
                binding.Graphic.color = pressed
                    ? Color.Lerp(binding.OriginalColor, Color.white, 0.35f)
                    : binding.IsHovered
                        ? Color.Lerp(binding.OriginalColor, Color.white, 0.18f)
                        : binding.OriginalColor;
        }

        private void UnsubscribeOptions()
        {
            foreach (OptionBinding binding in optionBindings)
            {
                if (binding.Handler != null)
                {
                    binding.Handler.Clicked -= binding.Clicked;
                    binding.Handler.PointerDown -= binding.PointerDown;
                    binding.Handler.PointerUp -= binding.PointerUp;
                    binding.Handler.PointerEntered -= binding.PointerEntered;
                    binding.Handler.PointerExited -= binding.PointerExited;
                }
            }

            optionBindings.Clear();
        }
    }
}
