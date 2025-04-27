using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace com.thelegends.unity.pooling
{
    /// <summary>
    /// Component that automatically returns a pooled object to its pool after a specified time delay
    /// or when a condition is met. Attach this to any pooled GameObject that needs to be automatically recycled.
    /// </summary>
    [AddComponentMenu("Pooling/Auto Return To Pool")]
    [RequireComponent(typeof(PooledObject))]
    public class AutoReturnToPool : MonoBehaviour
    {
        /// <summary>
        /// The method used to determine when to return the object to its pool
        /// </summary>
        public enum ReturnMethod
        {
            /// <summary>Returns the object after a fixed time delay</summary>
            AfterDelay,
            
            /// <summary>Returns the object when an event is triggered</summary>
            OnEvent,
            
            /// <summary>Returns the object when it goes out of camera view</summary>
            WhenOffscreen,
            
            /// <summary>Returns the object when a custom condition is met</summary>
            CustomCondition
        }
        
        [Header("Return Settings")]
        [Tooltip("How to determine when to return this object to its pool")]
        [SerializeField] private ReturnMethod _returnMethod = ReturnMethod.AfterDelay;
        
        [Tooltip("Time in seconds to wait before returning the object to the pool (for AfterDelay method)")]
        [SerializeField] private float _delaySeconds = 3.0f;
        
        [Tooltip("Additional random time variation to add to the delay (0 = no variation)")]
        [SerializeField] private float _randomVariation = 0.0f;
        
        [Tooltip("Whether to use unscaled time (not affected by Time.timeScale)")]
        [SerializeField] private bool _useUnscaledTime = false;
        
        [Header("Offscreen Settings")]
        [Tooltip("How far outside the camera view the object must be before being returned (for WhenOffscreen method)")]
        [SerializeField] private float _offscreenThreshold = 2.0f;
        
        [Tooltip("How often to check if the object is offscreen (in seconds)")]
        [SerializeField] private float _offscreenCheckInterval = 0.5f;
        
        [Header("Events")]
        [Tooltip("Event that will trigger the return (for OnEvent method)")]
        [SerializeField] private UnityEvent _onReturnEvent = null;
        
        // Reference to the PooledObject component
        private PooledObject _pooledObject;
        
        // Active coroutines for cleanup
        private Coroutine _returnCoroutine;
        private Coroutine _checkOffscreenCoroutine;
        
        // Custom condition callback
        private System.Func<bool> _customConditionCallback;
        
        /// <summary>
        /// Sets a custom condition callback that determines when to return the object to the pool.
        /// The object will be returned when this callback returns true.
        /// </summary>
        /// <param name="condition">Function that returns true when the object should be returned</param>
        public void SetCustomCondition(System.Func<bool> condition)
        {
            _customConditionCallback = condition;
            _returnMethod = ReturnMethod.CustomCondition;
            
            // Start checking the condition
            StopAllCoroutines();
            _returnCoroutine = StartCoroutine(CheckCustomConditionRoutine());
        }
        
        /// <summary>
        /// Sets a delay for returning the object to the pool.
        /// </summary>
        /// <param name="seconds">Delay in seconds</param>
        /// <param name="randomVariation">Optional random variation to add to the delay</param>
        /// <param name="useUnscaledTime">Whether to use unscaled time</param>
        public void SetDelay(float seconds, float randomVariation = 0f, bool useUnscaledTime = false)
        {
            _delaySeconds = seconds;
            _randomVariation = randomVariation;
            _useUnscaledTime = useUnscaledTime;
            _returnMethod = ReturnMethod.AfterDelay;
            
            // Restart the delay
            InitializeReturnMethod();
        }
        
        private void Awake()
        {
            _pooledObject = GetComponent<PooledObject>();
            
            // Connect event if using OnEvent method
            if (_returnMethod == ReturnMethod.OnEvent && _onReturnEvent != null)
            {
                _onReturnEvent.AddListener(ReturnNow);
            }
        }
        
        private void OnEnable()
        {
            // Initialize the appropriate return method when the object is enabled
            InitializeReturnMethod();
        }
        
        private void OnDisable()
        {
            // Clean up any active coroutines
            if (_returnCoroutine != null)
            {
                StopCoroutine(_returnCoroutine);
                _returnCoroutine = null;
            }
            
            if (_checkOffscreenCoroutine != null)
            {
                StopCoroutine(_checkOffscreenCoroutine);
                _checkOffscreenCoroutine = null;
            }
        }
        
        private void OnDestroy()
        {
            // Disconnect event listener
            if (_returnMethod == ReturnMethod.OnEvent && _onReturnEvent != null)
            {
                _onReturnEvent.RemoveListener(ReturnNow);
            }
        }
        
        /// <summary>
        /// Immediately returns the object to its pool.
        /// </summary>
        public void ReturnNow()
        {
            if (_pooledObject != null)
            {
                _pooledObject.ReturnToPool();
            }
            else
            {
                Debug.LogWarning($"[AutoReturnToPool] Cannot return to pool: missing PooledObject component on {gameObject.name}");
            }
        }
        
        private void InitializeReturnMethod()
        {
            // Stop any existing coroutines
            if (_returnCoroutine != null)
            {
                StopCoroutine(_returnCoroutine);
                _returnCoroutine = null;
            }
            
            if (_checkOffscreenCoroutine != null)
            {
                StopCoroutine(_checkOffscreenCoroutine);
                _checkOffscreenCoroutine = null;
            }
            
            // Start new coroutines based on the return method
            switch (_returnMethod)
            {
                case ReturnMethod.AfterDelay:
                    _returnCoroutine = StartCoroutine(ReturnAfterDelayRoutine());
                    break;
                    
                case ReturnMethod.WhenOffscreen:
                    _checkOffscreenCoroutine = StartCoroutine(CheckOffscreenRoutine());
                    break;
                    
                case ReturnMethod.CustomCondition:
                    if (_customConditionCallback != null)
                    {
                        _returnCoroutine = StartCoroutine(CheckCustomConditionRoutine());
                    }
                    break;
                    
                // OnEvent method doesn't need a coroutine, it uses the event listener
            }
        }
        
        private IEnumerator ReturnAfterDelayRoutine()
        {
            // Calculate the actual delay with random variation
            float actualDelay = _delaySeconds;
            if (_randomVariation > 0f)
            {
                actualDelay += Random.Range(-_randomVariation, _randomVariation);
                // Ensure delay is not negative
                actualDelay = Mathf.Max(0.01f, actualDelay);
            }
            
            // Wait for the delay
            if (_useUnscaledTime)
            {
                yield return new WaitForSecondsRealtime(actualDelay);
            }
            else
            {
                yield return new WaitForSeconds(actualDelay);
            }
            
            // Return the object to the pool
            ReturnNow();
        }
        
        private IEnumerator CheckOffscreenRoutine()
        {
            // Use WaitForSeconds for performance (creates fewer garbage allocations)
            WaitForSeconds wait = new WaitForSeconds(_offscreenCheckInterval);
            Camera mainCamera = Camera.main;
            
            while (true)
            {
                // Skip checks if there's no main camera
                if (mainCamera == null)
                {
                    mainCamera = Camera.main; // Try to find it again
                    yield return wait;
                    continue;
                }
                
                // Check if the object is offscreen
                Vector3 viewportPos = mainCamera.WorldToViewportPoint(transform.position);
                bool isOffscreen = viewportPos.x < -_offscreenThreshold || 
                                  viewportPos.x > 1 + _offscreenThreshold || 
                                  viewportPos.y < -_offscreenThreshold || 
                                  viewportPos.y > 1 + _offscreenThreshold ||
                                  viewportPos.z < 0;
                
                if (isOffscreen)
                {
                    ReturnNow();
                    yield break; // Exit the coroutine since the object is being returned
                }
                
                // Wait for the next check
                yield return wait;
            }
        }
        
        private IEnumerator CheckCustomConditionRoutine()
        {
            // Check less frequently to minimize performance impact
            WaitForSeconds wait = new WaitForSeconds(0.2f);
            
            while (_customConditionCallback != null)
            {
                if (_customConditionCallback())
                {
                    ReturnNow();
                    yield break; // Exit the coroutine since the object is being returned
                }
                
                yield return wait;
            }
        }
    }
} 