using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;

namespace com.thelegends.unity.pooling
{
    /// <summary>
    /// Component that preloads objects into pools during scene initialization 
    /// or on demand. This helps prevent performance hitches when objects are first requested.
    /// </summary>
    [AddComponentMenu("Pooling/Pool Preloader")]
    public class PoolPreloader : MonoBehaviour
    {
        /// <summary>
        /// When to perform the preloading operation
        /// </summary>
        public enum PreloadTiming
        {
            /// <summary>Preload immediately when this component is enabled</summary>
            OnEnable,
            
            /// <summary>Preload at the start of the scene (in Start method)</summary>
            OnStart,
            
            /// <summary>Preload when explicitly called via script</summary>
            OnDemand,
            
            /// <summary>Preload after a specified delay</summary>
            AfterDelay
        }
        
        [System.Serializable]
        public class PoolPreloadItem
        {
            [Tooltip("Addressable asset reference to the prefab to preload")]
            public AssetReference prefabReference;
            
            [Tooltip("String key to use for this pool (if empty, will use the asset name)")]
            public string poolKey;
            
            [Tooltip("Is this a UI element requiring UI-specific pooling?")]
            public bool isUIElement = false;
            
            [Tooltip("Number of instances to prewarm")]
            public int prewarmCount = 10;
            
            [Tooltip("Should the pool be allowed to grow beyond initial size?")]
            public bool allowGrowth = true;
            
            [Tooltip("Maximum pool size (if growth is allowed)")]
            public int maxSize = 100;
            
            [Tooltip("Enable automatic trimming of excess inactive objects?")]
            public bool enableTrimming = true;
        }
        
        [Header("Preload Configuration")]
        [Tooltip("When to perform the preloading operation")]
        [SerializeField] private PreloadTiming _preloadTiming = PreloadTiming.OnStart;
        
        [Tooltip("Delay in seconds before preloading (for AfterDelay timing)")]
        [SerializeField] private float _preloadDelay = 1.0f;
        
        [Tooltip("Whether to show debug logs during preloading")]
        [SerializeField] private bool _showDebugLogs = false;
        
        [Tooltip("Whether to preload pools sequentially or all at once")]
        [SerializeField] private bool _loadSequentially = false;
        
        [Header("Pools to Preload")]
        [Tooltip("List of pools to preload")]
        [SerializeField] private List<PoolPreloadItem> _poolsToPreload = new List<PoolPreloadItem>();
        
        // Tracking for completion
        private bool _isPreloading = false;
        private int _totalPools = 0;
        private int _completedPools = 0;
        
        /// <summary>
        /// Event invoked when all pools have finished preloading
        /// </summary>
        public System.Action onPreloadComplete;
        
        /// <summary>
        /// Event invoked when a pool has been created and prewarmed
        /// </summary>
        public System.Action<string, int> onPoolPreloaded;
        
        /// <summary>
        /// Gets whether preloading is currently in progress
        /// </summary>
        public bool IsPreloading => _isPreloading;
        
        /// <summary>
        /// Gets the preloading progress from 0.0 to 1.0
        /// </summary>
        public float PreloadProgress => _totalPools > 0 ? (float)_completedPools / _totalPools : 0f;
        
        private void OnEnable()
        {
            if (_preloadTiming == PreloadTiming.OnEnable)
            {
                PreloadAllPools();
            }
        }
        
        private void Start()
        {
            if (_preloadTiming == PreloadTiming.OnStart)
            {
                PreloadAllPools();
            }
            else if (_preloadTiming == PreloadTiming.AfterDelay)
            {
                StartCoroutine(PreloadAfterDelay());
            }
        }
        
        private IEnumerator PreloadAfterDelay()
        {
            yield return new WaitForSeconds(_preloadDelay);
            PreloadAllPools();
        }
        
        /// <summary>
        /// Manually starts the preloading process for all configured pools
        /// </summary>
        public void PreloadAllPools()
        {
            if (_isPreloading)
            {
                Debug.LogWarning("[PoolPreloader] Preloading is already in progress");
                return;
            }
            
            if (_poolsToPreload == null || _poolsToPreload.Count == 0)
            {
                Debug.LogWarning("[PoolPreloader] No pools configured for preloading");
                return;
            }
            
            _isPreloading = true;
            _totalPools = _poolsToPreload.Count;
            _completedPools = 0;
            
            if (_showDebugLogs)
            {
                Debug.Log($"[PoolPreloader] Starting to preload {_totalPools} pools");
            }
            
            if (_loadSequentially)
            {
                StartCoroutine(PreloadSequentially());
            }
            else
            {
                // Start all preloads simultaneously
                foreach (var poolItem in _poolsToPreload)
                {
                    PreloadPool(poolItem);
                }
            }
        }
        
