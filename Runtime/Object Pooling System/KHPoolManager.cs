using System.Collections.Generic;
using UnityEngine;

namespace KH
{
    /// <summary>
    /// Singleton that owns all named pools.
    /// Access via PoolManager.Instance anywhere in your project.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("KH/KHPoolManager")]
    public class KHPoolManager : MonoBehaviour
    {
        #region FIELDS

        // ── Singleton ─────────────────────────────────────────────────────────
        public static KHPoolManager Ins { get; private set; }

        // ── Registry ──────────────────────────────────────────────────────────
        // We store pools as object so we can hold mixed generic types.
        private readonly Dictionary<string, object> _registry = new();

        // ── Inspector ─────────────────────────────────────────────────────────
        [Tooltip("Do not destroy the target Object when loading a new Scene.")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        #endregion
        #region UNITY EVENTS

        private void Awake()
        {
            if (Ins != null && Ins != this)
            {
                Destroy(gameObject);
                return;
            }
            Ins = this;

            if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);
        }

        // ── Cleanup ───────────────────────────────────────────────────────────
        private void OnDestroy()
        {
            // Each ObjectPool<T> holds MonoBehaviours — Unity cleans up GameObjects,
            // but we call Dispose to trigger OnDespawn callbacks gracefully.
            foreach (var kvp in _registry)
            {
                // use reflection-free dynamic dispatch via the interface
                if (kvp.Value is IDisposablePool dp)
                    dp.Dispose();
            }
            _registry.Clear();
        }

        #endregion
        #region PUBLIC

        // ── Registration ──────────────────────────────────────────────────────

        /// <summary>
        /// Register (and optionally pre-warm) a pool. Call once — usually in Awake or a bootstrapper.
        /// </summary>
        public KHObjectPool<T> Register<T>(string key,
                                           T prefab,
                                           int initialSize = 10,
                                           bool expandable = true,
                                           int maxSize = 0) where T : MonoBehaviour, IKHPoolable
        {
            if (_registry.ContainsKey(key))
            {
                Debug.LogWarning($"[PoolManager] Pool '{key}' is already registered. Returning existing pool.");
                return Get<T>(key);
            }

            // create a dedicated parent transform to keep the hierarchy tidy
            var parent = new GameObject($"Pool [{key}]").transform;
            parent.SetParent(transform);

            var pool = new KHObjectPool<T>(prefab, initialSize, parent, expandable, maxSize);
            _registry[key] = pool;
            return pool;
        }

        // ── Access ────────────────────────────────────────────────────────────

        /// <summary>Retrieve a registered pool by key.</summary>
        public KHObjectPool<T> Get<T>(string key) where T : MonoBehaviour, IKHPoolable
        {
            if (_registry.TryGetValue(key, out var pool))
                return pool as KHObjectPool<T>;

            Debug.LogError($"[PoolManager] No pool registered with key '{key}'.");
            return null;
        }

        /// <summary>Retrieve all registered pools.</summary>
        public IEnumerable<KHObjectPool<T>> GetAll<T>() where T : MonoBehaviour, IKHPoolable
        {
            foreach (object value in _registry.Values)
            {
                if (value is KHObjectPool<T> pool)
                    yield return pool;
            }
        }

        /// <summary>Convenience: spawn directly via key.</summary>
        public T Spawn<T>(string key,
                          Vector3 position = default,
                          Quaternion rotation = default) where T : MonoBehaviour, IKHPoolable
        {
            return Get<T>(key)?.Spawn(position, rotation);
        }

        /// <summary>Convenience: despawn directly via key.</summary>
        public void Despawn<T>(string key, T instance) where T : MonoBehaviour, IKHPoolable
        {
            Get<T>(key)?.Despawn(instance);
        }

        /// <summary>Despawn every active object in a pool.</summary>
        public void DespawnAll<T>(string key) where T : MonoBehaviour, IKHPoolable
        {
            Get<T>(key)?.DespawnAll();
        }

        /// <summary>Destroy all instances and remove the pool from the registry.</summary>
        public void Unregister<T>(string key) where T : MonoBehaviour, IKHPoolable
        {
            if (_registry.TryGetValue(key, out var pool))
            {
                (pool as KHObjectPool<T>)?.Dispose();
                _registry.Remove(key);
            }
        }

        /// <summary>True if any registered pool has at least one active instance.</summary>
        public bool AnyActive()
        {
            foreach (var kvp in _registry)
            {
                if (kvp.Value is IPoolInfo info && info.CountActive > 0)
                    return true;
            }

            return false;
        }

        /// <summary>Total active instances across every pool.</summary>
        public int TotalActiveCount()
        {
            int total = 0;

            foreach (var kvp in _registry)
            {
                if (kvp.Value is IPoolInfo info)
                    total += info.CountActive;
            }

            return total;
        }

        #endregion
    }

    #region INTERNAL INTERFACE

    // Internal marker so PoolManager can dispose pools without knowing T.
    internal interface IDisposablePool { void Dispose(); }

    #endregion
}
