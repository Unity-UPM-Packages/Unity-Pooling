using System;
using UnityEngine;

namespace com.thelegends.unity.pooling
{
    /// <summary>
    /// Configuration for a pool, defining its initial size, growth behavior, and maximum size.
    /// </summary>
    [Serializable]
    public struct PoolConfig
    {
        /// <summary>
        /// The initial number of objects to prewarm in the pool.
        /// </summary>
        [Tooltip("Initial number of objects to create when the pool is initialized")]
        public int initialSize;

        /// <summary>
        /// Whether the pool is allowed to grow when more objects are requested than available.
        /// </summary>
        [Tooltip("Whether the pool should create more instances when all current instances are in use")]
        public bool allowGrowth;

        /// <summary>
        /// The maximum size the pool can grow to, if allowGrowth is true.
        /// </summary>
        [Tooltip("Maximum number of instances this pool can grow to (if allowGrowth is true)")]
        public int maxSize;
        
        /// <summary>
        /// Strategy to use when the pool reaches its maximum size.
        /// </summary>
        [Tooltip("How to handle requests when the pool reaches its maximum size")]
        public PoolRecyclingStrategy recyclingStrategy;

        /// <summary>
        /// Creates a new PoolConfig with the specified parameters.
        /// </summary>
        /// <param name="initialSize">Initial number of objects to prewarm</param>
        /// <param name="allowGrowth">Whether the pool can grow beyond initial size</param>
        /// <param name="maxSize">Maximum size the pool can grow to</param>
        /// <param name="recyclingStrategy">Strategy to use when pool reaches maximum size</param>
        public PoolConfig(int initialSize, bool allowGrowth, int maxSize, PoolRecyclingStrategy recyclingStrategy = PoolRecyclingStrategy.ExceedMaxSizeTemporarily)
        {
            this.initialSize = initialSize;
            this.allowGrowth = allowGrowth;
            this.maxSize = maxSize;
            this.recyclingStrategy = recyclingStrategy;
        }

        /// <summary>
        /// Gets the default pool configuration as specified in the design plan (ExceedMaxSizeTemporarily strategy).
        /// </summary>
        public static PoolConfig Default => new PoolConfig(10, true, 100, PoolRecyclingStrategy.ExceedMaxSizeTemporarily);
        
        /// <summary>
        /// Creates a pool configuration that returns null when the pool reaches its maximum size.
        /// This is the traditional object pooling behavior.
        /// </summary>
        /// <param name="initialSize">Initial number of objects to prewarm</param>
        /// <param name="allowGrowth">Whether the pool can grow beyond initial size</param>
        /// <param name="maxSize">Maximum size the pool can grow to</param>
        /// <returns>A pool configuration with ReturnNull strategy</returns>
        public static PoolConfig ReturnNullConfig(int initialSize, bool allowGrowth, int maxSize) =>
            new PoolConfig(initialSize, allowGrowth, maxSize, PoolRecyclingStrategy.ReturnNull);
            
        /// <summary>
        /// Creates a pool configuration that automatically recycles the least recently used object
        /// when the pool reaches its maximum size. This ensures a request always returns an object.
        /// </summary>
        /// <param name="initialSize">Initial number of objects to prewarm</param>
        /// <param name="allowGrowth">Whether the pool can grow beyond initial size</param>
        /// <param name="maxSize">Maximum size the pool can grow to</param>
        /// <returns>A pool configuration with RecycleLeastRecentlyUsed strategy</returns>
        public static PoolConfig RecycleLRUConfig(int initialSize, bool allowGrowth, int maxSize) => 
            new PoolConfig(initialSize, allowGrowth, maxSize, PoolRecyclingStrategy.RecycleLeastRecentlyUsed);
            
        /// <summary>
        /// Creates a pool configuration that temporarily allows exceeding the maximum size limit
        /// when the pool is full. The pool will try to shrink back when possible.
        /// </summary>
        /// <param name="initialSize">Initial number of objects to prewarm</param>
        /// <param name="allowGrowth">Whether the pool can grow beyond initial size</param>
        /// <param name="maxSize">Soft maximum size limit (may be exceeded temporarily)</param>
        /// <returns>A pool configuration with ExceedMaxSizeTemporarily strategy</returns>
        public static PoolConfig ExceedMaxSizeConfig(int initialSize, bool allowGrowth, int maxSize) => 
            new PoolConfig(initialSize, allowGrowth, maxSize, PoolRecyclingStrategy.ExceedMaxSizeTemporarily);
            
        /// <summary>
        /// Creates a fixed-size pool configuration that won't grow beyond the initial size.
        /// Uses the ReturnNull strategy when the pool is exhausted.
        /// </summary>
        /// <param name="fixedSize">The fixed size of the pool (both initial and max size)</param>
        /// <returns>A pool configuration with fixed size and ReturnNull strategy</returns>
        public static PoolConfig FixedSizeConfig(int fixedSize) => 
            new PoolConfig(fixedSize, false, fixedSize, PoolRecyclingStrategy.ReturnNull);
    }
}