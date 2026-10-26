using System;
using System.Collections;
using System.Collections.Generic;
using SlotMachine.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace SlotMachine.Core
{
    // Handles bets, the wallet, messages, and sounds for the slot machine.
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
        private AudioSource backgroundMusicSource;
        private Canvas uiCanvas;
        private UIPointerHandler screenInput;
        private Coroutine walletAnimation;
        private int balance;
        private int currentBet;
        private Vector2 lastClickPosition;
        private Vector3 walletTextScale;
        private Color walletTextColor;

        // Checks the scene references and prepares the wallet, input, and audio.
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

            CreateScreenInput();

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;

            backgroundMusicSource = gameObject.AddComponent<AudioSource>();
            backgroundMusicSource.playOnAwake = false;
            backgroundMusicSource.loop = true;
            backgroundMusicSource.spatialBlend = 0f;
            backgroundMusicSource.clip = presentation.BackgroundMusic;
            backgroundMusicSource.volume = presentation.BackgroundMusicVolume;

            walletTextScale = walletText.rectTransform.localScale;
            walletTextColor = walletText.color;
            balance = Mathf.Max(0, game.Config.Betting.StartingBalance);
            currentBet = Mathf.Max(1, game.Config.Betting.MinimumBet);
            BuildBetOptions();
            RefreshWallet();
        }

        // Connects to the spin events and starts music when one is assigned.
        private void OnEnable()
        {
            if (screenInput != null)
                screenInput.Clicked += HandleScreenClick;

            if (backgroundMusicSource != null && backgroundMusicSource.clip != null &&
                !backgroundMusicSource.isPlaying)
                backgroundMusicSource.Play();

            if (game == null)
                return;

            game.SpinStarted += HandleSpinStarted;
            game.SpinCompleted += HandleSpinCompleted;
            game.ResultsGenerated += HandleResultsGenerated;
        }

        // Removes event links and stops music when this object is turned off.
        private void OnDisable()
        {
            if (game != null)
            {
                game.SpinStarted -= HandleSpinStarted;
                game.SpinCompleted -= HandleSpinCompleted;
                game.ResultsGenerated -= HandleResultsGenerated;
            }

            if (bettingPopup != null)
                bettingPopup.Close();

            if (screenInput != null)
                screenInput.Clicked -= HandleScreenClick;

            if (backgroundMusicSource != null)
                backgroundMusicSource.Stop();

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

        // Adds a clear area over the canvas to receive clicks.
        private void CreateScreenInput()
        {
            GameObject inputObject = new("SlotMachineScreenInput");
            RectTransform inputRect = inputObject.AddComponent<RectTransform>();
            inputRect.SetParent(uiCanvas.transform, false);
            inputRect.anchorMin = Vector2.zero;
            inputRect.anchorMax = Vector2.one;
            inputRect.offsetMin = Vector2.zero;
            inputRect.offsetMax = Vector2.zero;

            Image inputImage = inputObject.AddComponent<Image>();
            inputImage.color = Color.clear;
            inputImage.raycastTarget = true;

            screenInput = inputObject.AddComponent<UIPointerHandler>();
            screenInput.AllowRightClick = true;
            inputObject.layer = uiCanvas.gameObject.layer;
            inputRect.SetAsLastSibling();
        }

        // Opens the bet menu where the player clicked.
        private void HandleScreenClick(PointerEventData eventData)
        {
            if (eventData == null || game == null || game.IsSpinning)
                return;

            OpenBetPopup(eventData.position);
        }

        // Creates the bet menu the first time, then opens it.
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
                PlaceBet,
                PlaySound,
                presentation.ButtonHover,
                presentation.ButtonClick);
            PlaySound(presentation.PopupOpen);
        }

        // Checks the bet, starts the spin, and takes the bet from the wallet.
        private bool PlaceBet(int amount)
        {
            if (!betOptions.Contains(amount))
            {
                Debug.LogWarning($"The selected bet {amount} is not in the configured bet options.", this);
                return false;
            }

            if (balance < amount)
            {
                ShowNotification("Not enough money", presentation.LossColor, lastClickPosition);
                PlaySound(presentation.Lose);
                return false;
            }

            currentBet = amount;
            if (!game.TrySpin())
            {
                ShowNotification("Could not start the spin", presentation.MessageColor, lastClickPosition);
                return false;
            }

            balance -= currentBet;
            RefreshWallet();
            AnimateWallet(false);
            PlaySound(presentation.LeverPull);
            return true;
        }

        // Plays the reel sound when a spin begins.
        private void HandleSpinStarted()
        {
            PlaySound(presentation.ReelSpin);
        }

        // Saves the results so they can be checked when the reels stop.
        private void HandleResultsGenerated(IReadOnlyList<SlotMachineConfig.SymbolDefinition> results)
        {
            spinResults = results;
        }

        // Gives a payout or shows a loss message after the spin.
        private void HandleSpinCompleted()
        {
            int payout = CalculatePayout(spinResults, currentBet);
            if (payout <= 0)
            {
                ShowNotification("No win this time", presentation.MessageColor, lastClickPosition);
                PlaySound(presentation.Lose);
                return;
            }

            balance = (int)Math.Min((long)balance + payout, int.MaxValue);
            RefreshWallet();
            AnimateWallet(true);
            ShowNotification($"You win {presentation.CurrencySymbol}{payout}!", presentation.WinColor, lastClickPosition);
            PlaySound(payout >= (long)currentBet * 5 ? presentation.Jackpot : presentation.Win);
        }

        // Pays only when every reel has the same symbol.
        private int CalculatePayout(
            IReadOnlyList<SlotMachineConfig.SymbolDefinition> results,
            int bet)
        {
            if (results == null || results.Count < 2 || bet <= 0)
                return 0;

            SlotMachineConfig.SymbolDefinition centerSymbol = results[results.Count / 2];
            if (centerSymbol == null || string.IsNullOrWhiteSpace(centerSymbol.Id))
                return 0;

            foreach (SlotMachineConfig.SymbolDefinition symbol in results)
            {
                if (symbol == null || symbol.Id != centerSymbol.Id)
                    return 0;
            }

            if (centerSymbol.PayoutMultiplier <= 0)
                return 0;

            long payout = (long)bet * centerSymbol.PayoutMultiplier;
            return (int)Math.Min(payout, int.MaxValue);
        }

        // Builds the available bet amounts from the config settings.
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

        // Updates the wallet text after money changes.
        private void RefreshWallet()
        {
            walletText.text = $"{presentation.CurrencySymbol}{balance}";
        }

        // Starts the wallet animation for a win or a spent bet.
        private void AnimateWallet(bool won)
        {
            if (walletAnimation != null)
                StopCoroutine(walletAnimation);

            walletAnimation = StartCoroutine(AnimateWalletRoutine(won));
        }

        // Briefly changes the wallet size and color.
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

        // Creates a short message near the last click.
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

        // Plays a sound only when a clip is assigned.
        private void PlaySound(AudioClip clip)
        {
            if (clip != null)
                audioSource.PlayOneShot(clip);
        }

        // Gets the camera used by the UI canvas, if needed.
        private Camera GetUICamera()
        {
            return uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : uiCanvas.worldCamera;
        }
    }
}
