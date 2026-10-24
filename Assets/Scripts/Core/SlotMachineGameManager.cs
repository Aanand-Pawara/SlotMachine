using System;
using System.Collections;
using System.Collections.Generic;
using SlotMachine.UI;
using TMPro;
using UnityEngine;

namespace SlotMachine.Core
{
    [DisallowMultipleComponent]
    public sealed class SlotMachineGameManager : MonoBehaviour
    {
        [SerializeField] private SlotGameController game;
        [SerializeField] private SlotMachinePresentationConfig presentation;
        [SerializeField] private TMP_Text walletText;

        private readonly List<int> betOptions = new();
        private IReadOnlyList<SlotMachineConfig.SymbolDefinition> spinResults;
        private BettingPopupController bettingPopup;
        private AudioSource audioSource;
        private Canvas uiCanvas;
        private Coroutine walletAnimation;
        private int balance;
        private int currentBet;
        private Vector2 lastClickPosition;
        private Vector3 walletTextScale;
        private Color walletTextColor;

        private void Awake()
        {
            if (game == null || game.Config == null || presentation == null || walletText == null)
            {
                Debug.LogError("Assign a game controller, its config, a presentation config, and the wallet TMP text.", this);
                enabled = false;
                return;
            }

            uiCanvas = walletText.canvas;
            if (uiCanvas == null)
            {
                Debug.LogError("The wallet text must be inside a Canvas.", walletText);
                enabled = false;
                return;
            }

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;

            walletTextScale = walletText.rectTransform.localScale;
            walletTextColor = walletText.color;
            balance = Mathf.Max(0, game.Config.Betting.StartingBalance);
            currentBet = Mathf.Max(1, game.Config.Betting.MinimumBet);
            BuildBetOptions();
            RefreshWallet();
        }

        private void OnEnable()
        {
            if (game == null)
                return;

            game.BetRequested += OpenBetPopup;
            game.SpinStarted += HandleSpinStarted;
            game.SpinCompleted += HandleSpinCompleted;
            game.ResultsGenerated += HandleResultsGenerated;
        }

        private void OnDisable()
        {
            if (game != null)
            {
                game.BetRequested -= OpenBetPopup;
                game.SpinStarted -= HandleSpinStarted;
                game.SpinCompleted -= HandleSpinCompleted;
                game.ResultsGenerated -= HandleResultsGenerated;
            }

            if (bettingPopup != null)
                bettingPopup.Close();

            if (walletAnimation != null)
            {
                StopCoroutine(walletAnimation);
                walletAnimation = null;
            }

            if (walletText != null)
            {
                walletText.rectTransform.localScale = walletTextScale;
                walletText.color = walletTextColor;
            }
        }

        private void OpenBetPopup(Vector2 screenPosition)
        {
            lastClickPosition = screenPosition;

            if (presentation.BettingPopupPrefab == null || presentation.BetOptionPrefab == null)
            {
                Debug.LogError("Assign a betting popup and bet option prefab in the presentation config.", this);
                return;
            }

            if (bettingPopup == null)
            {
                bettingPopup = Instantiate(presentation.BettingPopupPrefab, uiCanvas.transform);
                bettingPopup.gameObject.SetActive(false);
            }

            bettingPopup.Open(
                screenPosition,
                (RectTransform)uiCanvas.transform,
                GetUICamera(),
                betOptions,
                presentation.BetOptionPrefab,
                presentation.CurrencySymbol,
                PlaceBet);
            PlaySound(presentation.ButtonClick);
        }

        private void PlaceBet(int amount)
        {
            if (!betOptions.Contains(amount))
            {
                Debug.LogWarning($"The selected bet {amount} is not in the configured bet options.", this);
                return;
            }

            if (balance < amount)
            {
                ShowNotification("Not enough money", presentation.LossColor, lastClickPosition);
                PlaySound(presentation.ReelStop);
                return;
            }

            currentBet = amount;
            if (!game.TrySpin())
                return;

            balance -= currentBet;
            RefreshWallet();
            AnimateWallet(false);
            PlaySound(presentation.LeverPull);
        }

