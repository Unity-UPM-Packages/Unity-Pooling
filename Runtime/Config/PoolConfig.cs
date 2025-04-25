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
        /// Creates a new PoolConfig with the specified parameters.
        /// </summary>
        /// <param name="initialSize">Initial number of objects to prewarm</param>
        /// <param name="allowGrowth">Whether the pool can grow beyond initial size</param>
        /// <param name="maxSize">Maximum size the pool can grow to</param>
        public PoolConfig(int initialSize, bool allowGrowth, int maxSize)
        {
            this.initialSize = initialSize;
            this.allowGrowth = allowGrowth;
            this.maxSize = maxSize;
        }

        /// <summary>
        /// Gets the default pool configuration as specified in the design plan.
        /// </summary>
        public static PoolConfig Default => new PoolConfig(10, true, 100);

        /// <summary>
        /// Creates a new pool configuration with the specified initial size, using default values for other parameters.
        /// </summary>
        /// <param name="initialSize">Initial number of objects to prewarm</param>
        /// <returns>A new PoolConfig with the specified initial size and default values for other parameters</returns>
        public static PoolConfig WithInitialSize(int initialSize) => new PoolConfig(initialSize, true, Math.Max(initialSize * 2, 100));

        /// <summary>
        /// Creates a fixed-size pool configuration that won't grow beyond the initial size.
        /// </summary>
        /// <param name="size">The fixed size of the pool</param>
        /// <returns>A new PoolConfig with fixed size (won't grow)</returns>
        public static PoolConfig FixedSize(int size) => new PoolConfig(size, false, size);
    }
} 