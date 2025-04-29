using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace com.thelegends.unity.pooling
{
    /// <summary>
    /// Specialized pool for UI elements that extends the base ObjectPool with additional functionality 
    /// specific to UI GameObjects (RectTransform, Canvas, GraphicRaycaster, Layout).
    /// </summary>
    /// <typeparam name="TKey">The type of key used to identify this pool</typeparam>
    public class UIPool<TKey> : ObjectPool<TKey>
    {
        // Reference to parent transform where UI elements should be placed
        protected readonly RectTransform _parentTransform;
        
        // Whether to preserve the object's original world position/rotation when returning it to the parent
        protected readonly bool _preserveOriginalParenting;
        
        // Whether to automatically disable/enable GraphicRaycaster components when pooling
        protected readonly bool _manageRaycasters;
        
        // Whether to automatically disable/enable Canvas components when pooling
        protected readonly bool _manageCanvases;
        
        /// <summary>
        /// Creates a new UIPool for the specified prefab key with the given configurations.
        /// </summary>
        /// <param name="prefabKey">The key used to identify and load the UI prefab</param>
        /// <param name="parentTransform">The parent RectTransform where pooled UI elements will be placed</param>
        /// <param name="poolConfig">Configuration for initial size and growth behavior</param>
        /// <param name="addressableErrorConfig">Configuration for handling Addressable loading errors</param>
        /// <param name="trimmingConfig">Configuration for automatic pool trimming</param>
        /// <param name="preserveOriginalParenting">Whether to preserve the original world position/rotation when reparenting</param>
        /// <param name="manageRaycasters">Whether to disable GraphicRaycasters when objects are inactive</param>
        /// <param name="manageCanvases">Whether to disable Canvases when objects are inactive</param>
        public UIPool(
            TKey prefabKey, 
            RectTransform parentTransform,
            PoolConfig poolConfig, 
            AddressableErrorConfig addressableErrorConfig, 
            PoolTrimmingConfig trimmingConfig,
            bool preserveOriginalParenting = true,
            bool manageRaycasters = true,
            bool manageCanvases = true) 
            : base(prefabKey, poolConfig, addressableErrorConfig, trimmingConfig)
        {
            _parentTransform = parentTransform;
            _preserveOriginalParenting = preserveOriginalParenting;
            _manageRaycasters = manageRaycasters;
            _manageCanvases = manageCanvases;
        }
        
        /// <summary>
        /// Creates a new UIPool for the specified prefab with the given configurations.
        /// </summary>
        /// <param name="prefabKey">The key used to identify and load the UI prefab</param>
        /// <param name="parentTransform">The parent RectTransform where pooled UI elements will be placed</param>
        /// <param name="canvasRoot">Optional root Canvas to use for UI elements if null parent is used</param>
        public UIPool(
            TKey prefabKey, 
            RectTransform parentTransform) 
            : this(prefabKey, parentTransform, PoolConfig.Default, AddressableErrorConfig.Default, PoolTrimmingConfig.Default)
        {
        }
        
        /// <summary>
        /// Sets up the transform of a UI object when it's retrieved from the pool.
        /// For UI elements, this handles RectTransform parenting and layout preservation.
        /// </summary>
        protected override void SetupTransform(Transform transform)
        {
            if (transform == null)
                return;
                
            // Make sure we're working with a RectTransform
            if (!(transform is RectTransform rectTransform))
            {
                Debug.LogWarning($"[UIPool] UI object {transform.name} doesn't have a RectTransform component!");
                return;
            }
            
            // Set the parent transform with appropriate maintain settings
            if (_parentTransform != null)
            {
                // When getting from pool, move from container to the target parent
                if (_preserveOriginalParenting)
                {
                    rectTransform.SetParent(_parentTransform, false);
                }
                else
                {
                    rectTransform.SetParent(_parentTransform, true);
                }
                
                // Reset scale which might have been affected by parenting
                rectTransform.localScale = Vector3.one;
            }
            
            // Check for any layout elements and mark for rebuild if needed
            LayoutElement layoutElement = rectTransform.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                layoutElement.enabled = true;
            }
            
            // Ensure proper layout refresh
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }
        
        /// <summary>
        /// Called after a UI object is retrieved from the pool.
        /// Handles UI-specific components like Canvas and GraphicRaycaster.
        /// </summary>
        protected override void OnAfterGet(GameObject instance)
        {
            base.OnAfterGet(instance);
            
            if (instance == null)
                return;
                
            // Enable GraphicRaycaster if needed
            if (_manageRaycasters)
            {
                GraphicRaycaster[] raycasters = instance.GetComponentsInChildren<GraphicRaycaster>(true);
                foreach (var raycaster in raycasters)
                {
                    raycaster.enabled = true;
                }
            }
            
            // Enable Canvas if needed
            if (_manageCanvases)
            {
                Canvas[] canvases = instance.GetComponentsInChildren<Canvas>(true);
                foreach (var canvas in canvases)
                {
                    canvas.enabled = true;
                }
            }
            
            // If we have a RectTransform, ensure it's properly positioned
            if (instance.transform is RectTransform rectTransform)
            {
                // Force layout refresh in case it wasn't triggered automatically
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            }
        }
        
        /// <summary>
        /// Called before a UI object is returned to the pool.
        /// Disables UI-specific components to improve performance.
        /// </summary>
        protected override void OnBeforeReturn(GameObject instance)
        {
            base.OnBeforeReturn(instance);
            
            if (instance == null)
                return;
            
            // Disable GraphicRaycaster to reduce overhead while inactive
            if (_manageRaycasters)
            {
                GraphicRaycaster[] raycasters = instance.GetComponentsInChildren<GraphicRaycaster>(true);
                foreach (var raycaster in raycasters)
                {
                    raycaster.enabled = false;
                }
            }
            
            // Disable Canvas to reduce draw calls while inactive
            if (_manageCanvases)
            {
                Canvas[] canvases = instance.GetComponentsInChildren<Canvas>(true);
                foreach (var canvas in canvases)
                {
                    canvas.enabled = false;
                }
            }
            
            // Disable any layout elements to prevent unnecessary layout recalculations
            LayoutElement[] layoutElements = instance.GetComponentsInChildren<LayoutElement>(true);
            foreach (var layoutElement in layoutElements)
            {
                layoutElement.enabled = false;
            }
        }
        
        /// <summary>
        /// Resets the transform of a UI object when it's returned to the pool.
        /// </summary>
        protected override void ResetTransform(Transform transform)
        {
            if (transform == null)
                return;
                
            // Return it to our container instead of the UI parent when returning to pool
            if (_parentContainer != null)
            {
                transform.SetParent(_parentContainer.transform, false);
            }
            else if (_parentTransform != null) 
            {
                // Fallback to UI parent if we don't have a container
                transform.SetParent(_parentTransform, false);
            }
            else
            {
                transform.SetParent(null);
            }
            
            // Reset the RectTransform properties
            if (transform is RectTransform rectTransform)
            {
                rectTransform.localPosition = Vector3.zero;
                rectTransform.localRotation = Quaternion.identity;
                rectTransform.localScale = Vector3.one;
                
                // Reset anchors and pivot to default (center)
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                
                // Reset size delta
                rectTransform.sizeDelta = Vector2.zero;
                
                // Reset offset
                rectTransform.anchoredPosition = Vector2.zero;
            }
            else
            {
                // Fallback for non-RectTransform (shouldn't happen in UIPool)
                base.ResetTransform(transform);
            }
        }
        
        /// <summary>
        /// Create a new instance of the UI prefab.
        /// Ensures the instance has a RectTransform and proper UI setup.
        /// </summary>
        protected override GameObject CreateInstance()
        {
            GameObject instance = base.CreateInstance();
            
            if (instance != null)
            {
                // Check if it has a RectTransform (it should for UI elements)
                if (!(instance.transform is RectTransform))
                {
                    Debug.LogWarning($"[UIPool] UI object {instance.name} doesn't have a RectTransform component!");
                }
                
                // Check if any layout element exists and setup properly
                LayoutElement layoutElement = instance.GetComponent<LayoutElement>();
                if (layoutElement != null)
                {
                    layoutElement.enabled = false; // Initially disabled
                }
                
                // Parent properly with _parentTransform
                if (_parentTransform != null)
                {
                    instance.transform.SetParent(_parentTransform, false);
                }
            }
            
            return instance;
        }
        
        /// <summary>
        /// Gets a pooled UI element and casts it to the specified component type.
        /// </summary>
        /// <typeparam name="T">The component type to cast to</typeparam>
        /// <returns>The requested component, or null if it doesn't exist or the pool fails to provide an object</returns>
        public async Task<T> GetComponentAsync<T>() where T : Component
        {
            GameObject instance = await GetAsync();
            if (instance == null)
                return null;
                
            T component = instance.GetComponent<T>();
            if (component == null)
            {
                Debug.LogWarning($"[UIPool] UI object doesn't have component of type {typeof(T).Name}");
                Return(instance); // Return the object to pool since we couldn't use it
                return null;
            }
            
            return component;
        }
        
        /// <summary>
        /// Gets a pooled UI element and casts it to the specified component type (synchronous version).
        /// </summary>
        /// <typeparam name="T">The component type to cast to</typeparam>
        /// <returns>The requested component, or null if it doesn't exist or the pool fails to provide an object</returns>
        public T GetComponent<T>() where T : Component
        {
            GameObject instance = Get();
            if (instance == null)
                return null;
                
            T component = instance.GetComponent<T>();
            if (component == null)
            {
                Debug.LogWarning($"[UIPool] UI object doesn't have component of type {typeof(T).Name}");
                Return(instance); // Return the object to pool since we couldn't use it
                return null;
            }
            
            return component;
        }
    }
}