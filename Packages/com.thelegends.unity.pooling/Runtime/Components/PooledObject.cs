using System;
using UnityEngine;

namespace com.thelegends.unity.pooling
{
    /// <summary>
    /// Component attached to all pooled GameObjects. Handles the return-to-pool functionality and
    /// maintains a reference to the owner pool. This component is automatically added to objects
    /// when they are created by the pooling system.
    /// </summary>
    public class PooledObject : MonoBehaviour
    {
        // Reference to the pool this object belongs to (to avoid using string key lookups)
        private object _ownerPool;
        
        // Original key/identifier used to create this object (for matching when returning)
        private object _originalKey;
        
        // Cached array of IPoolable components
        private IPoolable[] _poolableComponents;
        
        // Time when this object was last accessed from the pool
        internal float _lastAccessTime;
        
        /// <summary>
        /// Setup this pooled object with a reference to its owning pool and original key
        /// </summary>
        internal void Initialize<TKey>(object ownerPool, TKey key)
        {
            _ownerPool = ownerPool;
            _originalKey = key;
            
            // Cache all IPoolable components on this GameObject and its children
            _poolableComponents = GetComponentsInChildren<IPoolable>(true);
            
            // Update access time
            UpdateAccessTime();
        }
        
        /// <summary>
        /// Return this object to its original pool
        /// </summary>
        public void ReturnToPool()
        {
            // Make sure we have a valid owner pool
            if (_ownerPool == null)
            {
                Debug.LogWarning($"[PooledObject] Cannot return object to pool: no owner pool reference for {gameObject.name}");
                return;
            }
            
            // Call OnReturnToPool on all IPoolable components
            if (_poolableComponents != null)
            {
                for (int i = 0; i < _poolableComponents.Length; i++)
                {
                    if (_poolableComponents[i] != null)
                    {
                        _poolableComponents[i].OnReturnToPool();
                    }
                }
            }
            
            // Use reflection to call the correct Return method on the owner pool
            // This is needed because we don't know the generic type at compile time
            Type poolType = _ownerPool.GetType();
            var returnMethod = poolType.GetMethod("Return");
            
            if (returnMethod != null)
            {
                returnMethod.Invoke(_ownerPool, new object[] { gameObject });
            }
            else
            {
                Debug.LogError($"[PooledObject] Could not find Return method on pool type {poolType.Name}");
            }
        }
        
        /// <summary>
        /// Called when this object is retrieved from the pool
        /// </summary>
        internal void OnGet()
        {
            // Update last access time
            UpdateAccessTime();
            
            // Call OnGetFromPool on all IPoolable components
            if (_poolableComponents != null)
            {
                for (int i = 0; i < _poolableComponents.Length; i++)
                {
                    if (_poolableComponents[i] != null)
                    {
                        _poolableComponents[i].OnGetFromPool();
                    }
                }
            }
        }
        
        /// <summary>
        /// Updates the last access time to the current time
        /// </summary>
        internal void UpdateAccessTime()
        {
            _lastAccessTime = Time.time;
        }
        
        /// <summary>
        /// Gets the original key used to identify this pooled object
        /// </summary>
        public TKey GetOriginalKey<TKey>()
        {
            if (_originalKey is TKey key)
            {
                return key;
            }
            
            Debug.LogError($"[PooledObject] Original key is of type {_originalKey?.GetType().Name} but requested type is {typeof(TKey).Name}");
            return default;
        }
    }
} 