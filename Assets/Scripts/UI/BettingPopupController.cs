using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotMachine.UI
{
    public sealed class BettingPopupController : MonoBehaviour
    {
        [Header("Popup")]
        [SerializeField] private RectTransform popupRoot;
        [SerializeField] private CanvasGroup popupGroup;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Button optionButtonPrefab;
        [SerializeField] private Transform optionContainer;
        [SerializeField] private int[] betOptions = { 10, 25, 50, 100 };

        [Header("Animation")]
        [SerializeField] private float openDuration = 0.18f;

        private Action<int> selectionCallback;
        private Canvas parentCanvas;

        private void Awake()
        {
            if (popupRoot == null)
                popupRoot = GetComponent<RectTransform>();

            if (popupGroup == null)
                popupGroup = GetComponent<CanvasGroup>();

            if (parentCanvas == null)
                parentCanvas = GetComponentInParent<Canvas>();

            if (popupRoot != null)
                popupRoot.gameObject.SetActive(false);
        }

        public void Open(Vector2 screenPosition, Action<int> onSelection)
        {
            selectionCallback = onSelection;
            RefreshButtons();

            if (popupRoot == null)
                return;

            popupRoot.gameObject.SetActive(true);

            Vector2 localPosition;
            if (parentCanvas != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)popupRoot.parent,
                    screenPosition,
                    parentCanvas.worldCamera,
                    out var point);
                localPosition = point + new Vector2(0f, 100f);
            }
            else
            {
                localPosition = new Vector2(0f, 100f);
            }

            popupRoot.anchoredPosition = localPosition;
            StartCoroutine(AnimateOpen());
        }

        public void Close()
        {
            if (popupRoot == null)
                return;

            popupRoot.gameObject.SetActive(false);
            selectionCallback = null;
        }

        private void RefreshButtons()
        {
            if (optionButtonPrefab == null || optionContainer == null)
                return;

            for (int i = optionContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(optionContainer.GetChild(i).gameObject);
            }

            for (int i = 0; i < betOptions.Length; i++)
            {
                var button = Instantiate(optionButtonPrefab, optionContainer);
                var text = button.GetComponentInChildren<TMP_Text>();
                if (text != null)
                    text.text = $"${betOptions[i]}";

                int optionValue = betOptions[i];
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    selectionCallback?.Invoke(optionValue);
                    Close();
                });
            }
        }

        private IEnumerator AnimateOpen()
        {
            if (popupGroup == null)
                yield break;

            popupGroup.alpha = 0f;
            popupRoot.localScale = Vector3.one * 0.75f;

            float elapsed = 0f;
            while (elapsed < openDuration)
            {
                float t = Mathf.Clamp01(elapsed / openDuration);
                popupGroup.alpha = Mathf.Lerp(0f, 1f, Mathf.SmoothStep(0f, 1f, t));
                popupRoot.localScale = Vector3.Lerp(Vector3.one * 0.75f, Vector3.one, Mathf.SmoothStep(0f, 1f, t));
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            popupGroup.alpha = 1f;
            popupRoot.localScale = Vector3.one;
        }
    }
}
