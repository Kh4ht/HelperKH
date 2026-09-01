using System.Collections.Generic;
using UnityEngine;

namespace KH
{
    [AddComponentMenu("KH/System Hub")]
    [System.Serializable]
    public class KHSystemHub : MonoBehaviour
    {
        #region FIELDS

        public static KHSystemHub Instance { get; private set; }

        private readonly List<IKHManagedUpdate> _updates = new();
        private readonly List<IKHManagedFixedUpdate> _fixedUpdates = new();

        #endregion
        #region UNITY EVENTS

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            for (int i = _updates.Count - 1; i >= 0; i--)
            {
                if (_updates[i] == null)
                {
                    _updates.RemoveAt(i);
                    continue;
                }

                _updates[i].KHManagedUpdate();
            }
        }

        private void FixedUpdate()
        {
            for (int i = _fixedUpdates.Count - 1; i >= 0; i--)
            {
                if (_fixedUpdates[i] == null)
                {
                    _fixedUpdates.RemoveAt(i);
                    continue;
                }

                _fixedUpdates[i].KHManagedFixedUpdate();
            }
        }


        #endregion
        #region PUBLIC

        public void Add(object obj)
        {
            if (obj is IKHManagedUpdate u && !_updates.Contains(u))
                _updates.Add(u);

            if (obj is IKHManagedFixedUpdate f && !_fixedUpdates.Contains(f))
                _fixedUpdates.Add(f);
        }

        public void Remove(object obj)
        {
            if (obj is IKHManagedUpdate u)
                _updates.Remove(u);

            if (obj is IKHManagedFixedUpdate f)
                _fixedUpdates.Remove(f);
        }

        #endregion
    }

    #region INTERFACE

    public interface IKHManagedUpdate
    {
        void KHManagedUpdate();
    }

    public interface IKHManagedFixedUpdate
    {
        void KHManagedFixedUpdate();
    }

    #endregion
    #region ManagedBehaviour

    public abstract class KHManagedBehaviour : MonoBehaviour
    {
        protected virtual void Start()
        {
            if (KHSystemHub.Instance != null)
                KHSystemHub.Instance.Add(this);
        }

        protected virtual void OnEnable()
        {
            if (KHSystemHub.Instance != null)
                KHSystemHub.Instance.Add(this);
        }

        protected virtual void OnDisable()
        {
            if (KHSystemHub.Instance != null)
                KHSystemHub.Instance.Remove(this);
        }
    }

    #endregion
}