        private void HandleSpinStarted()
        {
            PlaySound(presentation.ReelSpin);
        }

        private void HandleResultsGenerated(IReadOnlyList<SlotMachineConfig.SymbolDefinition> results)
        {
            spinResults = results;
        }

        private void HandleSpinCompleted()
        {
            int payout = CalculatePayout(spinResults, currentBet);
            if (payout <= 0)
            {
                ShowNotification("No win this time", presentation.MessageColor, lastClickPosition);
                PlaySound(presentation.ReelStop);
                return;
            }

            balance = (int)Math.Min((long)balance + payout, int.MaxValue);
            RefreshWallet();
            AnimateWallet(true);
            ShowNotification($"You win {presentation.CurrencySymbol}{payout}!", presentation.WinColor, lastClickPosition);
            PlaySound(payout >= (long)currentBet * 5 ? presentation.Jackpot : presentation.Win);
        }

        private int CalculatePayout(
            IReadOnlyList<SlotMachineConfig.SymbolDefinition> results,
            int bet)
        {
            if (results == null || results.Count == 0 || bet <= 0)
                return 0;

            SlotMachineConfig.SymbolDefinition centerSymbol = results[results.Count / 2];
            if (centerSymbol == null)
                return 0;

            int matches = 0;
            foreach (SlotMachineConfig.SymbolDefinition symbol in results)
            {
                if (symbol != null && symbol.Id == centerSymbol.Id)
                    matches++;
            }

            if (matches < 2 || centerSymbol.PayoutMultiplier <= 0)
                return 0;

            long payout = (long)bet * centerSymbol.PayoutMultiplier;
            return (int)Math.Min(payout, int.MaxValue);
        }

        private void BuildBetOptions()
        {
            SlotMachineConfig.BettingSettings settings = game.Config.Betting;
            int minimum = Mathf.Max(1, settings.MinimumBet);
            int maximum = Mathf.Max(minimum, settings.MaximumBet);
            int step = Mathf.Max(1, settings.BetStep);

            betOptions.Clear();
            for (long amount = minimum; amount <= maximum; amount += step)
                betOptions.Add((int)amount);

            if (betOptions.Count == 0 || betOptions[betOptions.Count - 1] != maximum)
                betOptions.Add(maximum);
        }

        private void RefreshWallet()
        {
            walletText.text = $"{presentation.CurrencySymbol}{balance}";
        }

        private void AnimateWallet(bool won)
        {
            if (walletAnimation != null)
                StopCoroutine(walletAnimation);

            walletAnimation = StartCoroutine(AnimateWalletRoutine(won));
        }

        private IEnumerator AnimateWalletRoutine(bool won)
        {
            const float duration = 0.24f;
            Color flashColor = won ? presentation.WinColor : presentation.LossColor;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float pulse = Mathf.Sin(t * Mathf.PI);
                walletText.rectTransform.localScale = Vector3.Lerp(
                    walletTextScale, walletTextScale * (1f + 0.16f * pulse), pulse);
                walletText.color = Color.Lerp(walletTextColor, flashColor, pulse);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            walletText.rectTransform.localScale = walletTextScale;
            walletText.color = walletTextColor;
            walletAnimation = null;
        }

        private void ShowNotification(string message, Color color, Vector2 screenPosition)
        {
            if (presentation.NotificationPrefab == null)
                return;

            FloatingNotificationController notification = Instantiate(
                presentation.NotificationPrefab, uiCanvas.transform);
            notification.Show(
                message,
                color,
                (RectTransform)uiCanvas.transform,
                GetUICamera(),
                screenPosition,
                presentation.NotificationDuration);
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null)
                audioSource.PlayOneShot(clip);
        }

        private Camera GetUICamera()
        {
            return uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : uiCanvas.worldCamera;
        }
    }
}
