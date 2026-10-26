
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlotMachine.Core
{
    // Stores the symbols, reel settings, wallet, and bet rules for the game.
    // Sound and UI assets are kept in SlotMachinePresentationConfig.
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

        // Setup for each reel in the machine.
        // This controls how the reels move and look.
        [Serializable]
        public sealed class ReelSettings
        {
            [SerializeField, Min(1)] private int visibleRows = 3;
            [SerializeField, Min(1)] private int bufferRows = 2;
            [SerializeField, Min(0.01f)] private float symbolHeight = 120f;
            [SerializeField, Min(0.01f)] private float cruiseSpeed = 1800f;
            [SerializeField, Min(0f)] private float accelerationDuration = 0.25f;
            [SerializeField, Min(0f)] private float cruiseDuration = 0.8f;
            [SerializeField, Min(0f)] private float decelerationDuration = 0.55f;
            [SerializeField, Min(0f)] private float stopStaggerDelay = 0.2f;
            [SerializeField, Min(1)] private int extraStopCycles = 2;

            // How many symbol rows fit in the reel view.
            public int VisibleRows => visibleRows;

            // Extra symbol rows kept outside the view during a spin.
            public int BufferRows => bufferRows;

            // Height of one symbol image in the UI.
            public float SymbolHeight => symbolHeight;

            // Reel speed while it is moving at full speed.
            public float CruiseSpeed => cruiseSpeed;

            // Time used to speed up the reel.
            public float AccelerationDuration => accelerationDuration;

            // Time the reel stays at full speed.
            public float CruiseDuration => cruiseDuration;

            // Helps choose how far the reel travels while slowing down.
            public float DecelerationDuration => decelerationDuration;

            // Delay between reel starts, so they stop at different times.
            public float StopStaggerDelay => stopStaggerDelay;
            public int ExtraStopCycles => extraStopCycles;

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

        [Header("Reels")]
        [SerializeField] private ReelSettings reels = new();

        [Header("Betting")]
        [SerializeField] private BettingSettings betting = new();

        // Add or edit symbols here in the Inspector.
        public IReadOnlyList<SymbolDefinition> Symbols => symbols;

        // Settings used to build and animate each reel.
        public ReelSettings Reels => reels;

        // Starting money and bet choices.
        public BettingSettings Betting => betting;

        // True when the asset has at least one symbol the game can use.
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

        // Finds a symbol with this ID. Returns false if it is not in the list.
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

        // Unity runs this when the asset changes in the Inspector.
        // It warns about repeated symbol IDs and bet limits that do not make sense.
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
}
