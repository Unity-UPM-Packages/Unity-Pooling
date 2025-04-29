using System;
using UnityEngine;

namespace com.thelegends.unity.pooling
{
    /// <summary>
    /// Configuration for handling Addressable asset loading errors.
    /// </summary>
    [Serializable]
    public struct AddressableErrorConfig
    {
        /// <summary>
        /// The strategy to use when an Addressable asset fails to load.
        /// </summary>
        [Tooltip("How to handle Addressable loading errors")]
        public AddressableErrorHandling errorHandlingStrategy;
        
        /// <summary>
        /// Fallback prefab to use when errorHandlingStrategy is set to ReturnPlaceholder.
        /// </summary>
        [Tooltip("Prefab to use when a requested asset fails to load (for ReturnPlaceholder strategy)")]
        public GameObject fallbackPrefab;
        
        /// <summary>
        /// Maximum number of retry attempts when errorHandlingStrategy is set to RetryWithTimeout.
        /// </summary>
        [Tooltip("Number of retry attempts for the RetryWithTimeout strategy")]
        public int maxRetries;
        
        /// <summary>
        /// Delay in seconds between retry attempts.
        /// </summary>
        [Tooltip("Delay in seconds between retry attempts")]
        public float retryDelay;
        
        /// <summary>
        /// Optional callback that is invoked when an Addressable load error occurs.
        /// This can be used for logging, analytics, or custom error handling.
        /// </summary>
        [NonSerialized]
        public Action<Exception, string> onAddressableLoadError;
        
        /// <summary>
        /// Creates a new AddressableErrorConfig with the specified parameters.
        /// </summary>
        public AddressableErrorConfig(
            AddressableErrorHandling errorHandlingStrategy, 
            GameObject fallbackPrefab = null, 
            int maxRetries = 3, 
            float retryDelay = 1.0f, 
            Action<Exception, string> onAddressableLoadError = null)
        {
            this.errorHandlingStrategy = errorHandlingStrategy;
            this.fallbackPrefab = fallbackPrefab;
            this.maxRetries = maxRetries;
            this.retryDelay = retryDelay;
            this.onAddressableLoadError = onAddressableLoadError;
        }
        
        /// <summary>
        /// Gets the default error handling configuration (LogAndReturnNull strategy).
        /// </summary>
        public static AddressableErrorConfig Default => new AddressableErrorConfig(
            errorHandlingStrategy: AddressableErrorHandling.LogAndReturnNull,
            fallbackPrefab: null,
            maxRetries: 3,
            retryDelay: 1.0f
        );
        
        /// <summary>
        /// Creates a configuration for the LogAndReturnNull strategy.
        /// This strategy logs errors and returns null when Addressable loading fails.
        /// </summary>
        /// <param name="onError">Optional callback to execute when an error occurs</param>
        /// <returns>A new AddressableErrorConfig configured for the LogAndReturnNull strategy</returns>
        public static AddressableErrorConfig LogAndReturnNullConfig(Action<Exception, string> onError = null) => 
            new AddressableErrorConfig(
                errorHandlingStrategy: AddressableErrorHandling.LogAndReturnNull,
                fallbackPrefab: null,
                maxRetries: 0,
                retryDelay: 0f,
                onAddressableLoadError: onError
            );
            
        /// <summary>
        /// Creates a configuration for the ThrowException strategy.
        /// This strategy throws exceptions when Addressable loading fails, allowing for try/catch handling.
        /// </summary>
        /// <param name="onError">Optional callback that runs before the exception is thrown</param>
        /// <returns>A new AddressableErrorConfig configured for the ThrowException strategy</returns>
        public static AddressableErrorConfig ThrowExceptionConfig(Action<Exception, string> onError = null) => 
            new AddressableErrorConfig(
                errorHandlingStrategy: AddressableErrorHandling.ThrowException,
                fallbackPrefab: null,
                maxRetries: 0,
                retryDelay: 0f,
                onAddressableLoadError: onError
            );
            
        /// <summary>
        /// Creates a configuration for the ReturnPlaceholder strategy.
        /// This strategy returns a fallback prefab when Addressable loading fails.
        /// </summary>
        /// <param name="fallbackPrefab">Required prefab to use as a placeholder (must not be null)</param>
        /// <param name="onError">Optional callback when errors occur</param>
        /// <returns>A new AddressableErrorConfig configured for the ReturnPlaceholder strategy</returns>
        public static AddressableErrorConfig ReturnPlaceholderConfig(
            GameObject fallbackPrefab, 
            Action<Exception, string> onError = null)
        {
            if (fallbackPrefab == null)
            {
                Debug.LogWarning("ReturnPlaceholder strategy requires a non-null fallbackPrefab. Consider using LogAndReturnNullConfig instead.");
            }
            
            return new AddressableErrorConfig(
                errorHandlingStrategy: AddressableErrorHandling.ReturnPlaceholder,
                fallbackPrefab: fallbackPrefab,
                maxRetries: 0,
                retryDelay: 0f,
                onAddressableLoadError: onError
            );
        }
            
        /// <summary>
        /// Creates a configuration for the RetryWithTimeout strategy.
        /// This strategy retries failed Addressable loading attempts with a delay between retries.
        /// </summary>
        /// <param name="maxRetries">Maximum number of retry attempts</param>
        /// <param name="retryDelay">Delay in seconds between retry attempts</param>
        /// <param name="onError">Optional callback when errors occur</param>
        /// <returns>A new AddressableErrorConfig configured for the RetryWithTimeout strategy</returns>
        public static AddressableErrorConfig RetryWithTimeoutConfig(
            int maxRetries = 3,
            float retryDelay = 1.0f,
            Action<Exception, string> onError = null) => 
            new AddressableErrorConfig(
                errorHandlingStrategy: AddressableErrorHandling.RetryWithTimeout,
                fallbackPrefab: null,
                maxRetries: maxRetries,
                retryDelay: retryDelay,
                onAddressableLoadError: onError
            );
    }
}