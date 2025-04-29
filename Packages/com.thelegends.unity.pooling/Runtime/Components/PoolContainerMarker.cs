using UnityEngine;

namespace com.thelegends.unity.pooling
{
    /// <summary>
    /// Component attached to pool container GameObjects to identify them and provide metadata.
    /// This component is for internal use by the pooling system and helps with editor visualization.
    /// </summary>
    [AddComponentMenu("")]  // Hide from Add Component menu
    internal class PoolContainerMarker : MonoBehaviour
    {
        /// <summary>
        /// The key used to identify this pool
        /// </summary>
        public string PoolKey { get; private set; }
        
        /// <summary>
        /// Reference to the actual pool object
        /// </summary>
        public object Pool { get; private set; }
        
        /// <summary>
        /// Initializes the container marker with pool information
        /// </summary>
        /// <param name="poolKey">String representation of the pool key</param>
        /// <param name="pool">Reference to the pool object</param>
        public void Initialize(string poolKey, object pool)
        {
            PoolKey = poolKey;
            Pool = pool;
        }
        
        /// <summary>
        /// Called when the component is reset to default values in the editor
        /// </summary>
        private void Reset()
        {
            // Make the GameObject static to improve performance
            gameObject.isStatic = true;
        }
    }
}