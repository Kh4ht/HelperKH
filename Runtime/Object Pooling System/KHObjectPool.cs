using System.Collections.Generic;
using UnityEngine;

namespace KH
{
    /// <summary>
    /// A generic, reusable pool for any MonoBehaviour that implements IPoolable.
    /// </summary>
    public class KHObjectPool<T> : IPoolInfo where T : MonoBehaviour, IKHPoolable
    {
        // ── Config ────────────────────────────────────────────────────────────
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly bool _expandable;   // grow when empty?
        private readonly int _maxSize;       // 0 = unlimited

        // ── State ─────────────────────────────────────────────────────────────
        private readonly Stack<T> _available = new();
        private readonly HashSet<T> _active = new();

        // ── Events ────────────────────────────────────────────────────────────
        public event System.Action<T> OnSpawned;
        public event System.Action<T> OnDespawned;

        // ── Diagnostics ───────────────────────────────────────────────────────
        public int CountAvailable => _available.Count;
        public int CountActive => _active.Count;
        public int CountTotal => CountAvailable + CountActive;

        // ── Constructor ───────────────────────────────────────────────────────
        /// <param name="prefab">Template object to clone.</param>
        /// <param name="initialSize">How many to pre-warm.</param>
        /// <param name="parent">Optional parent transform for pooled objects.</param>
        /// <param name="expandable">If true, creates new instances when empty; otherwise returns null.</param>
        /// <param name="maxSize">Hard cap on total instances (0 = no cap).</param>
        public KHObjectPool(T prefab, int initialSize = 10,
                          Transform parent = null,
                          bool expandable = true,
                          int maxSize = 0)
        {
            _prefab = prefab;
            _parent = parent;
            _expandable = expandable;
            _maxSize = maxSize;

            Prewarm(initialSize);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Get an object from the pool.</summary>
        public T Spawn(Vector3 position = default, Quaternion rotation = default)
        {
            T instance = GetOrCreate();
            if (instance == null) return null;         // pool exhausted, not expandable

            instance.transform.SetPositionAndRotation(position, rotation);
            instance.gameObject.SetActive(true);
            instance.OnSpawn();

            _active.Add(instance);
            OnSpawned?.Invoke(instance);
            return instance;
        }

        /// <summary>Return an object to the pool.</summary>
        public void Despawn(T instance)
        {
            if (instance == null)
                return;

            if (!_active.Contains(instance))
            {
                Debug.LogWarning($"[ObjectPool] Tried to despawn '{instance.name}' but it isn't tracked as active.", instance);
                return;
            }

            instance.OnDespawn();
            instance.gameObject.SetActive(false);
            instance.transform.SetParent(_parent);

            _active.Remove(instance);
            _available.Push(instance);
            OnDespawned?.Invoke(instance);
        }

        /// <summary>Return every active object to the pool at once.</summary>
        public void DespawnAll()
        {
            // copy because Despawn modifies _active
            var snapshot = new List<T>(_active);
            foreach (var instance in snapshot)
                Despawn(instance);
        }

        /// <summary>Destroy ALL instances and clear both collections.</summary>
        public void Dispose()
        {
            DespawnAll();

            while (_available.Count > 0)
            {
                var instance = _available.Pop();
                if (instance != null)
                    Object.Destroy(instance.gameObject);
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (_maxSize > 0 && CountTotal >= _maxSize)
                    break;

                _available.Push(CreateInstance());
            }
        }

        private T GetOrCreate()
        {
            if (_available.Count > 0)
                return _available.Pop();

            if (!_expandable)
            {
                Debug.LogWarning("[ObjectPool] Pool exhausted and not expandable.");
                return null;
            }

            if (_maxSize > 0 && CountTotal >= _maxSize)
            {
                Debug.LogWarning($"[ObjectPool] Hit max size ({_maxSize}). Cannot create more instances.");
                return null;
            }

            return CreateInstance();
        }

        private T CreateInstance()
        {
            var go = Object.Instantiate(_prefab, _parent);
            go.gameObject.SetActive(false);
            go.name = $"{_prefab.name} [{CountTotal}]";
            return go;
        }
    }
}
