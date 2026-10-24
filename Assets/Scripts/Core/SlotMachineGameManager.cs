using System;
using System.Collections.Generic;
using SlotMachine.Audio;
using SlotMachine.UI;
using TMPro;
using UnityEngine;

namespace SlotMachine.Core
{
    public sealed class SlotMachineGameManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SlotGameController slotGameController;
        [SerializeField] private Lever lever;
        [SerializeField] private WalletManager walletManager;
        [SerializeField] private BettingPopupController bettingPopup;
        [SerializeField] private FloatingNotificationController floatingNotifications;
        [SerializeField] private SoundManager soundManager;
        [SerializeField] private TMP_Text walletText;

        [Header("Betting")]
        [SerializeField] private int defaultBet = 10;

        private int currentBet;
        private IReadOnlyList<SlotMachineConfig.SymbolDefinition> currentResults;
        private Vector2 lastPointerScreenPosition;

        private void Awake()
        {
            if (walletManager != null)
            {
                if (walletText != null)
                    walletManager.SetBalanceText(walletText);

                walletManager.ResetWallet();
            }

            if (slotGameController != null)
            {
                slotGameController.SpinStarted += HandleSpinStarted;
                slotGameController.SpinCompleted += HandleSpinCompleted;
                slotGameController.ResultsGenerated += HandleResultsGenerated;
            }

            if (lever != null)
                lever.ActivationRequested += HandleLeverRequest;
        }

        private void OnDestroy()
        {
            if (slotGameController != null)
            {
                slotGameController.SpinStarted -= HandleSpinStarted;
                slotGameController.SpinCompleted -= HandleSpinCompleted;
                slotGameController.ResultsGenerated -= HandleResultsGenerated;
            }

            if (lever != null)
                lever.ActivationRequested -= HandleLeverRequest;
        }

        private void Start()
        {
            currentBet = Mathf.Max(1, defaultBet);
            if (walletManager != null)
                walletManager.SetCurrentBet(currentBet);
        }

        private void HandleLeverRequest(Lever targetLever, Vector2 screenPosition)
        {
            lastPointerScreenPosition = screenPosition;

            if (slotGameController == null || slotGameController.IsSpinning)
                return;

            if (bettingPopup != null)
            {
                bettingPopup.Open(screenPosition, OnBetSelected);
                return;
            }

            OnBetSelected(defaultBet);
        }

        private void OnBetSelected(int betAmount)
        {
            if (walletManager == null)
            {
                currentBet = Mathf.Max(1, betAmount);
                TriggerSpin();
                return;
            }

            if (!walletManager.CanAfford(betAmount))
            {
                ShowNotification("Not enough money!", new Color(1f, 0.35f, 0.35f), lastPointerScreenPosition);
                if (soundManager != null)
                    soundManager.PlayNoMoney();
                return;
            }

            currentBet = Mathf.Max(1, betAmount);
            walletManager.SetCurrentBet(currentBet);

            if (soundManager != null)
                soundManager.PlayButtonClick();

            if (!walletManager.TryPlaceBet(currentBet))
            {
                ShowNotification("Not enough money!", new Color(1f, 0.35f, 0.35f), lastPointerScreenPosition);
                if (soundManager != null)
                    soundManager.PlayNoMoney();
                return;
            }

            if (soundManager != null)
                soundManager.PlayLeverPull();

            if (lever != null)
                lever.TryActivate();
        }

        private void TriggerSpin()
        {
            if (slotGameController == null)
                return;

            if (walletManager != null)
            {
                if (!walletManager.TryPlaceBet(currentBet))
                {
                    ShowNotification("Not enough money!", new Color(1f, 0.35f, 0.35f), lastPointerScreenPosition);
                    if (soundManager != null)
                        soundManager.PlayNoMoney();
                    return;
                }
            }

            if (soundManager != null)
                soundManager.PlayLeverPull();

            if (lever != null)
                lever.TryActivate();
        }

        private void HandleSpinStarted()
        {
            if (soundManager != null)
                soundManager.PlayReelSpin();
        }

        private void HandleResultsGenerated(IReadOnlyList<SlotMachineConfig.SymbolDefinition> results)
        {
            currentResults = results;
        }

        private void HandleSpinCompleted()
        {
            int winAmount = CalculatePayout(currentResults, currentBet);

            if (winAmount > 0)
            {
                if (walletManager != null)
                    walletManager.AddWin(winAmount);

                ShowNotification($"You win ${winAmount}!", new Color(0.3f, 1f, 0.5f), lastPointerScreenPosition);

                if (soundManager != null)
                {
                    if (winAmount >= currentBet * 5)
                        soundManager.PlayJackpot();
                    else
                        soundManager.PlayWin();
                }
            }
            else
            {
                ShowNotification("No win this time", new Color(0.9f, 0.75f, 0.3f), lastPointerScreenPosition);
                if (soundManager != null)
                    soundManager.PlayReelStop();
            }
        }

        private int CalculatePayout(IReadOnlyList<SlotMachineConfig.SymbolDefinition> results, int bet)
        {
            if (results == null || results.Count == 0 || bet <= 0)
                return 0;

            bool allSame = true;
            string firstId = results[0].Id;

            for (int i = 1; i < results.Count; i++)
            {
                if (results[i].Id != firstId)
                {
                    allSame = false;
                    break;
                }
            }

            if (allSame)
            {
                int multiplier = results[0].PayoutMultiplier;
                return bet * multiplier;
            }

            int centerIndex = results.Count / 2;
            var center = results[centerIndex];
            int matches = 0;

            foreach (var symbol in results)
            {
                if (symbol != null && symbol.Id == center.Id)
                    matches++;
            }

            if (matches >= 2)
                return bet * center.PayoutMultiplier;

            return 0;
        }

        private void ShowNotification(string message, Color color, Vector2 screenPosition)
        {
            if (floatingNotifications == null)
                return;

            floatingNotifications.Show(message, color, screenPosition);
        }
    }
}
