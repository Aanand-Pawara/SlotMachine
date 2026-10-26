
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SlotMachine.Core;

namespace SlotMachine.UI
{
    // Builds the symbols for one reel, moves them during a spin, and shows its final result.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(RectMask2D))]
    public sealed class SlotReel : MonoBehaviour
    {
        [Header("Appearance")]
        [SerializeField] private Sprite fallbackSprite;
        [SerializeField] private bool preserveAspect = true;

        // Other scripts can listen to these events to react when this reel starts or stops.
        public event Action<SlotReel> SpinStarted;
        public event Action<SlotReel, SlotMachineConfig.SymbolDefinition> SpinCompleted;

        public bool IsSpinning { get; private set; }
        public bool IsInitialized => initialized;
        public SlotMachineConfig.SymbolDefinition CurrentResult { get; private set; }

        private RectTransform viewport;
        private readonly List<Image> symbolViews = new();

        private Coroutine spinRoutine;
        private bool initialized;
        private SlotMachineConfig config;
        private float symbolHeight;
        private float scrollOffset;
        private float currentSpeed;
        private int visibleRows;
        private int poolSize;

        private void Awake()
        {
            viewport = GetComponent<RectTransform>();
        }

        private void OnDisable()
        {
            StopSpinRoutine();
            IsSpinning = false;
            currentSpeed = 0f;
        }

        private void OnDestroy()
        {
            SpinStarted = null;
            SpinCompleted = null;
        }

        // Starts a spin and stops on the chosen target symbol.
        public void SpinTo(SlotMachineConfig.SymbolDefinition target)
        {
            if (!isActiveAndEnabled || !initialized || IsSpinning)
            {
                Debug.LogWarning($"[{nameof(SlotReel)}] Cannot start spin on '{name}'.", this);
                return;
            }

            if (!IsSelectable(target))
            {
                Debug.LogError($"[{nameof(SlotReel)}] Invalid target symbol.", this);
                return;
            }

            StopSpinRoutine();
            CurrentResult = target;
            spinRoutine = StartCoroutine(SpinRoutine(target));
        }

        // Stops the reel immediately in case the game needs to skip the animation.
        public void StopImmediately()
        {
            if (!IsSpinning)
                return;

            StopSpinRoutine();
            IsSpinning = false;
            currentSpeed = 0f;
            scrollOffset = 0f;
            ApplyPositions();
            ShowCenterSymbol(CurrentResult);
            SpinCompleted?.Invoke(this, CurrentResult);
        }

        // Replaces the config used for this reel.
        public void SetConfig(SlotMachineConfig newConfig)
        {
            if (IsSpinning)
            {
                Debug.LogWarning("Cannot change reel configuration during a spin.", this);
                return;
            }

            if (newConfig == null || !newConfig.HasValidSymbols || newConfig.Reels == null)
            {
                Debug.LogError("Cannot assign an invalid SlotMachineConfig.", this);
                return;
            }

            if (viewport == null)
                viewport = GetComponent<RectTransform>();

            config = newConfig;
            RebuildPool();
        }

        // Creates the list of symbol images used by this reel.
        // Clears old images and builds a fresh symbol pool.
        private void RebuildPool()
        {
            StopSpinRoutine();
            IsSpinning = false;
            initialized = false;

            foreach (Image view in symbolViews)
            {
                if (view != null)
                {
                    view.sprite = null;
                    Destroy(view.gameObject);
                }
            }

            symbolViews.Clear();

            if (config == null || viewport == null)
                return;

            visibleRows = config.Reels.VisibleRows;
            symbolHeight = config.Reels.SymbolHeight;

            if (visibleRows < 1 || symbolHeight <= 0f)
                return;

            float viewportHeight = viewport.rect.height;
            int requiredVisibleRows = Mathf.Max(
                visibleRows,
                Mathf.CeilToInt(viewportHeight / symbolHeight));

            poolSize = requiredVisibleRows + config.Reels.BufferRows * 2;

            if (poolSize % 2 == 0)
                poolSize++;

            for (int i = 0; i < poolSize; i++)
            {
                GameObject symbolObject = new($"Symbol_{i:00}");
                RectTransform rect = symbolObject.AddComponent<RectTransform>();
                Image image = symbolObject.AddComponent<Image>();

                rect.SetParent(viewport, false);
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(viewport.rect.width, symbolHeight);

                image.raycastTarget = false;
                image.preserveAspect = preserveAspect;
                image.sprite = GetRandomSymbol()?.Sprite ?? fallbackSprite;

                symbolViews.Add(image);
            }

            scrollOffset = 0f;
            ApplyPositions();
            initialized = symbolViews.Count == poolSize;
        }

