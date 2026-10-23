
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SlotMachineConfig", menuName = "Slot Machine/Configuration")]
public sealed class SlotMachineConfig : ScriptableObject
{
    [Serializable]
    public sealed class SymbolDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite sprite;
        [SerializeField, Min(0f)] private float weight = 1f;
        [SerializeField, Min(0)] private int payoutMultiplier = 1;

        public string Id => id;
        public string DisplayName => displayName;
        public Sprite Sprite => sprite;
        public float Weight => weight;
        public int PayoutMultiplier => payoutMultiplier;
    }

    [Serializable]
    public sealed class AudioSettings
    {
        [SerializeField] private AudioClip leverPull;
        [SerializeField] private AudioClip reelSpin;
        [SerializeField] private AudioClip reelStop;
        [SerializeField] private AudioClip buttonClick;
        [SerializeField] private AudioClip win;
        [SerializeField] private AudioClip jackpot;

        public AudioClip LeverPull => leverPull;
        public AudioClip ReelSpin => reelSpin;
        public AudioClip ReelStop => reelStop;
        public AudioClip ButtonClick => buttonClick;
        public AudioClip Win => win;
        public AudioClip Jackpot => jackpot;
    }

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

        public int ReelCount => reelCount;
        public int VisibleRows => visibleRows;
        public int BufferRows => bufferRows;
        public float SymbolHeight => symbolHeight;
        public float CruiseSpeed => cruiseSpeed;
        public float AccelerationDuration => accelerationDuration;
        public float CruiseDuration => cruiseDuration;
        public float DecelerationDuration => decelerationDuration;
        public float StopStaggerDelay => stopStaggerDelay;

        public int PoolSize => visibleRows + bufferRows * 2;
    }

    [Serializable]
    public sealed class BettingSettings
    {
        [SerializeField, Min(0)] private int startingBalance = 1000;
        [SerializeField, Min(1)] private int minimumBet = 10;
        [SerializeField, Min(1)] private int maximumBet = 100;
        [SerializeField, Min(1)] private int betStep = 10;

        public int StartingBalance => startingBalance;
        public int MinimumBet => minimumBet;
        public int MaximumBet => maximumBet;
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

    public IReadOnlyList<SymbolDefinition> Symbols => symbols;
    public AudioSettings Audio => audio;
    public ReelSettings Reels => reels;
    public BettingSettings Betting => betting;

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
