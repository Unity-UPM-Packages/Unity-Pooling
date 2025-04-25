using System;

namespace com.thelegends.unity.pooling
{
    /// <summary>
    /// Defines the strategies for handling errors during Addressable asset loading operations.
    /// </summary>
    public enum AddressableErrorHandling
    {
        /// <summary>
        /// Logs the error and returns null. The calling code needs to handle null returns.
        /// </summary>
        LogAndReturnNull = 0,
        
        /// <summary>
        /// Throws the original exception from the Addressable system. 
        /// This will crash the application if not caught by the calling code.
        /// </summary>
        ThrowException = 1,
        
        /// <summary>
        /// Returns a placeholder object (defined in AddressableErrorConfig) instead of the requested asset.
        /// This keeps the game running but may have visual artifacts.
        /// </summary>
        ReturnPlaceholder = 2,
        
        /// <summary>
        /// Retries the load operation a specified number of times with a delay between attempts.
        /// If all retries fail, falls back to LogAndReturnNull.
        /// </summary>
        RetryWithTimeout = 3
    }
} 