        // Controls the spin animation.
        private IEnumerator SpinRoutine(SlotMachineConfig.SymbolDefinition target)
        {
            IsSpinning = true;
            currentSpeed = 0f;
            SpinStarted?.Invoke(this);

            float elapsed = 0f;

            while (elapsed < config.Reels.AccelerationDuration)
            {
                float deltaTime = GetDeltaTime();
                elapsed += deltaTime;
                float t = Mathf.Clamp01(elapsed / config.Reels.AccelerationDuration);
                currentSpeed = config.Reels.CruiseSpeed * t;
                AdvanceReel(currentSpeed * deltaTime);
                yield return null;
            }

            currentSpeed = config.Reels.CruiseSpeed;

            elapsed = 0f;
            while (elapsed < config.Reels.CruiseDuration)
            {
                float deltaTime = GetDeltaTime();
                elapsed += deltaTime;
                AdvanceReel(currentSpeed * deltaTime);
                yield return null;
            }


            float distanceToBoundary = scrollOffset <= 0.001f
                ? symbolHeight
                : symbolHeight - scrollOffset;
            float minimumDistance =
                distanceToBoundary + config.Reels.ExtraStopCycles * symbolHeight;
            float cruiseSpeed = Mathf.Max(0.01f, config.Reels.CruiseSpeed);
            float minimumDistanceForDuration =
                cruiseSpeed * config.Reels.DecelerationDuration * 0.5f;
            float distanceToTravel = Mathf.Max(minimumDistance, minimumDistanceForDuration);
            float extraCycles = Mathf.Ceil(
                Mathf.Max(0f, distanceToTravel - distanceToBoundary) / symbolHeight);
            float decelerationDistance = distanceToBoundary + extraCycles * symbolHeight;
            float decelerationDuration = decelerationDistance * 2f / cruiseSpeed;
            float traveledDistance = 0f;
            elapsed = 0f;

            while (elapsed < decelerationDuration)
            {
                float dt = GetDeltaTime();
                elapsed += dt;

                float t = Mathf.Clamp01(elapsed / decelerationDuration);
                float eased = 2f * t - t * t;
                float desiredDistance = decelerationDistance * eased;
                float delta = desiredDistance - traveledDistance;
                traveledDistance = desiredDistance;

                AdvanceReel(delta);
                yield return null;
            }

            float remainingDistance = decelerationDistance - traveledDistance;
            if (remainingDistance > 0f)
                AdvanceReel(remainingDistance);

            scrollOffset = 0f;
            ApplyPositions();

            // The final symbol was already chosen before the animation started.
            // We now show it in the center row.
            ShowCenterSymbol(target);

            currentSpeed = 0f;
            IsSpinning = false;
            spinRoutine = null;

            SpinCompleted?.Invoke(this, target);
        }

        // Moves the reel by a given distance and wraps symbols at the top.
        private void AdvanceReel(float distance)
        {
            if (distance <= 0f)
                return;

            scrollOffset += distance;

            while (scrollOffset >= symbolHeight)
            {
                scrollOffset -= symbolHeight;
                RecycleTopSymbol();
            }

            ApplyPositions();
        }

        // Moves the first symbol to the end of the list and gives it a new random sprite.
        private void RecycleTopSymbol()
        {
            if (symbolViews.Count == 0)
                return;

            Image top = symbolViews[0];
            symbolViews.RemoveAt(0);
            symbolViews.Add(top);

            top.sprite = GetRandomSymbol()?.Sprite ?? fallbackSprite;
        }

        // Positions each symbol image in a vertical list.
        private void ApplyPositions()
        {
            if (symbolViews.Count == 0)
                return;

            float centerIndex = (symbolViews.Count - 1) * 0.5f;

            for (int i = 0; i < symbolViews.Count; i++)
            {
                RectTransform rect = symbolViews[i].rectTransform;
                float y = (centerIndex - i) * symbolHeight + scrollOffset;

                rect.anchoredPosition = new Vector2(0f, y);
                rect.sizeDelta = new Vector2(viewport.rect.width, symbolHeight);
            }
        }

        // Sets the center symbol to the chosen result.
        private void ShowCenterSymbol(SlotMachineConfig.SymbolDefinition target)
        {
            if (symbolViews.Count == 0 || target == null)
                return;

            int centerIndex = symbolViews.Count / 2;
            symbolViews[centerIndex].sprite = target.Sprite;
        }

        private SlotMachineConfig.SymbolDefinition lastDisplayedSymbol;

        // Chooses a symbol weighted by its drop chance.
        private SlotMachineConfig.SymbolDefinition GetRandomSymbol()
        {
            IReadOnlyList<SlotMachineConfig.SymbolDefinition> symbols = config.Symbols;

            float totalWeight = 0f;

            foreach (var symbol in symbols)
            {
                if (!IsSelectable(symbol))
                    continue;

                if (symbols.Count > 1 && symbol == lastDisplayedSymbol)
                    continue;

                totalWeight += symbol.Weight;
            }

            // If only one selectable symbol exists, allow it to repeat.
            if (totalWeight <= 0f)
                return GetLastValidSymbol();

            float roll = UnityEngine.Random.value * totalWeight;
            float cumulative = 0f;

            foreach (var symbol in symbols)
            {
                if (!IsSelectable(symbol))
                    continue;

                if (symbols.Count > 1 && symbol == lastDisplayedSymbol)
                    continue;

                cumulative += symbol.Weight;

                if (roll < cumulative)
                {
                    lastDisplayedSymbol = symbol;
                    return symbol;
                }
            }

            return GetLastValidSymbol();
        }

        // Finds a usable symbol if the weighted choice needs a fallback.
        private SlotMachineConfig.SymbolDefinition GetLastValidSymbol()
        {
            foreach (var symbol in config.Symbols)
            {
                if (IsSelectable(symbol))
                {
                    lastDisplayedSymbol = symbol;
                    return symbol;
                }
            }

            return null;
        }

        private static bool IsSelectable(SlotMachineConfig.SymbolDefinition symbol)
        {
            return symbol != null &&
                   symbol.Sprite != null &&
                   symbol.Weight > 0f &&
                   !string.IsNullOrWhiteSpace(symbol.Id);
        }

        // Uses real time so the reel can still animate when game time is paused.
        private float GetDeltaTime()
        {
            return Time.unscaledDeltaTime;
        }

        // Stops the animation coroutine when the reel is disabled or reset.
        private void StopSpinRoutine()
        {
            if (spinRoutine == null)
                return;

            StopCoroutine(spinRoutine);
            spinRoutine = null;
        }

        // Repositions the symbols if the reel view changes size.
        private void OnRectTransformDimensionsChange()
        {
            if (!initialized || IsSpinning || viewport == null)
                return;

            ApplyPositions();
        }
    }
}
