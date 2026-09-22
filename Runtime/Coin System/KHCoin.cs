using System;
using UnityEngine;

namespace KH
{
    /// <summary>
    /// One coin type (gold, gems, wood...). Create one instance per coin type.
    /// The balance never goes below 0 and never above <see cref="Max"/>.
    /// </summary>
    [Serializable]
    public class KHCoin
    {
        #region FIELDS

        [SerializeField, Min(0)] private long amount;

        // EVENTS

        /// <summary>Fired whenever the balance changes. Args: (newAmount, delta).</summary>
        public event Action<long, long> OnChanged;

        // GETTERS

        /// <summary>Display / log name, also handy as a save key.</summary>
        public string Name { get; }

        /// <summary>Upper limit of the balance.</summary>
        public long Max { get; }

        /// <summary>Current balance.</summary>
        public long Amount => amount;

        #endregion
        #region CONSTRUCTOR

        /// <param name="name">Used in warnings, UI and as a save key.</param>
        /// <param name="startAmount">Starting balance (clamped to [0, max]).</param>
        /// <param name="max">Upper limit for the balance. Default: no limit.</param>
        public KHCoin(string name = "Coin", long startAmount = 0, long max = long.MaxValue)
        {
            if (max < 0)
            {
                Debug.LogWarning($"[{nameof(KHCoin)}] '{name}': max can't be negative, using 0.");
                max = 0;
            }

            Name = name;
            Max = max;
            amount = Math.Max(0, Math.Min(max, startAmount));
        }

        #endregion
        #region PUBLIC

        /// <summary>True if the balance covers <paramref name="cost"/>. Negative costs are never affordable.</summary>
        public bool CanAfford(long cost)
        {
            return cost >= 0 && cost <= amount;
        }

        /// <summary>
        /// Adds coins (must be >= 0, use <see cref="TrySpend"/> to remove). Respects <see cref="Max"/>.
        /// Returns how much was really added, which is less than requested if the max was hit.
        /// </summary>
        public long Add(long value)
        {
            if (value < 0)
            {
                Debug.LogWarning($"[{nameof(KHCoin)}] '{Name}': Add() got a negative value. Use TrySpend() instead.");
                return 0;
            }

            long added = Math.Min(value, Max - amount);

            if (added <= 0)
                return 0;

            amount += added;
            OnChanged?.Invoke(amount, added);

            return added;
        }

        /// <summary>Removes coins if the balance covers it. Returns false (and changes nothing) otherwise.</summary>
        public bool TrySpend(long cost)
        {
            if (cost < 0)
            {
                Debug.LogWarning($"[{nameof(KHCoin)}] '{Name}': TrySpend() got a negative cost. Use Add() instead.");
                return false;
            }

            if (cost > amount)
                return false;

            if (cost == 0)
                return true;

            amount -= cost;
            OnChanged?.Invoke(amount, -cost);

            return true;
        }

        /// <summary>Sets the balance directly, clamped to [0, max]. Good for loading a save or cheats.</summary>
        public void Set(long value)
        {
            long newAmount = Math.Max(0, Math.Min(Max, value));
            long delta = newAmount - amount;

            if (delta == 0)
                return;

            amount = newAmount;
            OnChanged?.Invoke(amount, delta);
        }

        public override string ToString() => $"{Name}: {amount}";

        #endregion
        #region STATIC

        // MULTI-COIN PAYMENTS

        /// <summary>
        /// True if every (coin, cost) pair can be paid at once. If the same coin appears more than
        /// once its costs are added up, so (gold, 30) + (gold, 30) needs 60 gold.
        /// </summary>
        public static bool CanAffordAll(params (KHCoin coin, long cost)[] costs)
        {
            if (costs == null)
                return true;

            for (int i = 0; i < costs.Length; i++)
            {
                if (costs[i].coin == null || costs[i].cost < 0)
                    return false;

                long total = 0;

                for (int j = 0; j < costs.Length; j++)
                {
                    if (costs[j].coin == costs[i].coin)
                        total += costs[j].cost;
                }

                if (total > costs[i].coin.amount)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Atomic payment across several coins: either EVERY cost is paid, or nothing changes.
        /// Events fire only after all balances are updated, so listeners never see a half-paid state.
        /// Usage: KHCoin.TrySpendAll((gold, 50), (gems, 2));
        /// </summary>
        public static bool TrySpendAll(params (KHCoin coin, long cost)[] costs)
        {
            if (costs == null || costs.Length == 0)
                return true;

            if (!CanAffordAll(costs))
                return false;

            // Remember the balances before paying, so each coin fires ONE event with the right delta.
            long[] before = new long[costs.Length];

            for (int i = 0; i < costs.Length; i++)
                before[i] = costs[i].coin.amount;

            for (int i = 0; i < costs.Length; i++)
                costs[i].coin.amount -= costs[i].cost;

            for (int i = 0; i < costs.Length; i++)
            {
                KHCoin coin = costs[i].coin;

                // Only the first occurrence of a coin reports (its 'before' is the original balance).
                bool isFirstOccurrence = true;
                for (int j = 0; j < i; j++)
                {
                    if (costs[j].coin == coin)
                    {
                        isFirstOccurrence = false;
                        break;
                    }
                }

                if (!isFirstOccurrence)
                    continue;

                long delta = coin.amount - before[i];

                if (delta != 0)
                    coin.OnChanged?.Invoke(coin.amount, delta);
            }

            return true;
        }

        #endregion
    }
}
