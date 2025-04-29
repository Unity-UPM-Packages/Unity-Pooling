using UnityEngine;
using System.Collections; 
using com.thelegends.unity.pooling;

public class SimpleSpawner : MonoBehaviour
{
    public GameObject objectPrefab;
    public float spawnInterval = 0.1f;
    public string poolKey = "SpherePool";

    private PoolManager poolManager;
    private bool usePooling = true;

    async void Start()
    {
        if (objectPrefab == null)
        {
            Debug.LogError("Chưa gán Prefab cho Spawner!");
            this.enabled = false;
            return;
        }

        poolManager = PoolManager.Instance;

        // --- Cấu hình Pool ---
        // Thư viện luôn sử dụng pooling
        await poolManager.CreatePoolAsync(poolKey, PoolConfig.ExceedMaxSizeConfig(10, true, 100), AddressableErrorConfig.Default, PoolTrimmingConfig.Default);

        Debug.Log("Pooling is Enabled and initialized.");

        // Bắt đầu coroutine spawn
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            SpawnObject();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnObject()
    {
        Vector3 spawnPosition = transform.position + Random.insideUnitSphere * 2f; // Vị trí ngẫu nhiên gần Spawner

        if (usePooling)
        {
            // --- Lấy từ Pool ---
            // Dùng GetAsync nhưng không await ngay lập tức để không block coroutine quá lâu
            // Trong demo đơn giản này, việc fire-and-forget là chấp nhận được
            _ = GetPooledObjectAsync(spawnPosition);
        }
        else
        {
            // --- Instantiate thông thường ---
            GameObject instance = Instantiate(objectPrefab, spawnPosition, Quaternion.identity);
            // Quan trọng: Gọi Destroy sau một khoảng thời gian để mô phỏng vòng đời ngắn
            // Lấy thời gian từ AutoReturnToPool component (nếu có) hoặc dùng giá trị mặc định
            float lifetime = 2.0f;
            Destroy(instance, lifetime);
        }
    }

    // Hàm async riêng để lấy object từ pool
    async System.Threading.Tasks.Task GetPooledObjectAsync(Vector3 position)
    {
        GameObject pooledObject = await poolManager.GetAsync(poolKey);
        if (pooledObject != null)
        {
            // PoolManager (hoặc IPoolable.OnGetFromPool) đã gọi SetActive(true)
            // nên AnimateOnEnable.OnEnable() sẽ tự động được gọi.

            // Chỉ cần đặt vị trí và rotation
            pooledObject.transform.position = position;
            pooledObject.transform.rotation = Quaternion.identity;
            
            // Khắc phục vấn đề với AnimateOnEnable - reset và kích hoạt lại hiệu ứng
            AnimateOnEnable animateComponent = pooledObject.GetComponent<AnimateOnEnable>();
            if (animateComponent != null)
            {
                // Cần đảm bảo reset scale về 0 và restart animation
                // Vì OnEnable() có thể không được gọi lại nếu object đã active
                animateComponent.ResetAndPlayAnimation();
            }
        }
    }

    // --- Hàm để gọi từ UI Button (Tùy chọn) ---
    public void SetPoolingMode(bool poolingEnabled)
    {
        usePooling = poolingEnabled;
        Debug.Log("Switched to " + (usePooling ? "Pooling" : "Instantiate/Destroy") + " mode.");
    }
}