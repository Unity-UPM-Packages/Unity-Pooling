using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace com.thelegends.unity.pooling
{
    /// <summary>
    /// Core object pooling class that manages a collection of GameObjects for a specific prefab.
    /// Handles instantiation, recycling, and automatic trimming of pooled objects.
    /// </summary>
    /// <typeparam name="TKey">The type of key used to identify this pool (typically string for Addressables or GameObject for direct references)</typeparam>
    public class ObjectPool<TKey>
    {
        #region Fields and Properties
        
        // Configuration
        protected readonly PoolConfig _poolConfig;
        protected readonly PoolTrimmingConfig _trimmingConfig;
        protected readonly AddressableErrorConfig _addressableErrorConfig;
        
        // The key used to identify this pool and load the prefab
        protected readonly TKey _prefabKey;
        
        // Collections to track instances
        protected readonly Stack<GameObject> _inactiveObjects;
        protected readonly List<GameObject> _activeObjects;

        // Prefab management
        protected GameObject _parentContainer;
        protected AsyncOperationHandle<GameObject> _prefabLoadHandle;
        protected GameObject _loadedPrefab;
        
        // Trimming
        protected float _lastTrimTime;
        protected bool _isInitialized;
        
        // Stats
        protected int _totalCreated;
        protected int _totalGets;
        protected int _totalReturns;
        protected int _peakActive;
        protected int _failedGets;
        
        /// <summary>
        /// Gets whether this pool has been initialized with a prefab.
        /// </summary>
        public bool IsInitialized => _isInitialized;
        
        /// <summary>
        /// Gets the key used to identify this pool.
        /// </summary>
        public TKey PrefabKey => _prefabKey;
        
        /// <summary>
        /// Gets the current count of active objects from this pool.
        /// </summary>
        public int ActiveCount => _activeObjects.Count;
        
        /// <summary>
        /// Gets the current count of inactive (available) objects in this pool.
        /// </summary>
        public int InactiveCount => _inactiveObjects.Count;
        
        /// <summary>
        /// Gets the total number of objects created by this pool since initialization.
        /// </summary>
        public int TotalCreated => _totalCreated;
        
        /// <summary>
        /// Gets the total number of Get operations performed on this pool.
        /// </summary>
        public int TotalGets => _totalGets;
        
        /// <summary>
        /// Gets the total number of Return operations performed on this pool.
        /// </summary>
        public int TotalReturns => _totalReturns;
        
        /// <summary>
        /// Gets the peak number of simultaneously active objects from this pool.
        /// </summary>
        public int PeakActive => _peakActive;
        
        /// <summary>
        /// Gets the number of failed Get operations (typically due to Addressable loading errors).
        /// </summary>
        public int FailedGets => _failedGets;
        
        /// <summary>
        /// Gets the efficiency of this pool as a ratio of successful operations to creation operations.
        /// A higher ratio indicates better reuse of objects.
        /// </summary>
        public float EfficiencyRatio => _totalCreated > 0 ? (float)_totalGets / _totalCreated : 0f;
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Creates a new ObjectPool for the specified prefab key with the given configurations.
        /// </summary>
        /// <param name="prefabKey">The key used to identify and load the prefab</param>
        /// <param name="poolConfig">Configuration for initial size and growth behavior</param>
        /// <param name="addressableErrorConfig">Configuration for handling Addressable loading errors</param>
        /// <param name="trimmingConfig">Configuration for automatic pool trimming</param>
        public ObjectPool(TKey prefabKey, PoolConfig poolConfig, AddressableErrorConfig addressableErrorConfig, PoolTrimmingConfig trimmingConfig)
        {
            _prefabKey = prefabKey;
            _poolConfig = poolConfig;
            _addressableErrorConfig = addressableErrorConfig;
            _trimmingConfig = trimmingConfig;
            
            // Initialize collections
            _inactiveObjects = new Stack<GameObject>(_poolConfig.initialSize);
            _activeObjects = new List<GameObject>(_poolConfig.initialSize);
            
            // Initialize stats
            _totalCreated = 0;
            _totalGets = 0;
            _totalReturns = 0;
            _peakActive = 0;
            _failedGets = 0;
            
            // Initialize trimming
            _lastTrimTime = Time.time;
            _isInitialized = false;
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initializes the pool by loading the prefab and creating the initial instances.
        /// </summary>
        /// <returns>A task that completes when the pool is initialized</returns>
        public virtual async Task<bool> InitializeAsync()
        {
            if (_isInitialized)
            {
                return true;
            }
            
            try
            {
                // Load the prefab
                bool prefabLoaded = await LoadPrefabAsync();
                if (!prefabLoaded || _loadedPrefab == null)
                {
                    Debug.LogError($"[ObjectPool] Failed to load prefab for key {_prefabKey}");
                    return false;
                }

                // Create parent container if it doesn't exist
                if (_parentContainer == null)
                {
                    string containerName = $"Pool - {_loadedPrefab.name}";
                    _parentContainer = new GameObject(containerName);
                    
                    // Add a component to mark this as a pool container (for editor visualization)
                    var containerMarker = _parentContainer.AddComponent<PoolContainerMarker>();
                    containerMarker.Initialize(_prefabKey.ToString(), this);

                    // Parent container to PoolManager (which is already DontDestroyOnLoad)
                    _parentContainer.transform.SetParent(PoolManager.Instance.transform);
                }
                
                // Create initial pool objects
                for (int i = 0; i < _poolConfig.initialSize; i++)
                {
                    GameObject instance = CreateInstance();
                    if (instance != null)
                    {
                        instance.SetActive(false);
                        _inactiveObjects.Push(instance);
                    }
                }
                
                _isInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ObjectPool] Error initializing pool for {_prefabKey}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Loads the prefab from Addressables using the prefab key.
        /// </summary>
        protected virtual async Task<bool> LoadPrefabAsync()
        {
            if (_loadedPrefab != null)
            {
                return true;
            }
            
            try
            {
                if (_prefabKey is string assetKey)
                {
                    // Load prefab from Addressables using the string key
                    _prefabLoadHandle = Addressables.LoadAssetAsync<GameObject>(assetKey);
                    await _prefabLoadHandle.Task;
                    
                    if (_prefabLoadHandle.Status == AsyncOperationStatus.Succeeded)
                    {
                        _loadedPrefab = _prefabLoadHandle.Result;
                        return true;
                    }
                    else
                    {
                        // Handle error based on the error config
                        HandleAddressableLoadError(null, assetKey.ToString());
                        return false;
                    }
                }
                else if (_prefabKey is GameObject directPrefab)
                {
                    // Direct reference to a GameObject prefab
                    _loadedPrefab = directPrefab;
                    return true;
                }
                else
                {
                    Debug.LogError($"[ObjectPool] Unsupported prefab key type: {typeof(TKey).Name}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                string keyStr = _prefabKey?.ToString() ?? "null";
                return await HandleAddressableLoadError(ex, keyStr);
            }
        }
        
        /// <summary>
        /// Handles Addressable loading errors according to the configured strategy.
        /// </summary>
        protected virtual async Task<bool> HandleAddressableLoadError(Exception ex, string keyStr)
        {
            // Invoke error callback if provided
            _addressableErrorConfig.onAddressableLoadError?.Invoke(ex, keyStr);
            
            switch (_addressableErrorConfig.errorHandlingStrategy)
            {
                case AddressableErrorHandling.ThrowException:
                    if (ex != null)
                    {
                        throw ex;
                    }
                    else
                    {
                        throw new Exception($"Failed to load Addressable asset: {keyStr}");
                    }
                    
                case AddressableErrorHandling.ReturnPlaceholder:
                    if (_addressableErrorConfig.fallbackPrefab != null)
                    {
                        _loadedPrefab = _addressableErrorConfig.fallbackPrefab;
                        Debug.LogWarning($"[ObjectPool] Using fallback prefab for failed Addressable load: {keyStr}");
                        return true;
                    }
                    Debug.LogError($"[ObjectPool] No fallback prefab configured for ReturnPlaceholder strategy: {keyStr}");
                    return false;
                    
                case AddressableErrorHandling.RetryWithTimeout:
                    return await RetryLoadWithTimeout(keyStr);
                    
                case AddressableErrorHandling.LogAndReturnNull:
                default:
                    Debug.LogError($"[ObjectPool] Failed to load Addressable asset: {keyStr}, Error: {ex?.Message ?? "Unknown error"}");
                    return false;
            }
        }
        
        /// <summary>
        /// Retries loading the Addressable asset with a timeout based on the error config.
        /// </summary>
        protected virtual async Task<bool> RetryLoadWithTimeout(string keyStr)
        {
            int retryCount = 0;
            
            while (retryCount < _addressableErrorConfig.maxRetries)
            {
                Debug.LogWarning($"[ObjectPool] Retrying Addressable load ({retryCount + 1}/{_addressableErrorConfig.maxRetries}): {keyStr}");
                retryCount++;
                
                // Wait before retrying
                await Task.Delay(TimeSpan.FromSeconds(_addressableErrorConfig.retryDelay));
                
                try
                {
                    // Retry the load
                    _prefabLoadHandle = Addressables.LoadAssetAsync<GameObject>(keyStr);
                    await _prefabLoadHandle.Task;
                    
                    if (_prefabLoadHandle.Status == AsyncOperationStatus.Succeeded)
                    {
                        _loadedPrefab = _prefabLoadHandle.Result;
                        Debug.Log($"[ObjectPool] Successfully loaded Addressable after {retryCount} retries: {keyStr}");
                        return true;
                    }
                }
                catch (Exception)
                {
                    // Continue to next retry
                }
            }
            
            Debug.LogError($"[ObjectPool] Failed to load Addressable after {retryCount} retries: {keyStr}");
            return false;
        }
        
        #endregion
        
        #region Pool Operations
        
        /// <summary>
        /// Gets an object from the pool asynchronously with enhanced handling for maxSize limits.
        /// </summary>
        /// <returns>A task that resolves to the pooled GameObject, or null if the operation fails</returns>
        public virtual async Task<GameObject> GetAsync()
        {
            // Make sure pool is initialized
            if (!_isInitialized)
            {
                bool initialized = await InitializeAsync();
                if (!initialized)
                {
                    _failedGets++;
                    return null;
                }
            }
            
            // Get or create an instance
            GameObject instance = null;
            
            // Check if we have inactive objects available
            if (_inactiveObjects.Count > 0)
            {
                instance = _inactiveObjects.Pop();
            }
            // Otherwise, create new if growth is allowed and we haven't hit the max size
            else if (_poolConfig.allowGrowth && (_activeObjects.Count + _inactiveObjects.Count) < _poolConfig.maxSize)
            {
                instance = CreateInstance();
            }
            // Apply recycling strategy if we've hit our size limit
            else if (_poolConfig.allowGrowth && _poolConfig.recyclingStrategy != PoolRecyclingStrategy.ReturnNull)
            {
                switch (_poolConfig.recyclingStrategy)
                {
                    case PoolRecyclingStrategy.RecycleLeastRecentlyUsed:
                        // Find and recycle the least recently used active object
                        GameObject lruObject = FindLeastRecentlyUsedObject();
                        if (lruObject != null)
                        {
                            Debug.Log($"[ObjectPool] MaxSize limit reached for {_prefabKey}. Recycling least recently used object.");
                            
                            // Return the LRU object to the pool
                            Return(lruObject);
                            
                            // Get the newly returned object
                            instance = _inactiveObjects.Pop();
                        }
                        break;
                        
                    case PoolRecyclingStrategy.ExceedMaxSizeTemporarily:
                        // Allow creation beyond maxSize temporarily
                        Debug.Log($"[ObjectPool] Temporarily exceeding maxSize limit for {_prefabKey}. Current: {_activeObjects.Count + _inactiveObjects.Count}, Max: {_poolConfig.maxSize}");
                        instance = CreateInstance();
                        
                        // Schedule a check to trim back down when possible
                        StartDynamicResizeCooldown();
                        break;
                }
            }
            
            // Process the instance
            if (instance != null)
            {
                _activeObjects.Add(instance);
                _totalGets++;
                
                // Update peak count if needed
                if (_activeObjects.Count > _peakActive)
                {
                    _peakActive = _activeObjects.Count;
                }
                
                // Setup transform (position, rotation, scale)
                SetupTransform(instance.transform);
                
                // Enable the GameObject
                instance.SetActive(true);
                
                // Notify the PooledObject component
                var pooledObject = instance.GetComponent<PooledObject>();
                if (pooledObject != null)
                {
                    pooledObject.OnGet();
                }
                
                // Additional setup after getting the object
                OnAfterGet(instance);
                
                return instance;
            }
            else
            {
                _failedGets++;
                Debug.LogWarning($"[ObjectPool] Could not get instance from pool for {_prefabKey}. Active: {_activeObjects.Count}, Max: {_poolConfig.maxSize}");
                return null;
            }
        }
        
        /// <summary>
        /// Gets an object from the pool (synchronous version, less preferred).
        /// </summary>
        /// <returns>A pooled GameObject, or null if the operation fails or the pool isn't initialized</returns>
        public virtual GameObject Get()
        {
            // Ensure pool is initialized
            if (!_isInitialized)
            {
                Debug.LogWarning($"[ObjectPool] Cannot get instance synchronously - pool for {_prefabKey} is not initialized");
                _failedGets++;
                return null;
            }
            
            // Get or create an instance
            GameObject instance = null;
            
            // Check if we have inactive objects available
            if (_inactiveObjects.Count > 0)
            {
                instance = _inactiveObjects.Pop();
            }
            // Otherwise, create new if growth is allowed and we haven't hit the max size
            else if (_poolConfig.allowGrowth && (_activeObjects.Count + _inactiveObjects.Count) < _poolConfig.maxSize)
            {
                instance = CreateInstance();
            }
            // Apply recycling strategy if we've hit our size limit
            else if (_poolConfig.allowGrowth && _poolConfig.recyclingStrategy != PoolRecyclingStrategy.ReturnNull)
            {
                switch (_poolConfig.recyclingStrategy)
                {
                    case PoolRecyclingStrategy.RecycleLeastRecentlyUsed:
                        // Find and recycle the least recently used active object
                        GameObject lruObject = FindLeastRecentlyUsedObject();
                        if (lruObject != null)
                        {
                            Debug.Log($"[ObjectPool] MaxSize limit reached for {_prefabKey}. Recycling least recently used object.");
                            
                            // Return the LRU object to the pool
                            Return(lruObject);
                            
                            // Get the newly returned object
                            instance = _inactiveObjects.Pop();
                        }
                        break;
                        
                    case PoolRecyclingStrategy.ExceedMaxSizeTemporarily:
                        // Allow creation beyond maxSize temporarily
                        Debug.Log($"[ObjectPool] Temporarily exceeding maxSize limit for {_prefabKey}. Current: {_activeObjects.Count + _inactiveObjects.Count}, Max: {_poolConfig.maxSize}");
                        instance = CreateInstance();
                        
                        // Schedule a check to trim back down when possible
                        StartDynamicResizeCooldown();
                        break;
                }
            }
            
            // Process the instance
            if (instance != null)
            {
                _activeObjects.Add(instance);
                _totalGets++;
                
                // Update peak count if needed
                if (_activeObjects.Count > _peakActive)
                {
                    _peakActive = _activeObjects.Count;
                }
                
                // Setup transform (position, rotation, scale)
                SetupTransform(instance.transform);
                
                // Enable the GameObject
                instance.SetActive(true);
                
                // Notify the PooledObject component
                var pooledObject = instance.GetComponent<PooledObject>();
                if (pooledObject != null)
                {
                    pooledObject.OnGet();
                }
                
                // Additional setup after getting the object
                OnAfterGet(instance);
                
                return instance;
            }
            else
            {
                _failedGets++;
                Debug.LogWarning($"[ObjectPool] Could not get instance from pool for {_prefabKey}. Active: {_activeObjects.Count}, Max: {_poolConfig.maxSize}");
                return null;
            }
        }
        
        /// <summary>
        /// Returns an object to the pool, making it available for reuse.
        /// </summary>
        /// <param name="instance">The GameObject to return to the pool</param>
        /// <returns>True if the object was successfully returned, false otherwise</returns>
        public virtual bool Return(GameObject instance)
        {
            if (instance == null)
            {
                Debug.LogError("[ObjectPool] Cannot return null instance to pool");
                return false;
            }
            
            // Check if this instance is from this pool
            if (!_activeObjects.Contains(instance))
            {
                Debug.LogWarning($"[ObjectPool] Instance {instance.name} is not from this pool");
                return false;
            }
            
            // Remove from active list
            _activeObjects.Remove(instance);
            
            // Perform return operations
            OnBeforeReturn(instance);
            
            // Deactivate
            instance.SetActive(false);
            
            // Reset transform
            ResetTransform(instance.transform);
            
            // Add to inactive list
            _inactiveObjects.Push(instance);
            _totalReturns++;
            
            // Check if we need to trim the pool
            if (_trimmingConfig.enableAutoTrim && 
                Time.time - _lastTrimTime >= _trimmingConfig.trimCheckInterval)
            {
                TrimExcess();
            }
            
            return true;
        }
        
        /// <summary>
        /// Clears the pool, releasing all objects.
        /// </summary>
        public virtual void Clear()
        {
            // Destroy all active instances
            foreach (var instance in _activeObjects)
            {
                if (instance != null)
                {
                    GameObject.Destroy(instance);
                }
            }
            _activeObjects.Clear();
            
            // Destroy all inactive instances
            while (_inactiveObjects.Count > 0)
            {
                var instance = _inactiveObjects.Pop();
                if (instance != null)
                {
                    GameObject.Destroy(instance);
                }
            }
            
            // Release Addressable handle if we have one
            if (_prefabLoadHandle.IsValid())
            {
                Addressables.Release(_prefabLoadHandle);
            }
            
            // Destroy the container GameObject
            if (_parentContainer != null)
            {
                GameObject.Destroy(_parentContainer);
                _parentContainer = null;
            }
            
            _loadedPrefab = null;
            _isInitialized = false;
            
            // Reset stats
            _totalCreated = 0;
            _totalGets = 0;
            _totalReturns = 0;
            _peakActive = 0;
            _failedGets = 0;
        }
        
        /// <summary>
        /// Trims excess inactive objects from the pool based on trimming configuration.
        /// </summary>
        public virtual void TrimExcess()
        {
            _lastTrimTime = Time.time;
            
            if (!_trimmingConfig.enableAutoTrim || _inactiveObjects.Count <= _trimmingConfig.minimumRetainCount)
            {
                return;
            }
            
            // Create a temporary list to evaluate which objects to trim
            var tempList = new List<GameObject>(_inactiveObjects.Count);
            var currentTime = Time.time;
            
            // Move all objects to our temp list for evaluation
            while (_inactiveObjects.Count > 0)
            {
                tempList.Add(_inactiveObjects.Pop());
            }
            
            // Sort by inactive time (oldest first)
            tempList.Sort((a, b) => 
            {
                var pooledA = a.GetComponent<PooledObject>();
                var pooledB = b.GetComponent<PooledObject>();
                
                if (pooledA == null || pooledB == null)
                {
                    return 0;
                }
                
                return pooledA._lastAccessTime.CompareTo(pooledB._lastAccessTime);
            });
            
            // Count eligible objects (inactive long enough)
            int eligibleCount = 0;
            foreach (var obj in tempList)
            {
                var pooledObject = obj.GetComponent<PooledObject>();
                if (pooledObject != null && (currentTime - pooledObject._lastAccessTime) >= _trimmingConfig.inactiveTimeThreshold)
                {
                    eligibleCount++;
                }
            }
            
            // Calculate how many objects to keep
            int keepCount = Mathf.Max(
                _trimmingConfig.minimumRetainCount, 
                Mathf.CeilToInt(eligibleCount * _trimmingConfig.targetRetainRatio)
            );
            
            int destroyed = 0;
            int kept = 0;
            
            // Process each object
            foreach (var obj in tempList)
            {
                var pooledObject = obj.GetComponent<PooledObject>();
                
                // If not eligible for trimming or we haven't reached our keep count, keep it
                if (pooledObject == null || 
                    (currentTime - pooledObject._lastAccessTime) < _trimmingConfig.inactiveTimeThreshold || 
                    kept < keepCount)
                {
                    _inactiveObjects.Push(obj);
                    kept++;
                }
                else
                {
                    // Destroy excess objects
                    GameObject.Destroy(obj);
                    destroyed++;
                }
            }
            
            if (destroyed > 0)
            {
                Debug.Log($"[ObjectPool] Trimmed {destroyed} excess objects from pool for {_prefabKey}");
            }
        }
        
        /// <summary>
        /// Finds the least recently used active object based on access time.
        /// This is used by the RecycleLeastRecentlyUsed strategy when the pool reaches its maximum size.
        /// </summary>
        /// <returns>The least recently used active GameObject, or null if none found</returns>
        protected virtual GameObject FindLeastRecentlyUsedObject()
        {
            if (_activeObjects.Count == 0)
                return null;
                
            GameObject lruObject = _activeObjects[0];
            float oldestTime = float.MaxValue;
            
            // Find the object with the oldest access time
            foreach (var obj in _activeObjects)
            {
                var pooledObject = obj.GetComponent<PooledObject>();
                if (pooledObject != null && pooledObject._lastAccessTime < oldestTime)
                {
                    oldestTime = pooledObject._lastAccessTime;
                    lruObject = obj;
                }
            }
            
            return lruObject;
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Creates a new instance of the pooled prefab.
        /// </summary>
        protected virtual GameObject CreateInstance()
        {
            if (_loadedPrefab == null)
            {
                Debug.LogError($"[ObjectPool] Cannot create instance: prefab not loaded for {_prefabKey}");
                return null;
            }
            
            // Instantiate using the loaded prefab
            GameObject instance = GameObject.Instantiate(_loadedPrefab);
            
            // Name the instance for easier identification
            instance.name = $"{_loadedPrefab.name} (Pooled {_totalCreated})";
            
            // Add or get the PooledObject component
            PooledObject pooledObj = instance.GetComponent<PooledObject>();
            if (pooledObj == null)
            {
                pooledObj = instance.AddComponent<PooledObject>();
            }
            
            // Initialize the PooledObject component
            pooledObj.Initialize(this, _prefabKey);
            
            // Update stats
            _totalCreated++;
            
            // Set parent to the container
            if (_parentContainer != null)
            {
                instance.transform.SetParent(_parentContainer.transform, false);
            }
            
            return instance;
        }
        
        /// <summary>
        /// Sets up the transform of a pooled object when it's retrieved from the pool.
        /// Override in derived classes to provide custom transform handling.
        /// </summary>
        protected virtual void SetupTransform(Transform transform)
        {
            // Base implementation does nothing - the caller will set position/rotation
        }
        
        /// <summary>
        /// Resets the transform of a pooled object when it's returned to the pool.
        /// </summary>
        protected virtual void ResetTransform(Transform transform)
        {
            if (transform == null)
                return;
                
            // Set parent to container if available
            if (_parentContainer != null)
            {
                transform.SetParent(_parentContainer.transform, false);
            }
            else
            {
                transform.SetParent(null);
            }
            
            // Reset position, rotation and scale
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
        
        /// <summary>
        /// Called after an object is retrieved from the pool.
        /// Override in derived classes to perform additional setup (like for UI elements).
        /// </summary>
        protected virtual void OnAfterGet(GameObject instance)
        {
            // Base implementation does nothing
        }
        
        /// <summary>
        /// Called before an object is returned to the pool.
        /// Override in derived classes to perform additional cleanup.
        /// </summary>
        protected virtual void OnBeforeReturn(GameObject instance)
        {
            // Base implementation does nothing
        }
        
        #endregion
        
        #region Dynamic Resizing
        
        /// <summary>
        /// Starts a cooldown coroutine to check and potentially shrink the pool back to its original maxSize
        /// after it was temporarily allowed to exceed its size limit.
        /// </summary>
        protected virtual void StartDynamicResizeCooldown()
        {
            // Schedule a check to consider shrinking back after a delay
            // Since this is a non-MonoBehavior class, we'll use the PoolManager to start the coroutine
            PoolManager.Instance.StartCoroutine(ConsiderShrinkAfterDelay());
        }

        /// <summary>
        /// Coroutine that waits for a period and then attempts to shrink the pool back to its original size
        /// if the demand has decreased.
        /// </summary>
        protected virtual System.Collections.IEnumerator ConsiderShrinkAfterDelay()
        {
            // Store the original size for reference
            int originalMaxSize = _poolConfig.maxSize;
            int currentTotalSize = _activeObjects.Count + _inactiveObjects.Count;
            
            // Wait for the cooldown period (30 seconds is a reasonable default)
            yield return new WaitForSeconds(30f);
            
            // After waiting, check if we can safely shrink the pool back
            if (_activeObjects.Count <= originalMaxSize * 0.8f)
            {
                // The demand has decreased, we can shrink back closer to the original size
                // Perform a more aggressive trim to bring us closer to the original size
                Debug.Log($"[ObjectPool] Pool for {_prefabKey} has decreased demand. Performing aggressive trim to return to original size.");
                
                // First, force a trim operation
                _lastTrimTime = 0; // Reset the last trim time to force a trim
                TrimExcess();
                
                // Log the results
                int newSize = _activeObjects.Count + _inactiveObjects.Count;
                Debug.Log($"[ObjectPool] Pool for {_prefabKey} shrunk from {currentTotalSize} to {newSize} objects after demand decreased.");
            }
            else
            {
                // We're still using more objects than our original max size
                // Schedule another check for later
                Debug.Log($"[ObjectPool] Pool for {_prefabKey} still has high demand ({_activeObjects.Count} active objects). Will check again later.");
                
                // Optionally schedule another check
                // PoolManager.Instance.StartCoroutine(ConsiderShrinkAfterDelay());
            }
        }
        
        #endregion
    }
}