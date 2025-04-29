using UnityEngine;

namespace com.thelegends.unity.pooling
{
    /// <summary>
    /// Defines strategies for handling situations when a pool reaches its maximum size.
    /// These strategies determine how the system should behave when attempting to get an object
    /// from a pool that has reached its configured maxSize limit.
    /// </summary>
    public enum PoolRecyclingStrategy
    {
        /// <summary>
        /// Return null when the pool is full, letting the caller handle the situation.
        /// This is the default strategy and maintains backward compatibility.
        /// </summary>
        ReturnNull,
        
        /// <summary>
        /// Automatically recycle the least recently used active object.
        /// Useful for ensuring a request always returns an object, prioritizing newer requests.
        /// </summary>
        RecycleLeastRecentlyUsed,
        
        /// <summary>
        /// Temporarily exceed the maxSize limit, allowing the pool to grow beyond its
        /// configured size. The pool will attempt to shrink back when possible.
        /// Use with caution as this can lead to unexpected memory usage.
        /// </summary>
        ExceedMaxSizeTemporarily
    }
}