using System.Collections.Generic;
using UnityEngine;

namespace KH
{
    [AddComponentMenu("KH/Systems/" + nameof(KHSystemHub))]
    [System.Serializable]
    public class KHSystemHub : MonoBehaviour
    {
        #region FIELDS

        public static KHSystemHub Ins { get; private set; }

        private readonly List<IKHManagedUpdate> updates = new();
        private readonly List<IKHManagedFixedUpdate> fixedUpdates = new();

        // reusable buffers to avoid GC allocation every frame
        private readonly List<IKHManagedUpdate> updatesBuffer = new();
        private readonly List<IKHManagedFixedUpdate> fixedUpdatesBuffer = new();

        #endregion
        #region UNITY EVENTS

        private void Awake()
        {
            Ins = this;
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


        #endregion
        #region PUBLIC

        public void Add(object obj)
        {
            if (obj is IKHManagedUpdate u && !updates.Contains(u))
                updates.Add(u);

            if (obj is IKHManagedFixedUpdate f && !fixedUpdates.Contains(f))
                fixedUpdates.Add(f);
        }

        public void Remove(object obj)
        {
            if (obj is IKHManagedUpdate u)
                updates.Remove(u);

            if (obj is IKHManagedFixedUpdate f)
                fixedUpdates.Remove(f);
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