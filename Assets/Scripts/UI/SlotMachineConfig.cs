
using System;
using System.Collections.Generic;
using UnityEngine;

// This file stores the main settings for the slot machine.
// Think of it as the blueprint for the whole game: symbols, reels, sound, and betting rules.
// The values here are saved in a ScriptableObject, so we can edit them in the Unity Editor.
[CreateAssetMenu(fileName = "SlotMachineConfig", menuName = "Slot Machine/Configuration")]
public sealed class SlotMachineConfig : ScriptableObject
{
    // One symbol in the slot machine.
    // Example: cherry, lemon, bar, or jackpot icon.
    [Serializable]
    public sealed class SymbolDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite sprite;
        [SerializeField, Min(0f)] private float weight = 1f;
        [SerializeField, Min(0)] private int payoutMultiplier = 1;

        // Unique name used to find this symbol in the list.
        public string Id => id;

        // The text shown to players when this symbol is used in UI.
        public string DisplayName => displayName;

        // The image that appears on the reel.
        public Sprite Sprite => sprite;

        // How often this symbol can appear.
        // Bigger number = more chance to appear.
        public float Weight => weight;

        // How much this symbol can pay when it matches.
        public int PayoutMultiplier => payoutMultiplier;
    }

    // All sound clips used by the slot machine.
    [Serializable]
    public sealed class AudioSettings
    {
        [SerializeField] private AudioClip leverPull;
        [SerializeField] private AudioClip reelSpin;
        [SerializeField] private AudioClip reelStop;
        [SerializeField] private AudioClip buttonClick;
        [SerializeField] private AudioClip win;
        [SerializeField] private AudioClip jackpot;

        // Sound when the player pulls the lever.
        public AudioClip LeverPull => leverPull;

        // Sound while the reel is spinning.
        public AudioClip ReelSpin => reelSpin;

        // Sound when a reel stops.
        public AudioClip ReelStop => reelStop;

        // Sound for button taps or UI clicks.
        public AudioClip ButtonClick => buttonClick;

        // Sound for a normal win.
        public AudioClip Win => win;

        // Sound for a big win or jackpot.
        public AudioClip Jackpot => jackpot;
    }

    // Setup for each reel in the machine.
    // This controls how the reels move and look.
    [Serializable]
    public sealed class ReelSettings
    {
        [SerializeField, Min(1)] private int reelCount = 3;
        [SerializeField, Min(1)] private int visibleRows = 3;
        [SerializeField, Min(1)] private int bufferRows = 2;
        [SerializeField, Min(0.01f)] private float symbolHeight = 120f;
        [SerializeField, Min(0.01f)] private float cruiseSpeed = 1800f;
        [SerializeField, Min(0f)] private float accelerationDuration = 0.25f;
        [SerializeField, Min(0f)] private float cruiseDuration = 0.8f;
        [SerializeField, Min(0f)] private float decelerationDuration = 0.55f;
        [SerializeField, Min(0f)] private float stopStaggerDelay = 0.2f;

        // Number of reels in the machine.
        public int ReelCount => reelCount;

        // How many rows are shown to the player at the same time.
        public int VisibleRows => visibleRows;

        // Extra rows kept off-screen to make the spin feel smooth.
        public int BufferRows => bufferRows;

        // Height of one symbol in the UI.
        public float SymbolHeight => symbolHeight;

        // Speed of the reel when it is moving fast.
        public float CruiseSpeed => cruiseSpeed;

        // Time before the reel reaches full speed.
        public float AccelerationDuration => accelerationDuration;

        // Time the reel keeps spinning at a stable speed.
        public float CruiseDuration => cruiseDuration;

        // Time needed to slow down before stopping.
        public float DecelerationDuration => decelerationDuration;

        // Delay between each reel stop, so the reels do not stop at the exact same time.
        public float StopStaggerDelay => stopStaggerDelay;

        // Total number of symbol slots needed for a reel.
        public int PoolSize => visibleRows + bufferRows * 2;
    }

    // Betting rules that control the player money and bet values.
    [Serializable]
    public sealed class BettingSettings
    {
        [SerializeField, Min(0)] private int startingBalance = 1000;
        [SerializeField, Min(1)] private int minimumBet = 10;
        [SerializeField, Min(1)] private int maximumBet = 100;
        [SerializeField, Min(1)] private int betStep = 10;

        // Money the player has at the start.
        public int StartingBalance => startingBalance;

        // Smallest allowed bet.
        public int MinimumBet => minimumBet;

        // Biggest allowed bet.
        public int MaximumBet => maximumBet;

        // How much the bet changes when the player presses up or down.
        public int BetStep => betStep;
    }

    [Header("Symbols")]
    [SerializeField] private List<SymbolDefinition> symbols = new();

    [Header("Audio")]
    [SerializeField] private AudioSettings audio = new();

    [Header("Reels")]
    [SerializeField] private ReelSettings reels = new();

    [Header("Betting")]
    [SerializeField] private BettingSettings betting = new();

    // All symbols available in the machine.
    public IReadOnlyList<SymbolDefinition> Symbols => symbols;

    // Sound settings for the machine.
    public AudioSettings Audio => audio;

    // Reel movement settings.
    public ReelSettings Reels => reels;

    // Betting values and limits.
    public BettingSettings Betting => betting;

    // Check whether the machine has at least one good symbol to use.
    // A good symbol means it has a sprite, an ID, and positive weight.
    public bool HasValidSymbols
    {
        get
        {
            if (symbols == null || symbols.Count == 0)
                return false;

            foreach (SymbolDefinition symbol in symbols)
            {
                if (symbol == null || symbol.Sprite == null ||
                    string.IsNullOrWhiteSpace(symbol.Id) ||
                    symbol.Weight < 0f)
                    continue;

                if (symbol.Weight > 0f)
                    return true;
            }

            return false;
        }
    }

    // Try to find a symbol by its ID.
    // If the ID is valid and exists, return it; otherwise return false.
    public bool TryGetSymbol(string id, out SymbolDefinition result)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(id) || symbols == null)
            return false;

        foreach (SymbolDefinition symbol in symbols)
        {
            if (symbol != null && symbol.Id == id)
            {
                result = symbol;
                return true;
            }
        }

        return false;
    }

    // Unity calls this automatically when values change in the editor.
    // We use it to catch mistakes early, like duplicate symbol IDs or bad bet limits.
    private void OnValidate()
    {
        if (symbols == null)
            return;

        var ids = new HashSet<string>(StringComparer.Ordinal);

        foreach (SymbolDefinition symbol in symbols)
        {
            if (symbol == null || string.IsNullOrWhiteSpace(symbol.Id))
                continue;

            if (!ids.Add(symbol.Id))
                Debug.LogWarning($"Duplicate slot symbol ID: '{symbol.Id}'. IDs should be unique.", this);
        }

        if (betting.MinimumBet > betting.MaximumBet)
            Debug.LogWarning("Minimum bet exceeds maximum bet.", this);
    }
}
