using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheLegends.Base.UnitySingleton;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace com.thelegends.unity.pooling
{
    /// <summary>
    /// Central manager for object pools that provides a unified API for creating and accessing pools.
    /// Handles pool creation, access, and lifecycle management.
    /// </summary>
    public class PoolManager : PersistentMonoSingleton<PoolManager>
    {
      
        #region Fields and Properties
        
        // Dictionaries to store pools
        private readonly Dictionary<object, object> _pools = new Dictionary<object, object>();

        /// <summary>
        /// Gets the dictionary of pools managed by the PoolManager.
        /// </summary>
        public Dictionary<object, object> Pools => _pools;
        // Default configurations
        private PoolConfig _defaultPoolConfig = PoolConfig.Default;
        private PoolTrimmingConfig _defaultTrimmingConfig = PoolTrimmingConfig.Default;
        private AddressableErrorConfig _defaultAddressableErrorConfig = AddressableErrorConfig.Default;

        // Debug logging
        [SerializeField]
        private bool _isDebugLogEnabled = false;
        
        
        /// <summary>
        /// Gets or sets the default pool configuration used when creating new pools.
        /// </summary>
        public PoolConfig DefaultPoolConfig
        {
            get => _defaultPoolConfig;
            set => _defaultPoolConfig = value;
        }
        
        /// <summary>
        /// Gets or sets the default trimming configuration used when creating new pools.
        /// </summary>
        public PoolTrimmingConfig DefaultTrimmingConfig
        {
            get => _defaultTrimmingConfig;
            set => _defaultTrimmingConfig = value;
        }
        
        /// <summary>
        /// Gets or sets the default Addressable error handling configuration.
        /// </summary>
        public AddressableErrorConfig DefaultAddressableErrorConfig
        {
            get => _defaultAddressableErrorConfig;
            set => _defaultAddressableErrorConfig = value;
        }
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Creates a new PoolManager instance with default configurations.
        /// Private to enforce singleton pattern.
        /// </summary>
        private PoolManager()
        {
            // Register for scene change events to handle pool cleanup
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }
        
        #endregion
        
        #region Pool Creation - Standard Pools
        
        /// <summary>
        /// Creates a new object pool for the specified prefab key asynchronously.
        /// </summary>
        /// <typeparam name="TKey">The type of key used to identify the pool</typeparam>
        /// <param name="prefabKey">The key identifying the prefab to pool</param>
        /// <param name="poolConfig">Optional custom pool configuration</param>
        /// <param name="addressableErrorConfig">Optional custom error handling configuration</param>
        /// <param name="trimmingConfig">Optional custom trimming configuration</param>
        /// <returns>A task that resolves to the created ObjectPool</returns>
        public async Task<ObjectPool<TKey>> CreatePoolAsync<TKey>(
            TKey prefabKey,
            PoolConfig? poolConfig = null,
            AddressableErrorConfig? addressableErrorConfig = null,
            PoolTrimmingConfig? trimmingConfig = null)
        {
            // Use provided configs or fall back to defaults
            var finalPoolConfig = poolConfig ?? _defaultPoolConfig;
            var finalAddressableErrorConfig = addressableErrorConfig ?? _defaultAddressableErrorConfig;
            var finalTrimmingConfig = trimmingConfig ?? _defaultTrimmingConfig;
            
            if (_isDebugLogEnabled)
            {
                Debug.Log($"[PoolManager] Creating pool for prefab key: {prefabKey}");
            }
            
            // Check if a pool already exists for this key
            if (_pools.TryGetValue(prefabKey, out var existingPool))
            {
                if (_isDebugLogEnabled)
                {
                    Debug.Log($"[PoolManager] Pool already exists for prefab key: {prefabKey}, returning existing pool");
                }
                return (ObjectPool<TKey>)existingPool;
            }
            
            // Create a new pool
            var newPool = new ObjectPool<TKey>(
                prefabKey,
                finalPoolConfig,
                finalAddressableErrorConfig,
                finalTrimmingConfig);
            
            // Initialize the pool
            bool initialized = await newPool.InitializeAsync();
            
            if (!initialized)
            {
                Debug.LogError($"[PoolManager] Failed to initialize pool for prefab key: {prefabKey}");
                return null;
            }
            
            // Add the pool to our dictionary
            _pools[prefabKey] = newPool;
            
            return newPool;
        }
        
        #endregion
        
        #region Pool Creation - UI Pools
        
        /// <summary>
        /// Creates a new UI object pool for the specified prefab key asynchronously.
        /// </summary>
        /// <typeparam name="TKey">The type of key used to identify the pool</typeparam>
        /// <param name="prefabKey">The key identifying the UI prefab to pool</param>
        /// <param name="parentTransform">The parent RectTransform for pooled UI elements</param>
        /// <param name="poolConfig">Optional custom pool configuration</param>
        /// <param name="addressableErrorConfig">Optional custom error handling configuration</param>
        /// <param name="trimmingConfig">Optional custom trimming configuration</param>
        /// <param name="preserveOriginalParenting">Whether to preserve original parenting</param>
        /// <param name="manageRaycasters">Whether to manage GraphicRaycasters</param>
        /// <param name="manageCanvases">Whether to manage Canvases</param>
        /// <returns>A task that resolves to the created UIPool</returns>
        public async Task<UIPool<TKey>> CreateUIPoolAsync<TKey>(
            TKey prefabKey,
            RectTransform parentTransform,
            PoolConfig? poolConfig = null,
            AddressableErrorConfig? addressableErrorConfig = null,
            PoolTrimmingConfig? trimmingConfig = null,
            bool preserveOriginalParenting = true,
            bool manageRaycasters = true,
            bool manageCanvases = true)
        {
            // Use provided configs or fall back to defaults
            var finalPoolConfig = poolConfig ?? _defaultPoolConfig;
            var finalAddressableErrorConfig = addressableErrorConfig ?? _defaultAddressableErrorConfig;
            var finalTrimmingConfig = trimmingConfig ?? _defaultTrimmingConfig;
            
            // Create a composite key for UI pools to avoid collisions with standard pools
            var compositeKey = (object)new Tuple<TKey, string>(prefabKey, "UI_POOL");
            
            if (_isDebugLogEnabled)
            {
                Debug.Log($"[PoolManager] Creating UI pool for prefab key: {prefabKey}");
            }
            
            // Check if a pool already exists for this key
            if (_pools.TryGetValue(compositeKey, out var existingPool))
            {
                if (_isDebugLogEnabled)
                {
                    Debug.Log($"[PoolManager] UI Pool already exists for prefab key: {prefabKey}, returning existing pool");
                }
                return (UIPool<TKey>)existingPool;
            }
            
            // Create a new UI pool
            var newPool = new UIPool<TKey>(
                prefabKey,
                parentTransform,
                finalPoolConfig,
                finalAddressableErrorConfig,
                finalTrimmingConfig,
                preserveOriginalParenting,
                manageRaycasters,
                manageCanvases);
            
            // Initialize the pool
            bool initialized = await newPool.InitializeAsync();
            
            if (!initialized)
            {
                Debug.LogError($"[PoolManager] Failed to initialize UI pool for prefab key: {prefabKey}");
                return null;
            }
            
            // Add the pool to our dictionary
            _pools[compositeKey] = newPool;
            
            return newPool;
        }
        
        #endregion
        
        #region Object Retrieval - Standard Objects
        
        /// <summary>
        /// Gets a pooled object asynchronously, creating the pool if it doesn't exist.
        /// </summary>
        /// <typeparam name="TKey">The type of key used to identify the pool</typeparam>
        /// <param name="prefabKey">The key identifying the prefab to pool</param>
        /// <returns>A task that resolves to the pooled GameObject, or null if the operation fails</returns>
        public async Task<GameObject> GetAsync<TKey>(TKey prefabKey)
        {
            ObjectPool<TKey> pool = await GetOrCreatePoolAsync(prefabKey);
            if (pool == null)
            {
                return null;
            }
            
            return await pool.GetAsync();
        }
        
        /// <summary>
        /// Gets a pooled object synchronously, returning null if the pool doesn't exist or isn't initialized.
        /// </summary>
        /// <typeparam name="TKey">The type of key used to identify the pool</typeparam>
        /// <param name="prefabKey">The key identifying the prefab to pool</param>
        /// <returns>The pooled GameObject, or null if the operation fails</returns>
        public GameObject Get<TKey>(TKey prefabKey)
        {
            // Check if a pool exists for this key
            if (!_pools.TryGetValue(prefabKey, out var poolObj) || !(poolObj is ObjectPool<TKey> pool))
            {
                if (_isDebugLogEnabled)
                {
                    Debug.LogWarning($"[PoolManager] No initialized pool exists for prefab key: {prefabKey}. Use GetAsync instead.");
                }
                return null;
            }
            
            return pool.Get();
        }
        
        /// <summary>
        /// Gets a pooled object and casts it to the specified component type.
        /// </summary>
        /// <typeparam name="TKey">The type of key used to identify the pool</typeparam>
        /// <typeparam name="TComponent">The component type to get</typeparam>
        /// <param name="prefabKey">The key identifying the prefab to pool</param>
        /// <returns>A task that resolves to the requested component, or null if it doesn't exist</returns>
        public async Task<TComponent> GetAsync<TKey, TComponent>(TKey prefabKey) where TComponent : Component
        {
            GameObject instance = await GetAsync(prefabKey);
            if (instance == null)
                return null;
                
            TComponent component = instance.GetComponent<TComponent>();
            if (component == null)
            {
                Debug.LogWarning($"[PoolManager] Object doesn't have component of type {typeof(TComponent).Name}");
                ReturnToPool(instance);
                return null;
            }
            
            return component;
        }
        
        /// <summary>
        /// Gets a pooled object and casts it to the specified component type (synchronous version).
        /// </summary>
        /// <typeparam name="TKey">The type of key used to identify the pool</typeparam>
        /// <typeparam name="TComponent">The component type to get</typeparam>
        /// <param name="prefabKey">The key identifying the prefab to pool</param>
        /// <returns>The requested component, or null if it doesn't exist</returns>
        public TComponent Get<TKey, TComponent>(TKey prefabKey) where TComponent : Component
        {
            GameObject instance = Get(prefabKey);
            if (instance == null)
                return null;
                
            TComponent component = instance.GetComponent<TComponent>();
            if (component == null)
            {
                Debug.LogWarning($"[PoolManager] Object doesn't have component of type {typeof(TComponent).Name}");
                ReturnToPool(instance);
                return null;
            }
            
            return component;
        }
        
        #endregion
        
        #region Object Retrieval - UI Objects
        
        /// <summary>
        /// Gets a pooled UI object asynchronously.
        /// </summary>
        /// <typeparam name="TKey">The type of key used to identify the pool</typeparam>
        /// <param name="prefabKey">The key identifying the UI prefab to pool</param>
        /// <returns>A task that resolves to the pooled UI GameObject, or null if the operation fails</returns>
        public async Task<GameObject> GetUIAsync<TKey>(TKey prefabKey)
        {
            // Create a composite key for UI pools
            var compositeKey = (object)new Tuple<TKey, string>(prefabKey, "UI_POOL");
            
            // Check if a UI pool exists for this key
            if (!_pools.TryGetValue(compositeKey, out var poolObj) || !(poolObj is UIPool<TKey> uiPool))
            {
                if (_isDebugLogEnabled)
                {
                    Debug.LogWarning($"[PoolManager] No UI pool exists for prefab key: {prefabKey}. Create a UI pool first using CreateUIPoolAsync.");
                }
                return null;
            }
            
            return await uiPool.GetAsync();
        }
        
        /// <summary>
        /// Gets a pooled UI object and casts it to the specified component type.
        /// </summary>
        /// <typeparam name="TKey">The type of key used to identify the pool</typeparam>
        /// <typeparam name="TComponent">The component type to get</typeparam>
        /// <param name="prefabKey">The key identifying the UI prefab to pool</param>
        /// <returns>A task that resolves to the requested UI component, or null if it doesn't exist</returns>
        public async Task<TComponent> GetUIAsync<TKey, TComponent>(TKey prefabKey) where TComponent : Component
        {
            // Create a composite key for UI pools
            var compositeKey = (object)new Tuple<TKey, string>(prefabKey, "UI_POOL");
            
            // Check if a UI pool exists for this key
            if (!_pools.TryGetValue(compositeKey, out var poolObj) || !(poolObj is UIPool<TKey> uiPool))
            {
                if (_isDebugLogEnabled)
                {
                    Debug.LogWarning($"[PoolManager] No UI pool exists for prefab key: {prefabKey}. Create a UI pool first using CreateUIPoolAsync.");
                }
                return null;
            }
            
            return await uiPool.GetComponentAsync<TComponent>();
        }
        
        #endregion
        
        #region Object Return
        
        /// <summary>
        /// Returns an object to its pool. The object must have a PooledObject component to identify its pool.
        /// </summary>
        /// <param name="instance">The GameObject to return to its pool</param>
        /// <returns>True if the object was successfully returned, false otherwise</returns>
        public bool ReturnToPool(GameObject instance)
        {
            if (instance == null)
            {
                Debug.LogError("[PoolManager] Cannot return null instance to pool");
                return false;
            }
            
            PooledObject pooledObject = instance.GetComponent<PooledObject>();
            if (pooledObject == null)
            {
                if (_isDebugLogEnabled)
                {
                    Debug.LogWarning($"[PoolManager] Object {instance.name} is not a pooled object (no PooledObject component)");
                }
                return false;
            }
            
            // The PooledObject component will handle returning to the correct pool
            pooledObject.ReturnToPool();
            return true;
        }
        
        #endregion
        
        #region Pool Management
        
        /// <summary>
        /// Gets or creates an object pool for the specified prefab key.
        /// </summary>
        /// <typeparam name="TKey">The type of key used to identify the pool</typeparam>
        /// <param name="prefabKey">The key identifying the prefab to pool</param>
        /// <returns>A task that resolves to the pool, or null if the operation fails</returns>
        private async Task<ObjectPool<TKey>> GetOrCreatePoolAsync<TKey>(TKey prefabKey)
        {
            // Check if a pool already exists for this key
            if (_pools.TryGetValue(prefabKey, out var existingPool) && existingPool is ObjectPool<TKey> typedPool)
            {
                return typedPool;
            }
            
            // If not, create a new pool
            return await CreatePoolAsync(prefabKey);
        }
        
        /// <summary>
        /// Clears a specific pool by key, destroying all pooled objects.
        /// </summary>
        /// <typeparam name="TKey">The type of key used to identify the pool</typeparam>
        /// <param name="prefabKey">The key identifying the pool to clear</param>
        /// <param name="isUIPool">Whether this is a UI pool</param>
        /// <returns>True if the pool was found and cleared, false otherwise</returns>
        public bool ClearPool<TKey>(TKey prefabKey, bool isUIPool = false)
        {
            object key = prefabKey;
            
            if (isUIPool)
            {
                key = new Tuple<TKey, string>(prefabKey, "UI_POOL");
            }
            
            if (_pools.TryGetValue(key, out var pool))
            {
                if (pool is ObjectPool<TKey> objectPool)
                {
                    objectPool.Clear();
                    _pools.Remove(key);
                    
                    if (_isDebugLogEnabled)
                    {
                        Debug.Log($"[PoolManager] Cleared pool for {prefabKey}");
                    }
                    
                    return true;
                }
            }
            
            if (_isDebugLogEnabled)
            {
                Debug.LogWarning($"[PoolManager] No pool found for {prefabKey} to clear");
            }
            
            return false;
        }
        
        /// <summary>
        /// Clears all pools, destroying all pooled objects.
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var poolObj in _pools.Values)
            {
                if (poolObj is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                else if (poolObj.GetType().GetMethod("Clear") != null)
                {
                    // Use reflection to call Clear method
                    poolObj.GetType().GetMethod("Clear").Invoke(poolObj, null);
                }
            }
            
            _pools.Clear();
            
            if (_isDebugLogEnabled)
            {
                Debug.Log("[PoolManager] Cleared all pools");
            }
        }
        
        /// <summary>
        /// Trims excess objects from all pools based on their trimming configurations.
        /// </summary>
        public void TrimExcessPools()
        {
            foreach (var poolObj in _pools.Values)
            {
                if (poolObj.GetType().GetMethod("TrimExcess") != null)
                {
                    // Use reflection to call TrimExcess method
                    poolObj.GetType().GetMethod("TrimExcess").Invoke(poolObj, null);
                }
            }
            
            if (_isDebugLogEnabled)
            {
                Debug.Log("[PoolManager] Trimmed excess objects from all pools");
            }
        }
        
        /// <summary>
        /// Prewarms multiple pools simultaneously.
        /// </summary>
        /// <typeparam name="TKey">The type of key used to identify the pools</typeparam>
        /// <param name="prefabKeys">The keys identifying the prefabs to pool</param>
        /// <param name="poolConfig">Optional custom pool configuration</param>
        /// <param name="addressableErrorConfig">Optional custom error handling configuration</param>
        /// <param name="trimmingConfig">Optional custom trimming configuration</param>
        /// <returns>A task that completes when all pools are prewarmed</returns>
        public async Task PrewarmMultipleAsync<TKey>(
            IEnumerable<TKey> prefabKeys,
            PoolConfig? poolConfig = null,
            AddressableErrorConfig? addressableErrorConfig = null,
            PoolTrimmingConfig? trimmingConfig = null)
        {
            List<Task> initTasks = new List<Task>();
            
            foreach (var key in prefabKeys)
            {
                initTasks.Add(CreatePoolAsync(key, poolConfig, addressableErrorConfig, trimmingConfig));
            }
            
            await Task.WhenAll(initTasks);
            
            if (_isDebugLogEnabled)
            {
                Debug.Log($"[PoolManager] Prewarmed multiple pools");
            }
        }
        
        #endregion
        
        #region Scene Management
        
        /// <summary>
        /// Handles cleanup when a scene is unloaded.
        /// </summary>
        private void OnSceneUnloaded(Scene scene)
        {
            // No specific implementation yet - can be extended to handle scene-specific pools
            if (_isDebugLogEnabled)
            {
                Debug.Log($"[PoolManager] Scene unloaded: {scene.name}");
            }
        }
        
        #endregion
    }
}