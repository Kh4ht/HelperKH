using System;
using System.Collections;
using System.Collections.Generic;
using KH;
using UnityEngine;
namespace KH
{
    public sealed class KHHealthController
    {
        #region FIELDS

        // Listeners
        private readonly List<Action> _onDeathListeners = new();
        private readonly List<Action> _onReviveListeners = new();
        private readonly List<Action> _onHealthIncreaseListeners = new();
        private readonly List<Action> _onHealthDecreaseListeners = new();
        private readonly List<Action> _onHealthChangedListeners = new();
        private readonly List<Action> _onMaxHealthReachedListeners = new();
        private readonly List<Action> _onMaxHealthChangedListeners = new();

        // Private Fields
        private readonly MonoBehaviour owner;
        private float lastDamageTime = -1f;

        private readonly List<Coroutine> activeDots = new();

        // Getters
        public bool HasFullHealth => _health >= _maxHealth;

        // Properties
        private bool _isDead;
        public bool IsDead
        {
            get => _isDead;
            private set
            {
                if (_isDead == value)
                    return;

                _isDead = value;

                if (_isDead)
                    OnDeath();
                else
                    OnRevive();
            }
        }

        private float _health;
        public float Health
        {
            get => _health;
            set
            {
                if (IsDead || _health == value || (value >= _health && HasFullHealth))
                    return;

                if (_health < value)
                {
                    _health = Mathf.Clamp(value, 0, _maxHealth);
                    OnHealthIncrease();
                }
                else if (_health > value)
                {
                    _health = Mathf.Clamp(value, 0, _maxHealth);
                    OnHealthDecrease();
                }

                OnHealthChanged();

                if (HasFullHealth)
                    OnMaxHealthReached();

                IsDead = _health <= 0;
            }
        }

        private int _maxHealth;
        public int MaxHealth
        {
            get => _maxHealth;
            set
            {
                if (_maxHealth == (int)Mathf.Clamp(value, 1, Mathf.Infinity))
                    return;

                // Make sure Max Health is greater or equal to 1.
                _maxHealth = (int)Mathf.Clamp(value, 1, Mathf.Infinity);

                OnMaxHealthChanged();
            }
        }

        #endregion
        #region CONSTRUCTOR

        public KHHealthController(MonoBehaviour owner, int initialMaxHealth, float initialHealth)
        {
            this.owner = owner;
            _maxHealth = initialMaxHealth;
            _health = initialHealth;
        }

        #endregion
        #region PRIVATE

        private static void AddListener(List<Action> list, Action listener)
        {
            if (listener == null || list.Contains(listener))
                return;

            list.Add(listener);
        }

        private static void RemoveListener(List<Action> list, Action listener)
        {
            if (listener == null)
                return;

            list.Remove(listener);
        }

        private static void Notify(List<Action> list)
        {
            // Make a copy to safely iterate
            var listenersCopy = list.ToArray();

            listenersCopy.KHForEach(listener =>
            {
                try { listener?.Invoke(); }
                catch (Exception e) { Debug.LogError($"Listener threw exception: {e}"); }
            });
        }

        // On Death Listener
        public void AddOnDeathListener(Action listener) => AddListener(_onDeathListeners, listener);
        public void RemoveOnDeathListener(Action listener) => RemoveListener(_onDeathListeners, listener);

        // On Revive Listener
        public void AddOnReviveListener(Action listener) => AddListener(_onReviveListeners, listener);
        public void RemoveOnReviveListener(Action listener) => RemoveListener(_onReviveListeners, listener);

        // On Health Increase Listener
        public void AddOnHealthIncreaseListener(Action listener) => AddListener(_onHealthIncreaseListeners, listener);
        public void RemoveOnHealthIncreaseListener(Action listener) => RemoveListener(_onHealthIncreaseListeners, listener);

        // On Health Decrease Listener
        public void AddOnHealthDecreaseListener(Action listener) => AddListener(_onHealthDecreaseListeners, listener);
        public void RemoveOnHealthDecreaseListener(Action listener) => RemoveListener(_onHealthDecreaseListeners, listener);

        // On Health Changed Listener
        public void AddOnHealthChangedListener(Action listener) => AddListener(_onHealthChangedListeners, listener);
        public void RemoveOnHealthChangedListener(Action listener) => RemoveListener(_onHealthChangedListeners, listener);

        // On Max Health Reached Listener
        public void AddOnMaxHealthReachedListener(Action listener) => AddListener(_onMaxHealthReachedListeners, listener);
        public void RemoveOnMaxHealthReachedListener(Action listener) => RemoveListener(_onMaxHealthReachedListeners, listener);

