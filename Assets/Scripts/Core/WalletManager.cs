using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace SlotMachine.Core
{
    public sealed class WalletManager : MonoBehaviour
    {
        [Header("Wallet")]
        [SerializeField] private int startingBalance = 1000;
        [SerializeField] private TMP_Text balanceText;
        [SerializeField] private string currencySymbol = "$";

        [Header("Animation")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color positiveColor = new(0.31f, 1f, 0.47f, 1f);
        [SerializeField] private Color negativeColor = new(1f, 0.33f, 0.33f, 1f);
        [SerializeField] private Vector3 pulseScale = new(1.12f, 1.12f, 1.12f);
        [SerializeField] private float pulseDuration = 0.2f;

        public int Balance { get; private set; }
        public int CurrentBet { get; private set; }

        public event Action<int> BalanceChanged;
        public event Action<int> BetChanged;

        private Coroutine pulseRoutine;

        private void Awake()
        {
            ResetWallet();
        }

        public void ResetWallet()
        {
            Balance = Mathf.Max(0, startingBalance);
            CurrentBet = 0;
            BalanceChanged?.Invoke(Balance);
            BetChanged?.Invoke(CurrentBet);
            RefreshDisplay();
        }

        public bool CanAfford(int amount)
        {
            return amount > 0 && Balance >= amount;
        }

        public bool TryPlaceBet(int amount)
        {
            if (!CanAfford(amount))
                return false;

            Balance -= amount;
            CurrentBet = amount;
            BalanceChanged?.Invoke(Balance);
            BetChanged?.Invoke(CurrentBet);
            RefreshDisplay();
            PulseText(-amount, false);
            return true;
        }

        public void SetCurrentBet(int amount)
        {
            CurrentBet = Mathf.Max(0, amount);
            BetChanged?.Invoke(CurrentBet);
        }

        public void AddWin(int amount)
        {
            if (amount <= 0)
                return;

            Balance += amount;
            CurrentBet = 0;
            BalanceChanged?.Invoke(Balance);
            BetChanged?.Invoke(CurrentBet);
            RefreshDisplay();
            PulseText(amount, true);
        }

        public void SetBalanceText(TMP_Text text)
        {
            balanceText = text;
            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            if (balanceText == null)
                return;

            balanceText.text = $"{currencySymbol}{Balance}";
        }

        private void PulseText(int delta, bool isWin)
        {
            if (balanceText == null)
                return;

            if (pulseRoutine != null)
                StopCoroutine(pulseRoutine);

            var original = balanceText.color;
            var targetColor = isWin ? positiveColor : negativeColor;
            pulseRoutine = StartCoroutine(PulseRoutine(original, targetColor, delta));
        }

        private IEnumerator PulseRoutine(Color originalColor, Color targetColor, int delta)
        {
            var startScale = balanceText.rectTransform.localScale;
            var endScale = pulseScale;
            float elapsed = 0f;

            while (elapsed < pulseDuration)
            {
                float t = Mathf.Clamp01(elapsed / pulseDuration);
                balanceText.rectTransform.localScale = Vector3.Lerp(startScale, endScale, t);
                balanceText.color = Color.Lerp(originalColor, targetColor, t);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            balanceText.rectTransform.localScale = startScale;
            balanceText.color = originalColor;
            balanceText.text = $"{currencySymbol}{Balance}";
            pulseRoutine = null;
        }
    }
}
