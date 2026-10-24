using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SlotMachine.UI
{
    public sealed class BettingPopupController : MonoBehaviour
    {
        private sealed class OptionBinding
        {
            public UIPointerHandler Handler;
            public Action<UnityEngine.EventSystems.PointerEventData> Callback;
        }

        [SerializeField] private RectTransform popupRoot;
        [SerializeField] private CanvasGroup popupGroup;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Transform optionContainer;

        private readonly List<OptionBinding> optionBindings = new();
        private Action<int> selectionCallback;
        private string currencySymbol;
        private Camera uiCamera;

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
            Action<int> onSelection)
        {
            if (parentCanvas == null || bets == null || betOptionPrefab == null || optionContainer == null)
            {
                Debug.LogError("Bet popup needs a canvas, bet options, option prefab, and option container.", this);
                return;
            }

            uiCamera = cameraForCanvas;
            currencySymbol = currency;
            selectionCallback = onSelection;
            gameObject.SetActive(true);
            if (titleText != null)
                titleText.text = "Choose your bet";
            BuildOptions(bets, betOptionPrefab);

            popupRoot.gameObject.SetActive(true);
            PositionAt(screenPosition);
            PlayOpenAnimation();
        }

        public void Close()
        {
            selectionCallback = null;
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
                Action<UnityEngine.EventSystems.PointerEventData> callback = _ => SelectBet(bet);
                pointer.Clicked += callback;
                optionBindings.Add(new OptionBinding { Handler = pointer, Callback = callback });
            }
        }

        private void PositionAt(Vector2 screenPosition)
        {
            RectTransform parent = (RectTransform)popupRoot.parent;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parent, screenPosition, uiCamera, out Vector2 localPosition))
            {
                localPosition = Vector2.zero;
            }

            Vector2 halfSize = popupRoot.rect.size * 0.5f;
            Rect bounds = parent.rect;
            localPosition.x = Mathf.Clamp(localPosition.x, bounds.xMin + halfSize.x, bounds.xMax - halfSize.x);
            localPosition.y = Mathf.Clamp(localPosition.y, bounds.yMin + halfSize.y, bounds.yMax - halfSize.y);
            popupRoot.anchoredPosition = localPosition;
        }

        private void PlayOpenAnimation()
        {
            popupGroup.alpha = 0f;
            popupRoot.localScale = Vector3.one * 0.82f;
            StopAllCoroutines();
            StartCoroutine(AnimateOpen());
        }

        private System.Collections.IEnumerator AnimateOpen()
        {
            const float duration = 0.18f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                popupGroup.alpha = t;
                popupRoot.localScale = Vector3.Lerp(Vector3.one * 0.82f, Vector3.one, t);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            popupGroup.alpha = 1f;
            popupRoot.localScale = Vector3.one;
        }

        private void SelectBet(int amount)
        {
            Action<int> callback = selectionCallback;
            Close();
            callback?.Invoke(amount);
        }

        private void UnsubscribeOptions()
        {
            foreach (OptionBinding binding in optionBindings)
            {
                if (binding.Handler != null)
                    binding.Handler.Clicked -= binding.Callback;
            }

            optionBindings.Clear();
        }
    }
}
