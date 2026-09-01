using System;
using UnityEngine;

namespace KH
{
    [Serializable]
    public struct KHDamage
    {
        [Tooltip("The amount of damage to be dealt")]
        [Min(0)] public float mainDamage;

        [Tooltip("The percentage of the main damage to be dealt over time")]
        [Range(0f, 1f)] public float overTimeDamagePercent;

        [Tooltip("Duration of the over time damage in seconds")]
        [Min(0f)] public float duration;

        [Tooltip("How many ticks the over time damage will be applied")]
        [Min(1)] public int ticks;

        // Getters
        public readonly bool HasOvertimeDamage => duration != 0;
    }
}