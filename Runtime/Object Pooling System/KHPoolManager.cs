using System.Collections.Generic;
using KH.Enums;
using KH.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KH
{
    /// <summary>
    /// Owns all named pools. Creates itself on first access to <see cref="Ins"/>.
    /// Non-persistent pools are disposed when their scene unloads; persistent pools survive.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("KH/Systems/" + nameof(KHPoolManager))]
    public class KHPoolManager : MonoBehaviour
    {
        #region FIELDS

        [KHResetStatic] private static KHPoolManager _ins;
        [KHResetStatic] private static bool _quitting;

        /// <summary>
        /// Created on first access. Returns null only while the application is quitting,
        /// so shutdown code should use Ins?.Despawn(...).
        /// </summary>
        public static KHPoolManager Ins
        {
            get
            {
                if (_ins == null && !_quitting)
                    _ins = new GameObject(nameof(KHPoolManager)).AddComponent<KHPoolManager>();

                return _ins;
            }
        }

        // We store pools as object so we can hold mixed generic types.
        private readonly Dictionary<string, object> _registry = new();

        #endregion
        #region UNITY EVENTS

        private void Awake()
        {
            if (_ins != null && _ins != this)
            {
                Destroy(gameObject);
                return;
            }

            _ins = this;
            DontDestroyOnLoad(gameObject); // must be a root object, which it is when auto-created

            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnApplicationQuit()
        {
            _quitting = true;
        }

        private void OnDestroy()
        {
            // A duplicate that destroyed itself in Awake never subscribed and has no pools.
            if (_ins != this)
                return;

            SceneManager.sceneUnloaded -= OnSceneUnloaded;

            foreach (var kvp in _registry)
            {
                if (kvp.Value is IDisposablePool dp)
                    dp.Dispose();
            }

            _registry.Clear();
            _ins = null;
        }

        #endregion
        #region SCENE CLEANUP

        private void OnSceneUnloaded(Scene scene)
        {
            List<string> toRemove = null;

            foreach (var kvp in _registry)
            {
                if (kvp.Value is IDisposablePool dp && !dp.Persistent && dp.OwnerScene == scene)
                    (toRemove ??= new List<string>()).Add(kvp.Key);
            }

            if (toRemove == null)
                return;

            foreach (string key in toRemove)
            {
                ((IDisposablePool)_registry[key]).Dispose();
                _registry.Remove(key);
            }
        }

        #endregion
        #region PUBLIC

        // ── Registration ──────────────────────────────────────────────────────

        /// <summary>
        /// Register (and optionally pre-warm) a pool. Call once, usually in Awake or a bootstrapper.
        /// Non-persistent pools are destroyed when the scene they were registered in unloads.
        /// </summary>
        public KHObjectPool<T> Register<T>(string key,
                                           T prefab,
                                           int initialSize = 0,
                                           bool expandable = true,
                                           int maxSize = 0,
                                           bool showLogMessage = true,
                                           bool persistent = false) where T : MonoBehaviour, IKHPoolable
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError($"{nameof(KHPoolManager)} Cannot register a pool for '{(prefab != null ? prefab.name : "null")}' with an empty key. Is its ID generated?", prefab);
                return null;
            }

            Scene activeScene = SceneManager.GetActiveScene();

            if (_registry.TryGetValue(key, out object existing))
            {
                var existingInfo = existing as IDisposablePool;

                // Leftover from a previous scene whose unload event hasn't fired yet: replace it.
                if (existingInfo != null && !existingInfo.Persistent && existingInfo.OwnerScene != activeScene)
                {
                    existingInfo.Dispose();
                    _registry.Remove(key);
                }
                else
                {
                    if (existing is not KHObjectPool<T>)
                    {
                        Debug.LogError($"{nameof(KHPoolManager)} Key '{key}' is already used by a pool of a different type. Requested {typeof(T).Name}.");
                        return null;
                    }
                    if (existingInfo != null && existingInfo.Persistent != persistent)
                        Debug.LogWarning($"{nameof(KHPoolManager)} Pool '{key}' already exists with persistent = {existingInfo.Persistent}. The new value ({persistent}) is ignored.");

                    if (showLogMessage)
                        Debug.Log($"{nameof(KHPoolManager)} Pool '{key}' is already registered. Returning existing pool.".AddColorTag(XMLColors.White));

                    return GetPool<T>(key);
                }
            }

            // Every pool parent lives under the manager (which is DontDestroyOnLoad).
            // Non-persistent pools destroy their parent in Dispose.
            var parent = new GameObject($"Pool [{key}]").transform;
            parent.SetParent(transform);

            var pool = new KHObjectPool<T>(prefab, initialSize, parent, expandable, maxSize, persistent, activeScene);
            _registry[key] = pool;
            return pool;
        }

        // ── Access ────────────────────────────────────────────────────────────

        /// <summary>Retrieve a registered pool by key. Logs an error if it doesn't exist.</summary>
        public KHObjectPool<T> GetPool<T>(string key) where T : MonoBehaviour, IKHPoolable
        {
            if (_registry.TryGetValue(key, out var pool))
            {
                if (pool is KHObjectPool<T> typed)
                    return typed;

                Debug.LogError($"[PoolManager] Pool '{key}' exists but is not a pool of {typeof(T).Name}. Two prefabs may share the same key.");
                return null;
            }

            Debug.LogError($"[PoolManager] No pool registered with key '{key}'.");
            return null;
        }

        /// <summary>Like GetPool, but silent when the pool doesn't exist.</summary>
        public bool TryGetPool<T>(string key, out KHObjectPool<T> pool) where T : MonoBehaviour, IKHPoolable
        {
            pool = _registry.TryGetValue(key, out var found) ? found as KHObjectPool<T> : null;
            return pool != null;
        }

        /// <summary>Retrieve all registered pools.</summary>
        public IEnumerable<KHObjectPool<T>> GetAllPools<T>() where T : MonoBehaviour, IKHPoolable
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
            return GetPool<T>(key)?.Spawn(position, rotation);
        }

        /// <summary>Convenience: despawn directly via key.</summary>
        public void Despawn<T>(string key, T instance) where T : MonoBehaviour, IKHPoolable
        {
            GetPool<T>(key)?.Despawn(instance);
        }

        /// <summary>Despawn every active object in a pool.</summary>
        public void DespawnAll<T>(string key) where T : MonoBehaviour, IKHPoolable
        {
            GetPool<T>(key)?.DespawnAll();
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

        /// <summary>True if any registered pool has at least one active instance (any type).</summary>
        public bool GetAnyActive()
        {
            foreach (var kvp in _registry)
            {
                if (kvp.Value is IPoolInfo info && info.CountActive > 0)
                    return true;
            }

            return false;
        }

        /// <summary>True if the pool of type T has at least one active instance.</summary>
        public bool GetAnyActive<T>() where T : MonoBehaviour, IKHPoolable
        {
            foreach (KHObjectPool<T> pool in GetAllPools<T>())
            {
                if (pool.CountActive > 0)
                    return true;
            }

            return false;
        }

        /// <summary>Enumerates every active instance of type T across all registered pools of that type.</summary>
        public IEnumerable<T> GetAllActive<T>() where T : MonoBehaviour, IKHPoolable
        {
            foreach (KHObjectPool<T> pool in GetAllPools<T>())
            {
                List<T> poolSnapshot = new(pool.ActiveInstances); // copy because Despawn modifies _active

                foreach (T instance in poolSnapshot)
                {
                    yield return instance;
                }
            }
        }

        /// <summary>Total active instances across every pool.</summary>
        public int GetTotalActiveCount()
        {
            int total = 0;

            foreach (KeyValuePair<string, object> kvp in _registry)
            {
                if (kvp.Value is IPoolInfo info)
                    total += info.CountActive;
            }

            return total;
        }

        #endregion
    }
}