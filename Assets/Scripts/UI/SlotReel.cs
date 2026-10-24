
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// This script controls one reel in the slot machine.
// It creates the symbol pool, rotates symbols during the spin,
// and stops at the correct result for that reel.
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform), typeof(RectMask2D))]
public sealed class SlotReel : MonoBehaviour
{
    private enum ReelState
    {
        Idle,
        Accelerating,
        Cruising,
        Decelerating
    }

    [Header("Configuration")]
    [SerializeField] private SlotMachineConfig config;
    [SerializeField, Min(1)] private int extraBufferRows = 2;

    [Header("Appearance")]
    [SerializeField] private Sprite fallbackSprite;
    [SerializeField] private bool preserveAspect = true;

    [Header("Animation")]
    [SerializeField, Min(1f)] private float minimumCruiseSpeed = 1800f;
    [SerializeField, Min(0.01f)] private float accelerationDuration = 0.25f;
    [SerializeField, Min(0f)] private float minimumCruiseDuration = 0.8f;
    [SerializeField, Min(0.01f)] private float decelerationDuration = 0.55f;
    [SerializeField, Min(1)] private int extraStopCycles = 2;

    // Fired when this reel starts spinning.
    public event Action<SlotReel> SpinStarted;

    // Fired when this reel reaches its final symbol result.
    public event Action<SlotReel, SlotMachineConfig.SymbolDefinition> SpinCompleted;

    public bool IsSpinning { get; private set; }
    public bool IsInitialized => initialized;
    public SlotMachineConfig.SymbolDefinition CurrentResult { get; private set; }

    private RectTransform viewport;
    private readonly List<Image> symbolViews = new();

    private ReelState state = ReelState.Idle;
    private Coroutine spinRoutine;
    private bool initialized;
    private float symbolHeight;
    private float scrollOffset;
    private float currentSpeed;
    private int visibleRows;
    private int poolSize;

    private void Awake()
    {
        viewport = GetComponent<RectTransform>();

        if (config == null)
        {
            Debug.LogError($"[{nameof(SlotReel)}] Assign a SlotMachineConfig on '{name}'.", this);
            enabled = false;
            return;
        }

        if (!config.HasValidSymbols)
        {
            Debug.LogError($"[{nameof(SlotReel)}] Configuration contains no valid symbols.", this);
            enabled = false;
            return;
        }

        InitializePool();
    }

    private void OnDisable()
    {
        StopSpinRoutine();
        IsSpinning = false;
        currentSpeed = 0f;
        state = ReelState.Idle;
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
        state = ReelState.Idle;
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

        if (newConfig == null || !newConfig.HasValidSymbols)
        {
            Debug.LogError("Cannot assign an invalid SlotMachineConfig.", this);
            return;
        }

        config = newConfig;
        RebuildPool();
    }

    // Creates the list of symbol images used by this reel.
    private void InitializePool()
    {
        if (config == null)
            return;

        visibleRows = config.Reels.VisibleRows;
        symbolHeight = config.Reels.SymbolHeight;

        if (symbolHeight <= 0f || visibleRows <= 0)
        {
            Debug.LogError("Symbol height and visible row count must be positive.", this);
            enabled = false;
            return;
        }

        RebuildPool();
    }

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

        poolSize = requiredVisibleRows + extraBufferRows * 2;

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
        state = ReelState.Accelerating;
        currentSpeed = 0f;
        SpinStarted?.Invoke(this);

        float elapsed = 0f;

        while (elapsed < accelerationDuration)
        {
            elapsed += GetDeltaTime();
            float t = Mathf.Clamp01(elapsed / accelerationDuration);
            currentSpeed = minimumCruiseSpeed * Mathf.SmoothStep(0f, 1f, t);
            AdvanceReel(currentSpeed * GetDeltaTime());
            yield return null;
        }

        currentSpeed = minimumCruiseSpeed;
        state = ReelState.Cruising;

        elapsed = 0f;
        while (elapsed < minimumCruiseDuration)
        {
            elapsed += GetDeltaTime();
            AdvanceReel(currentSpeed * GetDeltaTime());
            yield return null;
        }

        state = ReelState.Decelerating;

        // Travel far enough to stop at the next exact symbol boundary.
        float distanceToBoundary = scrollOffset <= 0.001f
            ? symbolHeight
            : symbolHeight - scrollOffset;

        float decelerationDistance =
            distanceToBoundary + extraStopCycles * symbolHeight;

        float startOffset = scrollOffset;
        float remainingDistance = decelerationDistance;
        elapsed = 0f;

        // Integrate a smooth deceleration curve over a fixed duration.
        while (elapsed < decelerationDuration)
        {
            float dt = GetDeltaTime();
            elapsed += dt;

            float t = Mathf.Clamp01(elapsed / decelerationDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);

            float desiredOffset = startOffset + decelerationDistance * eased;
            float delta = desiredOffset - startOffset;
            startOffset = desiredOffset;

            AdvanceReel(delta);
            remainingDistance -= delta;
            yield return null;
        }

        // Finish on an exact symbol boundary.
        if (remainingDistance > 0f)
            AdvanceReel(remainingDistance);

        scrollOffset = 0f;
        ApplyPositions();

        // The final symbol was already chosen before the animation started.
        // We now show it in the center row.
        ShowCenterSymbol(target);

        currentSpeed = 0f;
        IsSpinning = false;
        state = ReelState.Idle;
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

    private float GetDeltaTime()
    {
        return Time.unscaledDeltaTime;
    }

    private void StopSpinRoutine()
    {
        if (spinRoutine == null)
            return;

        StopCoroutine(spinRoutine);
        spinRoutine = null;
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!initialized || IsSpinning || viewport == null)
            return;

        ApplyPositions();
    }
}
