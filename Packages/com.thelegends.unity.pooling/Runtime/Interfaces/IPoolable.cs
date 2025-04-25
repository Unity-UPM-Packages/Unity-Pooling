using UnityEngine;

namespace com.thelegends.unity.pooling
{
    /// <summary>
    /// Interface for objects that need to be notified when they are retrieved from or returned to a pool.
    /// Implement this interface on MonoBehaviour components to receive callbacks when the GameObject 
    /// is activated or deactivated through the pooling system.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// Called when the object is retrieved from a pool.
        /// Use this method to initialize or reset the object's state.
        /// </summary>
        void OnGetFromPool();

        /// <summary>
        /// Called when the object is returned to a pool.
        /// Use this method to clean up resources or reset the object's state before it becomes inactive.
        /// </summary>
        void OnReturnToPool();
    }
} 