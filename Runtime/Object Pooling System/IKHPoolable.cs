using UnityEngine.SceneManagement;

namespace KH
{
    /// <summary>
    /// Implement this on any MonoBehaviour you want to pool.
    /// </summary>
    public interface IKHPoolable
    {
        /// <summary>Called by the pool just before the object is handed out.</summary>
        void OnSpawn();

        /// <summary>Called by the pool just after the object is returned.</summary>
        void OnDespawn();
    }

    // non-generic marker, same pattern as IDisposablePool
    internal interface IPoolInfo
    {
        int CountActive { get; }
        int CountTotal { get; }
    }

    // Lets the manager dispose and filter pools without knowing T.
    internal interface IDisposablePool
    {
        bool Persistent { get; }
        Scene OwnerScene { get; }
        void Dispose();
    }
}