        // On Max Health Changed Listener
        public void AddOnMaxHealthChangedListener(Action listener) => AddListener(_onMaxHealthChangedListeners, listener);
        public void RemoveOnMaxHealthChangedListener(Action listener) => RemoveListener(_onMaxHealthChangedListeners, listener);

        private void OnDeath()
        {
            StopAllDots();

            Notify(_onDeathListeners);
        }

        private void OnRevive()
        {
            Notify(_onReviveListeners);
        }

        private void OnHealthIncrease()
        {
            Notify(_onHealthIncreaseListeners);
        }

        private void OnHealthDecrease()
        {
            lastDamageTime = Time.time;

            Notify(_onHealthDecreaseListeners);
        }

        private void OnHealthChanged()
        {
            Notify(_onHealthChangedListeners);
        }

        private void OnMaxHealthReached()
        {
            Notify(_onMaxHealthReachedListeners);
        }

        private void OnMaxHealthChanged()
        {
            Notify(_onMaxHealthChangedListeners);
        }

        private void StopAllDots()
        {
            foreach (Coroutine c in activeDots)
            {
                if (c != null)
                    owner.StopCoroutine(c);
            }

            activeDots.Clear();
        }

        private IEnumerator DamageTickCoroutine(MonoBehaviour runner,
                                                float duration,
                                                float interval,
                                                Action onTick,
                                                Action onComplete)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                // 🔥 stop immediately if object is destroyed
                if (!runner)
                    yield break;

                yield return new WaitForSeconds(interval);

                if (!runner)
                    yield break;

                onTick?.Invoke();

                elapsed += interval;
            }

            onComplete?.Invoke();
        }

        #endregion
        #region PUBLIC

        public void ApplyDamage(KHDamage khDamage)
        {
            RemoveHealth(khDamage.mainDamage);

            if (!owner || IsDead || !khDamage.HasOvertimeDamage)
                return;

            Coroutine c = null;

            c = owner.StartCoroutine(DamageTickCoroutine(runner: owner,
                                                         duration: khDamage.duration,
                                                         interval: khDamage.duration / khDamage.ticks,
                                                         onTick: () => RemoveHealth(khDamage.mainDamage * Mathf.Clamp01(khDamage.overTimeDamagePercent)),
                                                         onComplete: () => activeDots.Remove(c)));

            activeDots.Add(c);
        }

        /// <summary>
        /// Returns how many seconds have passed since the entity last took damage.
        /// <para>Returns Mathf.Infinity if no damage was ever taken.</para>
        /// </summary>
        public float GetTimeSinceLastDamage()
        {
            if (lastDamageTime < 0f)
                return Mathf.Infinity;

            return Time.time - lastDamageTime;
        }

        /// <summary>
        /// Reduces the entity's current health by the specified amount.
        /// <para>Trigger OnDeath logic when health reaches zero.</para>
        /// </summary>
        /// <param name="amount">The amount of health to remove.</param>
        public void RemoveHealth(float amount)
        {
            if (IsDead)
                return;

            Health -= amount;
        }

        /// <summary>
        /// Reduces the entity's current health by the specified <param name="percent" (0 - 1)>.
        /// <para>Trigger OnDeath logic when health reaches zero.</para>
        /// </summary>
        /// <param name="percent">The percentage of max health to remove (0 - 1).</param>
        public void RemoveHealthPercent(float percent)
        {
            if (IsDead)
                return;

            Health -= MaxHealth * Mathf.Clamp01(percent);
        }

        /// <summary>
        /// Increases the entity's health by the specified amount, up to the maximum health limit.
        /// </summary>
        /// <param name="amount">The amount of health to add.</param>
        public void AddHealth(float amount)
        {
            if (IsDead)
                return;

            Health += amount;
        }

        /// <summary>
        /// Restores health based on a percentage of the entity's maximum health.
        /// <br/>
        /// The <paramref name="percent"/> value is clamped between 0 and 1.
        /// <br/>
        /// For example: 0.5f restores 50% of <see cref="_maxHealth"/>.
        /// </summary>
        /// <param name="percent">
        /// A value between 0 and 1 representing the fraction of max health to restore.
        /// </param>
        public void AddHealthPercent(float percent)
        {
            if (IsDead)
                return;

            Health += _maxHealth * Mathf.Clamp01(percent);
        }

        /// <summary>
        /// Revives the entity if it is dead, restoring it to active status and adding health.
        /// <para>Triggers OnRevive logic.</para>
        /// </summary>
        public void Revive(float percent = 1)
        {
            if (!IsDead)
                return;

            IsDead = false;

            Health = _maxHealth * Mathf.Clamp(percent, 0.01f, 1);
        }

        #endregion
    }
}