        private IEnumerator PreloadSequentially()
        {
            foreach (var poolItem in _poolsToPreload)
            {
                // Start preloading this pool
                Task preloadTask = PreloadPoolAsync(poolItem);
                
                // Wait until this pool is done before starting the next
                while (!preloadTask.IsCompleted)
                {
                    yield return null;
                }
            }
        }
        
        private void PreloadPool(PoolPreloadItem poolItem)
        {
            // Bắt đầu quá trình preload bất đồng bộ
            _ = PreloadPoolAsync(poolItem);
        }
        
        private async Task PreloadPoolAsync(PoolPreloadItem poolItem)
        {
            // Ensure we have a valid asset reference
            if (poolItem.prefabReference == null || !poolItem.prefabReference.RuntimeKeyIsValid())
            {
                Debug.LogError($"[PoolPreloader] Invalid asset reference in PoolPreloader");
                IncrementCompletedPools();
                return;
            }
            
            // Get key from poolItem or use the asset GUID if none specified
            string key = string.IsNullOrEmpty(poolItem.poolKey) 
                ? poolItem.prefabReference.AssetGUID
                : poolItem.poolKey;
            
            try
            {
                if (_showDebugLogs)
                {
                    Debug.Log($"[PoolPreloader] Preloading pool for {key}, count: {poolItem.prewarmCount}");
                }
                
                // Create pool configuration
                PoolConfig config = new PoolConfig(
                    poolItem.prewarmCount,
                    poolItem.allowGrowth,
                    poolItem.maxSize
                );
                
                // Create trimming configuration
                PoolTrimmingConfig trimmingConfig = poolItem.enableTrimming 
                    ? PoolTrimmingConfig.Default 
                    : PoolTrimmingConfig.Disabled;
                
                // Create the pool based on type (UI or regular)
                if (poolItem.isUIElement)
                {
                    // For UI pools we need to find a suitable parent transform
                    // We'll use the main canvas if available
                    Canvas mainCanvas = FindObjectOfType<Canvas>();
                    if (mainCanvas == null)
                    {
                        Debug.LogError($"[PoolPreloader] Cannot create UI pool for {key}: No canvas found in scene");
                        IncrementCompletedPools();
                        return;
                    }
                    
                    RectTransform parentTransform = mainCanvas.transform as RectTransform;
                    if (parentTransform == null)
                    {
                        Debug.LogError($"[PoolPreloader] Canvas does not have a RectTransform");
                        IncrementCompletedPools();
                        return;
                    }
                    
                    // Create and prewarm a UI pool using the AssetGUID as the string key
                    await PoolManager.Instance.CreateUIPoolAsync(
                        key, 
                        parentTransform,
                        config,
                        AddressableErrorConfig.Default,
                        trimmingConfig);
                }
                else
                {
                    // Create and prewarm a regular gameobject pool using the AssetGUID as the string key
                    await PoolManager.Instance.CreatePoolAsync(
                        key, 
                        config,
                        AddressableErrorConfig.Default,
                        trimmingConfig);
                }
                
                // Notify that this specific pool has been preloaded
                onPoolPreloaded?.Invoke(key, poolItem.prewarmCount);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PoolPreloader] Error preloading pool for {key}: {e.Message}");
            }
            
            // Increment completed count
            IncrementCompletedPools();
        }
        
        private void IncrementCompletedPools()
        {
            _completedPools++;
            
            if (_completedPools >= _totalPools)
            {
                _isPreloading = false;
                
                if (_showDebugLogs)
                {
                    Debug.Log($"[PoolPreloader] All {_totalPools} pools have been preloaded");
                }
                
                // Notify that all pools have been preloaded
                onPreloadComplete?.Invoke();
            }
        }
        
        /// <summary>
        /// Adds a pool to the preload list programmatically
        /// </summary>
        public void AddPoolToPreload(AssetReference prefabReference, string key, bool isUI, int count, bool allowGrowth = true)
        {
            if (_poolsToPreload == null)
            {
                _poolsToPreload = new List<PoolPreloadItem>();
            }
            
            PoolPreloadItem item = new PoolPreloadItem
            {
                prefabReference = prefabReference,
                poolKey = key,
                isUIElement = isUI,
                prewarmCount = count,
                allowGrowth = allowGrowth,
                maxSize = Mathf.Max(count * 2, 100),
                enableTrimming = true
            };
            
            _poolsToPreload.Add(item);
        }
        
        /// <summary>
        /// Clears the preload list
        /// </summary>
        public void ClearPreloadList()
        {
            if (_isPreloading)
            {
                Debug.LogWarning("[PoolPreloader] Cannot clear preload list while preloading is in progress");
                return;
            }
            
            _poolsToPreload.Clear();
        }
    }
} 