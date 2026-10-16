using System.Collections.Generic;
using UnityEngine;

namespace KH
{
    [AddComponentMenu("KH/Systems/" + nameof(KHSystemHub))]
    public class KHSystemHub : MonoBehaviour
    {
        #region FIELDS

        [KHResetStatic]
        public static KHSystemHub Ins { get; private set; }

        private readonly List<IKHManagedUpdate> updates = new();
        private readonly List<IKHManagedFixedUpdate> fixedUpdates = new();
        private readonly List<IKHManagedLateUpdate> lateUpdates = new();

        // reusable buffers to avoid GC allocation every frame
        private readonly List<IKHManagedUpdate> updatesBuffer = new();
        private readonly List<IKHManagedFixedUpdate> fixedUpdatesBuffer = new();
        private readonly List<IKHManagedLateUpdate> lateUpdatesBuffer = new();

        #endregion
        #region BOOTSTRAP

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Ins != null)
                return;

            // AddComponent runs Awake immediately, which sets Ins and makes it persistent.
            new GameObject(nameof(KHSystemHub)).AddComponent<KHSystemHub>();
        }

        #endregion
        #region UNITY EVENTS

        private void Awake()
        {
            // Singleton guard: if a hub was also placed in a scene, keep the first one.
            if (Ins != null && Ins != this)
            {
                Destroy(gameObject);
                return;
            }

            Ins = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            updatesBuffer.Clear();
            updatesBuffer.AddRange(updates);

            for (int i = 0; i < updatesBuffer.Count; i++)
            {
                var item = updatesBuffer[i];

                // it may have been removed from the *real* list already,
                // but as long as it's not null, it's still safe to call
                if (item == null)
                    continue;

                item.KHUpdate();
            }

            // clean up any nulls that accumulated in the real list
            updates.RemoveAll(u => u == null);
        }

        private void FixedUpdate()
        {
            fixedUpdatesBuffer.Clear();
            fixedUpdatesBuffer.AddRange(fixedUpdates);

            for (int i = 0; i < fixedUpdatesBuffer.Count; i++)
            {
                var item = fixedUpdatesBuffer[i];

                if (item == null)
                    continue;

                item.KHFixedUpdate();
            }

            fixedUpdates.RemoveAll(f => f == null);
        }

        private void LateUpdate()
        {
            lateUpdatesBuffer.Clear();
            lateUpdatesBuffer.AddRange(lateUpdates);

            for (int i = 0; i < lateUpdatesBuffer.Count; i++)
            {
                var item = lateUpdatesBuffer[i];
                if (item == null)
                    continue;

                item.KHLateUpdate();
            }

            lateUpdates.RemoveAll(l => l == null);
        }

        private void OnDestroy()
        {
            if (Ins == this)
                Ins = null;
        }

        #endregion
        #region PUBLIC

        public void Add(object obj)
        {
            if (obj is IKHManagedUpdate u && !updates.Contains(u))
                updates.Add(u);

            if (obj is IKHManagedFixedUpdate f && !fixedUpdates.Contains(f))
                fixedUpdates.Add(f);

            if (obj is IKHManagedLateUpdate l && !lateUpdates.Contains(l))
                lateUpdates.Add(l);
        }

        public void Remove(object obj)
        {
            if (obj is IKHManagedUpdate u)
                updates.Remove(u);

            if (obj is IKHManagedFixedUpdate f)
                fixedUpdates.Remove(f);

            if (obj is IKHManagedLateUpdate l)
                lateUpdates.Remove(l);
        }

        #endregion
    }

    #region INTERFACE

    public interface IKHManagedUpdate
    {
        void KHUpdate();
    }

    public interface IKHManagedFixedUpdate
    {
        void KHFixedUpdate();
    }

    public interface IKHManagedLateUpdate
    {
        void KHLateUpdate();
    }

    #endregion
    #region ManagedBehaviour

    public abstract class KHManagedBehaviour : MonoBehaviour
    {
        protected virtual void Start()
        {
            if (KHSystemHub.Ins != null)
                KHSystemHub.Ins.Add(this);
        }

        protected virtual void OnEnable()
        {
            if (KHSystemHub.Ins != null)
                KHSystemHub.Ins.Add(this);
        }

        protected virtual void OnDisable()
        {
            if (KHSystemHub.Ins != null)
                KHSystemHub.Ins.Remove(this);
        }
    }

    #endregion
}