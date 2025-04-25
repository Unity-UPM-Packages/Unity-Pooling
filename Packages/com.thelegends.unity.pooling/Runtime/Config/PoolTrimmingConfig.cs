using System;
using UnityEngine;

namespace com.thelegends.unity.pooling
{
    /// <summary>
    /// Configuration for automatic pool trimming, defining when and how inactive objects should be removed.
    /// </summary>
    [Serializable]
    public struct PoolTrimmingConfig
    {
        /// <summary>
        /// Whether automatic trimming is enabled for this pool.
        /// </summary>
        [Tooltip("Enable automatic trimming of inactive pool objects")]
        public bool enableAutoTrim;
        
        /// <summary>
        /// Time interval in seconds between trim checks.
        /// </summary>
        [Tooltip("How often (in seconds) to check for objects to trim")]
        public float trimCheckInterval;
        
        /// <summary>
        /// Time in seconds an object must be inactive before it becomes eligible for trimming.
        /// </summary>
        [Tooltip("How long (in seconds) an object must be inactive before it can be trimmed")]
        public float inactiveTimeThreshold;
        
        /// <summary>
        /// Percentage of eligible objects to keep after trimming (0.0-1.0).
        /// </summary>
        [Tooltip("Percentage of eligible inactive objects to retain (0.0-1.0)")]
        [Range(0f, 1f)]
        public float targetRetainRatio;
        
        /// <summary>
        /// Minimum number of inactive objects to keep in the pool regardless of other settings.
        /// </summary>
        [Tooltip("Minimum number of inactive objects to always keep in the pool")]
        public int minimumRetainCount;
        
        /// <summary>
        /// Creates a new PoolTrimmingConfig with the specified parameters.
        /// </summary>
        public PoolTrimmingConfig(bool enableAutoTrim, float trimCheckInterval, float inactiveTimeThreshold, 
                               float targetRetainRatio, int minimumRetainCount)
        {
            this.enableAutoTrim = enableAutoTrim;
            this.trimCheckInterval = trimCheckInterval;
            this.inactiveTimeThreshold = inactiveTimeThreshold;
            this.targetRetainRatio = Mathf.Clamp01(targetRetainRatio);
            this.minimumRetainCount = minimumRetainCount;
        }
        
        /// <summary>
        /// Gets the default pool trimming configuration as specified in the design plan.
        /// </summary>
        public static PoolTrimmingConfig Default => new PoolTrimmingConfig(
            enableAutoTrim: true,
            trimCheckInterval: 30.0f,
            inactiveTimeThreshold: 60.0f,
            targetRetainRatio: 0.5f,
            minimumRetainCount: 5
        );
        
        /// <summary>
        /// Creates a configuration that disables automatic trimming.
        /// </summary>
        public static PoolTrimmingConfig Disabled => new PoolTrimmingConfig(
            enableAutoTrim: false,
            trimCheckInterval: 30.0f,
            inactiveTimeThreshold: 60.0f,
            targetRetainRatio: 0.5f,
            minimumRetainCount: 5
        );
        
        /// <summary>
        /// Creates an aggressive trimming configuration that removes more objects more frequently.
        /// </summary>
        public static PoolTrimmingConfig Aggressive => new PoolTrimmingConfig(
            enableAutoTrim: true,
            trimCheckInterval: 15.0f,
            inactiveTimeThreshold: 30.0f,
            targetRetainRatio: 0.25f,
            minimumRetainCount: 3
        );
        
        /// <summary>
        /// Creates a conservative trimming configuration that removes fewer objects less frequently.
        /// </summary>
        public static PoolTrimmingConfig Conservative => new PoolTrimmingConfig(
            enableAutoTrim: true,
            trimCheckInterval: 60.0f,
            inactiveTimeThreshold: 120.0f,
            targetRetainRatio: 0.75f,
            minimumRetainCount: 10
        );
    }
} 