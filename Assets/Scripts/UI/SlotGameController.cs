
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is the main manager for the slot machine.
// It checks the config, chooses symbols, starts the reel animations,
// and tells the rest of the game when a spin has started and finished.
[DisallowMultipleComponent]
public sealed class SlotGameController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private SlotMachineConfig config;

    [Header("Reels")]
    [SerializeField] private List<SlotReel> reels = new();

    [Header("Lever")]
    [SerializeField] private Lever lever;

    // Fired when a spin starts.
    public event Action SpinStarted;

    // Fired when the spin is complete.
    public event Action SpinCompleted;

    // Fired with the symbol result chosen for the spin.
    public event Action<IReadOnlyList<SlotMachineConfig.SymbolDefinition>> ResultsGenerated;

    public bool IsSpinning { get; private set; }
    public IReadOnlyList<SlotReel> Reels => reels;
    public SlotMachineConfig Config => config;

    private Coroutine spinRoutine;

    private void Awake()
    {
        RemoveInvalidReels();
        ConfigureReels();
    }

    private void OnDisable()
    {
        if (spinRoutine != null)
        {
            StopCoroutine(spinRoutine);
            spinRoutine = null;
        }

        IsSpinning = false;
    }

    // Try to start a spin.
    // Returns true if everything is valid and the machine begins spinning.
    public bool TrySpin()
    {
        if (!isActiveAndEnabled || IsSpinning)
            return false;

        if (!ValidateConfiguration())
            return false;

        RemoveInvalidReels();

        if (reels.Count == 0)
        {
            Debug.LogError("No valid reels are assigned.", this);
            return false;
        }

        List<SlotMachineConfig.SymbolDefinition> results = GenerateResults();

        if (results.Count != reels.Count)
            return false;

        if (lever != null && !lever.TryActivate())
            return false;

        IsSpinning = true;
        SpinStarted?.Invoke();
        ResultsGenerated?.Invoke(results.AsReadOnly());

        spinRoutine = StartCoroutine(SpinAllReels(results));
        return true;
    }

    // Replace the reel list safely.
    public void SetReels(IEnumerable<SlotReel> newReels)
    {
        if (IsSpinning)
        {
            Debug.LogWarning("Cannot replace reels during a spin.", this);
            return;
        }

        reels.Clear();

        if (newReels == null)
            return;

        foreach (SlotReel reel in newReels)
        {
            if (reel != null && !reels.Contains(reel))
            {
                reels.Add(reel);
                if (config != null)
                    reel.SetConfig(config);
            }
        }
    }

    // Starts all reels one by one with stagger to make the animation feel natural.
    private IEnumerator SpinAllReels(
        IReadOnlyList<SlotMachineConfig.SymbolDefinition> results)
    {
        float stagger = config.Reels.StopStaggerDelay;

        int startedCount = 0;

        for (int i = 0; i < reels.Count; i++)
        {
            SlotReel reel = reels[i];

            if (reel == null)
                continue;

            reel.SpinTo(results[i]);
            startedCount++;

            if (stagger > 0f && i < reels.Count - 1)
                yield return new WaitForSecondsRealtime(stagger);
        }

        if (startedCount == 0)
        {
            FinishSpin();
            yield break;
        }

        while (AnyReelSpinning())
            yield return null;

        FinishSpin();
    }

    // Randomly chooses a symbol for each reel using the symbol weights.
    private List<SlotMachineConfig.SymbolDefinition> GenerateResults()
    {
        var results = new List<SlotMachineConfig.SymbolDefinition>(reels.Count);
        IReadOnlyList<SlotMachineConfig.SymbolDefinition> symbols = config.Symbols;

        float totalWeight = 0f;

        foreach (SlotMachineConfig.SymbolDefinition symbol in symbols)
        {
            if (IsSelectable(symbol))
                totalWeight += symbol.Weight;
        }

        if (totalWeight <= 0f)
        {
            Debug.LogError("No selectable symbols with valid sprites and positive weights.", this);
            return results;
        }

        for (int i = 0; i < reels.Count; i++)
        {
            float roll = UnityEngine.Random.value * totalWeight;
            float cumulativeWeight = 0f;
            SlotMachineConfig.SymbolDefinition selected = null;

            foreach (SlotMachineConfig.SymbolDefinition symbol in symbols)
            {
                if (!IsSelectable(symbol))
                    continue;

                cumulativeWeight += symbol.Weight;

                if (roll < cumulativeWeight)
                {
                    selected = symbol;
                    break;
                }
            }

            selected ??= GetLastSelectableSymbol(symbols);

            if (selected == null)
            {
                Debug.LogError("Failed to select a symbol.", this);
                results.Clear();
                return results;
            }

            results.Add(selected);
        }

        return results;
    }

    private static bool IsSelectable(SlotMachineConfig.SymbolDefinition symbol)
    {
        return symbol != null &&
               symbol.Sprite != null &&
               symbol.Weight > 0f &&
               !string.IsNullOrWhiteSpace(symbol.Id);
    }

    private static SlotMachineConfig.SymbolDefinition GetLastSelectableSymbol(
        IReadOnlyList<SlotMachineConfig.SymbolDefinition> symbols)
    {
        for (int i = symbols.Count - 1; i >= 0; i--)
        {
            if (IsSelectable(symbols[i]))
                return symbols[i];
        }

        return null;
    }

    // Returns true if at least one reel is still spinning.
    private bool AnyReelSpinning()
    {
        foreach (SlotReel reel in reels)
        {
            if (reel != null && reel.IsSpinning)
                return true;
        }

        return false;
    }

    // Checks that the machine has enough setup before starting a spin.
    private bool ValidateConfiguration()
    {
        if (config == null)
        {
            Debug.LogError("SlotMachineConfig is not assigned.", this);
            return false;
        }

        if (!config.HasValidSymbols)
        {
            Debug.LogError("SlotMachineConfig has no valid selectable symbols.", this);
            return false;
        }

        if (config.Reels == null)
        {
            Debug.LogError("Reel settings are missing.", this);
            return false;
        }

        return true;
    }

    private void ConfigureReels()
    {
        if (config == null)
            return;

        foreach (SlotReel reel in reels)
        {
            if (reel != null)
                reel.SetConfig(config);
        }
    }

    // Removes broken or duplicate reel references.
    private void RemoveInvalidReels()
    {
        reels.RemoveAll(reel => reel == null);

        for (int i = reels.Count - 1; i >= 0; i--)
        {
            for (int j = 0; j < i; j++)
            {
                if (reels[i] == reels[j])
                {
                    reels.RemoveAt(i);
                    break;
                }
            }
        }
    }

    private void FinishSpin()
    {
        spinRoutine = null;
        IsSpinning = false;
        SpinCompleted?.Invoke();
    }
}
